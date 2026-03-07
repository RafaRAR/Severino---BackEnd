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

    public cadastroController(AppDbContext context)
    {
        _context = context;
    }

    public record CadastroBody(
        string? Nome,
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
    string? Cpf,
    DateTime? DataNascimento,
    string? Contato,
    string? Cep,
    string? Endereco,
    string? Role
);

    // POST: api/cadastro/cadastrar/2
    [HttpPost("cadastrar/{usuarioid}")]
    public async Task<IActionResult> Cadastrar(int usuarioid, [FromBody] CadastroBody dto)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioid);

        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        var cadastro = new Cadastro
        {
            UsuarioId = usuarioid,

            Nome = string.IsNullOrEmpty(dto.Nome)
                ? usuario.Nome
                : dto.Nome,

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
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioid);

        if (cadastro == null)
            return NotFound("Cadastro não encontrado");

        return Ok(cadastro);
    }

    [HttpPut("updatecadastro/{usuarioid}")]
    public async Task<IActionResult> EditarCadastro(int usuarioid, [FromBody] UpdateCadastroBody dto)
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

        await _context.SaveChangesAsync();

        return Ok(cadastro);
    }
}