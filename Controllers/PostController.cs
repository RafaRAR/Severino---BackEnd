using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;
using Imagekit.Sdk;


namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class postController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ImageKitService _imageKitService;

    public postController(AppDbContext context, ImageKitService imageKitService)
    {
        _context = context;
        _imageKitService = imageKitService;
    }

    public record PostBody(
     string Titulo,
     string Conteudo,
     string? Endereco,
     string? Cep,
     string? Contato,
     IFormFile? Imagem,
     List<int>? TagIds
 );

    public record UpdatePostBody(
     string? Titulo,
     string? Conteudo,
     string? Endereco,
     string? Cep,
     string? Contato,
     IFormFile? Imagem,
     List<int>? TagIds
 );



    // POST: api/post/postar/2
    [HttpPost("postar/{usuarioId}")]
    public async Task<IActionResult> Postar(int usuarioId, [FromForm] PostBody dto)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Cadastro)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return BadRequest("Usuário não encontrado");

        string? imageUrl = null;

        if (dto.Imagem != null)
        {
            try
            {
                imageUrl = await _imageKitService.UploadImage(dto.Imagem);
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao fazer upload da imagem: {ex.Message}");
            }
        }

        var post = new Post
        {
            UsuarioId = usuarioId,
            Titulo = dto.Titulo,
            Conteudo = dto.Conteudo,
            DataCriacao = DateTime.UtcNow,
            ImagemUrl = imageUrl,

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

        if (dto.TagIds != null && dto.TagIds.Any())
        {
            foreach (var tagId in dto.TagIds)
            {
                var tag = await _context.Tags.FindAsync(tagId);
                if (tag != null)
                {
                    post.Tags.Add(tag);
                }
                // If not found, ignore or error? For now, ignore.
            }
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            post.Id,
            post.Titulo,
            post.Conteudo,
            post.DataCriacao,
            post.Endereco,
            post.Cep,
            post.Contato,
            post.ImagemUrl,
            NomeUsuario = usuario.Nome,
            UsuarioId = post.UsuarioId,
            Tags = post.Tags.Select(t => new { t.Id, t.Nome }).ToList()
        });
    }
    // GET: api/post/getposts
    [HttpGet("getposts")]
    public async Task<IActionResult> GetPosts()
    {
        var posts = await _context.Posts
            .Include(p => p.Usuario)
            .Include(p => p.Tags)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Conteudo,
                p.DataCriacao,
                p.Endereco,
                p.Cep,
                p.Contato,
                p.ImagemUrl,
                NomeUsuario = p.Usuario.Nome,
                UsuarioId = p.Usuario.Id,
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList()
            })
            .ToListAsync();

        return Ok(posts);
    }

    // GET: api/post/usuario/1
    [HttpGet("getposts/usuario/{usuarioId}")]
    public async Task<IActionResult> GetPostsPorUsuario(int usuarioId)
    {
        var posts = await _context.Posts
            .Where(p => p.UsuarioId == usuarioId)
            .Include(p => p.Usuario)
            .Include(p => p.Tags)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Conteudo,
                p.DataCriacao,
                p.Endereco,
                p.Cep,
                p.Contato,
                p.ImagemUrl,
                NomeUsuario = p.Usuario.Nome,
                UsuarioId = p.Usuario.Id,
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList()
            })
            .ToListAsync();

        if (!posts.Any())
            return NotFound("Nenhum post encontrado");

        return Ok(posts);
    }

    // GET: api/post/getposts/tag/1
    [HttpGet("getposts/tag/{tagId}")]
    public async Task<IActionResult> GetPostsPorTag(int tagId)
    {
        var posts = await _context.Posts
            .Where(p => p.Tags.Any(t => t.Id == tagId))
            .Include(p => p.Usuario)
            .Include(p => p.Tags)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Conteudo,
                p.DataCriacao,
                p.Endereco,
                p.Cep,
                p.Contato,
                p.ImagemUrl,
                NomeUsuario = p.Usuario.Nome,
                UsuarioId = p.Usuario.Id,
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList()
            })
            .ToListAsync();

        if (!posts.Any())
            return NotFound("Nenhum post encontrado para esta tag");

        return Ok(posts);
    }


    // PUT: api/post/editar/5
    [HttpPut("editar/{idpost}")]
    public async Task<IActionResult> EditarPost(int idpost, [FromForm] UpdatePostBody dto)
    {
        var post = await _context.Posts
            .Include(p => p.Usuario)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == idpost);

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

        if (dto.Imagem != null)
        {
            var imageUrl = await _imageKitService.UploadImage(dto.Imagem);
            post.ImagemUrl = imageUrl;
        }

        if (dto.TagIds != null)
        {
            post.Tags.Clear();
            foreach (var tagId in dto.TagIds)
            {
                var tag = await _context.Tags.FindAsync(tagId);
                if (tag != null)
                {
                    post.Tags.Add(tag);
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            post.Id,
            post.Titulo,
            post.Conteudo,
            post.DataCriacao,
            post.Endereco,
            post.Cep,
            post.Contato,
            post.ImagemUrl,
            NomeUsuario = post.Usuario.Nome,
            UsuarioId = post.UsuarioId,
            Tags = post.Tags.Select(t => new { t.Id, t.Nome }).ToList()
        });
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
            .Include(p => p.Usuario)
            .Include(p => p.Tags)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Conteudo,
                p.DataCriacao,
                p.Endereco,
                p.Cep,
                p.Contato,
                p.ImagemUrl,
                NomeUsuario = p.Usuario.Nome,
                UsuarioId = p.Usuario.Id,
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList()
            })
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound("Post não encontrado");

        return Ok(post);
    }
}