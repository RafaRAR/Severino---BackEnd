using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int ChatRoomId { get; set; }

        [JsonIgnore]
        public ChatRoom ChatRoom { get; set; } = null!;

        public int SenderId { get; set; }

        // Nome do remetente para exibição rápida (desnormalizado)
        public string SenderNome { get; set; } = string.Empty;

        public string Conteudo { get; set; } = string.Empty;

        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    }
}