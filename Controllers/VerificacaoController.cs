using APIseverino.Data;
using APIseverino.Models;
using APIseverino.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VerificacaoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ImageKitService _imageKitService;

    public VerificacaoController(AppDbContext context, ImageKitService imageKitService)
    {
        _context = context;
        _imageKitService = imageKitService;
    }

    public record AvaliarBody(int AdminId, SituacaoVerificacao Situacao);

    // POST: api/verificacao/enviarverificacao/{cadastroId}
    [HttpPost("enviarverificacao/{cadastroId}")]
    public async Task<IActionResult> EnviarVerificacao(int cadastroId, IFormFile imagem)
    {
        var cadastro = await _context.Cadastros.FindAsync(cadastroId);
        if (cadastro == null)
            return BadRequest("Cadastro não encontrado");

        if (imagem == null)
            return BadRequest("Imagem é obrigatória");

        var solicitacaoPendente = await _context.Verificacoes
            .AnyAsync(v => v.CadastroId == cadastroId && v.Situacao == SituacaoVerificacao.Aguardando);

        if (solicitacaoPendente)
            return BadRequest("Já existe uma solicitação de verificação pendente para este cadastro");

        var solicitacaoAprovada = await _context.Verificacoes
            .AnyAsync(v => v.CadastroId == cadastroId && v.Situacao == SituacaoVerificacao.Aprovado && v.Cadastro.prestadorVerificado == true);

        if (solicitacaoAprovada)
            return BadRequest("Este cadastro já foi verificado e aprovado");

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
            CadastroId = cadastroId,
            ImagemUrl = url,
            ImagemFileId = fileId,
            Situacao = SituacaoVerificacao.Aguardando,
            DataSolicitacao = DateTime.UtcNow
        };

        _context.Verificacoes.Add(verificacao);
        await _context.SaveChangesAsync();

        return Ok();
    }

    // GET: api/verificacao/getestadoverificacao/{cadastroId}
    [HttpGet("getestadoverificacao/{cadastroId}")]
    public async Task<IActionResult> GetEstadoVerificacao(int cadastroId)
    {
        var verificacao = await _context.Verificacoes
            .Where(v => v.CadastroId == cadastroId)
            .OrderByDescending(v => v.DataSolicitacao)
            .Select(v => new
            {
                v.Id,
                v.CadastroId,
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
            return NotFound("Nenhuma solicitação de verificação encontrada para este cadastro");

        return Ok(verificacao);
    }

    // GET: api/verificacao/getestadoverificacaogeral
    [HttpGet("getestadoverificacaogeral")]
    public async Task<IActionResult> GetEstadoVerificacaoGeral()
    {
        var verificacoes = await _context.Verificacoes
            .OrderByDescending(v => v.DataSolicitacao)
            .Select(v => new
            {
                v.Id,
                v.CadastroId,
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

    // PUT: api/verificacao/avaliar/{verificacaoId}
    [HttpPut("avaliar/{verificacaoId}")]
    public async Task<IActionResult> Avaliar(int verificacaoId, [FromBody] AvaliarBody dto)
    {
        var verificacao = await _context.Verificacoes
            .Include(v => v.Cadastro)
            .FirstOrDefaultAsync(v => v.Id == verificacaoId);

        if (verificacao == null)
            return NotFound("Solicitação de verificação não encontrada");

        if (verificacao.Situacao != SituacaoVerificacao.Aguardando)
            return BadRequest("Esta solicitação já foi avaliada");

        var admin = await _context.Cadastros.FindAsync(dto.AdminId);
        if (admin == null)
            return BadRequest("Admin não encontrado");

        if (admin.TipoUsuario != TipoUsuario.Admin)
            return Unauthorized("Apenas admins podem avaliar verificações");

        verificacao.Situacao = dto.Situacao;
        verificacao.UpdatedById = dto.AdminId;
        verificacao.DataAvaliacao = DateTime.UtcNow;

        if (dto.Situacao == SituacaoVerificacao.Aprovado)
            verificacao.Cadastro.prestadorVerificado = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            verificacao.Id,
            verificacao.CadastroId,
            verificacao.Situacao,
            verificacao.DataAvaliacao,
            UpdatedBy = new { admin.Id, admin.Nome }
        });
    }
}