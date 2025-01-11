using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynCodeGen.Generators;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

[Generator]
public class WrappedValidationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Look for class/record candidates with RequiredTypeAttribute
        var targets = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntaxNode, _) => IsCandidateForGeneration(syntaxNode),
                transform: static (context, _) => GetWrapperType(context))
            .Where(static item => item is not null);

        var compilationAndWrappers = context.CompilationProvider.Combine(targets.Collect());

        context.RegisterSourceOutput(compilationAndWrappers, static (context, source) =>
        {


            // Check if the collection is empty
            if (!source.Right.Any())
            {
                // No targets found, you can handle this (e.g., emit a diagnostic)
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "GEN002",
                        "No Targets Found",
                        "The source generator could not find any valid targets to process.",
                        "SourceGenerator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None));
                return;
            }

            GenerateWrapperCode(context, source.Left, source.Right!);
        });
    }

    private static bool IsCandidateForGeneration(SyntaxNode syntaxNode) =>
        syntaxNode is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0;

    private static WrapperTypeInfo? GetWrapperType(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Ensure the class has the `partial` modifier.
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            return null;
        
        var expectedAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("RoslynCodeGen.WrappedValidationAttribute");
        
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                if (SymbolEqualityComparer.Default.Equals(attributeSymbol.ContainingType, expectedAttributeSymbol))
                {
                    var typeExpression = attribute.ArgumentList.Arguments[0].Expression;

                    if (typeExpression is TypeOfExpressionSyntax typeofExpression)
                    {
                        // Resolve the type inside typeof(...)
                        var typeSymbol = context.SemanticModel.GetTypeInfo(typeofExpression.Type).Type;
                        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
                        IEnumerable<IMethodSymbol>? methods = classSymbol?.GetMembers().OfType<IMethodSymbol>();
                        
                        if (typeSymbol != null && methods != null)
                        {
                            return new WrapperTypeInfo{
                                ClassName = classDeclaration.Identifier.Text,
                                Namespace = GetNamespace(classDeclaration),
                                TargetType = typeSymbol,
                                ImplementsEquatable = ImplementsEquatable(typeSymbol),
                                HasTryValidate = HasTryValidate(methods, typeSymbol),
                                HasValidate = HasValidate(methods, typeSymbol),
                                HasNormalize = HasNormalize(methods, typeSymbol)
                            };
                        }
                    }
                }
            }
        }

        return null;
    }

    private static bool ImplementsEquatable(ITypeSymbol typeSymbol)
    {
        return typeSymbol
            .AllInterfaces
            .Any(i => i.Name == "IEquatable" && i is INamedTypeSymbol namedType && namedType.TypeArguments.Any(t => t.Equals(typeSymbol, SymbolEqualityComparer.Default)));
    }



    private static bool HasMethod(IEnumerable<IMethodSymbol> methods, string methodName, ITypeSymbol parameterType,
        SpecialType? specialReturnType = SpecialType.None, ITypeSymbol? specificReturnType = null) =>
        methods.Any(method => method.Name == methodName &&
                              method.Parameters.Length == 1 &&
                              SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, parameterType) &&
                              (
                                  method.ReturnType.SpecialType == specialReturnType ||
                                  method.ReturnType.Equals(specificReturnType, SymbolEqualityComparer.Default)
                              )
        );

    private static bool HasTryValidate(IEnumerable<IMethodSymbol> methods, ITypeSymbol valueSymbol) =>
        HasMethod(methods, "TryValidate", valueSymbol, SpecialType.System_Boolean);

    private static bool HasValidate(IEnumerable<IMethodSymbol> methods, ITypeSymbol valueSymbol) =>
        HasMethod(methods, "Validate", valueSymbol, SpecialType.System_Void);

    private static bool HasNormalize(IEnumerable<IMethodSymbol> methods, ITypeSymbol valueSymbol) =>
        HasMethod(methods, "Normalize", valueSymbol, specificReturnType: valueSymbol);

    private static void GenerateWrapperCode(SourceProductionContext context, Compilation compilation, ImmutableArray<WrapperTypeInfo> wrappers)
    {

        foreach (var wrapper in wrappers)
        {
            var targetTypeSymbol = (INamedTypeSymbol)wrapper.TargetType;
            var isRecord = targetTypeSymbol.IsRecord;
            

            
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "GEN003",
                    "dump",
                    $"{wrapper}",
                    "SourceGenerator",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                Location.None));
            var sourceCode = GenerateWrapperSource(wrapper, isRecord);

            context.AddSource($"{wrapper.ClassName}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
    }

    private static string GenerateWrapperSource(WrapperTypeInfo info, bool isRecord)
    {
        var targetType = info.TargetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (string.IsNullOrEmpty(targetType))
            throw new InvalidOperationException("Target type cannot be resolved.");

        var equalsLogic = info.ImplementsEquatable
            ? $"Value.Equals(other.Value)"
            : $"EqualityComparer<{targetType}>.Default.Equals(Value, other.Value)";

        string validateCall = info.HasValidate || info.HasTryValidate
            ? "Validate(value);"
            : "";

        var normalizeCall = info.HasNormalize
            ? "value = Normalize(value);"
            : "";
        
        string validateMethod = "";
        if (info.HasTryValidate && !info.HasValidate)
        {
            validateMethod = $$"""
                                       
                                       private static void Validate({{targetType}} value)
                                       {
                                           if (!TryValidate(value))
                                               throw new ArgumentException("Validation failed for the provided value.");
                                       }
                               """;
        }

        var tryCreateMethod = info.HasTryValidate
            ? $$"""
                        
                        public static bool TryCreate({{targetType}} value, out {{info.ClassName}}? newValue)
                        {   
                            {{normalizeCall}}
                            if (TryValidate(value))
                            {
                                newValue = new {{info.ClassName}}(value);
                                return true;
                            }
                            newValue = null;
                            return false;
                        }
                """
            : "";
        
        return $$"""
                 // <auto-generated/>
                 #nullable enable

                 namespace {{info.Namespace}}
                 {
                     public partial class {{info.ClassName}}
                     {
                         public {{targetType}} Value { get; private init; }
                         private {{info.ClassName}}({{targetType}} value) => Value = value;
                 
                         public static {{info.ClassName}} Create({{targetType}} value)
                         {   
                             {{normalizeCall}}
                             {{validateCall}}
                             return new {{info.ClassName}}(value);
                         }
                         {{validateMethod}}
                         {{tryCreateMethod}}
                         
                         public override bool Equals(object? obj) => obj is {{info.ClassName}} other && {{equalsLogic}};
                         public override int GetHashCode() => Value.GetHashCode();
                         public static bool operator ==({{info.ClassName}}? wrapper, {{targetType}}? value) => wrapper?.Value.Equals(value) ?? value is null;
                         public static bool operator !=({{info.ClassName}}? wrapper, {{targetType}}? value) => !(wrapper == value);
                         public static bool operator ==({{targetType}}? value, {{info.ClassName}}? wrapper) => wrapper == value;
                         public static bool operator !=({{targetType}}? value, {{info.ClassName}}? wrapper) => !(wrapper == value);
                         public static implicit operator {{targetType}}({{info.ClassName}} wrapper) => wrapper.Value;
                         public static implicit operator {{info.ClassName}}({{targetType}} value) => Create(value);
                     }
                 }
                 """;
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

public record WrapperTypeInfo
{
    public string ClassName { get; set; }
    public string Namespace { get; set; }
    public ITypeSymbol TargetType { get; set; }
    public bool ImplementsEquatable { get; set; }
    public bool HasTryValidate { get; set; }
    public bool HasValidate { get; set; }
    public bool HasNormalize { get; set; }
}