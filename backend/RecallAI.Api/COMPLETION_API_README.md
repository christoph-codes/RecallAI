# Completion API Documentation

## Overview

The Completion API provides a generic LLM completion endpoint that accepts user input and streams responses. It integrates with the existing memory system to provide context-aware responses through a sophisticated pipeline.

## Pipeline Process

The completion pipeline follows these steps:

1. **Memory Evaluation**: Determines if the query would benefit from searching through stored memories
2. **HyDE Generation**: Creates a Hypothetical Document Embedding to improve search relevance
3. **Vector Search**: Searches through user's memories using vector similarity
4. **Context Building**: Combines relevant memories with the user's query
5. **Streaming Response**: Generates and streams the LLM response in real-time

## Endpoints

### POST `/api/completion`

Streams the completion response as plain text.

**Headers:**
- `Authorization: Bearer <JWT_TOKEN>`
- `Content-Type: application/json`

**Request Body:**
```json
{
  "message": "Your question or prompt here",
  "configuration": {
    "model": "gpt-4o",                    // Optional: Override default model
    "temperature": 0.7,                   // Optional: Control randomness (0.0-2.0)
    "maxTokens": 1000,                   // Optional: Maximum response length
    "enableMemorySearch": true,          // Optional: Enable memory search (default: true)
    "maxMemoryResults": 5,               // Optional: Max memories to include (default: 5)
    "memoryThreshold": 0.7               // Optional: Similarity threshold (default: 0.7)
  }
}
```

**Response:**
- Content-Type: `text/plain; charset=utf-8`
- Streams response chunks as they're generated

### POST `/api/completion/sse`

Streams the completion response using Server-Sent Events format.

**Headers:**
- `Authorization: Bearer <JWT_TOKEN>`
- `Content-Type: application/json`

**Request Body:** Same as above

**Response:**
- Content-Type: `text/event-stream`
- Events:
  - `connected`: Connection established
  - `data`: Response chunks
  - `done`: Completion finished
  - `error`: Error occurred

## Configuration Options

### Model Selection
- `gpt-4o`: Most capable model (default for final results)
- `gpt-4o-mini`: Faster, cost-effective option
- Custom models can be specified

### Memory Search Parameters
- `enableMemorySearch`: Whether to search through stored memories
- `maxMemoryResults`: Number of relevant memories to include (1-20)
- `memoryThreshold`: Similarity threshold for memory inclusion (0.0-1.0)

### Generation Parameters
- `temperature`: Controls randomness (0.0 = deterministic, 2.0 = very random)
- `maxTokens`: Maximum length of the response

## Error Handling

The API handles errors gracefully:
- Invalid requests return appropriate HTTP status codes
- Pipeline failures are logged and fallback responses provided
- Streaming errors are handled without breaking the connection
- Authentication failures return 401 with descriptive messages

## CORS Support

The API includes proper CORS headers for frontend integration:
- `Access-Control-Allow-Origin: *`
- `Access-Control-Allow-Headers: Cache-Control`

## Usage Examples

### Basic Chat Interface
```javascript
const response = await fetch('/api/completion', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    message: 'What did I learn about machine learning yesterday?'
  })
});

const reader = response.body.getReader();
const decoder = new TextDecoder();

while (true) {
  const { done, value } = await reader.read();
  if (done) break;
  
  const chunk = decoder.decode(value);
  console.log(chunk); // Display streaming response
}
```

### Server-Sent Events
```javascript
const eventSource = new EventSource('/api/completion/sse', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

eventSource.addEventListener('data', (event) => {
  console.log('Response chunk:', event.data);
});

eventSource.addEventListener('done', (event) => {
  console.log('Completion finished');
  eventSource.close();
});

eventSource.addEventListener('error', (event) => {
  console.error('Error:', event.data);
});
```

### Tool Integration
```javascript
async function askAI(question, options = {}) {
  const response = await fetch('/api/completion', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getAuthToken()}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      message: question,
      configuration: {
        enableMemorySearch: options.useMemory ?? true,
        temperature: options.creativity ?? 0.7,
        maxTokens: options.maxLength ?? 1000
      }
    })
  });

  return response.body;
}
```

## Performance Considerations

- Streaming reduces perceived latency
- Memory search adds ~200-500ms to initial response time
- Vector search is optimized with pgvector
- Connection pooling is handled automatically
- Responses are cached at the HTTP client level

## Security

- JWT authentication required for all requests
- User isolation through JWT user ID extraction
- Memory access is scoped to authenticated user
- Rate limiting should be implemented at the reverse proxy level
- API keys are managed through environment variables

## Monitoring and Logging

The API logs:
- Request initiation and completion
- Pipeline step execution times
- Memory search results and relevance scores
- Error conditions and recovery attempts
- Model usage and token consumption

## Integration Examples

### Chat Application
Perfect for building conversational interfaces that leverage user's stored knowledge.

### Documentation Assistant
Can answer questions about user's documents and notes with relevant context.

### Personal AI Assistant
Provides personalized responses based on user's history and preferences.

### Knowledge Management Tool
Helps users find and synthesize information from their knowledge base.