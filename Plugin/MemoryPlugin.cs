using System.ComponentModel;
using Dapper;
using Microsoft.SemanticKernel;
using Npgsql;

namespace PersonalAgent.Plugin;

public class MemoryPlugin
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=memory_db;Username=memory_user;Password=memory_password;";
    private const string TableName = "memory_notes";

    [KernelFunction]
    [Description("Store a note in memory")]
    public async Task<string> StoreNote([Description("The content to remember")] string content)
    {
        try
        {
            var id = $"note_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..8]}";

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var sql = $"INSERT INTO {TableName} (id, content) VALUES (@Id, @Content)";
            await connection.ExecuteAsync(sql, new { Id = id, Content = content });
            
            return $"Successfully stored note with ID: {id}";
        }
        catch (Exception ex)
        {
            return $"Error storing note: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Search for notes containing specific content")]
    public async Task<string> SearchNotes([Description("The search query")] string query)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var sql = $@"
                SELECT id, content, created_at 
                FROM {TableName} 
                WHERE content ILIKE @Query 
                ORDER BY created_at DESC 
                LIMIT 10";
                
            var notes = await connection.QueryAsync<MemoryNote>(sql, new { Query = $"%{query}%" });
            
            if (!notes.Any())
            {
                return "No matching notes found.";
            }
            
            var result = notes.Select(note => 
                $"ID: {note.Id} | {note.Content} (Created: {note.CreatedAt:yyyy-MM-dd HH:mm})");
                
            return string.Join("\n", result);
        }
        catch (Exception ex)
        {
            return $"Error searching notes: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("List all stored notes")]
    public async Task<string> ListNotes()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var sql = $"SELECT id, content, created_at FROM {TableName} ORDER BY created_at DESC LIMIT 50";
            var notes = await connection.QueryAsync<MemoryNote>(sql);
            
            if (!notes.Any())
            {
                return "No notes stored yet.";
            }
            
            var result = notes.Select(note => 
                $"ID: {note.Id} | {note.Content} (Created: {note.CreatedAt:yyyy-MM-dd HH:mm})");
                
            return string.Join("\n", result);
        }
        catch (Exception ex)
        {
            return $"Error listing notes: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Delete a note by ID")]
    public async Task<string> DeleteNote([Description("The ID of the note to delete")] string noteId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var sql = $"DELETE FROM {TableName} WHERE id = @Id";
            var result = await connection.ExecuteAsync(sql, new { Id = noteId });
            
            return result > 0 
                ? $"Successfully deleted note with ID: {noteId}" 
                : $"No note found with ID: {noteId}";
        }
        catch (Exception ex)
        {
            return $"Error deleting note: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get the total count of stored notes")]
    public async Task<string> GetNoteCount()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var sql = $"SELECT COUNT(*) FROM {TableName}";
            var count = await connection.QuerySingleAsync<int>(sql);
            
            return $"Total notes stored: {count}";
        }
        catch (Exception ex)
        {
            return $"Error getting note count: {ex.Message}";
        }
    }
}