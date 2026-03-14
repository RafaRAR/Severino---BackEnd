using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIseverino.Data;
using APIseverino.Models;

namespace APIseverino.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TagController : ControllerBase
{
    private readonly AppDbContext _context;

    public TagController(AppDbContext context)
    {
        _context = context;
    }

    public record CreateTagBody(string Nome);
    public record UpdateTagBody(string Nome);

    // GET: api/tag
    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _context.Tags
            .Select(t => new { t.Id, t.Nome })
            .ToListAsync();
        return Ok(tags);
    }

    // POST: api/tag
    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagBody dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest("Nome da tag é obrigatório");

        var existing = await _context.Tags.FirstOrDefaultAsync(t => t.Nome == dto.Nome);
        if (existing != null)
            return BadRequest("Tag já existe");

        var tag = new Tag { Nome = dto.Nome };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return Ok(new { tag.Id, tag.Nome });
    }

    // PUT: api/tag/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagBody dto)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null)
            return NotFound("Tag não encontrada");

        if (string.IsNullOrWhiteSpace(dto.Nome))
            return BadRequest("Nome da tag é obrigatório");

        var existing = await _context.Tags.FirstOrDefaultAsync(t => t.Nome == dto.Nome && t.Id != id);
        if (existing != null)
            return BadRequest("Tag já existe");

        tag.Nome = dto.Nome;
        await _context.SaveChangesAsync();
        return Ok(new { tag.Id, tag.Nome });
    }

    // DELETE: api/tag/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null)
            return NotFound("Tag não encontrada");

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return Ok("Tag deletada com sucesso");
    }
}
