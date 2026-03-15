using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;
using Imagekit.Sdk;

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
        string Conteudo
    );

    public record UpdateComentarioBody(
        string Conteudo
    );

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

        var comentario = new Comentario
        {
            UsuarioId = usuarioId,
            PostId = dto.PostId,
            Conteudo = dto.Conteudo,
            DataCriacao = DateTime.UtcNow
        };

        _context.Comentarios.Add(comentario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            NomeUsuario = usuario.Nome,
            comentario.Conteudo
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
                NomeUsuario = c.Usuario.Nome,
                c.Conteudo
            })
            .ToListAsync();

        if (!comentarios.Any())
            return NotFound("Nenhum comentário encontrado para este post");

        return Ok(comentarios);
    }

    // GET: /api/post/comentario/getcomentario/{id}
    [HttpGet("getcomentario/{comentarioId}")]
    public async Task<IActionResult> GetComentarioPorId(int comentarioId)
    {
        var comentario = await _context.Comentarios
            .Where(c => c.Id == comentarioId)
            .Include(c => c.Usuario)
            .Select(c => new
            {
                NomeUsuario = c.Usuario.Nome,
                c.Conteudo
            })
            .FirstOrDefaultAsync();

        if (comentario == null)
            return NotFound("Comentário não encontrado");

        return Ok(comentario);
    }

    // PUT: /api/post/comentario/editarcomentario/{id}
    [HttpPut("editarcomentario/{comentarioId}")]
    public async Task<IActionResult> EditarComentario(int id, [FromBody] UpdateComentarioBody dto)
    {
        var comentario = await _context.Comentarios
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comentario == null)
            return NotFound("Comentário não encontrado");

        if (!string.IsNullOrEmpty(dto.Conteudo))
            comentario.Conteudo = dto.Conteudo;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            NomeUsuario = comentario.Usuario.Nome,
            comentario.Conteudo
        });
    }

    // DELETE: /api/post/comentario/{id}
    [HttpDelete("comentario/{comentarioId}")]
    public async Task<IActionResult> DeletarComentario(int id)
    {
        var comentario = await _context.Comentarios.FindAsync(id);

        if (comentario == null)
            return NotFound("Comentário não encontrado");

        _context.Comentarios.Remove(comentario);
        await _context.SaveChangesAsync();

        return Ok("Comentário deletado com sucesso");
    }
}