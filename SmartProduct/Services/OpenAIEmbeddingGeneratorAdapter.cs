using Microsoft.Extensions.AI;
using OpenAI.Embeddings;
using AIEmbedding = Microsoft.Extensions.AI.Embedding<float>;
using AIEmbeddingOptions = Microsoft.Extensions.AI.EmbeddingGenerationOptions;

namespace SmartProduct.Services;

/// <summary>
/// Wrapper to adapt OpenAI EmbeddingClient to Microsoft.Extensions.AI.IEmbeddingGenerator
/// </summary>
public class OpenAIEmbeddingGeneratorAdapter : IEmbeddingGenerator<string, AIEmbedding>
{
    private readonly EmbeddingClient _client;

    public OpenAIEmbeddingGeneratorAdapter(EmbeddingClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public EmbeddingGeneratorMetadata Metadata => new("OpenAI");

    public async Task<GeneratedEmbeddings<AIEmbedding>> GenerateAsync(
        IEnumerable<string> values,
        AIEmbeddingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var valuesList = values.ToList();
        if (valuesList.Count == 0)
        {
            return new GeneratedEmbeddings<AIEmbedding>([]);
        }

        var response = await _client.GenerateEmbeddingsAsync(valuesList, cancellationToken: cancellationToken);
        
        var embeddings = new List<AIEmbedding>();
        foreach (var item in response.Value)
        {
            var vector = item.ToFloats().ToArray();
            embeddings.Add(new AIEmbedding(vector));
        }

        return new GeneratedEmbeddings<AIEmbedding>(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
        // EmbeddingClient doesn't implement IDisposable, nothing to dispose
    }
}
