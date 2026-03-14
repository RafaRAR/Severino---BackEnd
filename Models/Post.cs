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
        public string? ImagemUrl { get; set; }  // NOVO
        public string Role { get; set; } = string.Empty;
        public string? Impulsionar { get; set; } = string.Empty;

        [JsonIgnore]
        public Usuario Usuario { get; set; }

        public int UsuarioId { get; set; }

        [JsonIgnore]
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}