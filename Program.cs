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