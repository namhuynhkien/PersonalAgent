# PersonalAgent - AI Personal Assistant Application

## Project Overview

PersonalAgent is a C# console application built on .NET 9 that implements a conversational AI assistant with persistent memory capabilities. The application uses Microsoft Semantic Kernel to interact with local AI models via Ollama and maintains a PostgreSQL database for storing conversation memories.

---

## Technology Stack

- **Framework**: .NET 9 Console Application
- **AI Integration**: Microsoft Semantic Kernel 1.65.0 with Ollama connector
- **AI Model**: Qwen3:8b (via Ollama)
- **Database**: PostgreSQL 
- **Data Access**: Dapper ORM
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Configuration**: Microsoft.Extensions.Configuration (JSON-based)
- **Logging**: Microsoft.Extensions.Logging with Console output

---

## Project Structure

```
PersonalAgent/
├── Configuration/
│   └── AppSettings.cs              # Configuration models
├── Model/
│   └── MemoryNote.cs               # Data model for notes
├── Plugin/
│   ├── MemoryPlugin.cs             # Memory management functions
│   ├── MathPlugin.cs               # Basic arithmetic operations
│   └── TimePlugin.cs               # Date/time utilities
├── Services/
│   ├── ChatService.cs              # Main chat orchestration
│   ├── DatabaseService.cs          # Database initialization
│   └── MemoryService.cs            # Memory CRUD operations
├── Program.cs                       # Application entry point
├── appsettings.json                # Configuration file
└── PersonalAgent.csproj            # Project file
```

---

## Key Features

### 1. **Conversational AI with Function Calling**
- Powered by Ollama (Qwen3:8b model)
- Semantic Kernel integration for function calling
- Streaming responses for real-time interaction

### 2. **Persistent Memory System**
- PostgreSQL database storage
- CRUD operations for notes/memories
- Search functionality with pattern matching
- Automatic ID generation with timestamps

### 3. **Plugin Architecture**
- **MemoryPlugin**: Store, search, list, delete, and count notes
- **MathPlugin**: Basic arithmetic (add, subtract)
- **TimePlugin**: Comprehensive date/time utilities (current time, timezone conversions, days until date, etc.)

### 4. **Dependency Injection & Clean Architecture**
- Service-based architecture with interfaces
- Scoped service lifetimes
- Configuration management via Options pattern

---

## System Prompt

The AI assistant is configured with the following personality and behavior:

**Role**: Friendly personal AI assistant supporting:
- Personal assistant tasks (reminders, notes, information management)
- Training and fitness guidance
- Learning and educational support
- Nutrition advice and meal planning
- Health and wellness recommendations
- Financial planning and budgeting tips

**Personality**:
- Casual, friendly, and conversational
- Concise and to-the-point responses
- Warm and supportive tone

**Memory Management**:
- Proactively identifies and saves important information
- Automatically recalls relevant past information
- Stores: training schedules, learning goals, dietary preferences, health data, financial goals, etc.

---

## Code Files

### 1. Program.cs
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalAgent.Configuration;
using PersonalAgent.Services;

namespace PersonalAgent;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            
            // Initialize services
            await InitializeApplicationAsync(host.Services);
            
            // Run the chat application
            await RunChatApplicationAsync(host.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application Error: {ex.Message}");
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<AppSettings>(context.Configuration);
                
                // Services
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IMemoryService, MemoryService>();
                services.AddScoped<IChatService, ChatService>();
                
                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
    
    private static async Task InitializeApplicationAsync(IServiceProvider services)
    {
        var databaseService = services.GetRequiredService<IDatabaseService>();
        var chatService = services.GetRequiredService<IChatService>();
        
        await databaseService.InitializeDatabaseAsync();
        await chatService.InitializeAsync();
    }
    
    private static async Task RunChatApplicationAsync(IServiceProvider services)
    {
        var chatService = services.GetRequiredService<IChatService>();
        
        while (true)
        {
            Console.Write("\nYou: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.ToLower() == "quit")
                break;

            await chatService.ProcessUserInputAsync(userInput);
        }
    }
}
```

### 2. appsettings.json
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=memory_db;Username=memory_user;Password=memory_password;"
  },
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ModelId": "qwen3:8b",
    "AlternativeModelId": "qwen2.5-coder:7b"
  },
  "Application": {
    "TableName": "memory_notes",
    "MaxSearchResults": 10,
    "MaxListResults": 50
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.SemanticKernel": "Information"
    }
  }
}
```

### 3. Configuration/AppSettings.cs
```csharp
namespace PersonalAgent.Configuration;

public class AppSettings
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public OllamaSettings Ollama { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
}

public class ConnectionStrings
{
    public string PostgreSQL { get; set; } = string.Empty;
}

public class OllamaSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string AlternativeModelId { get; set; } = string.Empty;
}

public class ApplicationSettings
{
    public string TableName { get; set; } = string.Empty;
    public int MaxSearchResults { get; set; } = 10;
    public int MaxListResults { get; set; } = 50;
}
```

### 4. Model/MemoryNote.cs
```csharp
namespace PersonalAgent.Model;

public class MemoryNote
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string[]? Tags { get; set; }
}
```

### 5. Services/ChatService.cs
```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PersonalAgent.Configuration;
using PersonalAgent.Plugin;

namespace PersonalAgent.Services;

public interface IChatService
{
    Task ProcessUserInputAsync(string userInput);
    Task InitializeAsync();
}

public class ChatService : IChatService
{
    private readonly AppSettings _settings;
    private readonly ILogger<ChatService> _logger;
    private readonly IMemoryService _memoryService;
    private Kernel? _kernel;
    private IChatCompletionService? _chatCompletionService;
    private readonly ChatHistory _chatHistory = [];

    public ChatService(
        IOptions<AppSettings> settings, 
        ILogger<ChatService> logger,
        IMemoryService memoryService)
    {
        _settings = settings.Value;
        _logger = logger;
        _memoryService = memoryService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Semantic Kernel with Ollama...");
            
            var builder = Kernel.CreateBuilder();
            
            builder.AddOllamaChatCompletion(
                modelId: _settings.Ollama.ModelId,
                endpoint: new Uri(_settings.Ollama.Endpoint)
            );

            _kernel = builder.Build();
            
            // Register plugins with dependency injection
            _kernel.ImportPluginFromObject(new MemoryPlugin(_memoryService));
            _kernel.Plugins.AddFromType<MathPlugin>();
            _kernel.Plugins.AddFromType<TimePlugin>();
            
            
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            _logger.LogInformation("Semantic Kernel initialized successfully");
            Console.WriteLine("Assistant is ready! Type your messages below.");
            Console.WriteLine("Commands: 'quit' to exit, 'show notes' to see all stored notes");
            Console.WriteLine(new string('-', 50));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize chat service");
            throw;
        }
    }

    public async Task ProcessUserInputAsync(string userInput)
    {
        try
        {
            _logger.LogInformation("Processing user input: {UserInput}", userInput);
            
            // Build the system prompt
            var prompt = $"""
                          You are a friendly and helpful personal AI assistant. Your goal is to support the user with:
                          - Personal assistant tasks (reminders, notes, information management)
                          - Training and fitness guidance
                          - Learning and educational support
                          - Nutrition advice and meal planning
                          - Health and wellness recommendations
                          - Financial planning and budgeting tips

                          PERSONALITY:
                          - Be casual, friendly, and conversational
                          - Keep responses concise and to-the-point
                          - Use a warm, supportive tone
                          - Avoid being overly formal or robotic

                          MEMORY MANAGEMENT:
                          - Proactively identify and save important information the user shares (goals, preferences, schedules, health data, financial info, etc.)
                          - Automatically recall relevant past information when it's useful for the conversation
                          - When storing memories, be specific and organized
                          - Store things like: training schedules, learning goals, dietary preferences, health conditions, financial goals, important dates, etc.

                          FUNCTION USAGE:
                          Based on the user's message, intelligently decide when to:
                          1. **StoreNote()** - When the user shares important information that should be remembered:
                             - Training schedules, workout routines, fitness goals
                             - Learning plans, courses, study schedules
                             - Dietary preferences, meal plans, allergies
                             - Health information, medications, appointments
                             - Financial goals, budgets, savings targets
                             - Any personal preferences or important facts
                          
                          2. **SearchNotes()** - When the user asks about past information:
                             - "What's my workout schedule?"
                             - "What are my financial goals?"
                             - "What did I say about my diet?"
                             - Any query that requires recalling stored information
                          
                          3. **ListNotes()** - When the user wants to see all stored information:
                             - "Show me everything you know about me"
                             - "List all my notes"
                             - "What do you have stored?"
                          
                          4. **DeleteNote()** - When the user wants to remove information:
                             - "Delete my old workout plan"
                             - "Remove that note about..."
                          
                          5. **GetCurrentDateTime()** - When asked about current date/time or scheduling
                          
                          6. **Regular conversation** - For everything else, just chat naturally

                          Remember: Be proactive with memory - if someone mentions "I go to the gym on Mondays and Wednesdays", save that without being asked!

                          Current user input: {userInput}
                          """;
            _chatHistory.AddSystemMessage(prompt);

            // Add user message
            _chatHistory.AddUserMessage(userInput);
            
            // Process with streaming
            var streamingResponse = _chatCompletionService!.GetStreamingChatMessageContentsAsync(
                _chatHistory,
                executionSettings: new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
                },
                kernel: _kernel
            );
            
            Console.Write("Assistant: ");
            var fullResponseContent = "";
            
            await foreach (var contentPart in streamingResponse)
            {
                if (!string.IsNullOrEmpty(contentPart.Content))
                {
                    Console.Write(contentPart.Content);
                    fullResponseContent += contentPart.Content;
                }
            }
            
            Console.WriteLine(); // Add newline after streaming completes
            
            _chatHistory.AddAssistantMessage(fullResponseContent);
            
            _logger.LogInformation("Successfully processed user input");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user input: {UserInput}", userInput);
            Console.WriteLine($"Assistant: Sorry, I encountered an error: {ex.Message}");
        }
    }
}
```

### 6. Services/DatabaseService.cs
```csharp
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using PersonalAgent.Configuration;

namespace PersonalAgent.Services;

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
}

public class DatabaseService : IDatabaseService
{
    private readonly AppSettings _settings;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IOptions<AppSettings> settings, ILogger<DatabaseService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");
            
            await using var connection = new NpgsqlConnection(_settings.ConnectionStrings.PostgreSQL);
            await connection.OpenAsync();
            
            var createTableSql = $"""
                CREATE TABLE IF NOT EXISTS {_settings.Application.TableName} (
                    id TEXT PRIMARY KEY,
                    content TEXT NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                    tags TEXT[]
                )
                """;
                
            await connection.ExecuteAsync(createTableSql);
            
            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }
}
```

### 7. Services/MemoryService.cs
```csharp
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using PersonalAgent.Configuration;
using PersonalAgent.Model;

namespace PersonalAgent.Services;

public interface IMemoryService
{
    Task<string> StoreNoteAsync(string content);
    Task<string> SearchNotesAsync(string query);
    Task<string> ListNotesAsync();
    Task<string> DeleteNoteAsync(string noteId);
    Task<string> GetNoteCountAsync();
}

public class MemoryService : IMemoryService
{
    private readonly AppSettings _settings;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(IOptions<AppSettings> settings, ILogger<MemoryService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> StoreNoteAsync(string content)
    {
        try
        {
            var id = $"note_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..8]}";
            _logger.LogInformation("Storing note with ID: {NoteId}", id);

            await using var connection = new NpgsqlConnection(_settings.ConnectionStrings.PostgreSQL);
            await connection.OpenAsync();
            
            var sql = $"INSERT INTO {_settings.Application.TableName} (id, content) VALUES (@Id, @Content)";
            await connection.ExecuteAsync(sql, new { Id = id, Content = content });
            
            _logger.LogInformation("Successfully stored note with ID: {NoteId}", id);
            return $"Successfully stored note with ID: {id}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing note. Content length: {ContentLength}", content.Length);
            return $"Error storing note: {ex.Message}";
        }
    }

    public async Task<string> SearchNotesAsync(string query)
    {
        try
        {
            _logger.LogInformation("Searching notes with query: {Query}", query);
            
            await using var connection = new NpgsqlConnection(_settings.ConnectionStrings.PostgreSQL);
            await connection.OpenAsync();
            
            var sql = $@"
                SELECT id, content, created_at AS CreatedAt 
                FROM {_settings.Application.TableName} 
                WHERE content ILIKE @Query 
                ORDER BY created_at DESC 
                LIMIT @Limit";
                
            var notes = await connection.QueryAsync<MemoryNote>(sql, new { 
                Query = $"%{query}%", 
                Limit = _settings.Application.MaxSearchResults 
            });
            
            if (!notes.Any())
            {
                _logger.LogInformation("No matching notes found for query: {Query}", query);
                return "No matching notes found.";
            }
            
            var result = notes.Select(note => 
                $"ID: {note.Id} | {note.Content} (Created: {note.CreatedAt:yyyy-MM-dd HH:mm})");
                
            _logger.LogInformation("Found {Count} notes matching query: {Query}", notes.Count(), query);
            return string.Join("\n", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching notes with query: {Query}", query);
            return $"Error searching notes: {ex.Message}";
        }
    }

    public async Task<string> ListNotesAsync()
    {
        try
        {
            _logger.LogInformation("Listing all notes");
            
            await using var connection = new NpgsqlConnection(_settings.ConnectionStrings.PostgreSQL);
            await connection.OpenAsync();
            
            var sql = $"SELECT id, content, created_at AS CreatedAt FROM {_settings.Application.TableName} ORDER BY created_at DESC LIMIT @Limit";
            var notes = await connection.QueryAsync<MemoryNote>(sql, new { Limit = _settings.Application.MaxListResults });
            
            if (!notes.Any())
            {
                _logger.LogInformation("No notes found in database");
                return "No notes stored yet.";
            }
            
            var result = notes.Select(note => 
                $"ID: {note.Id} | {note.Content} (Created: {note.CreatedAt:yyyy-MM-dd HH:mm})");
                
            _logger.LogInformation("Listed {Count} notes", notes.Count());
            return string.Join("\n", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing notes");
            return $"Error listing notes: {ex.Message}";
        }
    }

    public async Task<string> DeleteNoteAsync(string noteId)
    {
        try
        {
            _logger.LogInformation("Deleting note with ID: {NoteId}", noteId);
            
            await using var connection = new NpgsqlConnection(_settings.ConnectionStrings.PostgreSQL);
            await connection.OpenAsync();
            
            var sql = $"DELETE FROM {_settings.Application.TableName} WHERE id = @Id";
            var result = await connection.ExecuteAsync(sql, new { Id = noteId });
            
            var message = result > 0 
                ? $"Successfully deleted note with ID: {noteId}" 
                : $"No note found with ID: {noteId}";
                
            _logger.LogInformation("Delete operation result: {Result}", message);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note with ID: {NoteId}", noteId);
            return $"Error deleting note: {ex.Message}";
        }
    }

    public async Task<string> GetNoteCountAsync()
    {
        try
        {
            _logger.LogInformation("Getting note count");
            
            await using var connection = new NpgsqlConnection(_settings.ConnectionStrings.PostgreSQL);
            await connection.OpenAsync();
            
            var sql = $"SELECT COUNT(*) FROM {_settings.Application.TableName}";
            var count = await connection.QuerySingleAsync<int>(sql);
            
            _logger.LogInformation("Total notes count: {Count}", count);
            return $"Total notes stored: {count}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note count");
            return $"Error getting note count: {ex.Message}";
        }
    }
}
```

### 8. Plugin/MemoryPlugin.cs
```csharp
using System.ComponentModel;
using Microsoft.SemanticKernel;
using PersonalAgent.Services;

namespace PersonalAgent.Plugin;

public class MemoryPlugin
{
    private readonly IMemoryService _memoryService;

    public MemoryPlugin(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    [KernelFunction]
    [Description("Store a note in memory")]
    public async Task<string> StoreNote([Description("The content to remember")] string content)
    {
        return await _memoryService.StoreNoteAsync(content);
    }

    [KernelFunction]
    [Description("Search for notes containing specific content")]
    public async Task<string> SearchNotes([Description("The search query")] string query)
    {
        return await _memoryService.SearchNotesAsync(query);
    }

    [KernelFunction]
    [Description("List all stored notes")]
    public async Task<string> ListNotes()
    {
        return await _memoryService.ListNotesAsync();
    }

    [KernelFunction]
    [Description("Delete a note by ID")]
    public async Task<string> DeleteNote([Description("The ID of the note to delete")] string noteId)
    {
        return await _memoryService.DeleteNoteAsync(noteId);
    }

    [KernelFunction]
    [Description("Get the total count of stored notes")]
    public async Task<string> GetNoteCount()
    {
        return await _memoryService.GetNoteCountAsync();
    }
}
```

### 9. Plugin/MathPlugin.cs
```csharp
using System.ComponentModel;
using Microsoft.SemanticKernel;

public class MathPlugin
{
    [KernelFunction]
    [Description("Adds two numbers")]
    public static double Add(
        [Description("The first number to add")] double number1,
        [Description("The second number to add")] double number2)
    {
        return number1 + number2;
    }

    [KernelFunction]
    [Description("Subtracts two numbers")]
    public static double Subtract(
        [Description("The number to subtract from")] double number1,
        [Description("The number to subtract")] double number2)
    {
        return number1 - number2;
    }
}
```

### 10. Plugin/TimePlugin.cs
```csharp
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace PersonalAgent.Plugin;

public class TimePlugin
{
    [KernelFunction]
    [Description("Gets the current date and time in the local timezone in a human-readable format")]
    public string GetCurrentDateTime()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        var timezone = TimeZoneInfo.Local;
        return $"{localTime.ToString("dddd, MMMM dd, yyyy 'at' hh:mm:ss tt")} ({timezone.StandardName})";
    }

    [KernelFunction]
    [Description("Gets the current time in the local timezone in 24-hour format")]
    public string GetCurrentTime()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        return localTime.ToString("HH:mm:ss");
    }

    [KernelFunction]
    [Description("Gets the current date in the local timezone")]
    public string GetCurrentDate()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        return localTime.ToString("dddd, MMMM dd, yyyy");
    }

    [KernelFunction]
    [Description("Gets the current day of the week in the local timezone")]
    public string GetDayOfWeek()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        return localTime.DayOfWeek.ToString();
    }

    [KernelFunction]
    [Description("Gets the current timezone information")]
    public string GetTimezone()
    {
        var timezone = TimeZoneInfo.Local;
        return $"{timezone.DisplayName} (UTC{timezone.BaseUtcOffset.Hours:+00;-00}:{timezone.BaseUtcOffset.Minutes:00})";
    }

    [KernelFunction]
    [Description("Gets the current UTC time")]
    public string GetUtcTime()
    {
        var utcNow = DateTime.UtcNow;
        return utcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
    }

    [KernelFunction]
    [Description("Calculates how many days until a specific date")]
    public string DaysUntil(
        [Description("The target date in format yyyy-MM-dd (e.g., 2025-12-25)")] string targetDate)
    {
        if (DateTime.TryParse(targetDate, out var target))
        {
            var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
            var today = localTime.Date;
            var days = (target.Date - today).Days;
            
            if (days < 0)
                return $"That date was {Math.Abs(days)} days ago";
            else if (days == 0)
                return "That date is today!";
            else if (days == 1)
                return "That date is tomorrow";
            else
                return $"There are {days} days until {target.ToString("MMMM dd, yyyy")}";
        }
        
        return "Invalid date format. Please use yyyy-MM-dd format (e.g., 2025-12-25)";
    }

    [KernelFunction]
    [Description("Gets the current week number of the year in the local timezone")]
    public string GetWeekNumber()
    {
        var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
        var weekNumber = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            localTime, 
            System.Globalization.CalendarWeekRule.FirstDay, 
            DayOfWeek.Monday);
        return $"Week {weekNumber} of {localTime.Year}";
    }

    [KernelFunction]
    [Description("Gets the current time in a specific timezone")]
    public string GetTimeInTimezone(
        [Description("The timezone ID (e.g., 'America/New_York', 'Europe/London', 'Asia/Tokyo', 'Pacific/Auckland')")] string timezoneId)
    {
        try
        {
            var targetTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var timeInZone = TimeZoneInfo.ConvertTime(DateTime.UtcNow, targetTimezone);
            return $"{timeInZone.ToString("dddd, MMMM dd, yyyy 'at' hh:mm:ss tt")} ({targetTimezone.StandardName})";
        }
        catch (TimeZoneNotFoundException)
        {
            return $"Timezone '{timezoneId}' not found. Common timezone IDs: America/New_York, Europe/London, Asia/Tokyo, Pacific/Auckland";
        }
    }

    [KernelFunction]
    [Description("Lists common timezone IDs that can be used with GetTimeInTimezone function")]
    public string ListCommonTimezones()
    {
        return """
               Common Timezone IDs:
               - America/New_York (Eastern Time)
               - America/Chicago (Central Time)
               - America/Denver (Mountain Time)
               - America/Los_Angeles (Pacific Time)
               - Europe/London (GMT/BST)
               - Europe/Paris (Central European Time)
               - Asia/Tokyo (Japan Standard Time)
               - Asia/Shanghai (China Standard Time)
               - Pacific/Auckland (New Zealand Time)
               - Australia/Sydney (Australian Eastern Time)
               """;
    }
}
```

### 11. PersonalAgent.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="Microsoft.Extensions.AI" Version="9.9.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.9" />
    <PackageReference Include="Microsoft.SemanticKernel" Version="1.65.0" />
    <PackageReference Include="Microsoft.SemanticKernel.Connectors.Ollama" Version="1.65.0-alpha" />
    <PackageReference Include="Npgsql" Version="9.0.3" />
    <PackageReference Include="OllamaSharp" Version="5.4.7" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

---

## Setup & Running

### Prerequisites
1. .NET 9 SDK
2. PostgreSQL running locally:
   - Database: `memory_db`
   - User: `memory_user`
   - Password: `memory_password`
   - Port: `5432`
3. Ollama installed and running with `qwen3:8b` model

### Commands
```bash
# Build
dotnet build

# Run
dotnet run

# Clean
dotnet clean

# Watch mode (for development)
dotnet watch run
```

---

## Database Schema

```sql
CREATE TABLE memory_notes (
    id TEXT PRIMARY KEY,
    content TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    tags TEXT[]
);
```

---

## Questions for Review

I'm looking for feedback on:

1. **Architecture & Design Patterns**: Is the current architecture well-structured? Any suggestions for improvements?

2. **Code Quality**: Are there any anti-patterns or code smells? Best practices I should follow?

3. **Performance**: Any potential performance bottlenecks, especially in the database operations or AI interactions?

4. **Security**: What security improvements should I implement (especially for database connections and credentials)?

5. **Extensibility**: I plan to add more features (training schedules, nutrition tracking, financial management). How can I structure the code to make this easier?

6. **Error Handling**: Is my error handling comprehensive enough? What should I improve?

7. **Testing**: What testing strategy would you recommend for this application?

8. **Memory/AI Integration**: Any suggestions for improving the AI's memory management or function calling behavior?

9. **User Experience**: How can I improve the console-based interaction?

10. **Future Enhancements**: What features or improvements would you suggest adding next?
