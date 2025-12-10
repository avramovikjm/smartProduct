using System.Text.Json;
using SmartProduct.Models;

namespace SmartProduct.Services;

public interface IProductCatalog
{
    IReadOnlyList<Product> All { get; }
    IEnumerable<Product> FilterByCategory(string category);
}

public class ProductCatalog : IProductCatalog
{
    private readonly List<Product> _products = new();
    public IReadOnlyList<Product> All => _products;

    public ProductCatalog(IWebHostEnvironment env, ILogger<ProductCatalog> logger)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "products.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("Products file not found at {Path}", path);
            return;
        }
        var json = File.ReadAllText(path);
        var products = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (products != null)
            _products.AddRange(products);
        logger.LogInformation("Loaded {Count} products", _products.Count);
    }

    public IEnumerable<Product> FilterByCategory(string category) => _products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
}
