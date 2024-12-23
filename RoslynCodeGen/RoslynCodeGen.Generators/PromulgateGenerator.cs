using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RoslynCodeGen.Generators;

[Generator]
public class PromulgateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get params for all fields with the [Promulgate] attribute
        var fieldMetaData = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsCandidateField,
                transform: GetPromulgateParams
            )
            .Where(field => field is not null);

        // Add diagnostics
        context.RegisterSourceOutput(
            source: fieldMetaData.Where(result => result.Diagnostic != null),
            action: (ctx, result) => ctx.ReportDiagnostic(result.Diagnostic!));

        // Generate
        context.RegisterSourceOutput(fieldMetaData, GenerateSource);
    }

    private static bool IsCandidateField(SyntaxNode node, CancellationToken cancellationToken)
    {
        // Look for field declarations with attributes
        return node is FieldDeclarationSyntax fieldSyntax && fieldSyntax.AttributeLists.Count > 0;
    }

    private static PromulgateParams? GetPromulgateParams(GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var fieldNode = (FieldDeclarationSyntax)context.Node;
        var variable = fieldNode.Declaration.Variables.First();
        var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable, cancellationToken) as IFieldSymbol;

        if (fieldSymbol == null)
            return null;

        // Check if the field is annotated with [Validate]
        var validateAttribute = fieldSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == nameof(PromulgateAttribute));

        if (validateAttribute == null)
            return null;

        string? verifyHandler = null;
        string? refineHandler = null;

        bool verify = true;
        bool refine = false;

        foreach (var namedArg in validateAttribute.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case nameof(PromulgateAttribute.VerifyHandler):
                    verifyHandler = namedArg.Value.Value is string verifyHandlerValue ? verifyHandlerValue : null;
                    break;
                case nameof(PromulgateAttribute.RefineHandler):
                    refineHandler = namedArg.Value.Value is string refineHandlerValue ? refineHandlerValue : null;
                    break;
                case nameof(PromulgateAttribute.Verify):
                    verify = namedArg.Value.Value is bool verifyValue && verifyValue;
                    break;
                case nameof(PromulgateAttribute.Refine):
                    refine = namedArg.Value.Value is bool refineValue && refineValue;
                    break;
                default:
                    continue;
            }
        }

        // Create a diagnostic if the field is not readonly
        var diagnostic = fieldSymbol.IsReadOnly
            ? null
            : Diagnostic.Create(
                FieldNotReadonlyError,
                fieldSymbol.Locations.FirstOrDefault(),
                fieldSymbol.Name
            );

        return new PromulgateParams
        {
            FieldSymbol = fieldSymbol,
            Diagnostic = diagnostic,
            VerifyHandler = verifyHandler,
            RefineHandler = refineHandler,
            Verify = verify,
            Refine = refine
        };
    }

    private static readonly DiagnosticDescriptor FieldNotReadonlyError = new DiagnosticDescriptor(
        id: "GEN001",
        title: "ValidateAttribute requires readonly backing fields",
        messageFormat: "The field '{0}' is decorated with [Validate] but must be declared as 'readonly'",
        category: "SourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static void GenerateSource(SourceProductionContext context, PromulgateParams promulgateParams)
    {
        var fieldSymbol = promulgateParams.FieldSymbol;
        var containingClass = fieldSymbol.ContainingType;
        var className = containingClass.Name;
        var namespaceName = containingClass.ContainingNamespace.ToDisplayString();
        var fieldName = fieldSymbol.Name;
        var dataType = fieldSymbol.Type.ToString();

        // Field named like `_myValue` will get a property named like `MyValue`   
        var propertyName = char.ToUpper(fieldName[1]) + fieldName.Substring(2);

        // Handlers will be named `VerifyMyValue` and `RefineMyValue` unless otherwise specified
        var verifyHandlerName = promulgateParams.VerifyHandler ?? $"{nameof(PromulgateAttribute.Verify)}{propertyName}";
        var refineHandlerName = promulgateParams.RefineHandler ?? $"{nameof(PromulgateAttribute.Refine)}{propertyName}";

        // Verify: call the predicate handler and potentially throw; or do nothing if disabled
        var verifySource = promulgateParams.Verify
            ? $"""
               if (!{verifyHandlerName}(value)) throw new RoslynCodeGen.PromulgateVerifyException("{className}", "{propertyName}", value);
               """
            : "";

        // Refine: call the transformation handler; or use the raw value if disabled
        var refineSource = promulgateParams.Refine
            ? $"{refineHandlerName}(value)"
            : "value";

        var verifyPartialDeclaration = promulgateParams.Verify
            ? $"private static partial bool {verifyHandlerName}({dataType} value);"
            : "";

        var refinePartialDeclaration = promulgateParams.Refine
            ? $"private static partial {dataType} {refineHandlerName}({dataType} value);"
            : "";

        var source =
            $$"""
              namespace {{namespaceName}}
              {
                public partial record {{className}}
                {
                  public required {{dataType}} {{propertyName}}
                  {
                    get
                    {
                      return {{fieldName}};
                    }
                    init
                    {
                       {{verifySource}}
                       {{fieldName}} = {{refineSource}};
                    }
                  }
                  {{verifyPartialDeclaration}}
                  {{refinePartialDeclaration}}
                }
              }
              """;

        // Add the generated code
        string filename = $"{namespaceName.Replace('.', '_')}_{className}_{propertyName}.g.cs";
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    }

    private class PromulgateParams
    {
        public IFieldSymbol FieldSymbol { get; set; }
        public Diagnostic? Diagnostic { get; set; }

        public string? VerifyHandler { get; set; }
        public string? RefineHandler { get; set; }

        public bool Verify { get; set; }
        public bool Refine { get; set; }
    }
}