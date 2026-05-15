namespace APIseverino.Models
{
    public class IAConversation
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public bool Finalizada { get; set; }

        public DateTime CriadoEm { get; set; }
            = DateTime.UtcNow;

        public List<IAMensagem> Mensagens { get; set; }
            = new();
    }
}