namespace VaultLib.Core.DB;

public class DatabaseOptions
{
    public DatabaseOptions(string gameId, DatabaseType type)
    {
        this.GameId = gameId;
        this.Type = type;
    }

    public string GameId { get; }

    public DatabaseType Type { get; }
}