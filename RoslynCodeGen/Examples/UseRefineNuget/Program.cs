using Refine;
using Refine.Generators;
using UseRefineNuget.DomainType;

namespace UseRefineNuget;


public static class Program
{
    public static void Main(string[] args)
    {

        PersonalName foo = new PersonalNameDto("Jim ", "\tKirk");
        Console.WriteLine(foo.Value);
    }
}
