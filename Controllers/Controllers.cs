using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;
using APIseverino.Helpers;
using APIseverino.Helper;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public UsuarioController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // POST: api/usuario/registrar
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar(string nome, string email, string senha)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Email == email))
            return BadRequest("Email já existe");

        PasswordHelper.CriarHashSenha(senha, out byte[] hash, out byte[] salt);

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = hash,
            SenhaSalt = salt
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok("Usuário criado");
    }

    // POST: api/usuario/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string senha)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email);

        if (usuario == null)
            return Unauthorized("Usuário inválido");

        if (!PasswordHelper.VerificarSenha(senha, usuario.SenhaHash, usuario.SenhaSalt))
            return Unauthorized("Senha inválida");

        var token = TokenService.GerarToken(usuario, _config);

        return Ok(new { token });
    }
}