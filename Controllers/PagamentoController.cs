using APIseverino.Data;
using APIseverino.Helpers;
using APIseverino.Models;
using APIseverino.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIseverino.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagamentoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly StripeService _stripe;

        // Taxa da plataforma: 15% sobre o valor bruto
        private const decimal TaxaPlataformaPercent = 0.15m;

        // Dias corridos após a criação em que qualquer parte pode cancelar unilateralmente
        private const int DiasLimiteCancelamentoUnilateral = 30;

        public PagamentoController(AppDbContext context, StripeService stripe)
        {
            _context = context;
            _stripe = stripe;
        }

        // ─── DTOs ─────────────────────────────────────────────────────────────────

        /// <param name="ChatRoomId">ID da sala de chat vinculada ao serviço.</param>
        /// <param name="Valor">Valor total combinado pelo serviço (BRL).</param>
        /// <param name="StripeContaPrestadorId">
        ///     Stripe Connect Account ID do prestador (ex: "acct_1Abc...").
        ///     Em produção, armazene isso no Cadastro do prestador após o onboarding.
        /// </param>
        public record CriarPagamentoBody(
            int ChatRoomId,
            decimal Valor,
            string StripeContaPrestadorId
        );

        public record SolicitarCancelamentoBody(int UsuarioId);


        // ─────────────────────────────────────────────────────────────────────────
        // POST: api/pagamento/criar
        //
        // Cria o registro de pagamento e um PaymentIntent no Stripe com
        // capture_method=manual (escrow). O cliente paga no frontend usando o
        // clientSecret retornado; o valor fica retido até a conclusão do serviço.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPost("criar")]
        public async Task<IActionResult> CriarPagamento([FromBody] CriarPagamentoBody dto)
        {
            if (dto.Valor <= 0)
                return BadRequest("O valor do pagamento deve ser maior que zero.");

            var sala = await _context.ChatRooms
                .Include(r => r.Post)
                .Include(r => r.Cliente)
                .Include(r => r.Prestador)
                .FirstOrDefaultAsync(r => r.Id == dto.ChatRoomId);

            if (sala == null)
                return NotFound("Sala de chat não encontrada.");

            if (sala.Post.Status != StatusPost.EmAndamento)
                return BadRequest("O serviço precisa estar Em Andamento para criar um pagamento.");

            // Impede pagamento duplicado para a mesma sala
            var pagamentoExistente = await _context.Pagamentos
                .AnyAsync(p => p.ChatRoomId == dto.ChatRoomId
                            && p.Status != StatusPagamento.Cancelado
                            && p.Status != StatusPagamento.Reembolsado);

            if (pagamentoExistente)
                return BadRequest("Já existe um pagamento ativo para este serviço.");

            // Calcula taxa e valor líquido
            var taxa = Math.Round(dto.Valor * TaxaPlataformaPercent, 2);
            var liquido = dto.Valor - taxa;

            // Cria o PaymentIntent no Stripe (valor retido, não capturado)
            string paymentIntentId, clientSecret;
            try
            {
                (paymentIntentId, clientSecret) = await _stripe.CriarIntencaoPagamento(
                    valor: dto.Valor,
                    descricao: $"Serviço #{sala.Post.Id} — {sala.Post.Titulo}",
                    stripeContaPrestadorId: dto.StripeContaPrestadorId
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao criar pagamento no Stripe: {ex.Message}");
            }

            var pagamento = new Pagamento
            {
                ChatRoomId = dto.ChatRoomId,
                ClienteId = sala.ClienteId,
                PrestadorId = sala.PrestadorId,
                Valor = dto.Valor,
                TaxaPlataforma = taxa,
                ValorLiquido = liquido,
                StripePaymentIntentId = paymentIntentId,
                StripeContaPrestadorId = dto.StripeContaPrestadorId,
                Status = StatusPagamento.Pendente,
                DataCriacao = DateTime.UtcNow,
                PrazoLimiteCancelamento = DateTime.UtcNow.AddDays(DiasLimiteCancelamentoUnilateral)
            };

            _context.Pagamentos.Add(pagamento);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                pagamento.Id,
                pagamento.ChatRoomId,
                pagamento.ClienteId,
                pagamento.PrestadorId,
                pagamento.Valor,
                pagamento.TaxaPlataforma,
                pagamento.ValorLiquido,
                pagamento.Status,
                pagamento.PrazoLimiteCancelamento,
                // O frontend usa este clientSecret para confirmar o pagamento via Stripe.js
                StripeClientSecret = clientSecret
            });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // PUT: api/pagamento/{id}/confirmar-retencao
        //
        // Chamado após o webhook do Stripe confirmar que o cliente pagou
        // (evento payment_intent.amount_capturable_updated).
        // Marca o pagamento como Retido no banco.
        //
        // Em produção: prefira receber isso via webhook assinado pelo Stripe
        // em vez de um endpoint manual, para garantia.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPut("{id}/confirmar-retencao")]
        public async Task<IActionResult> ConfirmarRetencao(int id)
        {
            var pagamento = await _context.Pagamentos.FindAsync(id);
            if (pagamento == null)
                return NotFound("Pagamento não encontrado.");

            if (pagamento.Status != StatusPagamento.Pendente)
                return BadRequest($"Pagamento não está Pendente (status atual: {pagamento.Status}).");

            pagamento.Status = StatusPagamento.Retido;
            pagamento.DataPagamento = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { pagamento.Id, pagamento.Status, pagamento.DataPagamento });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // PUT: api/pagamento/{id}/liberar
        //
        // Captura o valor no Stripe e transfere o valor líquido ao prestador.
        // Só pode ser chamado quando o serviço está Concluído.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPut("{id}/liberar")]
        public async Task<IActionResult> LiberarPagamento(int id)
        {
            var pagamento = await _context.Pagamentos
                .Include(p => p.ChatRoom)
                    .ThenInclude(r => r.Post)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pagamento == null)
                return NotFound("Pagamento não encontrado.");

            if (pagamento.Status != StatusPagamento.Retido)
                return BadRequest("O pagamento precisa estar Retido para ser liberado.");


            if (string.IsNullOrEmpty(pagamento.StripePaymentIntentId))
                return BadRequest("PaymentIntent do Stripe não encontrado neste pagamento.");

            string transferId;
            try
            {
                transferId = await _stripe.CapturarETransferir(
                    paymentIntentId: pagamento.StripePaymentIntentId,
                    valorLiquido: pagamento.ValorLiquido,
                    stripeContaPrestadorId: pagamento.StripeContaPrestadorId!
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao capturar/transferir no Stripe: {ex.Message}");
            }

            pagamento.Status = StatusPagamento.Liberado;
            pagamento.StripeTransferId = transferId;
            pagamento.DataLiberacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                pagamento.Id,
                pagamento.Status,
                pagamento.ValorLiquido,
                pagamento.DataLiberacao,
                pagamento.StripeTransferId
            });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // PUT: api/pagamento/{id}/solicitar-cancelamento
        //
        // Cliente OU prestador sinaliza que quer cancelar.
        // O cancelamento só ocorre automaticamente quando:
        //   a) Ambos sinalizaram, OU
        //   b) Passou o PrazoLimiteCancelamento (qualquer um pode acionar).
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPut("{id}/solicitar-cancelamento")]
        public async Task<IActionResult> SolicitarCancelamento(
            int id,
            [FromBody] SolicitarCancelamentoBody dto)
        {
            var pagamento = await _context.Pagamentos.FindAsync(id);
            if (pagamento == null)
                return NotFound("Pagamento não encontrado.");

            if (pagamento.Status == StatusPagamento.Liberado)
                return BadRequest("O pagamento já foi liberado ao prestador e não pode ser cancelado.");

            if (pagamento.Status == StatusPagamento.Cancelado || pagamento.Status == StatusPagamento.Reembolsado)
                return BadRequest("Este pagamento já foi cancelado/reembolsado.");

            bool eCliente = dto.UsuarioId == pagamento.ClienteId;
            bool ePrestador = dto.UsuarioId == pagamento.PrestadorId;

            if (!eCliente && !ePrestador)
                return Unauthorized("Apenas o cliente ou prestador deste pagamento podem solicitar cancelamento.");

            if (eCliente) pagamento.ClienteSolicitouCancelamento = true;
            if (ePrestador) pagamento.PrestadorSolicitouCancelamento = true;

            bool ambosAcordaram = pagamento.ClienteSolicitouCancelamento && pagamento.PrestadorSolicitouCancelamento;
            bool prazoExpirou = DateTime.UtcNow > pagamento.PrazoLimiteCancelamento;

            // Cancela imediatamente se houve acordo mútuo ou prazo expirou
            if (ambosAcordaram || prazoExpirou)
            {
                await ExecutarCancelamento(pagamento);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    pagamento.Id,
                    pagamento.Status,
                    Motivo = ambosAcordaram ? "Acordo mútuo entre cliente e prestador." : "Prazo limite expirado.",
                    pagamento.DataCancelamento
                });
            }

            // Salva a sinalização parcial
            await _context.SaveChangesAsync();

            var quemFalta = eCliente ? "O prestador ainda não concordou." : "O cliente ainda não concordou.";

            return Ok(new
            {
                pagamento.Id,
                pagamento.ClienteSolicitouCancelamento,
                pagamento.PrestadorSolicitouCancelamento,
                Mensagem = $"Solicitação registrada. {quemFalta} O cancelamento ocorrerá quando ambos concordarem.",
                PrazoLimite = pagamento.PrazoLimiteCancelamento
            });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // PUT: api/pagamento/{id}/verificar-prazo
        //
        // Endpoint auxiliar que pode ser chamado por um job ou pelo próprio
        // frontend para forçar o cancelamento quando o prazo expirou.
        // Em produção, substitua por um BackgroundService similar ao PostExpiracaoService.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPut("{id}/verificar-prazo")]
        public async Task<IActionResult> VerificarPrazo(int id)
        {
            var pagamento = await _context.Pagamentos.FindAsync(id);
            if (pagamento == null)
                return NotFound("Pagamento não encontrado.");

            if (pagamento.Status != StatusPagamento.Pendente && pagamento.Status != StatusPagamento.Retido)
                return Ok(new { Mensagem = "Nenhuma ação necessária.", pagamento.Status });

            if (DateTime.UtcNow <= pagamento.PrazoLimiteCancelamento)
                return Ok(new { Mensagem = "Prazo ainda não expirou.", pagamento.PrazoLimiteCancelamento });

            await ExecutarCancelamento(pagamento);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                pagamento.Id,
                pagamento.Status,
                Motivo = "Prazo limite atingido. Pagamento cancelado e reembolso iniciado.",
                pagamento.DataCancelamento
            });
        }


        // ─────────────────────────────────────────────────────────────────────────
        // GET: api/pagamento/{id}
        // ─────────────────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPagamento(int id)
        {
            var pagamento = await _context.Pagamentos
                .Include(p => p.Cliente)
                .Include(p => p.Prestador)
                .Include(p => p.ChatRoom)
                    .ThenInclude(r => r.Post)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.ChatRoomId,
                    TituloPost = p.ChatRoom.Post.Titulo,
                    p.ClienteId,
                    NomeCliente = p.Cliente.Nome,
                    p.PrestadorId,
                    NomePrestador = p.Prestador.Nome,
                    p.Valor,
                    p.TaxaPlataforma,
                    p.ValorLiquido,
                    p.Status,
                    p.ClienteSolicitouCancelamento,
                    p.PrestadorSolicitouCancelamento,
                    p.DataCriacao,
                    p.DataPagamento,
                    p.DataLiberacao,
                    p.DataCancelamento,
                    p.PrazoLimiteCancelamento
                })
                .FirstOrDefaultAsync();

            if (pagamento == null)
                return NotFound("Pagamento não encontrado.");

            return Ok(pagamento);
        }


        // ─────────────────────────────────────────────────────────────────────────
        // GET: api/pagamento/chatroom/{chatRoomId}
        // ─────────────────────────────────────────────────────────────────────────
        [HttpGet("chatroom/{chatRoomId}")]
        public async Task<IActionResult> GetPagamentoDaSala(int chatRoomId)
        {
            var pagamento = await _context.Pagamentos
                .Where(p => p.ChatRoomId == chatRoomId)
                .OrderByDescending(p => p.DataCriacao)
                .Select(p => new
                {
                    p.Id,
                    p.Valor,
                    p.TaxaPlataforma,
                    p.ValorLiquido,
                    p.Status,
                    p.ClienteSolicitouCancelamento,
                    p.PrestadorSolicitouCancelamento,
                    p.DataCriacao,
                    p.DataPagamento,
                    p.DataLiberacao,
                    p.DataCancelamento,
                    p.PrazoLimiteCancelamento
                })
                .FirstOrDefaultAsync();

            if (pagamento == null)
                return NotFound("Nenhum pagamento encontrado para esta sala.");

            return Ok(pagamento);
        }


        // ─────────────────────────────────────────────────────────────────────────
        // Método interno: executa o cancelamento no Stripe e atualiza o model.
        // ─────────────────────────────────────────────────────────────────────────
        private async Task ExecutarCancelamento(Pagamento pagamento)
        {
            if (!string.IsNullOrEmpty(pagamento.StripePaymentIntentId))
            {
                try
                {
                    await _stripe.CancelarOuReembolsar(pagamento.StripePaymentIntentId);
                    pagamento.Status = StatusPagamento.Reembolsado;
                }
                catch
                {
                    // Se falhar no Stripe, marca como Cancelado localmente de qualquer forma
                    // e logue o erro para revisão manual.
                    pagamento.Status = StatusPagamento.Cancelado;
                }
            }
            else
            {
                pagamento.Status = StatusPagamento.Cancelado;
            }

            pagamento.DataCancelamento = DateTime.UtcNow;
        }
    }
}