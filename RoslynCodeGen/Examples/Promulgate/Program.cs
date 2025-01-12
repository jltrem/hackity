using RoslynCodeGen;

namespace Promulgate;

#pragma warning disable CS8618

public partial record TestRec
{
    private const string X2 = "DoubleValue";

    [Promulgate(Refine = true, RefineHandler = X2)]
    private readonly int _funkyCount;

    [Promulgate] private readonly string _name;

    [Promulgate(Verify = false, Refine = true)]
    private readonly string _school;

    private static partial bool VerifyName(string value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private static partial string RefineSchool(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }

    private static partial bool VerifyFunkyCount(int value)
    {
        return value is > 0 and <= 10;
    }

    private static partial int DoubleValue(int value)
    {
        return value * 2;
    }
}

public partial record PersonalName
{
    [Promulgate(VerifyHandler = "VerifyName", RefineHandler = "TrimValue")]
    private readonly string _first;

    [Promulgate(VerifyHandler = "VerifyName", RefineHandler = "TrimValue")]
    private readonly string _last;

    private static partial bool VerifyName(string value)
    {
        var cleaned = value?.Trim() ?? "";
        return cleaned.Length > 1 && cleaned.Length < 80;
    }

    private static partial string TrimValue(string value)
    {
        return value?.Trim() ?? "";
    }
}

            Console.WriteLine($"NonNegative.Create(5) : {sut.Value}");
        }

        {
            var sut = NonNegative.Create(0);
            Console.WriteLine($"NonNegative.Create(0) : {sut.Value}");
        }

        {
            try
            {
                var sut = NonNegative.Create(-5);
                Console.WriteLine(sut.Value);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"NonNegative.Create(-5) : {e.Message}");
            }
        }

        {
            StrictlyPositive sut = 1;
public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(StringWrapper.Create("howdy").Value);

        {
            var sut = X10.Create(2);
            Console.WriteLine($"X10.Create(2) : {sut.Value}");
        }

        {
            var sut = NonNegative.Create(5);
            Console.WriteLine($"StrictlyPositive sut = 1 : {sut.Value}");
        }

        {
            if (StrictlyPositive.TryCreate(0, out var sut))
                Console.WriteLine($"StrictlyPositive.TryCreate(0, out var sut) : {sut!.Value}");
            else
                Console.WriteLine("StrictlyPositive.TryCreate(0, out var sut) : false");
        }

        {
            try
            {
                Exception? nullException = null;
                NonNullException sut = nullException;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"NonNullException = null : {e.Message}");
            }
        }

        {
            var sut = NonNullException.Create(new AggregateException("foobar"));
            Console.WriteLine(
                $"NonNullException.Create(new AggregateException(\"foobar\")) : {Environment.NewLine}\t{sut}{Environment.NewLine}\t{sut.Value}");
        }

        {
            var sut = ValidatedPerson.Create(new Person(" James Dean\t", 42));
            Console.WriteLine(sut.Value);
        }

        {
            BasketballScore sut = (76, 41);
            Console.WriteLine(sut.Value);
        }

        {
            try
            {
                BasketballScore sut = (76, -1);
                Console.WriteLine(sut.Value);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"BasketballScore sut = (76, -1) : {Environment.NewLine}{e.Message}");
            }
        }

        {
            try
            {
                BasketballScore sut = (-2, -1);
                Console.WriteLine(sut.Value);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"BasketballScore sut = (-2, -1) : {Environment.NewLine}{e.Message}");
            }
        }

        return;

        Console.WriteLine("Method Logger!");
        var mlogger = new MethodLogger();
        var result = mlogger.Divide(10, 2);

        Console.WriteLine(result);


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