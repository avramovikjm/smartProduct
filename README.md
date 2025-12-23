# SmartProduct Recommendation Agent

AI-powered product recommendation system using Azure OpenAI for semantic search and intelligent ranking.

## ?? Overview

This application provides intelligent product recommendations using:
- **Azure OpenAI Embeddings** (text-embedding-3-small) for semantic search
- **Azure OpenAI Chat** (GPT-4o) for intelligent ranking and explanations
- **Agent-based architecture** with clean separation of concerns
- **Fallback mode** for operation without AI configuration

## ?? Important Note: Architecture Clarification

This implementation uses:
- ? **Azure.AI.OpenAI SDK** - Direct Azure OpenAI API integration
- ? **Custom agent implementation** - Agent-like patterns and design
- ? **NOT Microsoft Agent Framework** - Not using Azure.AI.Projects package

For the official Microsoft Agent Framework, see: https://learn.microsoft.com/en-us/agent-framework/

## ??? Architecture

```
User Request
    ?
RecommendController (API Endpoint)
    ?
ProductRecommendationAgent (Custom Implementation)
    ??? Generate Embeddings (text-embedding-3-small)
    ??? Vector Search (Cosine Similarity)
    ??? AI Ranking (GPT-4o)
    ??? Generate Explanations
    ?
Azure OpenAI Service
```

## ?? Getting Started

### Prerequisites

- .NET 9 SDK
- Azure OpenAI resource with deployed models:
  - `gpt-4o` (chat completion)
  - `text-embedding-3-small` (embeddings)

### Configuration

Update `appsettings.json` and `appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-small"
  }
}
```

### Run the Application

```bash
cd SmartProduct
dotnet restore
dotnet build
dotnet run
```

Expected output:
```
? Azure OpenAI configured - Initializing Product Recommendation Agent (.NET 9)...
? Agent initialized successfully
   - Agent: ProductRecommendationAgent
   - Chat Model: gpt-4o
   - Embeddings: text-embedding-3-small
```

## ?? API Usage

### Endpoint

```
POST /api/recommend
```

### Request

```json
{
  "query": "food for puppies",
  "count": 5
}
```

### Response

```json
[
  {
    "id": "prod-001",
    "name": "Premium Puppy Food",
    "category": "Dog Food",
    "price": 29.99,
    "rating": 4.8,
    "explanation": "This Dog Food product matches your food search. Excellent 4.8 star rating.",
    "relevanceScore": 0.95
  }
]
```

### Test with curl

```bash
curl -X POST https://localhost:7171/api/recommend \
  -H "Content-Type: application/json" \
  -d '{"query":"food for puppies","count":5}'
```

### Test with Swagger UI

Navigate to: `https://localhost:7171/swagger`

## ?? Features

### ? Semantic Search
- Vector embeddings using Azure OpenAI text-embedding-3-small
- Cosine similarity matching for finding relevant products
- Caches product embeddings for performance

### ? AI-Powered Ranking
- GPT-4o analyzes candidates and user query
- Generates personalized explanations
- Returns relevance scores (0.0 - 1.0)

### ? Fallback Mode
- Hash-based embeddings when AI not configured
- Keyword matching for product search
- Top-rated products as fallback results

### ? Agent Pattern
- Named agent with description and instructions
- Structured logging with agent context
- Clean architecture with dependency injection

## ?? Project Structure

```
SmartProduct/
??? Agents/
?   ??? ProductRecommendationAgent.cs    # Main agent implementation
??? Controllers/
?   ??? RecommendController.cs           # API endpoint
??? Services/
?   ??? IRecommendationAgent.cs         # Agent interface
?   ??? AgentBasedRecommendationService.cs  # Service adapter
?   ??? IProductCatalog.cs              # Catalog interface
?   ??? ProductCatalog.cs               # In-memory catalog
??? Models/
?   ??? Product.cs                       # Product model
?   ??? RecommendedProduct.cs           # Recommendation result
?   ??? RecommendRequest.cs             # API request model
??? Configuration/
?   ??? AzureOpenAISettings.cs          # Azure OpenAI configuration
??? Program.cs                           # Application startup
??? appsettings.json                     # Configuration file
```

## ??? Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Framework | .NET | 9.0 |
| Language | C# | 13.0 |
| AI SDK | Azure.AI.OpenAI | 2.7.0-beta.2 |
| Embeddings | text-embedding-3-small | - |
| Chat Model | GPT-4o | - |
| API Framework | ASP.NET Core | 9.0 |
| Documentation | Swagger/OpenAPI | 9.0 |

## ?? How It Works

### 1. Query Processing
1. User submits query via API
2. Agent generates embedding for query using Azure OpenAI
3. Vector search finds similar products using cosine similarity

### 2. Candidate Selection
1. Top N similar products selected as candidates (N = count × 2)
2. Candidates include product details, ratings, and descriptions

### 3. AI Ranking
1. Candidates sent to GPT-4o with user query
2. AI analyzes relevance and generates explanations
3. Returns ranked list with relevance scores

### 4. Response
1. Top recommendations formatted with explanations
2. Includes product details and relevance scores
3. Returns as JSON array

## ?? Agent Metadata

```csharp
Name: "ProductRecommendationAgent"
Description: "An intelligent agent that recommends products based on user queries using semantic search and AI-powered ranking"

Instructions:
- Understand user intent and preferences
- Match products based on semantic similarity
- Consider product ratings and categories
- Provide clear explanations for recommendations
- Rank products by relevance to the query
```

## ?? Troubleshooting

### Error: "No such host is known"
**Cause:** Incorrect endpoint in configuration  
**Solution:** Verify `AzureOpenAI:Endpoint` in both `appsettings.json` and `appsettings.Development.json`

### Error: "Unauthorized" or "401"
**Cause:** Invalid API key  
**Solution:** Check `AzureOpenAI:ApiKey` in configuration

### Warning: "AI Enabled: False"
**Cause:** Missing or incomplete Azure OpenAI configuration  
**Solution:** Ensure all AzureOpenAI settings are configured

### Fallback mode active
**Info:** Application working without AI  
**Solution:** Configure Azure OpenAI credentials to enable AI features

## ?? Security Best Practices

1. **Never commit API keys** to source control
2. Use **User Secrets** for development:
   ```bash
   dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"
   ```
3. Use **Azure Key Vault** for production
4. Use **Managed Identity** when deployed to Azure

## ?? Performance Considerations

- Product embeddings are cached in memory
- First request generates all product embeddings
- Subsequent requests use cached embeddings
- Consider implementing distributed cache for production

## ?? Testing

### Manual Testing
1. Run application: `dotnet run`
2. Open Swagger UI: `https://localhost:7171/swagger`
3. Try endpoint with sample queries

### Sample Queries
- "food for puppies"
- "toys for cats"
- "grooming supplies"
- "outdoor pet accessories"

## ?? License

This project is for demonstration purposes.

## ?? Contributing

This is a sample implementation. Customize as needed for your use case.

## ?? Additional Resources

- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)

---

**Built with .NET 9 and Azure OpenAI** ??
