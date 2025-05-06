using System.Text.Json.Serialization;

namespace Abyx.AI.EvaluationTests.Models;

public class EvaluationItem(string id, string query, string groundTruth)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;
    
    [JsonPropertyName("query")]
    public string Query { get; set; } = query;
    
    [JsonPropertyName("ground_truth")]
    public string GroundTruth { get; set; } = groundTruth;
}