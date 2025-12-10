using SmartProduct.Services;
using SmartProduct.Agents;
using SmartProduct.Configuration;
using System.Reflection;
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

// Configure Microsoft Agent Framework with Azure OpenAI
if (azureOpenAISettings.IsConfigured)
{
    Console.WriteLine("? Azure OpenAI configured - Initializing Microsoft Agent Framework (.NET 9)...");
    
    // Register Azure OpenAI Client
    builder.Services.AddSingleton<AzureOpenAIClient>(sp =>
    {
        return new AzureOpenAIClient(
            new Uri(azureOpenAISettings.Endpoint),
            new AzureKeyCredential(azureOpenAISettings.ApiKey));
    });
    
    // Register the Product Recommendation Agent
    builder.Services.AddScoped<ProductRecommendationAgent>(sp => 
        new ProductRecommendationAgent(
            sp.GetRequiredService<IProductCatalog>(),
            sp.GetRequiredService<ILogger<ProductRecommendationAgent>>(),
            sp.GetRequiredService<AzureOpenAIClient>(),
            azureOpenAISettings.DeploymentName,
            azureOpenAISettings.EmbeddingDeploymentName));
    
    // Register adapter for backward compatibility
    builder.Services.AddScoped<IRecommendationAgent>(sp => 
        new AgentBasedRecommendationService(
            sp.GetRequiredService<ProductRecommendationAgent>(),
            sp.GetRequiredService<ILogger<AgentBasedRecommendationService>>()));
    
    builder.Services.AddScoped<IRecommendationService>(sp => 
        sp.GetRequiredService<IRecommendationAgent>() as IRecommendationService 
        ?? new RecommendationServiceAdapter(sp.GetRequiredService<IRecommendationAgent>()));
    
    Console.WriteLine("? Microsoft Agent Framework initialized successfully");
    Console.WriteLine("   - Agent: ProductRecommendationAgent");
    Console.WriteLine("   - Framework: Microsoft.Agents.AI.OpenAI v1.0.0-preview");
    Console.WriteLine("   - AI Model: Azure OpenAI " + azureOpenAISettings.DeploymentName);
    Console.WriteLine("   - Embeddings: " + azureOpenAISettings.EmbeddingDeploymentName);
}
else
{
    Console.WriteLine("?? Azure OpenAI NOT configured - Using fallback semantic search");
    Console.WriteLine("  Configure AzureOpenAI settings in appsettings.json to enable AI features");
    
    // Register agent without AI capabilities
    builder.Services.AddScoped<ProductRecommendationAgent>(sp =>
    {
        return new ProductRecommendationAgent(
            sp.GetRequiredService<IProductCatalog>(),
            sp.GetRequiredService<ILogger<ProductRecommendationAgent>>(),
            azureClient: null,
            chatDeployment: null,
            embeddingDeployment: null);
    });
    
    // Register adapter for backward compatibility
    builder.Services.AddScoped<IRecommendationAgent>(sp => 
        new AgentBasedRecommendationService(
            sp.GetRequiredService<ProductRecommendationAgent>(),
            sp.GetRequiredService<ILogger<AgentBasedRecommendationService>>()));
    
    builder.Services.AddScoped<IRecommendationService>(sp => 
        sp.GetRequiredService<IRecommendationAgent>() as IRecommendationService 
        ?? new RecommendationServiceAdapter(sp.GetRequiredService<IRecommendationAgent>()));
}

var app = builder.Build();

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        
        if (exception is ReflectionTypeLoadException rtle)
        {
            logger.LogError(rtle, "ReflectionTypeLoadException occurred");
            LogLoaderExceptions(rtle, logger);
        }
        else
        {
            logger.LogError(exception, "Unhandled exception occurred");
        }
        
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An error occurred. Check logs for details.");
    });
});

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void LogLoaderExceptions(ReflectionTypeLoadException ex, ILogger? logger)
{
    if (ex.LoaderExceptions != null)
    {
        for (int i = 0; i < ex.LoaderExceptions.Length; i++)
        {
            var loaderEx = ex.LoaderExceptions[i];
            if (loaderEx != null)
            {
                logger?.LogError("Loader Exception [{Index}]: {Type} - {Message}", 
                    i, loaderEx.GetType().Name, loaderEx.Message);
                if (!string.IsNullOrEmpty(loaderEx.StackTrace))
                {
                    logger?.LogError("Stack Trace: {StackTrace}", loaderEx.StackTrace);
                }
            }
        }
    }
    
    if (ex.Types != null)
    {
        for (int i = 0; i < ex.Types.Length; i++)
        {
            if (ex.Types[i] == null && ex.LoaderExceptions != null && i < ex.LoaderExceptions.Length)
            {
                logger?.LogError("Failed to load type at index {Index}. LoaderException: {Exception}", 
                    i, ex.LoaderExceptions[i]?.Message ?? "Unknown");
            }
        }
    }
}
