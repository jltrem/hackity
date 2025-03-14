using ExecutionPipeline.Pipeline;
using ExecutionPipeline.AppServices;

namespace ExecutionPipeline;

public class MyPipelineContext
{
    public required int Seed { get; init; }
    public List<string> StepLog { get; } = [];
    public int? Squared { get; set; }
    public int? SumOfDigits { get; set; }
    public long? Factorial { get; set; }
    public long? Reversed { get; set; }
}

public interface IMyExecutionPipeline : IExecutionPipeline<MyPipelineContext>;

public sealed class MyExecutionPipeline : IMyExecutionPipeline
{
    private readonly IExecutionPipeline<MyPipelineContext> _pipeline;

    public MyExecutionPipeline(IExecutionPipelineBuilderFactory factory)
    {
        _pipeline = Build(factory);
    }
    
    public Task<MyPipelineContext> ExecuteAsync(MyPipelineContext context) =>
        _pipeline.ExecuteAsync(context);

    private static ExecutionPipeline<MyPipelineContext> Build(IExecutionPipelineBuilderFactory factory) =>
        factory.Create<MyPipelineContext>()
            .AddAsyncStep(
                async (ctx) =>
                {
                    //throw new Exception("this will be an outer exception");

                    ctx.StepLog.Add("Step 1 - AddAsyncStep with async and use await");
                    ctx.Squared = ctx.Seed * ctx.Seed;
                    await Task.Delay(10);
                    return ctx;
                })
            .AddAsyncStep<IDigitsSumService>(
                (ctx, summer) =>
                {
                    //throw new Exception("this will be an inner exception");
                    
                    ctx.StepLog.Add("Step 2 - AddAsyncStep: return Task but no async");
                    if (ctx.Squared is null) throw new ExecutionPipelineStepException("Squared is null");
                    
                    ctx.SumOfDigits = summer.SumDigits(ctx.Squared.Value);
                    return Task.FromResult(ctx);
                })
            .AddStep(
                (MyPipelineContext ctx, IFactorialService factorialService) =>
                {
                    //throw new Exception("this will be an inner exception");
                    
                    ctx.StepLog.Add("Step 3 - AddStep: return context synchronously");
                    if (ctx.SumOfDigits is null) throw new ExecutionPipelineStepException("SumOfDigits is null");
                    
                    ctx.Factorial = factorialService.Calculate(ctx.SumOfDigits.Value);
                    return ctx;
                })
            .AddAsyncStep<IReverseDigitsService>(ReverseDigits)
            .Build();

    private static async Task<MyPipelineContext> ReverseDigits(MyPipelineContext ctx, IReverseDigitsService reverseDigitsService)
    {
        ctx.StepLog.Add("Step 3 - AddStep: AddAsyncStep with async but no await");
        if (ctx.Factorial is null) throw new ExecutionPipelineStepException("Factorial is null");

        ctx.Reversed = reverseDigitsService.ReverseDigits(ctx.Factorial.Value);
        return ctx;
    }
}
