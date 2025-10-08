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