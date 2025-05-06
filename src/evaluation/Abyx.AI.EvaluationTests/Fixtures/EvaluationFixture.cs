using System.ClientModel;
using System.Text.Json;
using Abyx.AI.EvaluationTests.Models;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

namespace Abyx.AI.EvaluationTests.Fixtures;

public class EvaluationFixture : IAsyncLifetime
{
    public IKernelMemory Memory { get; private set; } = null!;
    public ReportingConfiguration ReportingConfigurationWithEquivalenceAndGroundedness { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("testsettings.json", optional: false, reloadOnChange: false)
            .Build(); 
        
        InitialiseKernelMemory(configuration);

        await ImportRagDocuments();
        
        ReportingConfigurationWithEquivalenceAndGroundedness =
            DiskBasedReportingConfiguration.Create(
                storageRootPath: configuration.GetValue("StorageRootPath", string.Empty),
                evaluators: [
                    new EquivalenceEvaluator(), 
                    new GroundednessEvaluator(), 
                    new RelevanceTruthAndCompletenessEvaluator()],
                chatConfiguration: GetAzureOpenAiChatConfiguration(configuration),
                enableResponseCaching: false,
                executionName: $"Execution_{DateTime.Now:yyyyMMddTHHmmss}");
    }

    private async Task ImportRagDocuments()
    {
        await Memory.ImportDocumentAsync(
            filePath: "Data/story.MD",
            documentId: "black-mirror-story-01");
    }

    private void InitialiseKernelMemory(IConfiguration configuration)
    {
        var azureOpenAiTextConfig = new AzureOpenAIConfig();
        var azureOpenAiEmbeddingConfig = new AzureOpenAIConfig();
        var azureAiSearchConfig = new AzureAISearchConfig();
        
        configuration
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAiTextConfig)
            .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAiEmbeddingConfig)
            .BindSection("KernelMemory:Services:AzureAISearch", azureAiSearchConfig);
        
        Memory = new KernelMemoryBuilder()
            .With(new KernelMemoryConfig { DefaultIndexName = "rag-evaluation-demo" })
            .WithAzureOpenAITextEmbeddingGeneration(azureOpenAiEmbeddingConfig)
            .WithAzureOpenAITextGeneration(azureOpenAiTextConfig)
            .WithAzureAISearchMemoryDb(azureAiSearchConfig)
            .Build<MemoryServerless>(new KernelMemoryBuilderBuildOptions {
                AllowMixingVolatileAndPersistentData = true
            });
    }

    public async Task DisposeAsync()
    {
        await Memory.DeleteIndexAsync();
    }
    
    private static ChatConfiguration GetAzureOpenAiChatConfiguration(IConfiguration configuration)
    {
        var azureOpenAiTextConfig = new AzureOpenAIConfig();
        configuration
            .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAiTextConfig);
            
        var client =
            new AzureOpenAIClient(
                    new Uri(azureOpenAiTextConfig.Endpoint), 
                    new ApiKeyCredential(azureOpenAiTextConfig.APIKey))
                .AsChatClient(modelId: azureOpenAiTextConfig.Deployment);
        
        return new ChatConfiguration(client);
    }
}