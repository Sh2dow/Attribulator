namespace Attribulator.API
{
    public class DatabaseOptions
    {
        public DatabaseOptions(string gameId, DatabaseType type)
        {
            GameId = gameId;
            Type = type;
        }

        public string GameId { get; }
        public DatabaseType Type { get; }
    }
}
