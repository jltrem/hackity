namespace ExecutionPipeline.AppServices;

public interface IReverseDigitsService
{
    long ReverseDigits(long n);
}

public class ReverseDigitsService : IReverseDigitsService
{
    public long ReverseDigits(long n)
    {
        long reversed = 0;
        while (n != 0)
        {
            long digit = n % 10;
            reversed = reversed * 10 + digit;
            n /= 10;
        }
        return reversed;
    }
}