namespace ExecutionPipeline.AppServices;

public interface IFactorialService
{
    long Calculate(long n);
}

public class FactorialService : IFactorialService
{
    public long Calculate(long n) =>
        n is 0 or 1 ? 1 : n * Calculate(n - 1);
}