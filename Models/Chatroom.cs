using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;

        public int ClienteId { get; set; }

        [JsonIgnore]
        public Usuario Cliente { get; set; } = null!;

        public int PrestadorId { get; set; }

        [JsonIgnore]
        public Usuario Prestador { get; set; } = null!;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ICollection<ChatMessage> Mensagens { get; set; } = new List<ChatMessage>();
    }
}