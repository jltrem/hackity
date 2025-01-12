using Refine;

namespace UseRefineNuget.DomainType;

[RefinedType(typeof(string))]
public partial class StringWrapper;

[RefinedType(typeof(PersonalNameDto))]
public partial class PersonalName
{
    private static PersonalNameDto Transform(PersonalNameDto value) =>
        new(value.FirstName.Trim(), value.LastName.Trim());

    private static void Validate(PersonalNameDto value)
    {
        if (string.IsNullOrWhiteSpace(value.FirstName))
            throw new ArgumentException($"{nameof(value.FirstName)} must be specified.");
        
        if (string.IsNullOrWhiteSpace(value.LastName))
            throw new ArgumentException($"{nameof(value.LastName)} must be specified.");
    }
}


