namespace APIseverino.Models
{
    public class IAMensagem
    {
        public int Id { get; set; }

        public int IAConversationId { get; set; }

        public string Role { get; set; }

        public string Conteudo { get; set; }

        public DateTime DataEnvio { get; set; }
            = DateTime.UtcNow;

        public IAConversation Conversation { get; set; }
    }
}