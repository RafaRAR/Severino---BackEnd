using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;
using APIseverino.Helpers;
using APIseverino.Helper;
using Microsoft.AspNetCore.Http.Metadata;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    public record RegistroBody(string Nome, string Email, string Senha);
    public record LoginBody(string Email, string Senha);

    public UsuarioController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    // ... mantendo os namespaces e construtor ...

    // POST: api/Usuario/registrar
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistroBody dto)
    {
        // Acessamos as propriedades via dto.Email, dto.Senha, etc.
        if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email já existe");

        PasswordHelper.CriarHashSenha(dto.Senha, out byte[] hash, out byte[] salt);

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            SenhaHash = hash,
            SenhaSalt = salt
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var token = TokenService.GerarToken(usuario, _config);

        return Ok(new { token });
    }

    // POST: api/Usuario/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginBody dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario == null)
            return Unauthorized("Usuário inválido");

        if (!PasswordHelper.VerificarSenha(dto.Senha, usuario.SenhaHash, usuario.SenhaSalt))
            return Unauthorized("Senha inválida");

        var token = TokenService.GerarToken(usuario, _config);

        return Ok(new { token });
    }

}