using System;
using RoslynCodeGen;

namespace Promulgate;

#pragma warning disable CS8618

public partial record Person
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


public static class Program
{

    public static void Main(string[] args)
    {
        Console.WriteLine("Promulgate!");

        var personA = new Person { FunkyCount = 1, Name = "Jack", School = "School of Rock" };
        Console.WriteLine($"success - {personA}");

        var personB = new Person { FunkyCount = 2, Name = "Jill", School = null };
        Console.WriteLine($"success - {personB}");

        try
        {
            var personC = new Person { FunkyCount = 11, Name = "Jim", School = null };
            Console.WriteLine("FAIL - did not get expected exception");
            Console.WriteLine(personC);
        }
        catch (PromulgateVerifyException e)
        {
            Console.WriteLine($"success (expected exception) - {e.Message}");
        }
    }
}