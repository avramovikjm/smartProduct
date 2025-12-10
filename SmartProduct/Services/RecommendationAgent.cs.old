using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using SmartProduct.Models;
using System.Text;
using System.Text.Json;

namespace SmartProduct.Services;

public interface IRecommendationAgent
{
    Task<IReadOnlyList<RecommendedProduct>> RecommendAsync(string userQuery, int count = 5, CancellationToken ct = default);
}

/// <summary>
/// MAF-based recommendation agent using Semantic Kernel for AI orchestration
/// </summary>
public class RecommendationAgent : IRecommendationAgent
{
    private readonly IProductCatalog _catalog;
    private readonly ILogger<RecommendationAgent> _logger;
    private readonly Kernel _kernel;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private readonly Dictionary<string, ReadOnlyMemory<float>> _productEmbeddings;
    private readonly bool _useAI;

    public RecommendationAgent(
        IProductCatalog catalog,
        ILogger<RecommendationAgent> logger,
        Kernel kernel,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        _catalog = catalog;
        _logger = logger;
        _kernel = kernel;
        _embeddingGenerator = embeddingGenerator;
        _productEmbeddings = new Dictionary<string, ReadOnlyMemory<float>>();
        _useAI = _embeddingGenerator != null;
        
        if (_useAI)
        {
            _logger.LogInformation("RecommendationAgent initialized with AI capabilities (Semantic Kernel + Azure OpenAI)");
        }
        else
        {
            _logger.LogWarning("RecommendationAgent initialized WITHOUT AI - using fallback semantic search");
        }
    }

    public async Task<IReadOnlyList<RecommendedProduct>> RecommendAsync(
        string userQuery,
        int count = 5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Processing recommendation query: {Query}", userQuery);

        if (_useAI)
        {
            return await RecommendWithAIAsync(userQuery, count, ct);
        }
        else
        {
            return await RecommendWithFallbackAsync(userQuery, count, ct);
        }
    }

    private async Task<IReadOnlyList<RecommendedProduct>> RecommendWithAIAsync(
        string userQuery,
        int count,
        CancellationToken ct)
    {
        try
        {
            // Step 1: Generate embeddings for query
            var embeddingResult = await _embeddingGenerator!.GenerateAsync([userQuery], cancellationToken: ct);
            var queryEmbedding = embeddingResult[0].Vector;
            
            // Step 2: Ensure product embeddings are generated
            await EnsureProductEmbeddingsAsync(ct);
            
            // Step 3: Find similar products using vector search
            var candidates = FindSimilarProducts(queryEmbedding, count * 2);
            
            if (candidates.Count == 0)
            {
                _logger.LogWarning("No semantic matches found with AI embeddings, using fallback");
                return await RecommendWithFallbackAsync(userQuery, count, ct);
            }
            
            // Step 4: Use LLM to rank and explain recommendations
            var recommendations = await RankWithLLMAsync(userQuery, candidates, count, ct);
            
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI-based recommendation failed, falling back to semantic search");
            return await RecommendWithFallbackAsync(userQuery, count, ct);
        }
    }

    private async Task EnsureProductEmbeddingsAsync(CancellationToken ct)
    {
        if (_productEmbeddings.Count > 0)
            return;

        _logger.LogInformation("Generating embeddings for {Count} products...", _catalog.All.Count);
        
        foreach (var product in _catalog.All)
        {
            try
            {
                var productText = CreateProductText(product);
                var embeddingResult = await _embeddingGenerator!.GenerateAsync([productText], cancellationToken: ct);
                _productEmbeddings[product.Id] = embeddingResult[0].Vector;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for product {ProductId}", product.Id);
            }
        }
        
        _logger.LogInformation("Successfully generated embeddings for {Count} products", _productEmbeddings.Count);
    }

    private string CreateProductText(Product product)
    {
        return $"{product.Name}. {product.Description}. Category: {product.Category}. Tags: {string.Join(", ", product.Tags)}";
    }

    private List<Product> FindSimilarProducts(ReadOnlyMemory<float> queryEmbedding, int topK)
    {
        var similarities = new List<(Product Product, double Similarity)>();
        
        foreach (var product in _catalog.All)
        {
            if (_productEmbeddings.TryGetValue(product.Id, out var productEmbedding))
            {
                var similarity = CosineSimilarity(queryEmbedding, productEmbedding);
                similarities.Add((product, similarity));
            }
        }
        
        return similarities
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .Select(x => x.Product)
            .ToList();
    }

    private double CosineSimilarity(ReadOnlyMemory<float> vec1, ReadOnlyMemory<float> vec2)
    {
        var span1 = vec1.Span;
        var span2 = vec2.Span;
        
        if (span1.Length != span2.Length)
            return 0;
        
        double dot = 0, mag1 = 0, mag2 = 0;
        
        for (int i = 0; i < span1.Length; i++)
        {
            dot += span1[i] * span2[i];
            mag1 += span1[i] * span1[i];
            mag2 += span2[i] * span2[i];
        }
        
        if (mag1 == 0 || mag2 == 0)
            return 0;
        
        return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }

    private async Task<IReadOnlyList<RecommendedProduct>> RankWithLLMAsync(
        string userQuery,
        List<Product> candidates,
        int count,
        CancellationToken ct)
    {
        var prompt = CreateRankingPrompt(userQuery, candidates, count);
        
        try
        {
            var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: ct);
            var result = response.GetValue<string>() ?? "";
            
            var recommendations = ParseLLMResponse(result, candidates);
            
            if (recommendations.Count > 0)
            {
                _logger.LogInformation("LLM successfully ranked {Count} recommendations", recommendations.Count);
                return recommendations.Take(count).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM ranking failed, using similarity-based ranking");
        }
        
        // Fallback: return top candidates with generic explanations
        return candidates
            .Take(count)
            .Select((p, idx) => new RecommendedProduct(
                p.Id,
                p.Name,
                p.Category,
                p.Price,
                p.Rating,
                $"Recommended based on similarity to your query. Rating: {p.Rating:F1} stars.",
                1.0f - (idx * 0.1f)
            ))
            .ToList();
    }

    private string CreateRankingPrompt(string userQuery, List<Product> candidates, int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a product recommendation expert. Given a user query and candidate products, rank the most relevant products and explain why they match.");
        sb.AppendLine();
        sb.AppendLine($"User Query: {userQuery}");
        sb.AppendLine();
        sb.AppendLine("Candidate Products:");
        
        foreach (var product in candidates)
        {
            sb.AppendLine($"- ID: {product.Id}");
            sb.AppendLine($"  Name: {product.Name}");
            sb.AppendLine($"  Category: {product.Category}");
            sb.AppendLine($"  Description: {product.Description}");
            sb.AppendLine($"  Price: ${product.Price:F2}");
            sb.AppendLine($"  Rating: {product.Rating:F1} stars");
            sb.AppendLine($"  Tags: {string.Join(", ", product.Tags)}");
            sb.AppendLine();
        }
        
        sb.AppendLine($"Please select the top {count} products that best match the user's query and return them as a JSON array with this format:");
        sb.AppendLine("[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"id\": \"product_id\",");
        sb.AppendLine("    \"explanation\": \"Brief explanation of why this product matches the query\",");
        sb.AppendLine("    \"relevance\": 0.95");
        sb.AppendLine("  }");
        sb.AppendLine("]");
        sb.AppendLine();
        sb.AppendLine("Return ONLY the JSON array, no additional text.");
        
        return sb.ToString();
    }

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
                                    ? (float)Math.Clamp(relElement.GetDouble(), 0.0, 1.0)
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
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON");
        }
        
        return recommendations;
    }

    private async Task<IReadOnlyList<RecommendedProduct>> RecommendWithFallbackAsync(
        string userQuery,
        int count,
        CancellationToken ct)
    {
        await Task.CompletedTask; // Keep async signature
        
        var queryEmbedding = CreateSimpleEmbedding(userQuery);
        var candidates = _catalog.All
            .Select(p => new
            {
                Product = p,
                Similarity = CosineSimilarity(queryEmbedding, CreateSimpleEmbedding(CreateProductText(p)))
            })
            .Where(x => x.Similarity > 0.1)
            .OrderByDescending(x => x.Similarity)
            .ThenByDescending(x => x.Product.Rating)
            .Take(count)
            .ToList();

        if (candidates.Count == 0)
        {
            _logger.LogWarning("No semantic matches found, using top-rated products");
            return _catalog.All
                .OrderByDescending(p => p.Rating)
                .Take(count)
                .Select(p => new RecommendedProduct(
                    p.Id,
                    p.Name,
                    p.Category,
                    p.Price,
                    p.Rating,
                    $"Top-rated product ({p.Rating:F1} stars)",
                    0.5f
                ))
                .ToList();
        }

        return candidates
            .Select(c => new RecommendedProduct(
                c.Product.Id,
                c.Product.Name,
                c.Product.Category,
                c.Product.Price,
                c.Product.Rating,
                GenerateExplanation(c.Product, userQuery),
                (float)c.Similarity
            ))
            .ToList();
    }

    private ReadOnlyMemory<float> CreateSimpleEmbedding(string text)
    {
        var allTerms = new HashSet<string>();
        foreach (var word in text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
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
        
        return new ReadOnlyMemory<float>(embedding);
    }

    private string GenerateExplanation(Product product, string query)
    {
        var explanation = new StringBuilder();
        explanation.Append("Recommended because ");
        
        var queryLower = query.ToLowerInvariant();
        
        if (queryLower.Contains(product.Category.ToLowerInvariant()))
        {
            explanation.Append($"it's in the {product.Category} category you're looking for. ");
        }
        
        var matchingTags = product.Tags.Where(tag => queryLower.Contains(tag.ToLowerInvariant())).ToList();
        if (matchingTags.Any())
        {
            explanation.Append($"It matches your interest in {string.Join(" and ", matchingTags)}. ");
        }
        
        if (product.Rating >= 4.5)
        {
            explanation.Append($"It has an excellent rating of {product.Rating:F1} stars. ");
        }
        
        if (explanation.Length == "Recommended because ".Length)
        {
            explanation.Append($"it's relevant to your search. Rating: {product.Rating:F1} stars.");
        }
        
        return explanation.ToString().Trim();
    }
}
