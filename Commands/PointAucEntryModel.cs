using System.Text.Json.Serialization;

namespace FilmsBot.Commands
{
    public class PointAucEntryModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("extra")]
        public object? Extra { get; set; }

        [JsonPropertyName("fastId")]
        public int FastId { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}