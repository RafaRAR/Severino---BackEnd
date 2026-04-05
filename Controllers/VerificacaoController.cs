using APIseverino.Data;
using APIseverino.Models;
using APIseverino.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // todas as rotas exigem Bearer token
public class VerificacaoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ImageKitService _imageKitService;

    public VerificacaoController(AppDbContext context, ImageKitService imageKitService)
    {
        _context = context;
        _imageKitService = imageKitService;
    }

    public record AvaliarBody(SituacaoVerificacao Situacao);

    // Helper: lê o usuarioId do token JWT
    private int? GetUsuarioIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    // Helper: verifica se o usuário autenticado é Admin
    private async Task<bool> IsAdmin(int usuarioId)
    {
        var cadastro = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        return cadastro?.TipoUsuario == TipoUsuario.Admin;
    }

    // POST: api/verificacao/enviarverificacao/{usuarioId}
    [HttpPost("enviarverificacao/{usuarioId}")]
    public async Task<IActionResult> EnviarVerificacao(int usuarioId, IFormFile imagem)
    {
        var tokenUserId = GetUsuarioIdFromToken();
        if (tokenUserId == null || tokenUserId != usuarioId)
            return Unauthorized("Token inválido ou não pertence a este usuário.");

        var cadastro = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        if (cadastro == null)
            return BadRequest("Cadastro não encontrado");

        if (imagem == null)
            return BadRequest("Imagem é obrigatória");

        var solicitacaoPendente = await _context.Verificacoes
            .AnyAsync(v => v.UsuarioId == usuarioId && v.Situacao == SituacaoVerificacao.Aguardando);

        if (solicitacaoPendente)
            return BadRequest("Já existe uma solicitação de verificação pendente para este usuário.");

        var solicitacaoAprovada = await _context.Verificacoes
            .AnyAsync(v => v.UsuarioId == usuarioId
                        && v.Situacao == SituacaoVerificacao.Aprovado
                        && v.Cadastro.prestadorVerificado == true);

        if (solicitacaoAprovada)
            return BadRequest("Este usuário já foi verificado e aprovado.");

        string url, fileId;
        try
        {
            var upload = await _imageKitService.UploadImage(imagem);
            url = upload.url;
            fileId = upload.fileId;
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao fazer upload da imagem: {ex.Message}");
        }

        var verificacao = new Verificacao
        {
            UsuarioId = usuarioId,
            ImagemUrl = url,
            ImagemFileId = fileId,
            Situacao = SituacaoVerificacao.Aguardando,
            DataSolicitacao = DateTime.UtcNow
        };

        _context.Verificacoes.Add(verificacao);
        await _context.SaveChangesAsync();

        return Ok();
    }

    // GET: api/verificacao/getestadoverificacao/{usuarioId}
    [HttpGet("getestadoverificacao/{usuarioId}")]
    public async Task<IActionResult> GetEstadoVerificacao(int usuarioId)
    {
        var tokenUserId = GetUsuarioIdFromToken();
        if (tokenUserId == null)
            return Unauthorized("Token inválido.");

        if (tokenUserId != usuarioId && !await IsAdmin(tokenUserId.Value))
            return Forbid();

        var verificacao = await _context.Verificacoes
            .Where(v => v.UsuarioId == usuarioId)
            .OrderByDescending(v => v.DataSolicitacao)
            .Select(v => new
            {
                v.Id,
                v.UsuarioId,
                v.ImagemUrl,
                v.Situacao,
                v.DataSolicitacao,
                v.DataAvaliacao,
                UpdatedBy = v.UpdatedBy == null ? null : new
                {
                    v.UpdatedBy.Id,
                    v.UpdatedBy.Nome
                }
            })
            .FirstOrDefaultAsync();

        if (verificacao == null)
            return NotFound("Nenhuma solicitação de verificação encontrada para este usuário.");

        return Ok(verificacao);
    }

    // GET: api/verificacao/getestadoverificacaogeral  (apenas admins)
    [HttpGet("getestadoverificacaogeral")]
    public async Task<IActionResult> GetEstadoVerificacaoGeral()
    {
        var tokenUserId = GetUsuarioIdFromToken();
        if (tokenUserId == null)
            return Unauthorized("Token inválido.");

        if (!await IsAdmin(tokenUserId.Value))
            return Forbid();

        var verificacoes = await _context.Verificacoes
            .OrderByDescending(v => v.DataSolicitacao)
            .Select(v => new
            {
                v.Id,
                v.UsuarioId,
                NomeCadastro = v.Cadastro.Nome,
                v.ImagemUrl,
                v.Situacao,
                v.DataSolicitacao,
                v.DataAvaliacao,
                UpdatedBy = v.UpdatedBy == null ? null : new
                {
                    v.UpdatedBy.Id,
                    v.UpdatedBy.Nome
                }
            })
            .ToListAsync();

        return Ok(verificacoes);
    }

    // PUT: api/verificacao/avaliar/{verificacaoId}  (apenas admins)
    // AdminId vem do próprio token — não precisa mais passar no body
    [HttpPut("avaliar/{verificacaoId}")]
    public async Task<IActionResult> Avaliar(int verificacaoId, [FromBody] AvaliarBody dto)
    {
        var tokenUserId = GetUsuarioIdFromToken();
        if (tokenUserId == null)
            return Unauthorized("Token inválido.");

        if (!await IsAdmin(tokenUserId.Value))
            return Forbid();

        var verificacao = await _context.Verificacoes
            .Include(v => v.Cadastro)
            .FirstOrDefaultAsync(v => v.Id == verificacaoId);

        if (verificacao == null)
            return NotFound("Solicitação de verificação não encontrada.");

        if (verificacao.Situacao != SituacaoVerificacao.Aguardando)
            return BadRequest("Esta solicitação já foi avaliada.");

        verificacao.Situacao = dto.Situacao;
        verificacao.UpdatedById = tokenUserId.Value;
        verificacao.DataAvaliacao = DateTime.UtcNow;

        if (dto.Situacao == SituacaoVerificacao.Aprovado)
            verificacao.Cadastro.prestadorVerificado = true;

        await _context.SaveChangesAsync();

        var adminCadastro = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.UsuarioId == tokenUserId.Value);

        return Ok(new
        {
            verificacao.Id,
            verificacao.UsuarioId,
            verificacao.Situacao,
            verificacao.DataAvaliacao,
            UpdatedBy = new { Id = tokenUserId.Value, adminCadastro!.Nome }
        });
    }
}