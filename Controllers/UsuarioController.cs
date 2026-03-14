﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;
using APIseverino.Helpers;
using APIseverino.Helper;
using System.Security.Cryptography;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]


public class UsuarioController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public record RegistroBody(string Nome, string Email, string Senha);
    public record LoginBody(string Email, string Senha);
    public record VerifyBody(string Email, string Codigo);
    public record EmailBody(string Email);
    public record ResetBody(string Email, string Codigo, string NovaSenha);
    public record ConfirmDeleteBody(string Codigo);


    public UsuarioController(AppDbContext context, IConfiguration config, IEmailService emailService)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
    }

    // POST: api/usuario/registrar
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistroBody dto)
    {
        var emailExiste = await _context.Usuarios
            .AnyAsync(u => u.Email == dto.Email);
        
        if (emailExiste)
        {
            var emailConfirmado = await _context.Usuarios
                .Where(u => u.Email == dto.Email)
                .Select(u => u.EmailConfirmado)
                .FirstOrDefaultAsync();
            if (emailConfirmado)
                return BadRequest("Email já existe");
            else
                return Ok(new { message = "Usuário não verificado" });
        }

        PasswordHelper.CriarHashSenha(dto.Senha, out byte[] hash, out byte[] salt);

        var code = RandomNumberGenerator
            .GetInt32(100000, 999999)
            .ToString();

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            SenhaHash = hash,
            SenhaSalt = salt,
            EmailConfirmado = false,
            CodigoVerificacao = code,
            ExpiracaoVerificacao = DateTime.UtcNow.AddMinutes(30)
        };

        try
        {
            await _emailService.EnviarCodigo(
                usuario.Email,
                code,
                "Seu código de verificação é:",
                "Verificação de conta"
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Falha ao enviar email: {ex.Message}");
        }

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Usuário criado. Código de verificação enviado para o email."
        });
    }

    // POST: api/usuario/solicitarverificacao
    [HttpPost("solicitarverificacao")]
    public async Task<IActionResult> SolicitarVerificacao([FromBody] EmailBody dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        if (usuario.EmailConfirmado)
            return BadRequest("Email já confirmado");

        // Reutiliza código existente se ainda válido, senão gera um novo
        string code;
        if (!string.IsNullOrEmpty(usuario.CodigoVerificacao) && usuario.ExpiracaoVerificacao.HasValue && usuario.ExpiracaoVerificacao.Value > DateTime.UtcNow)
        {
            code = usuario.CodigoVerificacao;
        }
        else
        {
            code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            usuario.CodigoVerificacao = code;
            usuario.ExpiracaoVerificacao = DateTime.UtcNow.AddMinutes(30);
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }

        try
        {
            await _emailService.EnviarCodigo(usuario.Email, code, "Seu código de verificação é:", "Verifique seu e-mail");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Falha ao enviar email: {ex.Message}");
        }

        return Ok(new { message = "Código de verificação enviado para o email." });
    }

    // POST: api/usuario/verificar
    [HttpPost("verificar")]
    public async Task<IActionResult> Verificar([FromBody] VerifyBody dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        if (usuario.EmailConfirmado)
            return BadRequest("Email já confirmado");

        if (usuario.CodigoVerificacao == null || usuario.ExpiracaoVerificacao == null)
            return BadRequest("Nenhum código foi gerado para esse usuário");

        if (DateTime.UtcNow > usuario.ExpiracaoVerificacao.Value)
            return BadRequest("Código expirado");

        if (usuario.CodigoVerificacao != dto.Codigo)
            return BadRequest("Código inválido");

        usuario.EmailConfirmado = true;
        usuario.CodigoVerificacao = null;
        usuario.ExpiracaoVerificacao = null;

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        var token = TokenService.GerarToken(usuario, _config);
        return Ok(new { token, usuario.Id });
    }

    // POST: api/usuario/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginBody dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario == null)
            return Unauthorized("Usuário inválido");

        if (!PasswordHelper.VerificarSenha(dto.Senha, usuario.SenhaHash, usuario.SenhaSalt))
            return Unauthorized("Senha inválida");

        if (!usuario.EmailConfirmado)
            return Ok(new { message = "Usuário não verificado" });
        
        var token = TokenService.GerarToken(usuario, _config);

        return Ok(new { token, usuario.Id });
    }

    // POST: api/Usuario/solicitarreset
    [HttpPost("solicitarreset")]
    public async Task<IActionResult> SolicitarReset([FromBody] EmailBody dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        usuario.CodigoResetSenha = code;
        usuario.ExpiracaoResetSenha = DateTime.UtcNow.AddMinutes(30);

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.EnviarCodigo(usuario.Email, code, "Use este código para alterar sua senha:", "Altere sua senha");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Falha ao enviar email: {ex.Message}");
        }

        return Ok(new { message = "Código de reset enviado para o email." });
    }

    // POST: api/usuario/resetar
    [HttpPost("resetar")]
    public async Task<IActionResult> Resetar([FromBody] ResetBody dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        if (usuario.CodigoResetSenha == null || usuario.ExpiracaoResetSenha == null)
            return BadRequest("Nenhum código foi gerado para esse usuário");

        if (DateTime.UtcNow > usuario.ExpiracaoResetSenha.Value)
            return BadRequest("Código expirado");

        if (usuario.CodigoResetSenha != dto.Codigo)
            return BadRequest("Código inválido");

        // Atualiza senha
        PasswordHelper.CriarHashSenha(dto.NovaSenha, out byte[] hash, out byte[] salt);
        usuario.SenhaHash = hash;
        usuario.SenhaSalt = salt;

        // Limpa códigos de reset
        usuario.CodigoResetSenha = null;
        usuario.ExpiracaoResetSenha = null;

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Senha atualizada com sucesso." });
    }

    // POST: api/usuario/deletar/id
    [HttpPost("deletar/{Id}")]
    public async Task<IActionResult> Deletar(int Id)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == Id);

        if (usuario == null)
            return NotFound("Usuário não encontrado");

        // Gera código de confirmação de exclusão e salva no DB
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        usuario.CodigoDelete = code;
        usuario.ExpiracaoDelete = DateTime.UtcNow.AddMinutes(30);

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.EnviarCodigo(
                usuario.Email,
                code,
                "Use este código para confirmar a exclusão da sua conta:",
                "Pedido para excluir a conta"
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Falha ao enviar email de confirmação: {ex.Message}");
        }

        return Ok(new { message = "Email de confirmação enviado. Verifique sua caixa de entrada." });
    }

    // POST: api/usuario/confirmardelete
    [HttpPost("confirmardelete/{Id}")]
    public async Task<IActionResult> ConfirmarDelete(int Id, [FromBody] ConfirmDeleteBody body)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Cadastro)
            .Include(u => u.Posts)
            .FirstOrDefaultAsync(u => u.Id == Id);

        if (usuario == null)
            return NotFound("Usuário não encontrado");

        if (string.IsNullOrEmpty(usuario.CodigoDelete) || !usuario.ExpiracaoDelete.HasValue)
            return BadRequest("Nenhum código de exclusão foi gerado para esse usuário");

        if (DateTime.UtcNow > usuario.ExpiracaoDelete.Value)
            return BadRequest("Código de exclusão expirado");

        if (usuario.CodigoDelete != body.Codigo)
            return BadRequest("Código inválido");

        // Remove entidades relacionadas manualmente (se necessário)
        if (usuario.Cadastro != null)
        {
            _context.Cadastros.Remove(usuario.Cadastro);
        }

        if (usuario.Posts != null && usuario.Posts.Count > 0)
        {
            _context.Posts.RemoveRange(usuario.Posts);
        }

        // Remove o usuário
        _context.Usuarios.Remove(usuario);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/usuario/getidbyemail
    [HttpPost("getidbyemail")]
    public async Task<IActionResult> GetIdByEmail([FromBody] EmailBody dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario == null)
            return NotFound("Usuário não encontrado");

        return Ok(new { usuario.Id });
    }

    // GET: api/usuario/all
    [HttpGet("getall")]
    public async Task<IActionResult> GetAllUsers()
    {
        var list = await _context.Usuarios
            .Select(u => new { u.Id, u.Nome, u.Email })
            .ToListAsync();

        return Ok(list);
    }
}