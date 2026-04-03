using APIseverino.Models.Enums;
using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class Post
    {
        public int Id { get; set; }

        public DateTime DataCriacao { get; set; }

        // Expira 15 dias após a criação (ou após reinício de negociação abortada)
        public DateTime DataExpiracao { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public string? Endereco { get; set; } = string.Empty;
        public string? Cep { get; set; } = string.Empty;
        public string? Contato { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Impulsionar { get; set; }

        // 0=Aberto, 1=Concluido, 2=Expirado, 3=EmAndamento
        public StatusPost Status { get; set; } = StatusPost.Aberto;

        // Prestador com quem está negociando (quando EmAndamento)
        public int? PrestadorEmNegociacaoId { get; set; }

        [JsonIgnore]
        public Usuario Usuario { get; set; } = null!;

        public int UsuarioId { get; set; }

        [JsonIgnore]
        public ICollection<PostImagem> Imagens { get; set; } = new List<PostImagem>();

        [JsonIgnore]
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        [JsonIgnore]
        public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();

        [JsonIgnore]
        public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

        [JsonIgnore]
        public ICollection<Lance> Lances { get; set; } = new List<Lance>();
    }
}