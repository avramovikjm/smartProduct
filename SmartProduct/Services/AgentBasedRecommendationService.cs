using SmartProduct.Models;
using SmartProduct.Agents;

namespace SmartProduct.Services;

/// <summary>
/// Adapter that wraps the Microsoft Agent Framework ProductRecommendationAgent
/// Provides backward compatibility with IRecommendationAgent and IRecommendationService
/// </summary>
public class AgentBasedRecommendationService : IRecommendationAgent, IRecommendationService
{
    private readonly ProductRecommendationAgent _agent;
    private readonly ILogger<AgentBasedRecommendationService> _logger;

    public AgentBasedRecommendationService(
        ProductRecommendationAgent agent,
        ILogger<AgentBasedRecommendationService> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecommendedProduct>> RecommendAsync(
        string userQuery,
        int count = 5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("AgentBasedRecommendationService routing request to ProductRecommendationAgent");
        return await _agent.RecommendAsync(userQuery, count, ct);
    }
}
