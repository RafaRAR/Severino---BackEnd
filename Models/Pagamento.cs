using APIseverino.Models.Enums;
using System.Text.Json.Serialization;

namespace APIseverino.Models
{
    public class Pagamento
    {
        public int Id { get; set; }

        // ─── Relacionamentos ──────────────────────────────────────────────────────

        public int ChatRoomId { get; set; }

        [JsonIgnore]
        public ChatRoom ChatRoom { get; set; } = null!;

        public int ClienteId { get; set; }

        [JsonIgnore]
        public Usuario Cliente { get; set; } = null!;

        public int PrestadorId { get; set; }

        [JsonIgnore]
        public Usuario Prestador { get; set; } = null!;

        // ─── Valores ──────────────────────────────────────────────────────────────

        /// <summary>Valor combinado pelo serviço (em BRL).</summary>
        public decimal Valor { get; set; }

        /// <summary>Taxa da plataforma retida (ex: 10%). Calculada na criação.</summary>
        public decimal TaxaPlataforma { get; set; }

        /// <summary>Valor líquido que o prestador recebe (Valor - TaxaPlataforma).</summary>
        public decimal ValorLiquido { get; set; }

        // ─── Gateway (Stripe) ─────────────────────────────────────────────────────

        /// <summary>ID do PaymentIntent no Stripe. Usado para capturar ou cancelar.</summary>
        public string? StripePaymentIntentId { get; set; }

        /// <summary>ID da Transfer no Stripe (preenchido ao liberar).</summary>
        public string? StripeTransferId { get; set; }

        /// <summary>ID da conta Stripe Connect do prestador (stripe_account_id).</summary>
        public string? StripeContaPrestadorId { get; set; }

        // ─── Status e confirmações de cancelamento ────────────────────────────────

        public StatusPagamento Status { get; set; } = StatusPagamento.Pendente;

        /// <summary>Cliente concordou com o cancelamento.</summary>
        public bool ClienteSolicitouCancelamento { get; set; } = false;

        /// <summary>Prestador concordou com o cancelamento.</summary>
        public bool PrestadorSolicitouCancelamento { get; set; } = false;

        // ─── Datas ────────────────────────────────────────────────────────────────

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public DateTime? DataPagamento { get; set; }

        public DateTime? DataLiberacao { get; set; }

        public DateTime? DataCancelamento { get; set; }

        /// <summary>
        /// Prazo máximo para conclusão do serviço. Após esse prazo, qualquer parte
        /// pode cancelar unilateralmente e o reembolso é automático.
        /// </summary>
        public DateTime PrazoLimiteCancelamento { get; set; }
    }
}