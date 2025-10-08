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