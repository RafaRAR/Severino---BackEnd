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

    public class BuscarPostBody
    {
        public string Termo { get; set; } = string.Empty;

    }

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
        string? imageFileId = null;

        if (dto.Imagem != null)
        {
            try
            {
                var upload = await _imageKitService.UploadImage(dto.Imagem);
                imageUrl = upload.url;
                imageFileId = upload.fileId;
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
            ImagemFileId = imageFileId,
            Impulsionar = dto.Impulsionar,
            Endereco = string.IsNullOrEmpty(dto.Endereco) ? usuario.Cadastro?.Endereco : dto.Endereco,
            Cep = string.IsNullOrEmpty(dto.Cep) ? usuario.Cadastro?.Cep : dto.Cep,
            Contato = string.IsNullOrEmpty(dto.Contato) ? usuario.Cadastro?.Contato : dto.Contato
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

        return Ok(post);
    }

    // GET: api/post/getposts?page=1
    [HttpGet("getposts")]
    public async Task<IActionResult> GetPosts(int page = 1)
    {
        const int pageSize = 50;

        var posts = await _context.Posts
            .Include(p => p.Usuario)
            .Include(p => p.Tags)
            .OrderByDescending(p => p.DataCriacao) // 🔥 importante
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            // 🔥 deleta imagem antiga
            if (!string.IsNullOrEmpty(post.ImagemFileId))
            {
                await _imageKitService.DeleteImage(post.ImagemFileId);
            }

            var upload = await _imageKitService.UploadImage(dto.Imagem);

            post.ImagemUrl = upload.url;
            post.ImagemFileId = upload.fileId;
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
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == idpost);

        if (post == null)
            return NotFound("Post não encontrado");

        // 🔥 deleta imagem no ImageKit
        if (!string.IsNullOrEmpty(post.ImagemFileId))
        {
            await _imageKitService.DeleteImage(post.ImagemFileId);
        }

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

  [HttpPost("getpost/buscar")]
  public async Task<IActionResult> Buscar(
[FromBody] BuscarPostBody body,
[FromQuery] int page = 1,
[FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(body.Termo))
            return BadRequest(new
            {
                message = "O termo de busca não pode ser vazio."
            });

        var stopWords = new[]
        {
        "de", "da", "do", "das", "dos",
        "com", "sem", "para", "por",
        "no", "na", "nos", "nas",
        "a", "o", "e", "em"
    };

        var palavras = body.Termo
            .ToLower()
            .Trim()
            .Split(" ", StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !stopWords.Contains(p))
            .ToArray();

        if (palavras.Length == 0)
        {
            return BadRequest(new
            {
                message = "Digite termos válidos para busca."
            });
        }

        // 🔥 INCLUDE COMPLETO
        var query = _context.Posts
            .Include(p => p.Usuario)
                .ThenInclude(u => u.Cadastro)
            .Include(p => p.Tags)
            .Include(p => p.Comentarios)
                .ThenInclude(c => c.Usuario)
            .Where(p =>
                palavras.Any(palavra =>
                    EF.Functions.ILike(p.Titulo, "%" + palavra + "%") ||
                    EF.Functions.ILike(p.Conteudo, "%" + palavra + "%") ||
                    p.Tags.Any(t => EF.Functions.ILike(t.Nome, "%" + palavra + "%"))
                )
            );

        var total = await query.CountAsync();

        var resultados = await query
            .OrderByDescending(p => p.DataCriacao)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.Conteudo,
                p.Role,
                p.ImagemUrl,
                p.DataCriacao,
                p.Endereco,
                p.Cep,
                p.Contato,
                p.Impulsionar,

                // 🔥 USUARIO
                p.UsuarioId,
                NomeUsuario = p.Usuario.Nome,

                // 🔥 TAGS COMPLETAS
                Tags = p.Tags.Select(t => new { t.Id, t.Nome }).ToList(),

                // 🔥 CADASTRO
                Cadastro = p.Usuario.Cadastro == null ? null : new
                {
                    p.Usuario.Cadastro.Nome,
                    p.Usuario.Cadastro.Cpf,
                    p.Usuario.Cadastro.DataNascimento,
                    p.Usuario.Cadastro.Endereco,
                    p.Usuario.Cadastro.Cep,
                    p.Usuario.Cadastro.Contato,
                    p.Usuario.Cadastro.ImagemUrl
                },

            })
            .ToListAsync();

        var final = resultados
            .Select(p =>
            {
                var tituloPalavras = (p.Titulo ?? "")
                    .ToLower()
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries);

                var conteudoPalavras = (p.Conteudo ?? "")
                    .ToLower()
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries);

                var tagsPalavras = p.Tags
                    .Select(t => t.Nome.ToLower())
                    .ToList();

                return new
                {
                    p.Id,
                    p.Titulo,
                    p.Conteudo,
                    p.Role,
                    p.ImagemUrl,
                    p.DataCriacao,
                    p.Endereco,
                    p.Cep,
                    p.Contato,
                    p.Impulsionar,
                    p.Tags,
                    p.Cadastro,
                   

                    p.UsuarioId,
                    p.NomeUsuario,

                    Score =
                        palavras.Count(palavra =>
                            tituloPalavras.Contains(palavra)) * 2 +

                        palavras.Count(palavra =>
                            conteudoPalavras.Contains(palavra)) +

                        palavras.Count(palavra =>
                            tagsPalavras.Contains(palavra)) * 3
                };
            })
            .Where(p => p.Score > 0)
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Role)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = final
        });
    }
}