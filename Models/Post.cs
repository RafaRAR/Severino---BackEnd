using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class Post
    {
        public int Id { get; set; }

        public DateTime DataCriacao { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public string? Endereco { get; set; } = string.Empty;
        public string? Cep { get; set; } = string.Empty;
        public string? Contato { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string? ImagemFileId { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool Impulsionar { get; set; }

        [JsonIgnore]
        public Usuario Usuario { get; set; } = null!;

        public int UsuarioId { get; set; }

        [JsonIgnore]
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        [JsonIgnore]
        public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
    }
}