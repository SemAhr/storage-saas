namespace MediaService.Infrastructure.Database;

public sealed class PostgresConfig
{
    public const string SectionName = "Postgres";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Database { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
