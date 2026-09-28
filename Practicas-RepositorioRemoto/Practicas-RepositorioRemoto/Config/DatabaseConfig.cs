namespace Practicas_RepositorioRemoto.Config;

public class DatabaseConfig {
    
    public string Provider { get; set; } = "Sqlite";
    public string SqliteConnectionString { get; set; } = "Data Source=app.db";
    public string BaseUrl { get; set; } = "https://jsonplaceholder.typicode.com";
}