using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class PostImagem
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? FileId { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;
    }
}