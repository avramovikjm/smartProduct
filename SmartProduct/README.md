# SmartProduct - AI-Powered Product Recommendation API

## Overview

SmartProduct is a modern ASP.NET Core 8.0 Web API that provides intelligent product recommendations using **Microsoft AI Framework (MAF)** with **Semantic Kernel** and **Azure OpenAI**.

## Architecture

### MAF Integration

This project implements the **Microsoft AI Framework (MAF)** pattern with the following components:

1. **Semantic Kernel** - AI orchestration framework for managing AI services
2. **Azure OpenAI** - LLM for intelligent ranking and explanations
3. **Text Embeddings** - Semantic search using vector embeddings
4. **Agent Pattern** - `RecommendationAgent` implements AI-driven recommendations

### Key Components

#### 1. RecommendationAgent (`Services/RecommendationAgent.cs`)
- **MAF-based agent** using Semantic Kernel
- Implements `IRecommendationAgent` interface
- Features:
  - **Vector embeddings** for semantic product search
  - **LLM-based ranking** for intelligent product recommendations
  - **Fallback mode** when AI is not configured
  - Cosine similarity for finding relevant products

#### 2. Configuration (`Configuration/AzureOpenAISettings.cs`)
- Centralized Azure OpenAI configuration
- Settings loaded from `appsettings.json`
- Validates configuration completeness

#### 3. Embedding Generator Adapter (`Services/OpenAIEmbeddingGeneratorAdapter.cs`)
- Adapts OpenAI `EmbeddingClient` to Microsoft.Extensions.AI interface
- Enables use of Azure OpenAI embeddings with MAF

#### 4. Product Catalog (`Services/ProductCatalog.cs`)
- Loads products from JSON data file
- Provides product filtering capabilities

## Configuration

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-small"
  }
}
```

### Required Azure OpenAI Resources

1. **Chat Completion Model**: GPT-4o or GPT-4
   - Used for: Ranking products and generating explanations
   
2. **Embedding Model**: text-embedding-3-small or text-embedding-ada-002
   - Used for: Vector search and semantic similarity

## How It Works

### With AI Enabled (MAF Mode)

1. **Query received** ? User sends natural language query
2. **Generate embeddings** ? Query converted to vector using Azure OpenAI
3. **Vector search** ? Products compared using cosine similarity
4. **LLM ranking** ? GPT model ranks and explains recommendations
5. **Return results** ? Structured recommendations with explanations

### Fallback Mode (No AI)

1. **Query received** ? User sends query
2. **Hash-based embeddings** ? Simple term-based similarity
3. **Rule-based ranking** ? Sort by similarity and rating
4. **Return results** ? Basic recommendations

## API Usage

### Endpoint: `POST /api/recommend`

**Request:**
```json
{
  "query": "food for puppies",
  "count": 5
}
```

**Response:**
```json
[
  {
    "id": "P001",
    "name": "Puppy Starter Food",
    "category": "Pet Care",
    "price": 14.99,
    "rating": 4.8,
    "explanation": "Perfect for young puppies (0-6 months) with essential nutrients for growth and immunity. Highly rated at 4.8 stars.",
    "relevance": 0.95
  }
]
```

## Dependencies

### NuGet Packages

- **Microsoft.SemanticKernel** (1.68.0) - Core MAF orchestration
- **Microsoft.SemanticKernel.Connectors.OpenAI** (1.68.0) - Azure OpenAI integration
- **Azure.AI.OpenAI** (2.7.0-beta.2) - OpenAI client library
- **Azure.Identity** (1.13.1) - Azure authentication
- **Microsoft.Extensions.AI** (10.0.1) - AI abstractions
- **System.Text.Json** (10.0.0) - JSON serialization

## Running the Application

### 1. Configure Azure OpenAI

Edit `appsettings.Development.json`:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-small"
  }
}
```

### 2. Run the API

```bash
dotnet run --project SmartProduct/SmartProduct.csproj
```

### 3. Test with Swagger

Navigate to: `https://localhost:5001/swagger`

### 4. Test with curl

```bash
curl -X POST https://localhost:5001/api/recommend \
  -H "Content-Type: application/json" \
  -d '{"query":"food for puppies","count":5}'
```

## Operational Modes

### ? AI Mode (MAF with Azure OpenAI)
When Azure OpenAI is configured:
- Vector-based semantic search
- LLM-powered ranking and explanations
- High-quality, context-aware recommendations

### ? Fallback Mode
When Azure OpenAI is NOT configured:
- Hash-based text embeddings
- Rule-based ranking
- Basic but functional recommendations

## Features

? **Semantic Search** - Understands intent, not just keywords  
? **AI-Powered Ranking** - Intelligent product ordering  
? **Contextual Explanations** - Why each product is recommended  
? **Graceful Degradation** - Works without AI configuration  
? **Swagger/OpenAPI** - Interactive API documentation  
? **Structured Logging** - Comprehensive logging with ILogger  
? **Dependency Injection** - Clean architecture with DI  

## Project Structure

```
SmartProduct/
??? Configuration/
?   ??? AzureOpenAISettings.cs       # Azure OpenAI settings
??? Controllers/
?   ??? RecommendController.cs       # API endpoint
??? Data/
?   ??? products.json                # Product catalog
??? Models/
?   ??? Product.cs                   # Product entity
??? Services/
?   ??? IRecommendationAgent.cs      # Agent interface
?   ??? RecommendationAgent.cs       # MAF-based agent ?
?   ??? IRecommendationService.cs    # Legacy interface
?   ??? RecommendationService.cs     # Legacy implementation
?   ??? RecommendationServiceAdapter.cs # Adapter pattern
?   ??? OpenAIEmbeddingGeneratorAdapter.cs # Embedding adapter
?   ??? IProductCatalog.cs           # Catalog interface
?   ??? ProductCatalog.cs            # Catalog implementation
??? Program.cs                       # DI & Kernel setup ?
??? appsettings.json                 # Configuration
??? README.md                        # This file
```

## MAF Benefits

1. **Standardized AI Integration** - Consistent patterns across Microsoft AI services
2. **Semantic Kernel Orchestration** - Powerful AI workflow management
3. **Extensibility** - Easy to add new AI capabilities
4. **Testability** - Clear abstractions for unit testing
5. **Modern Architecture** - Following Microsoft's recommended practices

## Future Enhancements

- [ ] Add caching for embeddings
- [ ] Implement user preference learning
- [ ] Add A/B testing for recommendation strategies
- [ ] Multi-modal search (text + images)
- [ ] Real-time inventory integration
- [ ] Personalized recommendations based on user history

## License

MIT License

## Support

For issues or questions, please create an issue in the repository.
