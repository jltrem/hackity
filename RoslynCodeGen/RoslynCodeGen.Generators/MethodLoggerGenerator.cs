namespace RoslynCodeGen.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

public class MethodLoggerSyntaxReceiver : ISyntaxReceiver
{
    public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
            methodDeclaration.Modifiers.Any(m => m.Text == "partial"))
        {
            // Check if the method has our specific attribute
            foreach (var attributeList in methodDeclaration.AttributeLists)
            {
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

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not MethodLoggerSyntaxReceiver receiver)
            return;

        foreach (var methodDeclaration in receiver.CandidateMethods)
        {
            // Extract method information
            var methodName = methodDeclaration.Identifier.ToString();
            var parameters = string.Join(", ", methodDeclaration.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
            var returnType = methodDeclaration.ReturnType.ToString();
            var namespaceName = GetNamespace(methodDeclaration);
            var classNode = methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var className = classNode?.Identifier.Text;

            // Extract the method's body
            var methodBody = methodDeclaration.Body?.ToString() ?? string.Empty;

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
            context.AddSource($"{className}_{methodName}_Generated.cs", SourceText.From(enhancedBody, Encoding.UTF8));
        }
    }
}