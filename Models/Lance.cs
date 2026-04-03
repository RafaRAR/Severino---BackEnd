using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class Lance
    {
        public int Id { get; set; }

        public decimal ValorDeLance { get; set; }

        public bool IsAccepted { get; set; } = false;

        public DateTime DataCriacao { get; set; }

        // Relacionamento com o Post
        public int IdPost { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;

        // Relacionamento com o Usuário (Prestador)
        public int IdPrestadorResponsavel { get; set; }

        [JsonIgnore]
        public Usuario Prestador { get; set; } = null!;
    }
}