using RoslynCodeGen;

namespace Promulgate;


[WrappedValidation(typeof(string))]
public partial class StringWrapper;


[WrappedValidation(typeof(int))]
public partial class X10
{
    private static int Normalize(int value) => value * 10;
}


[WrappedValidation(typeof(int))]
public partial class NonNegative
{
    private static void Validate(int value)
    {
        if (value < 0) throw new ArgumentException("Value cannot be negative.");
    }
}


[WrappedValidation(typeof(int))]
public partial class StrictlyPositive
{
    private static bool TryValidate(int value) =>
        value > 0;
}


[WrappedValidation(typeof(Exception))]
public partial class NonNullException
{
    private static Exception Normalize(Exception value) => value != null ? new ApplicationException(value?.Message) : null;
    
    private static void Validate(Exception value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }
}


public record Person(string FullName, int Age);

[WrappedValidation(typeof(Person))]
public partial class ValidatedPerson
{
    private static Person Normalize(Person value) =>
        value with { FullName = value.FullName.Trim() };
    
    private static void Validate(Person value)
    {
        if (string.IsNullOrWhiteSpace(value.FullName))
            throw new ArgumentOutOfRangeException(nameof(value.FullName), "Name must be specified.");
        
        if (value.Age < 0)
            throw new ArgumentOutOfRangeException(nameof(value.Age), "Age must be non-negative.");
    }
}


[WrappedValidation(typeof((int Home, int Away)))]
public partial class BasketballScore
{
    private static void Validate((int Home, int Away) val)
    {
        string?[] validations =
        [
            Validate(nameof(val.Home), val.Home),
            Validate(nameof(val.Away), val.Away)
        ];

        var errors = validations.Where(e => e != null).ToArray();
        if (errors.Length <= 0) return;
        
        string fail = string.Join(Environment.NewLine, errors);
        throw new ArgumentException(fail);
    }
    
    private static string? Validate(string team, int score) =>
        score < 0 ? $"score must be non-negative: ({team} = {score})" : null;    
}
