using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

IList<ChatMessage> chatMessages = [
        new ChatMessage(
            ChatRole.System,
            """
            You are an AI assistant that can answer questions related to astronomy.
            Keep your responses concise staying under 100 words as much as possible.
            Use the imperial measurement system for all measurements in your response.
            """),
        new ChatMessage(
            ChatRole.User,
            "How far is the planet Venus from the Earth at its closest and furthest points?")];

var chatClient =
            new AzureOpenAIClient(
                new Uri(config["AzureOpenAIText:Endpoint"]!),
                new ApiKeyCredential(config["AzureOpenAIText:APIKey"]!))
                .AsChatClient(modelId: config["AzureOpenAIText:Deployment"]!);

var chatConfiguration = new ChatConfiguration(chatClient);

var chatOptions =
            new ChatOptions
            {
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Text
            };

// This should be a call to your AI Chat API (for simplicity we just call directly the LLM)
var response = await chatConfiguration.ChatClient.GetResponseAsync(chatMessages, chatOptions);
var modelResponse = response.Message;

var relevanceEvaluator = new RelevanceTruthAndCompletenessEvaluator();
var result = await relevanceEvaluator.EvaluateAsync(chatMessages, modelResponse, chatConfiguration);
var relevance = result.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.RelevanceMetricName);

Console.WriteLine($"Relevance value (0-5): {relevance.Value}");
Console.WriteLine($"Failed?: {relevance.Interpretation!.Failed}");
Console.WriteLine($"Rating: {relevance.Interpretation!.Rating}");
