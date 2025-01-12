namespace UseRefineNuget;

public record PersonalNameDto(string FirstName, string LastName);

public record BirthDate(int Day, int Month, int Year);

public record HomeAddress(string Street, string City, string State, int Zip);
