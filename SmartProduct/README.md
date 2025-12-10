# SmartProduct - AI-Powered Product Recommendation API

## Overview

SmartProduct is a modern **ASP.NET Core 9.0** Web API that provides intelligent product recommendations using the **official Semantic Kernel Agents Framework** with **Azure OpenAI**.

## ?? .NET 9 + Semantic Kernel Agents Framework

This project now uses:
- ? **.NET 9** - Latest .NET framework
- ? **Semantic Kernel Agents Framework** - Official Microsoft agent patterns
- ? **ChatCompletionAgent** - Structured AI conversations
- ? **Azure OpenAI** - GPT-4o and embeddings integration

## Architecture

### Semantic Kernel Agents Framework

This project implements the **official Semantic Kernel Agents Framework** pattern with the following components:

1. **Semantic Kernel Core** - AI orchestration framework for managing AI services
2. **Semantic Kernel Agents** - Official agent patterns and abstractions
3. **ChatCompletionAgent** - Structured AI conversations with instructions
4. **Azure OpenAI** - LLM for intelligent ranking and explanations
5. **Text Embeddings** - Semantic search using vector embeddings
6. **Agent Pattern** - `ProductRecommendationAgent` implements official SK agent patterns

### Key Components

#### 1. ProductRecommendationAgent (`Agents/ProductRecommendationAgent.cs`) ??
- **Official Semantic Kernel Agents Framework implementation**
- Uses `ChatCompletionAgent` for structured AI conversations
- Implements agent pattern with instructions and kernel functions
- Features:
  - **Vector embeddings** for semantic product search
  - **LLM-based ranking** with agent instructions
  - **Kernel function registration** for tool calling
  - **Fallback mode** when AI is not configured
  - Cosine similarity for finding relevant products

#### 2. AgentBasedRecommendationService (`Services/AgentBasedRecommendationService.cs`)
- Adapter wrapping ProductRecommendationAgent
- Maintains backward compatibility with `IRecommendationAgent` and `IRecommendationService`
- Provides seamless integration with existing controllers

#### 3. Configuration (`Configuration/AzureOpenAISettings.cs`)
- Centralized Azure OpenAI configuration
- Settings loaded from `appsettings.json`
- Validates configuration completeness

#### 4. Embedding Generator Adapter (`Services/OpenAIEmbeddingGeneratorAdapter.cs`)
- Adapts OpenAI `EmbeddingClient` to Microsoft.Extensions.AI interface
- Enables use of Azure OpenAI embeddings with Semantic Kernel

#### 5. Product Catalog (`Services/ProductCatalog.cs`)
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

### With AI Enabled (Semantic Kernel Agents Mode)

1. **Query received** ? User sends natural language query
2. **Agent initialization** ? ProductRecommendationAgent with ChatCompletionAgent
3. **Generate embeddings** ? Query converted to vector using Azure OpenAI
4. **Vector search** ? Products compared using cosine similarity
5. **Agent invocation** ? ChatCompletionAgent ranks and explains using agent instructions
6. **LLM ranking** ? GPT model ranks and explains recommendations
7. **Return results** ? Structured recommendations with explanations

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

### NuGet Packages (.NET 9)

- **Microsoft.SemanticKernel** (1.68.0) - Core AI orchestration
- **Microsoft.SemanticKernel.Connectors.OpenAI** (1.68.0) - Azure OpenAI integration
- **Microsoft.SemanticKernel.Agents.Abstractions** (1.68.0) - Agent patterns (NEW!)
- **Microsoft.SemanticKernel.Agents.Core** (1.68.0) - Agent implementation (NEW!)
- **Azure.AI.OpenAI** (2.7.0-beta.2) - OpenAI client library
- **Azure.Identity** (1.13.1) - Azure authentication
- **Microsoft.Extensions.AI** (10.0.1) - AI abstractions
- **System.Text.Json** (10.0.0) - JSON serialization
- **Microsoft.AspNetCore.OpenApi** (9.0.0) - ASP.NET Core OpenAPI (.NET 9)

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

### ? AI Mode (Semantic Kernel Agents with Azure OpenAI)
When Azure OpenAI is configured:
- **ChatCompletionAgent** with structured instructions
- Vector-based semantic search
- LLM-powered ranking and explanations
- Agent-managed conversations
- High-quality, context-aware recommendations

### ?? Fallback Mode
When Azure OpenAI is NOT configured:
- Hash-based text embeddings
- Rule-based ranking
- Basic but functional recommendations
- Agent still initialized (without AI capabilities)

## Features

? **Official Semantic Kernel Agents Framework** - Uses Microsoft's agent patterns  
? **ChatCompletionAgent** - Structured AI conversations with instructions  
? **.NET 9** - Latest framework with performance improvements  
? **Semantic Search** - Understands intent, not just keywords  
? **AI-Powered Ranking** - Intelligent product ordering  
? **Contextual Explanations** - Why each product is recommended  
? **Kernel Functions** - Pluggable tool calling capabilities  
? **Graceful Degradation** - Works without AI configuration  
? **Swagger/OpenAPI** - Interactive API documentation  
? **Structured Logging** - Comprehensive logging with ILogger  
? **Dependency Injection** - Clean architecture with DI  
? **Backward Compatible** - Existing APIs unchanged

## Project Structure

```
SmartProduct/
??? Agents/                         # NEW: Semantic Kernel Agents ??
?   ??? ProductRecommendationAgent.cs # ChatCompletionAgent implementation
??? Configuration/
?   ??? AzureOpenAISettings.cs      # Azure OpenAI settings
??? Controllers/
?   ??? RecommendController.cs      # API endpoint
??? Data/
?   ??? products.json               # Product catalog
??? Models/
?   ??? Product.cs                  # Product entity
??? Services/
?   ??? IRecommendationAgent.cs     # Agent interface
?   ??? AgentBasedRecommendationService.cs # NEW: Agent adapter
?   ??? RecommendationAgent.cs      # Legacy implementation
?   ??? IRecommendationService.cs   # Legacy interface
?   ??? RecommendationService.cs    # Legacy implementation
?   ??? RecommendationServiceAdapter.cs # Adapter pattern
?   ??? OpenAIEmbeddingGeneratorAdapter.cs # Embedding adapter
?   ??? IProductCatalog.cs          # Catalog interface
?   ??? ProductCatalog.cs           # Catalog implementation
??? Program.cs                      # DI & Kernel setup with Agents
??? SmartProduct.csproj             # .NET 9 project file
??? appsettings.json                # Configuration
??? README.md                       # This file
??? MAF_IMPLEMENTATION.md           # Implementation details
```

## Semantic Kernel Agents Framework Benefits

1. **Official Microsoft Patterns** - Using the official SK Agents Framework
2. **ChatCompletionAgent** - Structured AI conversations with instructions
3. **Agent Abstractions** - Clear agent lifecycle and patterns
4. **Standardized AI Integration** - Consistent patterns across Microsoft AI services
5. **Semantic Kernel Orchestration** - Powerful AI workflow management
6. **Kernel Functions** - Tool calling and function invocation
7. **Multi-Agent Ready** - Foundation for agent collaboration (future)
8. **Extensibility** - Easy to add new AI capabilities
9. **Testability** - Clear abstractions for unit testing
10. **Modern Architecture** - Following Microsoft's latest recommended practices
11. **.NET 9 Performance** - Latest framework optimizations

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
