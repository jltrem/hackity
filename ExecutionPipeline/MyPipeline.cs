using ExecutionPipeline.Pipeline;
using ExecutionPipeline.AppServices;

namespace ExecutionPipeline;

public class MyPipelineContext
{
    public required int Seed { get; init; }
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
            .AddStep(
                async (ctx) =>
                {
                    ctx.Squared = ctx.Seed * ctx.Seed;
                    return ctx;
                })
            .AddStep(
                async (MyPipelineContext ctx, IDigitsSumService summer) =>
                {
                    if (ctx.Squared is null) throw new ExecutionPipelineStepException("PlusTwo is null");
                    
                    ctx.SumOfDigits = summer.SumDigits(ctx.Squared.Value);
                    return ctx;
                })
            .AddStep<IFactorialService, IReverseDigitsService>(LastThing)
            .Build();

    private static async Task<MyPipelineContext> LastThing(MyPipelineContext ctx, IFactorialService factorialService, IReverseDigitsService reverseDigitsService)
    {
        if (ctx.SumOfDigits is null) throw new ExecutionPipelineStepException("SumOfDigits is null");

        ctx.Factorial = factorialService.Calculate(ctx.SumOfDigits.Value);
        ctx.Reversed = reverseDigitsService.ReverseDigits(ctx.Factorial.Value);
        return ctx;
    }
}
