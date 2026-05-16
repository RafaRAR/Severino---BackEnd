using Microsoft.AspNetCore.Mvc;
using APIseverino.Helpers; 
using System.Threading.Tasks;
using System;

namespace APIseverino.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly StripeService _stripeService;

        public StripeController(StripeService stripeService)
        {
            _stripeService = stripeService;
        }

        [HttpPost("onboarding/{usuarioId}")]
        public async Task<IActionResult> GerarLink(int usuarioId)
        {
            try
            {
                // URLs para onde o Stripe vai redirecionar o prestador ao terminar (ou falhar)
                // Num ambiente de produção real, puxe isto do appsettings.json
                string returnUrl = "http://localhost:5173/";
                string refreshUrl = "http://localhost:5173/";

                // 1. Cria ou recupera o acct_xxxxxx
                string accountId = await _stripeService.ObterOuCriarContaExpress(usuarioId);

                // 2. Gera o URL para a interface do Stripe
                string urlOnboarding = await _stripeService.GerarLinkDeOnboarding(accountId, returnUrl, refreshUrl);

                // 3. Devolve para o Front-end redirecionar
                return Ok(new { url = urlOnboarding });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao configurar Stripe: {ex.Message}" });
            }
        }
    }
}