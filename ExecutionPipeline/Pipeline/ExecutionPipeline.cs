using System.Reflection;

namespace ExecutionPipeline.Pipeline;


// StepFunc delegates (for 0, 1, and 2 dependencies)
public delegate TContext StepFunc<TContext>(TContext context);
public delegate TContext StepFunc<TContext, T1>(TContext context, T1 dep1);
public delegate TContext StepFunc<TContext, T1, T2>(TContext context, T1 dep1, T2 dep2);
public delegate Task<TContext> StepFuncAsync<TContext>(TContext context);
public delegate Task<TContext> StepFuncAsync<TContext, T1>(TContext context, T1 dep1);
public delegate Task<TContext> StepFuncAsync<TContext, T1, T2>(TContext context, T1 dep1, T2 dep2);


public interface IPipelineBuilder<TContext>
{
    PipelineBuilder<TContext> AddStep(StepFunc<TContext> stepFunc, string stepName = "");
    PipelineBuilder<TContext> AddStep<T1>(StepFunc<TContext, T1> stepFunc, string stepName = "");
    PipelineBuilder<TContext> AddStep<T1, T2>(StepFunc<TContext, T1, T2> stepFunc, string stepName = "");
    PipelineBuilder<TContext> AddAsyncStep(StepFuncAsync<TContext> stepFunc, string stepName = "");
    PipelineBuilder<TContext> AddAsyncStep<T1>(StepFuncAsync<TContext, T1> stepFunc, string stepName = "");
    PipelineBuilder<TContext> AddAsyncStep<T1, T2>(StepFuncAsync<TContext, T1, T2> stepFunc, string stepName = "");
}

public class PipelineBuilder<TContext> : IPipelineBuilder<TContext>
{
    private readonly List<(Delegate Function, Type[] DependencyTypes, string StepName)> _steps = new();
    private readonly IServiceProvider _provider;

    public PipelineBuilder(IServiceProvider provider)
    {
        _provider = provider;
    }

    public PipelineBuilder<TContext> AddStep(StepFunc<TContext> stepFunc, string stepName = "")
    {
        _steps.Add((stepFunc, [], stepName));
        return this;
    }

    public PipelineBuilder<TContext> AddStep<T1>(StepFunc<TContext, T1> stepFunc, string stepName = "")
    {
        _steps.Add((stepFunc, [typeof(T1)], stepName));
        return this;
    }

    public PipelineBuilder<TContext> AddStep<T1, T2>(StepFunc<TContext, T1, T2> stepFunc, string stepName = "")
    {
        _steps.Add((stepFunc, [typeof(T1), typeof(T2)], stepName));
        return this;
    }
    public PipelineBuilder<TContext> AddAsyncStep(StepFuncAsync<TContext> stepFunc, string stepName = "")
    {
        _steps.Add((stepFunc, [], stepName));
        return this;
    }

    public PipelineBuilder<TContext> AddAsyncStep<T1>(StepFuncAsync<TContext, T1> stepFunc, string stepName = "")
    {
        _steps.Add((stepFunc, [typeof(T1)], stepName));
        return this;
    }

    public PipelineBuilder<TContext> AddAsyncStep<T1, T2>(StepFuncAsync<TContext, T1, T2> stepFunc, string stepName = "")
    {
        _steps.Add((stepFunc, [typeof(T1), typeof(T2)], stepName));
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
    private readonly List<(Delegate Function, Type[] DependencyTypes, string StepName)> _steps;

    public ExecutionPipeline(IServiceProvider provider, List<(Delegate Function, Type[] DependencyTypes, string StepName)> steps)
    {
        _provider = provider;
        _steps = steps;
    }

    public async Task<TContext> ExecuteAsync(TContext context)
    {
        for (int i = 0; i < _steps.Count; i++)
        {
            try
            {
                context = await ExecuteStepAsync(_steps[i], context, i);
            }
            catch (MyPipelineException e)
            {
                Console.WriteLine($"Ending early with MyPipelineException: {e.Message}");
                return context;
            }
        }
        return context;
    }

    private async Task<TContext> ExecuteStepAsync(
        (Delegate Function, Type[] DependencyTypes, string StepName) step,
        TContext context, int stepIndex)
    {
        // Resolve dependencies
        var dependencies = step.DependencyTypes.Select(type => _provider.GetService(type)!).ToArray();

        var arguments = new object[dependencies.Length + 1];
        arguments[0] = context!;
        Array.Copy(dependencies, 0, arguments, 1, dependencies.Length);

        try
        {
            // Safely invoke the delegate and handle both synchronous and asynchronous results
            object? result = step.Function.DynamicInvoke(arguments);
        
            if (result is Task<TContext> taskResult)
            {
                // Handle properly awaitable Task<TContext>
                // This handles both true async methods and non-async Task-returning methods.
                // Any exceptions in the task will be thrown when awaited.
                Console.WriteLine($"Step {stepIndex + 1} - awaiting Task");
                return await taskResult;
            }
            if (result is TContext directResult)
            {
                // Handle synchronous functions that return TContext directly
                Console.WriteLine($"Step {stepIndex + 1} - synchronous non-task");
                return directResult;
            }
            throw new InvalidOperationException($"Step '{step.StepName}' returned an invalid result type. Expected Task<{typeof(TContext).Name}> or {typeof(TContext).Name}, but got {result?.GetType().Name ?? "null"}.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            // Unwrap exceptions from synchronous methods

            Console.WriteLine(tie.InnerException.Message);
            Console.WriteLine("INNER EXCEPTION");

            if (tie.InnerException is MyPipelineException)
            {
                throw tie.InnerException;
            }
            throw new ExecutionPipelineStepException($"INNER EXCEPTION: Error executing pipeline step {stepIndex}", tie.InnerException);
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("OUTER EXCEPTION");
            
            if (ex is MyPipelineException)
            {
                throw;
            }
            throw new ExecutionPipelineStepException($"OUTER EXCEPTION: Error executing pipeline step {stepIndex}", ex);
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
