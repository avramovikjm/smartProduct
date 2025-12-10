namespace SmartProduct.Services;

/// <summary>
/// Adapter to maintain backward compatibility with existing IRecommendationService interface
/// while using the new MAF-based IRecommendationAgent
/// </summary>
public class RecommendationServiceAdapter : IRecommendationService
{
    private readonly IRecommendationAgent _agent;

    public RecommendationServiceAdapter(IRecommendationAgent agent)
    {
        _agent = agent;
    }

    public Task<IReadOnlyList<RecommendedProduct>> RecommendAsync(string userQuery, int count = 5, CancellationToken ct = default)
    {
        return _agent.RecommendAsync(userQuery, count, ct);
    }
}
