using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class cadastroController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ImageKitService _imageKitService;

    public cadastroController(AppDbContext context, ImageKitService imageKitService)
    {
        _context = context;
        _imageKitService = imageKitService;
    }

    public record CadastroBody(
        string? Nome,
        IFormFile? Imagem,
        int UsuarioId,
        string Cpf,
        DateTime DataNascimento,
        string Contato,
        string Cep,
        string Endereco,
        string Role
    );

    public record UpdateCadastroBody(
        string? Nome,
        IFormFile? Imagem,
        string? Cpf,
        DateTime? DataNascimento,
        string? Contato,
        string? Cep,
        string? Endereco,
        string? Role
    );

    // POST: api/cadastro/cadastrar/2
    [HttpPost("cadastrar/{usuarioid}")]
    public async Task<IActionResult> Cadastrar(int usuarioid, [FromForm] CadastroBody dto)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioid);

        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        var cpfExistente = await _context.Cadastros
            .AnyAsync(c => c.Cpf == dto.Cpf);

        if (cpfExistente)
            return BadRequest("CPF já cadastrado");

        string? imageUrl = null;

        if (dto.Imagem != null)
        {
            imageUrl = await _imageKitService.UploadImage(dto.Imagem);
        }

        var cadastro = new Cadastro
        {
            UsuarioId = usuarioid,
            Nome = string.IsNullOrEmpty(dto.Nome) ? usuario.Nome : dto.Nome,
            ImagemUrl = imageUrl,
            Cpf = dto.Cpf,
            DataNascimento = dto.DataNascimento.ToString("yyyy-MM-dd"),
            Contato = dto.Contato,
            Cep = dto.Cep,
            Endereco = dto.Endereco,
            Role = dto.Role
        };

        _context.Cadastros.Add(cadastro);
        await _context.SaveChangesAsync();

        return Ok(cadastro);
    }

    // GET: api/cadastro/getcadastro/1
    [HttpGet("getcadastro/{usuarioid}")]
    public async Task<IActionResult> GetCadastro(int usuarioid)
    {
        var cadastro = await _context.Cadastros
            .Include(c => c.Usuario)
            .Where(c => c.UsuarioId == usuarioid)
            .Select(c => new
            {
                c.Id,
                c.UsuarioId,
                c.Nome,
                c.ImagemUrl,
                c.Cpf,
                c.DataNascimento,
                c.Contato,
                c.Cep,
                c.Endereco,
                c.Role
            })
            .FirstOrDefaultAsync();

        if (cadastro == null)
            return NotFound("Cadastro não encontrado");

        return Ok(cadastro);
    }

    // PUT: api/cadastro/updatecadastro/2
    [HttpPut("updatecadastro/{usuarioid}")]
    public async Task<IActionResult> EditarCadastro(int usuarioid, [FromForm] UpdateCadastroBody dto)
    {
        var cadastro = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioid);

        if (cadastro == null)
            return NotFound("Cadastro não encontrado");

        if (!string.IsNullOrEmpty(dto.Nome))
            cadastro.Nome = dto.Nome;

        if (!string.IsNullOrEmpty(dto.Cpf))
            cadastro.Cpf = dto.Cpf;

        if (dto.DataNascimento.HasValue)
            cadastro.DataNascimento = dto.DataNascimento.Value.ToString("yyyy-MM-dd");

        if (!string.IsNullOrEmpty(dto.Contato))
            cadastro.Contato = dto.Contato;

        if (!string.IsNullOrEmpty(dto.Cep))
            cadastro.Cep = dto.Cep;

        if (!string.IsNullOrEmpty(dto.Endereco))
            cadastro.Endereco = dto.Endereco;

        if (!string.IsNullOrEmpty(dto.Role))
            cadastro.Role = dto.Role;

        if (dto.Imagem != null)
        {
            var imageUrl = await _imageKitService.UploadImage(dto.Imagem);
            cadastro.ImagemUrl = imageUrl;
        }

        await _context.SaveChangesAsync();

        return Ok(cadastro);
    }
}