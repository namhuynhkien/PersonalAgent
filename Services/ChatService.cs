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