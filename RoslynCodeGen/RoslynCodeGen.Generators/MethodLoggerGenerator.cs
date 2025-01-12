using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RoslynCodeGen.Generators
{
    public class MethodLoggerSyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.Modifiers.Any(m => m.Text == "partial"))
                // Check if the method has our specific attribute
                foreach (var attributeList in methodDeclaration.AttributeLists)
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (name == "MethodLogger" || name == "MethodLoggerAttribute")
                    {
                        CandidateMethods.Add(methodDeclaration);
                        break;
                    }
                }
        }
    }


    [Generator]
    public class MethodLoggerGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver to collect methods with [MyCustomAttribute]
            context.RegisterForSyntaxNotifications(() => new MethodLoggerSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as MethodLoggerSyntaxReceiver;
            if (receiver == null)
                return;

            foreach (var methodDeclaration in receiver.CandidateMethods)
            {
                // Extract method information
                var methodName = methodDeclaration.Identifier.ToString();
                var parameters = string.Join(", ",
                    methodDeclaration.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                var returnType = methodDeclaration.ReturnType.ToString();
                var namespaceName = GetNamespace(methodDeclaration);
                var classNode = methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                var className = classNode?.Identifier.Text;

                // Extract the method's body
                var methodBody = methodDeclaration.Body?.ToString() ?? "";

                var stubCode = $@"
namespace {namespaceName}
{{
    public partial class {className}
    {{
        public partial {returnType} {methodName}({parameters});
    }}
}}";
                context.AddSource($"{className}_{methodName}_Stub.cs", SourceText.From(stubCode, Encoding.UTF8));

// Generate the enhanced method body
                var enhancedBody = $@"
namespace {namespaceName}
{{
    public partial class {className}
    {{
        public partial {returnType} {methodName}({parameters})
        {{
            try
            {{
                Console.WriteLine(""starting"");
{methodBody}
            }}
            catch (Exception ex)
            {{
                Console.WriteLine(ex.Message);
                throw;
            }}
            finally
            {{
                Console.WriteLine(""exiting"");
            }}
        }}
    }}
}}";
                // Emit the generated code
                context.AddSource($"{className}_{methodName}_Generated.cs",
                    SourceText.From(enhancedBody, Encoding.UTF8));
            }
        }

        private static string GetNamespace(SyntaxNode node)
        {
            var namespaceDeclaration = node.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
            if (namespaceDeclaration == null)
                return "GlobalNamespace";

            var namespaceParts = new Stack<string>();

            while (namespaceDeclaration != null)
            {
                namespaceParts.Push(namespaceDeclaration.Name.ToString());
                namespaceDeclaration = namespaceDeclaration.Parent as BaseNamespaceDeclarationSyntax;
            }

            return string.Join(".", namespaceParts);
        }
    }
}