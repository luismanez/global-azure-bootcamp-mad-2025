using System.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Abyx.AI.Diagnostic.Helpers;

public static class ChatHelper
{
    public static async Task RunAzureOpenAiChatAsync(
        Kernel kernel, 
        ActivitySource activitySource)
    {
        Console.WriteLine("============= Azure OpenAI Chat Completion =============");

        using var activity = activitySource.StartActivity("AzureOpenAIChat");
        SetTargetService(kernel, "AzureOpenAI");
        try
        {
            await RunChatAsync(kernel);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void SetTargetService(Kernel kernel, string targetServiceKey)
    {
        kernel.Data["TargetService"] = targetServiceKey;
    }

    private static async Task RunChatAsync(Kernel kernel)
    {
        var plugin = 
            kernel.CreateFunctionFromPrompt(
                "Tell me a joke about {{$input}}",
                functionName: "func_Joke");
    
        var joke = await kernel.InvokeAsync<string>(
            plugin,
            new KernelArguments { ["input"] = "Chuck Norris." });
        
        Console.WriteLine($"Joke:\n{joke}\n");
    }

    public static async Task RunAzureOpenAiToolCallsAsync(
        Kernel kernel, 
        ActivitySource activitySource)
    {
        Console.WriteLine("============= Azure OpenAI ToolCalls =============");

        using var activity = activitySource.StartActivity("AzureOpenAITools");
        SetTargetService(kernel, "AzureOpenAI");
        try
        {
            await RunAutoToolCallAsync(kernel);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task RunAutoToolCallAsync(Kernel kernel)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
        
        var toolFunction = 
            kernel.CreateFunctionFromPrompt(
                "What is the weather like in my location?", 
                settings,
                "func_Weather");
        
        var result = 
            await kernel.InvokeAsync<string>(function: toolFunction);

        Console.WriteLine(result);
    }
}