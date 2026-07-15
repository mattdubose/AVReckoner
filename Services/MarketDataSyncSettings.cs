namespace Reckoner.Services
{
    public class MarketDataSyncSettings
    {
        public string FiPyExePath { get; set; } = "";

        public static MarketDataSyncSettings Load(string path)
        {
            if (!File.Exists(path))
            {
                var defaults = new MarketDataSyncSettings();
                File.WriteAllText(path, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
                return defaults;
            }

            try
            {
                return JsonSerializer.Deserialize<MarketDataSyncSettings>(File.ReadAllText(path))
                       ?? new MarketDataSyncSettings();
            }
            catch
            {
                return new MarketDataSyncSettings();
            }
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
