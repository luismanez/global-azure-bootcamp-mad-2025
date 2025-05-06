using System.Text.Json;
using Abyx.AI.EvaluationTests.Fixtures;
using Abyx.AI.EvaluationTests.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.KernelMemory;

namespace Abyx.AI.EvaluationTests.Tests;

public class RagAskShould : IClassFixture<EvaluationFixture>
{
    private readonly IKernelMemory? _memory;
    private readonly ReportingConfiguration _reportingConfiguration;

    public RagAskShould(
        EvaluationFixture fixture)
    {
        _memory = fixture.Memory;
        _reportingConfiguration = fixture.ReportingConfigurationWithEquivalenceAndGroundedness;
    }

    public static IEnumerable<object[]> GetQuestionsToEvaluate()
    {
        var datasetFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data/evaluation-dataset.json");
        var jsonContent = File.ReadAllText(datasetFullPath);
        var data = JsonSerializer.Deserialize<IEnumerable<EvaluationItem>>(jsonContent);
        return data!.Select(item => new object[] { item });
    }

    [Theory]
    [MemberData(nameof(GetQuestionsToEvaluate))]
    public async Task EvaluateQuestion(EvaluationItem question)
    {
        question.Query.Should().NotBeNull();

        // This SHOULD be a call to YOUR AI Chat API!
        var responseFromRagSystem = await _memory!.AskAsync(question.Query);

        var answerFromRagSystem = responseFromRagSystem.Result;
        var facts = string.Join(
            "\n",
            responseFromRagSystem.RelevantSources
                .SelectMany(c => c.Partitions)
                .OrderByDescending(p => p.Relevance)
                .Take(5)
                .Select(p => p.Text)
        );

        await using var scenarioRun =
            await _reportingConfiguration.CreateScenarioRunAsync($"Question_{question.Id}");

        var baselineResponseForEquivalenceEvaluator =
            new EquivalenceEvaluatorContext(question.GroundTruth);

        var groundingContextForGroundednessEvaluator =
            new GroundednessEvaluatorContext(facts);
        
        var evaluationResult =
            await scenarioRun.EvaluateAsync(
                question.Query,
                answerFromRagSystem,
                [baselineResponseForEquivalenceEvaluator, groundingContextForGroundednessEvaluator]);

        using var _ = new AssertionScope();

        var equivalence = evaluationResult.Get<NumericMetric>(EquivalenceEvaluator.EquivalenceMetricName);
        equivalence.Interpretation!.Failed.Should().NotBe(true);
        equivalence.Interpretation.Rating.Should().BeOneOf(EvaluationRating.Good, EvaluationRating.Exceptional);
        equivalence.Value.Should().BeGreaterThanOrEqualTo(3);

        var groundedness = evaluationResult.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        groundedness.Interpretation!.Failed.Should().NotBe(true);
        groundedness.Interpretation.Rating.Should().BeOneOf(EvaluationRating.Good, EvaluationRating.Exceptional);
        groundedness.Value.Should().BeGreaterThanOrEqualTo(3);
        
        var relevance = evaluationResult.Get<NumericMetric>(RelevanceTruthAndCompletenessEvaluator.RelevanceMetricName);
        relevance.Interpretation!.Failed.Should().NotBe(true);
        relevance.Interpretation.Rating.Should().BeOneOf(EvaluationRating.Good, EvaluationRating.Exceptional);
        relevance.Value.Should().BeGreaterThanOrEqualTo(3);
    }
}