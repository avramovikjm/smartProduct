using System.Text.Json.Nodes;

namespace SmartProduct.Models;

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonObject? Attributes { get; set; }
    public decimal Price { get; set; }
    public double Rating { get; set; }
    public int Stock { get; set; }
    public List<string> Tags { get; set; } = new();
}
