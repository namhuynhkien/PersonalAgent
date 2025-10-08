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