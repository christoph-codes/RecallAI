# Vector Memory Search Implementation

## Overview
This implementation adds semantic search functionality to the RecallAI memory system using OpenAI embeddings and PostgreSQL's pgvector extension for vector similarity search.

## Features Implemented

### 🔍 Search Endpoint
- **Endpoint**: `GET /api/memory/search`
- **Authentication**: Required (JWT Bearer token)
- **Parameters**:
  - `query` (required): Search text (max 500 characters)
  - `limit` (optional): Number of results (1-50, default: 10)
  - `threshold` (optional): Similarity threshold (0.0-1.0, default: 0.7)

### 🧠 OpenAI Integration
- **Service**: `EmbeddingService` with `IEmbeddingService` interface
- **Model**: `text-embedding-3-small` (configurable)
- **Features**:
  - Single and batch embedding generation
  - In-memory LRU cache for repeated queries
  - Retry logic and error handling
  - Configurable timeouts

### 🗄️ Vector Database Operations
- **Repository**: Extended `IMemoryRepository` with search methods
- **Database**: PostgreSQL with pgvector extension
- **Search**: Cosine similarity using `<=>` operator
- **Security**: User isolation enforced at database level

## API Usage Examples

### Basic Search
```http
GET /api/memory/search?query=machine learning&limit=10&threshold=0.7
Authorization: Bearer your-jwt-token
```

### High Precision Search
```http
GET /api/memory/search?query=artificial intelligence&limit=5&threshold=0.9
Authorization: Bearer your-jwt-token
```

### Broad Search
```http
GET /api/memory/search?query=programming&limit=20&threshold=0.5
Authorization: Bearer your-jwt-token
```

## Response Format

```json
{
  "results": [
    {
      "id": "guid",
      "title": "Memory Title",
      "content": "Memory content...",
      "contentType": "text",
      "similarityScore": 0.8542,
      "createdAt": "2023-12-01T10:00:00Z",
      "metadata": {}
    }
  ],
  "query": "machine learning",
  "resultCount": 5,
  "executionTimeMs": 245
}
```

## Configuration

### Environment Variables
```bash
OPENAI_API_KEY=your-openai-api-key
```

### appsettings.json
```json
{
  "OpenAI": {
    "ApiKey": "",
    "Model": "text-embedding-3-small",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  },
  "EmbeddingCache": {
    "MaxSize": 1000,
    "ExpiryHours": 1
  }
}
```

## Architecture Components

### 1. Search Models
- `SearchResultItem`: Individual search result with similarity score
- `SearchResponse`: Complete search response with metadata

### 2. Embedding Service
- `IEmbeddingService`: Interface for embedding generation
- `EmbeddingService`: OpenAI implementation with caching

### 3. Repository Extensions
- `SearchSimilarAsync()`: Vector similarity search
- `HasEmbeddingAsync()`: Check if memory has embedding

### 4. Controller Integration
- Search endpoint in `MemoryController`
- Input validation and error handling
- Performance monitoring and logging

## Database Schema

The implementation uses existing tables:
- `Memories`: Core memory data
- `MemoryEmbeddings`: Vector embeddings with pgvector

### Vector Search Query
```sql
SELECT m.*, (1 - (me."Embedding" <=> @queryVector)) as similarity
FROM "Memories" m
INNER JOIN "MemoryEmbeddings" me ON m."Id" = me."MemoryId"
WHERE m."UserId" = @userId 
  AND (1 - (me."Embedding" <=> @queryVector)) >= @threshold
ORDER BY me."Embedding" <=> @queryVector
LIMIT @limit
```

## Performance Considerations

### Caching
- In-memory LRU cache for embeddings
- Configurable cache size and expiry
- Reduces OpenAI API calls for repeated queries

### Database Optimization
- pgvector index on embedding column recommended
- User-based filtering for security and performance
- Parameterized queries for safety

### API Performance
- 30-second timeout for embedding generation
- Execution time tracking in responses
- Async operations throughout

## Security Features

### User Isolation
- All searches filtered by authenticated user ID
- No cross-user data leakage possible
- JWT authentication required

### Input Validation
- Query length limits (500 characters)
- Parameter bounds checking
- SQL injection prevention via parameterized queries

### Error Handling
- Graceful OpenAI API failure handling
- Detailed logging without exposing sensitive data
- Appropriate HTTP status codes

## Error Responses

### 400 Bad Request
```json
{
  "message": "Query parameter is required"
}
```

### 401 Unauthorized
```json
{
  "message": "User authentication required"
}
```

### 503 Service Unavailable
```json
{
  "message": "Search service temporarily unavailable"
}
```

## Deployment Requirements

### Prerequisites
1. PostgreSQL with pgvector extension
2. OpenAI API key with embedding access
3. Existing memory embeddings in database

### Environment Setup
1. Set `OPENAI_API_KEY` environment variable
2. Configure database connection string
3. Ensure pgvector extension is enabled

### Performance Tuning
1. Create index on embedding column:
   ```sql
   CREATE INDEX CONCURRENTLY idx_memory_embeddings_vector 
   ON "MemoryEmbeddings" USING ivfflat ("Embedding" vector_cosine_ops);
   ```

2. Monitor cache hit rates and adjust cache size
3. Set appropriate similarity thresholds for use case

## Monitoring and Logging

### Metrics Tracked
- Search execution time
- Cache hit/miss rates
- OpenAI API response times
- Search result counts

### Log Levels
- Info: Successful searches with metrics
- Warning: API timeouts or retries
- Error: Failed searches or API errors

## Future Enhancements

### Potential Improvements
1. Hybrid search (vector + keyword)
2. Search result ranking algorithms
3. Distributed caching (Redis)
4. Search analytics and insights
5. Batch search operations
6. Search filters by content type or metadata

### Scalability Considerations
1. Embedding service clustering
2. Database read replicas for search
3. CDN caching for frequent queries
4. Async embedding generation pipeline