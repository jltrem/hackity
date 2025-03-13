using ExecutionPipeline;
using ExecutionPipeline.AppServices;
using ExecutionPipeline.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = CreateHostBuilder(args).Build();
await host.Services.GetRequiredService<App>().RunAsync();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddTransient<IDigitsSumService, DigitsSumService>();
            services.AddTransient<IFactorialService, FactorialService>();
            services.AddTransient<IReverseDigitsService, ReverseDigitsService>();
            services.AddSingleton<App>();
            
            services.AddSingleton<IExecutionPipelineBuilderFactory, ExecutionPipelineBuilderFactory>();
            services.AddTransient<IMyExecutionPipeline, MyExecutionPipeline>();
        });
