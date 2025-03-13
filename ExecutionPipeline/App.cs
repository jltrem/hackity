using ExecutionPipeline;

public class App
{
    private readonly IMyExecutionPipeline _pipeline;

    public App(IMyExecutionPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Hello!");

        MyPipelineContext result = await _pipeline.ExecuteAsync(new MyPipelineContext { Seed = 123 });
        
        Console.WriteLine($"Seed: {result.Seed}");
        Console.WriteLine($"Squared: {result.Squared}");
        Console.WriteLine($"SumOfDigits: {result.SumOfDigits}");
        Console.WriteLine($"Factorial: {result.Factorial}");
        Console.WriteLine($"Reversed: {result.Reversed}");
        
        await Task.CompletedTask;
    }
}

