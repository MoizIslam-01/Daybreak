using System.Text.Json;

namespace Daybreak.CloudCode
{
    /// <summary>
    /// One JSON configuration for the whole module. IncludeFields is on because the shared DTOs
    /// (SquadDto, DayResultDto, …) use public fields — the same shape the Unity client serializes
    /// with Newtonsoft, so the two sides interoperate.
    /// </summary>
    public static class Json
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        public static string To<T>(T value) => JsonSerializer.Serialize(value, Options);

        public static T From<T>(string json) =>
            string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, Options);
    }
}
