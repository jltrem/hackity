using System;
using RoslynCodeGen;

namespace Promulgate;

#pragma warning disable CS8618

public partial record TestRec
{
    private const string X2 = "DoubleValue";

    [Promulgate] private readonly string _name;

    [Promulgate(Verify = false, Refine = true)]
    private readonly string _school;

    [Promulgate(Refine = true, RefineHandler = X2)]
    private readonly int _funkyCount;


    private static partial bool VerifyName(string value) => !string.IsNullOrWhiteSpace(value);

    private static partial string RefineSchool(string value) =>
        string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

    private static partial bool VerifyFunkyCount(int value) =>
        value is > 0 and <= 10;

    private static partial int DoubleValue(int value) =>
        value * 2;
}

public partial record PersonalName
{
    [Promulgate(VerifyHandler = "VerifyName", RefineHandler = "TrimValue")]
    private readonly string _first;

    [Promulgate(VerifyHandler = "VerifyName", RefineHandler = "TrimValue")]
    private readonly string _last;
    
    private static partial bool VerifyName(string value)
    {
        string cleaned = value?.Trim() ?? "";
        return cleaned.Length > 1 && cleaned.Length < 80;
    }
    private static partial string TrimValue(string value) => value?.Trim() ?? "";
}


public static class Program
{

    public static void Main(string[] args)
    {
        Console.WriteLine("Promulgate!");

        var personA = new TestRec { FunkyCount = 1, Name = "Jack", School = "School of Rock" };
        Console.WriteLine($"success - {personA}");

        var personB = new TestRec { FunkyCount = 2, Name = "Jill", School = null };
        Console.WriteLine($"success - {personB}");

        try
        {
            var personC = new TestRec { FunkyCount = 11, Name = "Jim", School = null };
            Console.WriteLine("FAIL - did not get expected exception");
            Console.WriteLine(personC);
        }
        catch (PromulgateVerifyException e)
        {
            Console.WriteLine($"success (expected exception) - {e.Message}");
        }
        
        try
        {
            var personalName1 = new PersonalName { First = null, Last = "Black" };
            Console.WriteLine("FAIL - did not get expected exception");
            Console.WriteLine(personalName1);
        }
        catch (PromulgateVerifyException e)
        {
            Console.WriteLine($"success (expected exception) - {e.Message}");
        }

        try
        {
            var personalName2 = new PersonalName { First = "Jack", Last = "" };
            Console.WriteLine("FAIL - did not get expected exception");
            Console.WriteLine(personalName2);
        }
        catch (PromulgateVerifyException e)
        {
            Console.WriteLine($"success (expected exception) - {e.Message}");
        }
        
        var personalName = new PersonalName { First = "Jack", Last = "Black" };
        Console.WriteLine(personalName);
    }
}