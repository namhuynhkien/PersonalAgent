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