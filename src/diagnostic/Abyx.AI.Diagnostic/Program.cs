using System.Diagnostics;
using Abyx.AI.Diagnostic.Helpers;
using Abyx.AI.Diagnostic.Providers;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

ActivitySource activitySource = new("Abyx.AI.Diagnostic_Demo");

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var connectionString = config.GetConnectionString("AppInsightsConnectionString");

// Enable model diagnostics with sensitive data.
AppContext.SetSwitch(
    "Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", 
    true);

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("Abyx.AI.Diagnostic");

using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("Microsoft.SemanticKernel*")
    .AddSource("Abyx.AI.Diagnostic_Demo")
    .AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString)
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter("Microsoft.SemanticKernel*")
    .AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString)
    .Build();
    
    
using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddAzureMonitorLogExporter(monitorExporterOptions => 
            monitorExporterOptions.ConnectionString = connectionString);
        // Format log messages. This is default to false.
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Debug);
});

var kernel = KernelProvider.GetKernel(loggerFactory, config);

using var activity = activitySource.StartActivity("Main");
Console.WriteLine($"Operation/Trace ID: {Activity.Current?.TraceId}");
Console.WriteLine();

Console.WriteLine("Write a funny Joke.");
using (var _ = activitySource.StartActivity("Chat"))
{
    await ChatHelper.RunAzureOpenAiChatAsync(kernel, activitySource);
    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine();

Console.WriteLine("Get weather.");
using (var _ = activitySource.StartActivity("ToolCalls"))
{
    await ChatHelper.RunAzureOpenAiToolCallsAsync(kernel, activitySource);
    Console.WriteLine();
}

