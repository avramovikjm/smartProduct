using SmartProduct.Services;
using SmartProduct.Agents;
using SmartProduct.Configuration;
using Azure.AI.OpenAI;
using Azure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register product catalog
builder.Services.AddSingleton<IProductCatalog, ProductCatalog>();

// Configure Azure OpenAI settings
var azureOpenAISettings = builder.Configuration
    .GetSection(AzureOpenAISettings.SectionName)
    .Get<AzureOpenAISettings>() ?? new AzureOpenAISettings();

builder.Services.AddSingleton(azureOpenAISettings);

// Configure Azure OpenAI with Agent patterns
if (azureOpenAISettings.IsConfigured)
{
    Console.WriteLine("✅ Azure OpenAI configured - Initializing Product Recommendation Agent (.NET 9)...");
    
    // Register Azure OpenAI Client
    var azureClient = new AzureOpenAIClient(
        new Uri(azureOpenAISettings.Endpoint),
        new AzureKeyCredential(azureOpenAISettings.ApiKey));
    
    builder.Services.AddSingleton(azureClient);
    
    // Register the Product Recommendation Agent
    builder.Services.AddScoped<ProductRecommendationAgent>(sp => 
        new ProductRecommendationAgent(
            sp.GetRequiredService<IProductCatalog>(),
            sp.GetRequiredService<ILogger<ProductRecommendationAgent>>(),
            sp.GetRequiredService<AzureOpenAIClient>(),
            azureOpenAISettings.DeploymentName,
            azureOpenAISettings.EmbeddingDeploymentName));
    
    // Register adapters for backward compatibility
    builder.Services.AddScoped<IRecommendationAgent>(sp => 
        new AgentBasedRecommendationService(
            sp.GetRequiredService<ProductRecommendationAgent>(),
            sp.GetRequiredService<ILogger<AgentBasedRecommendationService>>()));
    
    builder.Services.AddScoped<IRecommendationService>(sp => 
        sp.GetRequiredService<IRecommendationAgent>() as IRecommendationService 
        ?? new RecommendationServiceAdapter(sp.GetRequiredService<IRecommendationAgent>()));
    
    Console.WriteLine("✅ Agent initialized successfully");
    Console.WriteLine("   - Agent: ProductRecommendationAgent");
    Console.WriteLine("   - Chat Model: " + azureOpenAISettings.DeploymentName);
    Console.WriteLine("   - Embeddings: " + azureOpenAISettings.EmbeddingDeploymentName);
}
else
{
    Console.WriteLine("⚠️ Azure OpenAI NOT configured - Using fallback mode");
    Console.WriteLine("  Configure AzureOpenAI settings in appsettings.json");
    
    // Register agent without AI capabilities
    builder.Services.AddScoped<ProductRecommendationAgent>(sp =>
        new ProductRecommendationAgent(
            sp.GetRequiredService<IProductCatalog>(),
            sp.GetRequiredService<ILogger<ProductRecommendationAgent>>(),
            azureClient: null,
            chatDeployment: null,
            embeddingDeployment: null));
    
    // Register adapters
    builder.Services.AddScoped<IRecommendationAgent>(sp => 
        new AgentBasedRecommendationService(
            sp.GetRequiredService<ProductRecommendationAgent>(),
            sp.GetRequiredService<ILogger<AgentBasedRecommendationService>>()));
    
    builder.Services.AddScoped<IRecommendationService>(sp => 
        sp.GetRequiredService<IRecommendationAgent>() as IRecommendationService 
        ?? new RecommendationServiceAdapter(sp.GetRequiredService<IRecommendationAgent>()));
}

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
