using APIseverino.Data;
using APIseverino.Migrations;
using APIseverino.Models;
using Imagekit.Sdk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIseverino.Controllers;

[Route("api/Post/[controller]")]
[ApiController]
public class ComentarioController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComentarioController(AppDbContext context)
    {
        _context = context;
    }

    public record ComentarioBody(
        int PostId,
        string Conteudo,
        decimal ValorDeLance
    );

    public record UpdateComentarioBody(
        string Conteudo,
        decimal ValorDeLance
    );

    // DTO para retornar informações do usuário com Id e Nome
    public record UsuarioDto(int Id, string Nome);

    // POST: /api/post/comentario/comentar/{usuarioId}
    [HttpPost("comentar/{usuarioId}")]
    public async Task<IActionResult> Comentar(int usuarioId, [FromBody] ComentarioBody dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        var post = await _context.Posts.FindAsync(dto.PostId);
        if (post == null)
            return BadRequest("Post não encontrado");

        var comentario = new Models.Comentario
        {
            UsuarioId = usuario.Id,
            PostId = dto.PostId,
            Conteudo = dto.Conteudo,
            DataCriacao = DateTime.UtcNow,
            ValorDeLance = dto.ValorDeLance
        };

        _context.Comentarios.Add(comentario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            comentario.Id,
            Usuario = new UsuarioDto(usuario.Id, usuario.Nome),
            comentario.Conteudo,
            comentario.ValorDeLance
        });
    }

    // GET: /api/post/comentario/getcomentarioporpost/{postId}
    [HttpGet("getcomentarioporpost/{postId}")]
    public async Task<IActionResult> GetComentariosPorPost(int postId)
    {
        var comentarios = await _context.Comentarios
            .Where(c => c.PostId == postId)
            .Include(c => c.Usuario)
            .OrderBy(c => c.DataCriacao)
            .Select(c => new
            {
                c.Id,
                Usuario = new UsuarioDto(c.Usuario.Id, c.Usuario.Nome),
                c.Conteudo,
                c.ValorDeLance
            })
            .ToListAsync();

        if (!comentarios.Any())
            return NotFound("Nenhum comentário encontrado para este post");

        return Ok(comentarios);
    }

    // GET: /api/post/comentario/getcomentario/{comentarioId}
    [HttpGet("getcomentario/{comentarioId}")]
    public async Task<IActionResult> GetComentarioPorId(int comentarioId)
    {
        var comentario = await _context.Comentarios
            .Where(c => c.Id == comentarioId)
            .Include(c => c.Usuario)
            .Select(c => new
            {
                c.Id,
                Usuario = new UsuarioDto(c.Usuario.Id, c.Usuario.Nome),
                c.Conteudo,
                c.ValorDeLance
            })
            .FirstOrDefaultAsync();

        if (comentario == null)
            return NotFound("Comentário não encontrado");

        return Ok(comentario);
    }

    // PUT: /api/post/comentario/editarcomentario/{comentarioId}
    [HttpPut("editarcomentario/{comentarioId}")]
    public async Task<IActionResult> EditarComentario(int comentarioId, [FromBody] UpdateComentarioBody dto)
    {
        var comentario = await _context.Comentarios
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Id == comentarioId);

        if (comentario == null)
            return NotFound("Comentário não encontrado");

        bool atualizado = false;

        // Atualiza Conteudo se foi fornecido e é diferente
        if (!string.IsNullOrEmpty(dto.Conteudo) && dto.Conteudo != comentario.Conteudo)
        {
            comentario.Conteudo = dto.Conteudo;
            atualizado = true;
        }

        // Atualiza ValorDeLance somente se mudou
        if (dto.ValorDeLance != comentario.ValorDeLance && dto.ValorDeLance > 0)
        {
            comentario.ValorDeLance = dto.ValorDeLance;
            atualizado = true;
        }

        if (atualizado)
            await _context.SaveChangesAsync();

        return Ok(new
        {
            Usuario = comentario.Usuario != null ? new UsuarioDto(comentario.Usuario.Id, comentario.Usuario.Nome) : null,
            comentario.Conteudo,
            comentario.ValorDeLance,
            Atualizado = atualizado
        });
    }

    // DELETE: /api/post/comentario/{comentarioId}
    [HttpDelete("comentario/{comentarioId}")]
    public async Task<IActionResult> DeletarComentario(int comentarioId)
    {
        var comentario = await _context.Comentarios.FindAsync(comentarioId);

        if (comentario == null)
            return NotFound("Comentário não encontrado");

        _context.Comentarios.Remove(comentario);
        await _context.SaveChangesAsync();

        return Ok("Comentário deletado com sucesso");
    }
}