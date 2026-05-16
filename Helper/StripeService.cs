using APIseverino.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace APIseverino.Helpers
{
    /// <summary>
    /// Abstrai as chamadas ao Stripe. Injete via DI como Scoped.
    /// 
    /// Pré-requisitos:
    ///   1. dotnet add package Stripe.net
    ///   2. Defina a variável de ambiente STRIPE_SECRET_KEY (ex: no .env.test ou no ambiente de produção):
    ///        STRIPE_SECRET_KEY=sk_test_...   (desenvolvimento)
    ///        STRIPE_SECRET_KEY=sk_live_...   (produção)
    ///   3. Cada prestador precisa criar uma conta Stripe Connect (Express ou Standard).
    ///      Armazene o stripe_account_id dele em Cadastro ou em Pagamento.StripeContaPrestadorId.
    /// </summary>
    public class StripeService
    {
        private readonly AppDbContext _context;

        public StripeService(AppDbContext context)
        {
            _context = context;

            if (System.IO.File.Exists(".env.test")) Env.Load(".env.test");
            else Env.Load(".env");

            StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                ?? throw new InvalidOperationException("Variável de ambiente STRIPE_SECRET_KEY não configurada.");
        }

        public async Task<string> ObterOuCriarContaExpress(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
                throw new Exception("Utilizador não encontrado.");

            // Se já tem conta, devolve o ID existente
            if (!string.IsNullOrEmpty(usuario.StripeAccountId))
            {
                return usuario.StripeAccountId;
            }

            // Se não tem, cria a conta no Stripe
            var options = new AccountCreateOptions
            {
                Type = "express",
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                }
            };

            var service = new AccountService();
            var account = await service.CreateAsync(options);

            // Guarda na base de dados
            usuario.StripeAccountId = account.Id;
            await _context.SaveChangesAsync();

            return account.Id;
        }

        /// <summary>
        /// Gera o URL único para o prestador preencher os dados bancários no Stripe.
        /// </summary>
        public async Task<string> GerarLinkDeOnboarding(string accountId, string returnUrl, string refreshUrl)
        {
            var options = new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            };

            var service = new AccountLinkService();
            var accountLink = await service.CreateAsync(options);

            return accountLink.Url;
        }
    

        // ─────────────────────────────────────────────────────────────────────────
        // Cria um PaymentIntent com capture_method = manual.
        // O valor fica AUTORIZADO (retido) mas NÃO capturado até chamarmos Capturar().
        // O frontend usa o client_secret retornado para confirmar o pagamento.
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<(string paymentIntentId, string clientSecret)> CriarIntencaoPagamento(
            decimal valor,
            string descricao,
            string stripeContaPrestadorId)
        {
            var options = new PaymentIntentCreateOptions
            {
                // Stripe trabalha em centavos (inteiros)
                Amount = (long)(valor * 100),
                Currency = "brl",
                CaptureMethod = "manual",           // retém sem capturar
                Description = descricao,

                // Instrui o Stripe a transferir automaticamente ao prestador na captura
                TransferData = new PaymentIntentTransferDataOptions
                {
                    Destination = stripeContaPrestadorId
                },

                // Permite que o PaymentIntent fique aberto por até 7 dias (máximo do Stripe)
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return (intent.Id, intent.ClientSecret);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Captura o valor já autorizado e o repassa ao prestador via Transfer.
        // Chame isso ao concluir o serviço.
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<string> CapturarETransferir(
            string paymentIntentId,
            decimal valorLiquido,
            string stripeContaPrestadorId)
        {
            // 1. Captura o PaymentIntent (cobra o cliente)
            var intentService = new PaymentIntentService();
            await intentService.CaptureAsync(paymentIntentId);

            // 2. Cria a transferência para a conta Connect do prestador
            var transferOptions = new TransferCreateOptions
            {
                Amount = (long)(valorLiquido * 100),
                Currency = "brl",
                Destination = stripeContaPrestadorId,
                SourceTransaction = paymentIntentId
            };

            var transferService = new TransferService();
            var transfer = await transferService.CreateAsync(transferOptions);

            return transfer.Id;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Cancela o PaymentIntent (antes de capturado) ou emite reembolso total.
        // ─────────────────────────────────────────────────────────────────────────
        public async Task CancelarOuReembolsar(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(paymentIntentId);

            if (intent.Status == "requires_capture" || intent.Status == "requires_confirmation")
            {
                // Ainda não capturado → cancela sem cobrar
                await service.CancelAsync(paymentIntentId);
            }
            else if (intent.Status == "succeeded")
            {
                // Já capturado → emite reembolso total
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId
                };
                var refundService = new RefundService();
                await refundService.CreateAsync(refundOptions);
            }
            // outros status (canceled, etc.) → já está cancelado, nada a fazer
        }
    }
}