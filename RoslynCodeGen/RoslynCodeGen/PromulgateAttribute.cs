using System;

namespace RoslynCodeGen;

[AttributeUsage(AttributeTargets.Field)]
public class PromulgateAttribute : Attribute
{
    private readonly bool _explicitRefine;

    // Backing fields to track explicitly set Verify/Refine values
    private readonly bool _explicitVerify;

    /// <summary>
    ///     Refinement method name. Default (null) will use "Refine[FieldName]".
    ///     Setting this forces `Refine = true`.
    /// </summary>
    private string? _refineHandler;

    /// <summary>
    ///     Verification method name. Default (null) will use "Verify[FieldName]".
    ///     Setting this forces `Verify = true`.
    /// </summary>
    private string? _verifyHandler;

    public PromulgateAttribute(
        string? verifyHandler = null,
        string? refineHandler = null,
        bool verify = true,
        bool refine = false)
    {
        // Initialize fields and user-provided explicit values
        _explicitVerify = verify;
        _explicitRefine = refine;

        VerifyHandler = verifyHandler;
        RefineHandler = refineHandler;

        RecalculateVerify();
        RecalculateRefine();
    }

    /// <summary>
    ///     Whether to verify (validate) the value during initialization.
    ///     Default is true, or true if `VerifyHandler` is set.
    /// </summary>
    public bool Verify { get; set; } = true;

    /// <summary>
    ///     Whether to refine (transform) the value during initialization.
    ///     Default is false, or true if `RefineHandler` is set.
    /// </summary>
    public bool Refine { get; set; }

    public string? VerifyHandler
    {
        get => _verifyHandler;
        set
        {
            _verifyHandler = value;
            // If handler exists, force Verify to true unless explicitly disabled
            RecalculateVerify();
        }
    }

    public string? RefineHandler
    {
        get => _refineHandler;
        set
        {
            _refineHandler = value;
            // If handler exists, force Refine to true unless explicitly disabled
            RecalculateRefine();
        }
    }

    private void RecalculateVerify()
    {
        // Verify is true if explicitly set to true or if a handler exists
        Verify = _explicitVerify || !string.IsNullOrEmpty(VerifyHandler);
    }

    private void RecalculateRefine()
    {
        // Refine is true if explicitly set to true or if a handler exists
        Refine = _explicitRefine || !string.IsNullOrEmpty(RefineHandler);
    }
}