using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;
using System.Text.RegularExpressions; // Adicionar para usar Regex
using APIseverino.Helpers;
using APIseverino.Models.Enums; // Adicionar para usar CpfHelper

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CadastroController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ImageKitService _imageKitService;

    public CadastroController(AppDbContext context, ImageKitService imageKitService)
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
        TipoUsuario TipoUsuario
    );

    public record UpdateCadastroBody(
        string? Nome,
        IFormFile? Imagem,
        string? Cpf,
        DateTime? DataNascimento,
        string? Contato,
        string? Cep,
        string? Endereco,
        TipoUsuario TipoUsuario
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

        // Validação de CPF
        if (!CpfHelper.IsValidCpf(dto.Cpf))
            return BadRequest("CPF inválido.");

        // Validação de Data de Nascimento no futuro
        if (dto.DataNascimento > DateTime.Today)
            return BadRequest("A data de nascimento não pode ser uma data futura.");

        // Validação de idade mínima (18 anos)
        DateTime birthDate = dto.DataNascimento;
        int age = DateTime.Today.Year - birthDate.Year;
        if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;

        if (age < 18)
            return BadRequest("O usuário deve ter no mínimo 18 anos.");

        string? imageUrl = null;

        if (dto.Imagem != null)
        {
            var (url, _) = await _imageKitService.UploadImage(dto.Imagem);
            imageUrl = url;
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
            TipoUsuario = dto.TipoUsuario
        };

        _context.Cadastros.Add(cadastro);
        await _context.SaveChangesAsync();

        cadastro = await _context.Cadastros
            .Include(c => c.Usuario)
            .Where(c => c.Id == cadastro.Id)
            .Select(c => new Cadastro
            {
                Id = c.Id,
                UsuarioId = c.UsuarioId,
                Nome = c.Nome,
                ImagemUrl = c.ImagemUrl,
                Cpf = c.Cpf,
                DataNascimento = c.DataNascimento,
                Contato = c.Contato,
                Cep = c.Cep,
                Endereco = c.Endereco,
                TipoUsuario = c.TipoUsuario,
                prestadorVerificado = c.prestadorVerificado
            })
            .FirstOrDefaultAsync();

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
                c.TipoUsuario,
                c.prestadorVerificado
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
        {
            // Validação de CPF ao atualizar
            if (!CpfHelper.IsValidCpf(dto.Cpf))
                return BadRequest("CPF inválido.");

            // Validação de unicidade do CPF ao atualizar (se for diferente do atual)
            if (cadastro.Cpf != dto.Cpf)
            {
                var cpfExistente = await _context.Cadastros
                    .AnyAsync(c => c.Cpf == dto.Cpf && c.UsuarioId != usuarioid);
                if (cpfExistente)
                    return BadRequest("CPF já cadastrado para outro usuário.");
            }
            cadastro.Cpf = dto.Cpf;
        }

        if (dto.DataNascimento.HasValue)
        {
            // Validação de Data de Nascimento no futuro
            if (dto.DataNascimento.Value > DateTime.Today)
                return BadRequest("A data de nascimento não pode ser uma data futura.");

            // Validação de idade mínima (18 anos) ao atualizar
            DateTime birthDate = dto.DataNascimento.Value;
            int age = DateTime.Today.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;

            if (age < 18)
                return BadRequest("O usuário deve ter no mínimo 18 anos.");
            cadastro.DataNascimento = dto.DataNascimento.Value.ToString("yyyy-MM-dd");
        }

        if (!string.IsNullOrEmpty(dto.Contato))
        {
            cadastro.Contato = dto.Contato;
        }

        if (!string.IsNullOrEmpty(dto.Cep))
            cadastro.Cep = dto.Cep;

        if (!string.IsNullOrEmpty(dto.Endereco))
            cadastro.Endereco = dto.Endereco;

        if (dto.Imagem != null)
        {
            var (url, _) = await _imageKitService.UploadImage(dto.Imagem);
            cadastro.ImagemUrl = url;
        }

        if (dto.TipoUsuario != cadastro.TipoUsuario)
            cadastro.TipoUsuario = dto.TipoUsuario;

        await _context.SaveChangesAsync();

        cadastro = await _context.Cadastros
            .Include(c => c.Usuario)
            .Where(c => c.Id == cadastro.Id)
            .Select(c => new Cadastro
            {
                Id = c.Id,
                UsuarioId = c.UsuarioId,
                Nome = c.Nome,
                ImagemUrl = c.ImagemUrl,
                Cpf = c.Cpf,
                DataNascimento = c.DataNascimento,
                Contato = c.Contato,
                Cep = c.Cep,
                Endereco = c.Endereco,
                TipoUsuario = c.TipoUsuario,
                prestadorVerificado = c.prestadorVerificado
            })
            .FirstOrDefaultAsync();

        return Ok(cadastro);
    }
}