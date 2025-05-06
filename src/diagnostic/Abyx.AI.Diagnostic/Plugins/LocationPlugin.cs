using Microsoft.SemanticKernel;

namespace Abyx.AI.Diagnostic.Plugins;

public sealed class LocationPlugin
{
    [KernelFunction]
    public string GetCurrentLocation()
    {
        return "Valencia, Spain";
    }
}