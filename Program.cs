using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Npgsql;
using PersonalAgent.Plugin;

class SimpleMemoryChatApp
{
    private static Kernel? _kernel;
    private static IChatCompletionService? _chatCompletionService;
    private static readonly ChatHistory _chatHistory = new();
    private static string _connectionString = "Host=localhost;Port=5432;Database=memory_db;Username=memory_user;Password=memory_password;";
    private static readonly string TableName = "memory_notes";

    static async Task Main(string[] args)
    {
        try
        {
            await InitializeDatabase();
            await InitializeKernel();
            await RunChatApplication();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task InitializeDatabase()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var createTableSql = $@"
            CREATE TABLE IF NOT EXISTS {TableName} (
                id TEXT PRIMARY KEY,
                content TEXT NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                tags TEXT[]
            )";
            
        await connection.ExecuteAsync(createTableSql);
        
        Console.WriteLine("Database initialized successfully.");
    }
    
    private static async Task InitializeKernel()
    {
        Console.WriteLine("Initializing Qwen3 with Semantic Kernel...");
        
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(logging => logging.AddSimpleConsole());
        
        builder.AddOllamaChatCompletion(
            // modelId: "qwen2.5-coder:7b",
            modelId: "qwen3:8b",
            endpoint: new Uri("http://localhost:11434")
        );

        _kernel = builder.Build();
        
        _kernel.ImportPluginFromType<MemoryPlugin>();
        _kernel.Plugins.AddFromType<MathPlugin>();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

        Console.WriteLine("Assistant is ready! Type your messages below.");
        Console.WriteLine("Commands: 'quit' to exit, 'show notes' to see all stored notes");
        Console.WriteLine(new string('-', 50));
    }

    private static async Task RunChatApplication()
    {
        while (true)
        {
            Console.Write("\nYou: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.ToLower() == "quit")
                break;

            // Add user message to chat history
            _chatHistory.AddUserMessage(userInput);

            // Let the AI process the input and potentially call plugins
            await ProcessWithAi(userInput);
        }
    }

    private static async Task ProcessWithAi(string userInput)
    {
        try
        {
            // This approach lets Qwen3 automatically decide when to call functions
            var prompt = $"""
                          User: {userInput}

                          Based on the user input, decide if you need to:
                          1. Store information using StoreNote() if they want to remember something
                          2. Search information using SearchNotes() if they're asking about past content
                          3. List all notes using ListNotes() if they want to see everything
                          4. Delete a note using DeleteNote() if they want to remove something
                          5. Just have a conversation if none of the above apply

                          Available functions: StoreNote, SearchNotes, ListNotes, DeleteNote
                          """;
            _chatHistory.AddSystemMessage(prompt);

            // Add user message
            _chatHistory.AddUserMessage(userInput);
            
            // Let Qwen3 process with potential function calls
            var response = await _chatCompletionService!.GetChatMessageContentAsync(
                _chatHistory,
                executionSettings: new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
                },
                kernel: _kernel
            );
            
            Console.WriteLine($"Assistant: {response.Content}");
            
            _chatHistory.AddAssistantMessage(response.Content!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Assistant: Sorry, I encountered an error: {ex.Message}");
        }
    }
    
    // note for me that I will go to gym 4 times a week on Monday, Tuesday, Thursday and Friday at 4:45pm
}