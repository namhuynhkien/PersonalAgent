# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

PersonalAgent is a C# console application built on .NET 9 that implements a conversational AI assistant with persistent memory capabilities. The application uses Microsoft Semantic Kernel to interact with local AI models via Ollama and maintains a PostgreSQL database for storing conversation memories.

## Key Architecture Components

### Core Technologies
- **Framework**: .NET 9 Console Application
- **AI Integration**: Microsoft Semantic Kernel with Ollama connector (Qwen3:8b model)
- **Database**: PostgreSQL with Dapper for data access
- **Plugin System**: Semantic Kernel plugins for extensible functionality

### Application Structure
- `Program.cs`: Main entry point with chat loop and Semantic Kernel initialization
- `Plugin/`: Contains Semantic Kernel function plugins
  - `MemoryPlugin.cs`: Handles CRUD operations for notes/memories
  - `MathPlugin.cs`: Basic arithmetic operations
- `Model/`: Data models (currently just `MemoryNote`)

### Database Schema
The application uses a PostgreSQL table `memory_notes` with:
- `id` (TEXT PRIMARY KEY): Auto-generated note identifier
- `content` (TEXT NOT NULL): The note content
- `created_at` (TIMESTAMP WITH TIME ZONE): Creation timestamp
- `tags` (TEXT[]): Array of tags (currently unused but prepared)

## Development Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Clean build artifacts
dotnet clean
```

### Development Workflow
```bash
# Restore packages
dotnet restore

# Run in watch mode for development
dotnet watch run
```

### Database Setup
The application requires PostgreSQL running locally with these credentials:
- Host: localhost:5432
- Database: memory_db
- Username: memory_user
- Password: memory_password

```bash
# Start PostgreSQL (if using Docker)
docker run --name memory-postgres -e POSTGRES_DB=memory_db -e POSTGRES_USER=memory_user -e POSTGRES_PASSWORD=memory_password -p 5432:5432 -d postgres:latest

# The application automatically creates required tables on startup
```

### Ollama Model Setup
```bash
# Pull the required Qwen3 model
ollama pull qwen3:8b

# Start Ollama server (should run on localhost:11434)
ollama serve
```

## Plugin Development Patterns

### Creating New Plugins
1. Create a class in the `Plugin/` directory
2. Add `[KernelFunction]` attribute to public methods
3. Use `[Description]` attributes for function and parameter documentation
4. Register the plugin in `Program.cs` using `_kernel.ImportPluginFromType<YourPlugin>()`

### Plugin Method Signature Pattern
```csharp path=null start=null
[KernelFunction]
[Description("Description of what the function does")]
public async Task<string> FunctionName(
    [Description("Parameter description")] string parameter)
{
    // Implementation
    return "Result string";
}
```

## Configuration Notes

### Model Configuration
- Currently configured for Qwen3:8b model
- Alternative model `qwen2.5-coder:7b` is commented out in `Program.cs` line 59
- Ollama endpoint: http://localhost:11434

### Database Connection
- Connection string is hardcoded in both `Program.cs` and `MemoryPlugin.cs`
- Consider extracting to configuration file for production use

### Function Choice Behavior
- Uses `FunctionChoiceBehavior.Required()` to force function calling
- System prompts guide the AI on when to use specific functions

## Extension Points

### Adding New Memory Types
- Extend `MemoryNote` model with additional properties
- Update database schema accordingly
- Modify `MemoryPlugin` methods to handle new fields

### Adding New AI Connectors
- Replace or supplement Ollama connector in `Program.cs`
- Semantic Kernel supports OpenAI, Azure OpenAI, and other providers

### Enhancing Plugin Capabilities
- Add new plugins for specific domains (file operations, web APIs, etc.)
- Implement async operations where needed
- Add proper error handling and logging