using Abyx.AI.Diagnostic.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Abyx.AI.Diagnostic.Providers;

public static class KernelProvider
{
    public static Kernel GetKernel(
        ILoggerFactory loggerFactory, 
        IConfiguration configuration)
    {
        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton(loggerFactory);
        builder
            .AddAzureOpenAIChatCompletion(
                deploymentName: configuration["AzureOpenAIText:Deployment"]!,
                endpoint: configuration["AzureOpenAIText:Endpoint"]!,
                apiKey: configuration["AzureOpenAIText:ApiKey"]!,
                serviceId: "AzureOpenAI");
            
        builder.Plugins.AddFromType<WeatherPlugin>();
        builder.Plugins.AddFromType<LocationPlugin>();

        return builder.Build();
    }
}