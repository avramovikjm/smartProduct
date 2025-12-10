using SmartProduct.Models;

namespace SmartProduct.Services;

/// <summary>
/// Interface for recommendation agents
/// </summary>
public interface IRecommendationAgent
{
    Task<IReadOnlyList<RecommendedProduct>> RecommendAsync(string userQuery, int count = 5, CancellationToken ct = default);
}
