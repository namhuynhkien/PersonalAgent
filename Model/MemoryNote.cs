namespace PersonalAgent;

internal class MemoryNote
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string[]? Tags { get; set; }
}