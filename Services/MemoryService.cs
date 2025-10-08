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