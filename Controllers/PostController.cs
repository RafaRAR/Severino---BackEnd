using APIseverino.Data;
using APIseverino.Models;
using Imagekit.Sdk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ImageKitService _imageKitService;

    public PostController(AppDbContext context, ImageKitService imageKitService)
    {
        _context = context;
        _imageKitService = imageKitService;
    }

    public record PostBody(
        string Titulo,
        string Conteudo,
        string Role,
        string? Endereco,
        string? Cep,
        string? Contato,
        IFormFile? Imagem,
        bool Impulsionar,
        List<int>? TagIds
    );

    public record UpdatePostBody(
        string? Titulo,
        string Role,
        string? Conteudo,
        string? Endereco,
        string? Cep,
        string? Contato,
        IFormFile? Imagem,
        bool Impulsionar,
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
            Role = dto.Role,
            DataCriacao = DateTime.UtcNow,
            ImagemUrl = imageUrl,
            Impulsionar = dto.Impulsionar,

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
            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.Id))
                .ToListAsync();
            
            post.Tags = tags;
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
            post.UsuarioId,
            post.Impulsionar,
            NomeUsuario = usuario.Nome,
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
                p.Role,
                p.Contato,
                p.Impulsionar,

                // imagem do post
                p.ImagemUrl,

                UsuarioId = p.Usuario.Id,
                NomeUsuario = p.Usuario.Nome,
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList(),

                Cadastro = p.Usuario.Cadastro == null ? null : new
                {
                    p.Usuario.Cadastro.Nome,
                    p.Usuario.Cadastro.Cpf,
                    p.Usuario.Cadastro.DataNascimento,
                    p.Usuario.Cadastro.Endereco,
                    p.Usuario.Cadastro.Cep,
                    p.Usuario.Cadastro.Contato,
                    p.Usuario.Cadastro.ImagemUrl
                }
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
                p.Role,
                p.Contato,
                p.Impulsionar,
                p.ImagemUrl,

                UsuarioId = p.Usuario.Id,
                NomeUsuario = p.Usuario.Nome,
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList(),

                Cadastro = p.Usuario.Cadastro == null ? null : new
                {
                    p.Usuario.Cadastro.Nome,
                    p.Usuario.Cadastro.Cpf,
                    p.Usuario.Cadastro.DataNascimento,
                    p.Usuario.Cadastro.Endereco,
                    p.Usuario.Cadastro.Cep,
                    p.Usuario.Cadastro.Contato,
                    p.Usuario.Cadastro.ImagemUrl
                }
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
                p.Impulsionar,
                UsuarioId = p.Usuario.Id,
                NomeUsuario = p.Usuario.Nome,
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
        
        post.Impulsionar = dto.Impulsionar;

        if (dto.Imagem != null)
        {
            var imageUrl = await _imageKitService.UploadImage(dto.Imagem);
            post.ImagemUrl = imageUrl;
        }

        if (dto.TagIds != null)
        {
            post.Tags.Clear();
            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.Id))
                .ToListAsync();
            
            post.Tags = tags;
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
            post.Impulsionar,
            post.UsuarioId,
            NomeUsuario = post.Usuario.Nome,
            Tags = post.Tags.Select(t => new { t.Id, t.Nome }).ToList(),
            Comentarios = post.Comentarios.Select(c => new
            {
                c.Id,
                c.Conteudo,
                c.DataCriacao,
                NomeUsuario = c.Usuario.Nome
            }).ToList()
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
                p.Role,
                p.Contato,
                p.ImagemUrl,
                p.Impulsionar,

                p.UsuarioId,
                NomeUsuario = p.Usuario.Nome,
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList(),

                Cadastro = p.Usuario.Cadastro == null ? null : new
                {
                    p.Usuario.Cadastro.Nome,
                    p.Usuario.Cadastro.Cpf,
                    p.Usuario.Cadastro.DataNascimento,
                    p.Usuario.Cadastro.Endereco,
                    p.Usuario.Cadastro.Cep,
                    p.Usuario.Cadastro.Contato,

                    // IMAGEM DO CADASTRO (foto do perfil)
                    p.Usuario.Cadastro.ImagemUrl
                },
                Comentarios = p.Comentarios.Select(c => new
                {
                    c.Id,
                    c.Conteudo,
                    c.DataCriacao,
                    NomeUsuario = c.Usuario.Nome
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound("Post não encontrado");

        return Ok(post);
    }
}