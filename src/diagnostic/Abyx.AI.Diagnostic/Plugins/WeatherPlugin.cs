using Microsoft.SemanticKernel;

namespace Abyx.AI.Diagnostic.Plugins;

public sealed class WeatherPlugin
{
    [KernelFunction]
    public string GetWeather(string location) => $"Weather in {location} is 27°C.";
}