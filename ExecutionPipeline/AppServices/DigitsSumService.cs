namespace ExecutionPipeline.AppServices;

public interface IDigitsSumService
{
    int SumDigits(int n);
}

public class DigitsSumService: IDigitsSumService
{
    public int SumDigits(int n)
    {
        int sum = 0;
        while (n != 0)
        {
            sum += n % 10;
            n /= 10;
        }
        return sum;
    }
}