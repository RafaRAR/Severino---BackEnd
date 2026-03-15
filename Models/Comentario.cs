using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class Comentario
    {
        public int Id { get; set; }

        public DateTime DataCriacao { get; set; }

        public string Conteudo { get; set; } = string.Empty;

        public decimal ValorDeLance { get; set; }

        public int UsuarioId { get; set; }

        [JsonIgnore]
        public Usuario Usuario { get; set; } = null!;

        public int PostId { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;
    }
}