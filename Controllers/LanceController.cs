using APIseverino.Data;
using APIseverino.Models;
using APIseverino.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public LanceController(AppDbContext context)
    {
        _context = context;
    }

    // DTOs (Records)
    public record CreateLanceBody(int IdPost, int IdPrestadorResponsavel, decimal ValorDeLance);
    public record UpdateValorLanceBody(decimal NovoValor);

    // POST: api/lance/criar
    [HttpPost("criar")]
    public async Task<IActionResult> CriarLance([FromBody] CreateLanceBody dto)
    {
        var postExiste = await _context.Posts.AnyAsync(p => p.Id == dto.IdPost);
        if (!postExiste) return NotFound("Post não encontrado.");

        var prestadorExiste = await _context.Usuarios.AnyAsync(u => u.Id == dto.IdPrestadorResponsavel);
        if (!prestadorExiste) return NotFound("Prestador não encontrado.");

        var lance = new Lance
        {
            IdPost = dto.IdPost,
            IdPrestadorResponsavel = dto.IdPrestadorResponsavel,
            ValorDeLance = dto.ValorDeLance,
            DataCriacao = DateTime.UtcNow,
            IsAccepted = false
        };

        _context.Lances.Add(lance);
        await _context.SaveChangesAsync();

        return Ok(lance);
    }

    // GET: api/lance/post/{idPost}
    [HttpGet("post/{idPost}")]
    public async Task<IActionResult> GetLancesDoPost(int idPost)
    {
        var lances = await _context.Lances
            .Include(l => l.Prestador)
            .Where(l => l.IdPost == idPost)
            .OrderByDescending(l => l.DataCriacao)
            .Select(l => new
            {
                l.Id,
                l.ValorDeLance,
                l.IsAccepted,
                l.DataCriacao,
                l.IdPrestadorResponsavel,
                NomePrestador = l.Prestador.Nome
            })
            .ToListAsync();

        return Ok(lances);
    }

    // PUT: api/lance/atualizar-valor/{idLance}
    [HttpPut("atualizar-valor/{idLance}")]
    public async Task<IActionResult> AtualizarValor(int idLance, [FromBody] UpdateValorLanceBody dto)
    {
        var lance = await _context.Lances.FindAsync(idLance);

        if (lance == null) return NotFound("Lance não encontrado.");

        if (lance.IsAccepted)
            return BadRequest("Não é possível alterar o valor de um lance que já foi aceito.");

        lance.ValorDeLance = dto.NovoValor;

        await _context.SaveChangesAsync();

        return Ok(lance);
    }

    // PUT: api/lance/aceitar/{idLance}
    [HttpPut("aceitar/{idLance}")]
    public async Task<IActionResult> AceitarLance(int idLance)
    {
        var lance = await _context.Lances
            .Include(l => l.Post)
            .FirstOrDefaultAsync(l => l.Id == idLance);

        if (lance == null) return NotFound("Lance não encontrado.");

        // Verifica se o post já não está em andamento com outro prestador
        if (lance.Post.Status != StatusPost.Aberto)
            return BadRequest("Este post não está mais aberto para aceitar novos lances.");

        // 1. Marca o lance como aceito
        lance.IsAccepted = true;

        // 2. Atualiza o status do Post pai automaticamente para "EmAndamento" e seta o Prestador
        lance.Post.Status = StatusPost.EmAndamento;
        lance.Post.PrestadorEmNegociacaoId = lance.IdPrestadorResponsavel;

        // Opcional: Se quiser rejeitar todos os outros lances deste post automaticamente, 
        // você pode buscar os outros e colocar "IsAccepted = false", embora o padrão já seja false.

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensagem = "Lance aceito com sucesso. O post agora está em andamento.",
            LanceId = lance.Id,
            PostId = lance.IdPost
        });
    }
}