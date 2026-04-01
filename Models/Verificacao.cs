using APIseverino.Models.Enums;
using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class Verificacao
    {
        public int Id { get; set; }

        public int CadastroId { get; set; }

        [JsonIgnore]
        public Cadastro Cadastro { get; set; } = null!;

        public string ImagemUrl { get; set; } = string.Empty;
        public string? ImagemFileId { get; set; }

        public SituacaoVerificacao Situacao { get; set; } = SituacaoVerificacao.Aguardando;

        public int? UpdatedById { get; set; }

        [JsonIgnore]
        public Usuario? UpdatedBy { get; set; }

        public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAvaliacao { get; set; }
    }
}