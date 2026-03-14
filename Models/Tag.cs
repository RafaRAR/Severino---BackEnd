using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}