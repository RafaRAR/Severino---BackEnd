using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class postController : ControllerBase
{
    private readonly AppDbContext _context;

    public postController(AppDbContext context)
    {
        _context = context;
    }

    public record PostBody(
     string Titulo,
     string Conteudo,
     string? Endereco,
     string? Cep,
     string? Contato);

    public record UpdatePostBody(string Titulo,
    string Conteudo,
    string? Endereco,
    string? Cep,
    string? Contato);

    // POST: api/post/postar/2
    [HttpPost("postar/{usuarioId}")]
    public async Task<IActionResult> Postar(int usuarioId, [FromBody] PostBody dto)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Cadastro)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        var post = new Post
        {
            UsuarioId = usuarioId,
            Titulo = dto.Titulo,
            Conteudo = dto.Conteudo,
            DataCriacao = DateTime.UtcNow,

            Endereco = string.IsNullOrEmpty(dto.Endereco)
                ? usuario.Cadastro?.Endereco
                : dto.Endereco,

            Cep = string.IsNullOrEmpty(dto.Cep)
                ? usuario.Cadastro?.Cep
                : dto.Cep,

            Contato = string.IsNullOrEmpty(dto.Contato)
                ? usuario.Cadastro?.Contato
                : dto.Contato
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return Ok(post);
    }
    // GET: api/post/getposts
    [HttpGet("getposts")]
    public async Task<IActionResult> GetPosts()
    {
        var posts = await _context.Posts
            .Include(p => p.Usuario)
            .ThenInclude(u => u.Cadastro)
            .ToListAsync();

        return Ok(posts);
    }

    // GET: api/post/usuario/1
    [HttpGet("getposts/usuario/{usuarioId}")]
    public async Task<IActionResult> GetPostsPorUsuario(int usuarioId)
    {
        var posts = await _context.Posts
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Conteudo,
                p.DataCriacao,
                p.Endereco,
                p.Cep,
                p.Contato
            })
            .ToListAsync();

        if (!posts.Any())
            return NotFound("Nenhum post encontrado");

        return Ok(posts);
    }
    // PUT: api/post/editar/5
    [HttpPut("editar/{idpost}")]
    public async Task<IActionResult> EditarPost(int idpost, [FromBody] UpdatePostBody dto)
    {
        var post = await _context.Posts.FindAsync(idpost);

        if (post == null)
            return NotFound("Post não encontrado");

        if (!string.IsNullOrEmpty(dto.Titulo))
            post.Titulo = dto.Titulo;

        if (!string.IsNullOrEmpty(dto.Conteudo))
            post.Conteudo = dto.Conteudo;

        if (!string.IsNullOrEmpty(dto.Endereco))
            post.Endereco = dto.Endereco;

        if (!string.IsNullOrEmpty(dto.Cep))
            post.Cep = dto.Cep;

        if (!string.IsNullOrEmpty(dto.Contato))
            post.Contato = dto.Contato;

        await _context.SaveChangesAsync();

        return Ok(post);
    }

    [HttpDelete("deletarpost/{idpost}")]
    public async Task<IActionResult> DeletarPost(int idpost)
    {
        var post = await _context.Posts.FindAsync(idpost);

        if (post == null)
            return NotFound("Post não encontrado");

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return Ok("Post deletado com sucesso");
    }
    // GET: api/post/getpost/5
    [HttpGet("getpost/{idpost}")]
    public async Task<IActionResult> GetPostPorId(int idpost)
    {
        var post = await _context.Posts
            .Where(p => p.Id == idpost)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Conteudo,
                p.DataCriacao,
                p.Endereco,
                p.Cep,
                p.Contato
            })
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound("Post não encontrado");

        return Ok(post);
    }
}