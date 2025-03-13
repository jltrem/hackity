namespace ExecutionPipeline.Pipeline;


// StepFunc delegates (for 0, 1, and 2 dependencies)
public delegate Task<TContext> StepFunc<TContext>(TContext context);
public delegate Task<TContext> StepFunc<TContext, T1>(TContext context, T1 dep1);
public delegate Task<TContext> StepFunc<TContext, T1, T2>(TContext context, T1 dep1, T2 dep2);


public interface IPipelineBuilder<TContext>
{
    PipelineBuilder<TContext> AddStep(StepFunc<TContext> stepFunc);
    PipelineBuilder<TContext> AddStep<T1>(StepFunc<TContext, T1> stepFunc);
    PipelineBuilder<TContext> AddStep<T1, T2>(StepFunc<TContext, T1, T2> stepFunc);
}

public class PipelineBuilder<TContext> : IPipelineBuilder<TContext>
{
    private readonly List<(Delegate StepFunc, Type[] DependencyTypes)> _steps = new();
    private readonly IServiceProvider _provider;

    public PipelineBuilder(IServiceProvider provider)
    {
        _provider = provider;
    }

    public PipelineBuilder<TContext> AddStep(StepFunc<TContext> stepFunc)
    {
        _steps.Add((stepFunc, []));
        return this;
    }

    public PipelineBuilder<TContext> AddStep<T1>(StepFunc<TContext, T1> stepFunc)
    {
        _steps.Add((stepFunc, new[] { typeof(T1) }));
        return this;
    }

    public PipelineBuilder<TContext> AddStep<T1, T2>(StepFunc<TContext, T1, T2> stepFunc)
    {
        _steps.Add((stepFunc, [typeof(T1), typeof(T2)]));
        return this;
    }

    public ExecutionPipeline<TContext> Build()
    {
        return new ExecutionPipeline<TContext>(_provider, _steps);
    }
}

public class ExecutionPipeline<TContext> : IExecutionPipeline<TContext>
{
    private readonly IServiceProvider _provider;
    private readonly List<(Delegate StepFunc, Type[] DependencyTypes)> _steps;

    public ExecutionPipeline(IServiceProvider provider, List<(Delegate StepFunc, Type[] DependencyTypes)> steps)
    {
        _provider = provider;
        _steps = steps;
    }

    public async Task<TContext> ExecuteAsync(TContext context)
    {
        for (int i = 0; i < _steps.Count; i++)
        {
            context = await ExecuteStepAsync(_steps[i], context, i);
        }
        return context;
    }

    private async Task<TContext> ExecuteStepAsync(
        (Delegate StepFunc, Type[] DependencyTypes) step,
        TContext context, int stepIndex)
    {
        // Resolve dependencies
        var dependencies = step.DependencyTypes.Select(type => _provider.GetService(type)!).ToArray();

        var arguments = new object[dependencies.Length + 1];
        arguments[0] = context!;
        Array.Copy(dependencies, 0, arguments, 1, dependencies.Length);

        try
        {
            TContext result = await (Task<TContext>)step.StepFunc.DynamicInvoke(arguments)!;
            return result;
        }
        catch (Exception ex) 
        {
            // Log or handle inner exception appropriately here
            throw new ExecutionPipelineStepException($"Error executing pipeline step {stepIndex}", ex);
        }
    }
}

public class ExecutionPipelineStepException : Exception
{
    public ExecutionPipelineStepException(string message, Exception? innerException = null) : base(message, innerException) { }
}


public interface IExecutionPipelineBuilderFactory
{
    IPipelineBuilder<TContext> Create<TContext>();
}

public class ExecutionPipelineBuilderFactory(IServiceProvider provider) : IExecutionPipelineBuilderFactory
{
    public IPipelineBuilder<TContext> Create<TContext>() =>
        new PipelineBuilder<TContext>(provider);
}


public interface IExecutionPipeline<TContext>
{
    Task<TContext> ExecuteAsync(TContext context);
}
