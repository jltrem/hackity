using System;

namespace RoslynCodeGen;


[AttributeUsage(AttributeTargets.Field)]
public sealed class PromulgateAttribute : Attribute
{
    /// <summary>
    /// Validation method name. Default (null) will use "Verify[FieldName]".
    /// </summary>
    public string? VerifyHandler { get; set; }

    /// <summary>
    /// Validation method name. Default (null) will use "Refine[FieldName]".
    /// </summary>
    public string? RefineHandler { get; set; }

    /// <summary>
    /// Whether to verify (validate) the value during initialization.
    /// Default is true.
    /// </summary>
    public bool Verify { get; set; }

    /// <summary>
    /// Whether to refine (transform) the value during initialization.
    /// Default is false.
    /// </summary>
    public bool Refine { get; set; }


    public PromulgateAttribute(
        string? verifyHandler = null, string? refineHandler = null, 
        bool verify = true, bool refine = false)
    {
        VerifyHandler = verifyHandler;
        RefineHandler = refineHandler;

        Verify = verify;
        Refine = refine;
    }
}
