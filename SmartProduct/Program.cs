using SmartProduct.Services;
using SmartProduct.Configuration;
using System.Reflection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;

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

// Configure Semantic Kernel with MAF
if (azureOpenAISettings.IsConfigured)
{
    Console.WriteLine("? Azure OpenAI configured - Initializing MAF with Semantic Kernel...");
    
    // Register Semantic Kernel with Azure OpenAI
    builder.Services.AddSingleton<Kernel>(sp =>
    {
        var kernelBuilder = Kernel.CreateBuilder();
        
        // Add Azure OpenAI Chat Completion
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: azureOpenAISettings.DeploymentName,
            endpoint: azureOpenAISettings.Endpoint,
            apiKey: azureOpenAISettings.ApiKey);
        
        // Add logging
        kernelBuilder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
        });
        
        return kernelBuilder.Build();
    });
    
    // Register Embedding Generator using Azure OpenAI client
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
    {
        var client = new AzureOpenAIClient(
            new Uri(azureOpenAISettings.Endpoint),
            new Azure.AzureKeyCredential(azureOpenAISettings.ApiKey));
        
        var embeddingClient = client.GetEmbeddingClient(azureOpenAISettings.EmbeddingDeploymentName);
        return new OpenAIEmbeddingGeneratorAdapter(embeddingClient);
    });
    
    // Register MAF-based Recommendation Agent
    builder.Services.AddScoped<IRecommendationAgent>(sp => 
        new RecommendationAgent(
            sp.GetRequiredService<IProductCatalog>(),
            sp.GetRequiredService<ILogger<RecommendationAgent>>(),
            sp.GetRequiredService<Kernel>(),
            sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()));
    
    // Keep backward compatibility with old interface
    builder.Services.AddScoped<IRecommendationService>(sp => 
        new RecommendationServiceAdapter(sp.GetRequiredService<IRecommendationAgent>()));
    
    Console.WriteLine("? MAF initialized successfully with Azure OpenAI integration");
}
else
{
    Console.WriteLine("? Azure OpenAI NOT configured - Using fallback semantic search");
    Console.WriteLine("  Configure AzureOpenAI settings in appsettings.json to enable AI features");
    
    // Register agent without AI capabilities
    builder.Services.AddScoped<IRecommendationAgent>(sp =>
    {
        var kernel = Kernel.CreateBuilder().Build(); // Empty kernel
        return new RecommendationAgent(
            sp.GetRequiredService<IProductCatalog>(),
            sp.GetRequiredService<ILogger<RecommendationAgent>>(),
            kernel,
            embeddingGenerator: null);
    });
    
    // Keep backward compatibility
    builder.Services.AddScoped<IRecommendationService>(sp => 
        new RecommendationServiceAdapter(sp.GetRequiredService<IRecommendationAgent>()));
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
