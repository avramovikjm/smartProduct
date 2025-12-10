using SmartProduct.Models;
using System.Text;
using System.Text.Json;

namespace SmartProduct.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendedProduct>> RecommendAsync(string userQuery, int count = 5, CancellationToken ct = default);
}

public record RecommendedProduct(
    string Id, 
    string Name, 
    string Category, 
    decimal Price, 
    double Rating, 
    string Explanation, 
    float Relevance
);

public class RecommendationService : IRecommendationService
{
    private readonly IProductCatalog _catalog;
    private readonly ILogger<RecommendationService> _logger;
    private readonly Dictionary<string, float[]> _productEmbeddings;

    public RecommendationService(
        IProductCatalog catalog, 
        ILogger<RecommendationService> logger,
        IServiceProvider serviceProvider)
    {
        _catalog = catalog;
        _logger = logger;
        _productEmbeddings = new Dictionary<string, float[]>();
        
        InitializeEmbeddings();
    }

    private void InitializeEmbeddings()
    {
        foreach (var product in _catalog.All)
        {
            var embedding = CreateSimpleEmbedding(product);
            _productEmbeddings[product.Id] = embedding;
        }
        _logger.LogInformation("Initialized embeddings for {Count} products", _productEmbeddings.Count);
    }

    private float[] CreateSimpleEmbedding(Product product)
    {
        var allTerms = new HashSet<string>();
        
        foreach (var word in product.Name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            allTerms.Add(word);
        }
        
        allTerms.Add(product.Category.ToLowerInvariant());
        
        foreach (var tag in product.Tags)
        {
            allTerms.Add(tag.ToLowerInvariant());
        }
        
        foreach (var word in product.Description.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length > 3) allTerms.Add(word);
        }
        
        var embedding = new float[128];
        foreach (var term in allTerms)
        {
            var hash = term.GetHashCode();
            embedding[Math.Abs(hash % 128)] += 1.0f;
        }
        
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(embedding[i] / magnitude);
            }
        }
        
        return embedding;
    }

    private float CosineSimilarity(float[] vec1, float[] vec2)
    {
        if (vec1.Length != vec2.Length) return 0;
        
        float dot = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < vec1.Length; i++)
        {
            dot += vec1[i] * vec2[i];
            mag1 += vec1[i] * vec1[i];
            mag2 += vec2[i] * vec2[i];
        }
        
        if (mag1 == 0 || mag2 == 0) return 0;
        return dot / (float)(Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }

    public Task<IReadOnlyList<RecommendedProduct>> RecommendAsync(
        string userQuery, 
        int count = 5, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Processing recommendation query: {Query}", userQuery);

        var queryEmbedding = CreateQueryEmbedding(userQuery);
        var candidates = _catalog.All
            .Select(p => new
            {
                Product = p,
                Similarity = _productEmbeddings.ContainsKey(p.Id) 
                    ? CosineSimilarity(queryEmbedding, _productEmbeddings[p.Id])
                    : 0.0f
            })
            .Where(x => x.Similarity > 0.1f)
            .OrderByDescending(x => x.Similarity)
            .ThenByDescending(x => x.Product.Rating)
            .Take(count * 2)
            .ToList();

        if (candidates.Count == 0)
        {
            _logger.LogWarning("No semantic matches found, using fallback recommendations");
            var fallbackResults = _catalog.All
                .OrderByDescending(p => p.Rating)
                .Take(count)
                .Select(p => new RecommendedProduct(
                    p.Id,
                    p.Name,
                    p.Category,
                    p.Price,
                    p.Rating,
                    $"Recommended based on high rating ({p.Rating:F1} stars)",
                    0.5f
                ))
                .ToList();
            return Task.FromResult<IReadOnlyList<RecommendedProduct>>(fallbackResults);
        }

        // LLM-based ranking is temporarily disabled due to package compatibility issues
        // Will be re-enabled once Azure.AI.OpenAI package is properly configured
        // if (_openAiClient != null && !string.IsNullOrEmpty(_deploymentName))
        // {
        //     try
        //     {
        //         var recommendations = await GenerateRecommendationsWithLLM(
        //             userQuery, 
        //             candidates.Select(c => c.Product).ToList(), 
        //             count, 
        //             ct);
        //         
        //         if (recommendations.Count > 0)
        //         {
        //             return recommendations;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(ex, "LLM recommendation failed, falling back to semantic search");
        //     }
        // }

        // Return semantic search results with improved explanations
        var results = candidates
            .Take(count)
            .Select(c => new RecommendedProduct(
                c.Product.Id,
                c.Product.Name,
                c.Product.Category,
                c.Product.Price,
                c.Product.Rating,
                GenerateExplanation(c.Product, userQuery, c.Similarity),
                c.Similarity
            ))
            .ToList();
        
        return Task.FromResult<IReadOnlyList<RecommendedProduct>>(results);
    }

    private string GenerateExplanation(Product product, string query, float similarity)
    {
        var explanation = new StringBuilder();
        explanation.Append($"Recommended because ");
        
        // Check for age-related matches
        if (query.ToLowerInvariant().Contains("puppy") || query.ToLowerInvariant().Contains("young"))
        {
            if (product.Tags.Contains("puppy") || product.Name.ToLowerInvariant().Contains("puppy"))
            {
                explanation.Append($"it's specifically designed for puppies and young dogs. ");
            }
        }
        
        // Check for category matches
        var queryLower = query.ToLowerInvariant();
        if (queryLower.Contains(product.Category.ToLowerInvariant()))
        {
            explanation.Append($"it's in the {product.Category} category you're looking for. ");
        }
        
        // Check for tag matches
        var matchingTags = product.Tags.Where(tag => queryLower.Contains(tag.ToLowerInvariant())).ToList();
        if (matchingTags.Any())
        {
            explanation.Append($"it matches your interest in {string.Join(" and ", matchingTags)}. ");
        }
        
        // Add rating info
        if (product.Rating >= 4.5)
        {
            explanation.Append($"It has an excellent rating of {product.Rating:F1} stars. ");
        }
        else if (product.Rating >= 4.0)
        {
            explanation.Append($"It has a good rating of {product.Rating:F1} stars. ");
        }
        
        // Add price info if relevant
        if (queryLower.Contains("cheap") || queryLower.Contains("affordable") || queryLower.Contains("budget"))
        {
            explanation.Append($"It's priced at ${product.Price:F2}, which is affordable. ");
        }
        
        if (explanation.Length == "Recommended because ".Length)
        {
            explanation.Append($"it matches your query with a relevance score of {similarity:F2}. ");
        }
        
        return explanation.ToString().Trim();
    }

    private float[] CreateQueryEmbedding(string query)
    {
        var allTerms = new HashSet<string>();
        foreach (var word in query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length > 2) allTerms.Add(word);
        }
        
        var embedding = new float[128];
        foreach (var term in allTerms)
        {
            var hash = term.GetHashCode();
            embedding[Math.Abs(hash % 128)] += 1.0f;
        }
        
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(embedding[i] / magnitude);
            }
        }
        
        return embedding;
    }

    // LLM-based recommendation generation - temporarily disabled
    // Will be re-enabled once Azure.AI.OpenAI package compatibility is resolved
    /*
    private async Task<List<RecommendedProduct>> GenerateRecommendationsWithLLM(
        string userQuery,
        List<Product> candidates,
        int count,
        CancellationToken ct)
    {
        // Implementation will be added back once package issues are resolved
        return new List<RecommendedProduct>();
    }
    */

    private List<RecommendedProduct> ParseLLMResponse(string response, List<Product> candidates)
    {
        var recommendations = new List<RecommendedProduct>();
        
        try
        {
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                var items = JsonSerializer.Deserialize<List<JsonElement>>(json);
                
                if (items != null)
                {
                    var productDict = candidates.ToDictionary(p => p.Id, p => p);
                    
                    foreach (var item in items)
                    {
                        if (item.TryGetProperty("id", out var idElement))
                        {
                            var productId = idElement.GetString();
                            if (productId != null && productDict.TryGetValue(productId, out var product))
                            {
                                var explanation = item.TryGetProperty("explanation", out var expElement) 
                                    ? expElement.GetString() ?? "Recommended based on your query"
                                    : "Recommended based on your query";
                                
                                var relevance = item.TryGetProperty("relevance", out var relElement) 
                                    ? (float)Math.Clamp(relElement.GetSingle(), 0f, 1f)
                                    : 0.8f;
                                
                                recommendations.Add(new RecommendedProduct(
                                    product.Id,
                                    product.Name,
                                    product.Category,
                                    product.Price,
                                    product.Rating,
                                    explanation,
                                    relevance
                                ));
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON, using fallback");
        }
        
        return recommendations;
    }
}
