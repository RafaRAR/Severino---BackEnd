using APIseverino.Data;
using APIseverino.Hubs;
using APIseverino.Models;
using APIseverino.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace APIseverino.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(AppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        public record AbrirChatBody(int PostId, int ClienteId, int PrestadorId);
        public record AceitarPropostaBody(int PrestadorId);
        public record ConfirmarAcaoBody(int RoomId, int UsuarioId);

        // ─────────────────────────────────────────────────────────────────────────────
        // POST: api/chat/abrir
        // Cria ou recupera a sala de chat entre um cliente e um prestador para um post.
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirChat([FromBody] AbrirChatBody dto)
        {
            var post = await _context.Posts.FindAsync(dto.PostId);
            if (post == null)
                return NotFound("Post não encontrado");

            if (post.Status == StatusPost.Concluido || post.Status == StatusPost.Expirado)
                return BadRequest($"Este post está {post.Status} e não aceita novas conversas.");

            var cliente = await _context.Usuarios.FindAsync(dto.ClienteId);
            if (cliente == null)
                return NotFound("Cliente não encontrado");

            var prestador = await _context.Usuarios.FindAsync(dto.PrestadorId);
            if (prestador == null)
                return NotFound("Prestador não encontrado");

            // Verifica se já existe sala para esse trio
            var sala = await _context.ChatRooms
                .FirstOrDefaultAsync(r =>
                    r.PostId == dto.PostId &&
                    r.ClienteId == dto.ClienteId &&
                    r.PrestadorId == dto.PrestadorId);

            if (sala == null)
            {
                sala = new ChatRoom
                {
                    PostId = dto.PostId,
                    ClienteId = dto.ClienteId,
                    PrestadorId = dto.PrestadorId,
                    DataCriacao = DateTime.UtcNow
                };

                _context.ChatRooms.Add(sala);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                sala.Id,
                sala.PostId,
                sala.ClienteId,
                NomeCliente = cliente.Nome,
                sala.PrestadorId,
                NomePrestador = prestador.Nome,
                sala.DataCriacao
            });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // GET: api/chat/history/{roomId}
        // Retorna o histórico de mensagens de uma sala (para recarregar ao abrir o app).
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpGet("history/{roomId}")]
        public async Task<IActionResult> GetHistory(int roomId)
        {
            var sala = await _context.ChatRooms.FindAsync(roomId);
            if (sala == null)
                return NotFound("Sala não encontrada");

            var mensagens = await _context.ChatMessages
                .Where(m => m.ChatRoomId == roomId)
                .OrderBy(m => m.DataEnvio)
                .Select(m => new
                {
                    m.Id,
                    m.ChatRoomId,
                    m.SenderId,
                    m.SenderNome,
                    m.Conteudo,
                    m.DataEnvio
                })
                .ToListAsync();

            return Ok(mensagens);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // GET: api/chat/salas/usuario/{usuarioId}
        // Retorna todas as salas em que o usuário participa (como cliente ou prestador).
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpGet("salas/usuario/{usuarioId}")]
        public async Task<IActionResult> GetSalasDoUsuario(int usuarioId)
        {
            var salas = await _context.ChatRooms
                .Where(r => r.ClienteId == usuarioId || r.PrestadorId == usuarioId)
                .Include(r => r.Post)
                .Include(r => r.Cliente)
                .Include(r => r.Prestador)
                .OrderByDescending(r => r.DataCriacao)
                .Select(r => new
                {
                    r.Id,
                    r.PostId,
                    TituloPost = r.Post.Titulo,
                    StatusPost = r.Post.Status,
                    r.ClienteId,
                    NomeCliente = r.Cliente.Nome,
                    r.PrestadorId,
                    NomePrestador = r.Prestador.Nome,
                    r.ClienteConfirmou,
                    r.PrestadorConfirmou,
                    r.DataCriacao,
                    LanceAtual = r.Post.Comentarios
                .Where(c => c.UsuarioId == r.PrestadorId)
                .Select(c => c.ValorDeLance)
                .FirstOrDefault(),
                    LanceId = r.Post.Comentarios
                .Where(c => c.UsuarioId == r.PrestadorId)
                .Select(c => c.Id)
                .FirstOrDefault(),
                    UltimaMensagem = r.Mensagens
                        .OrderByDescending(m => m.DataEnvio)
                        .Select(m => new { m.Conteudo, m.DataEnvio, m.SenderNome })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(salas);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // PUT: api/chat/post/{postId}/aceitarproposta
        // Cliente aceita a proposta de um prestador:
        //   - Muda status do post para EmAndamento
        //   - Registra qual prestador está negociando
        //   - Dispara evento SignalR na sala avisando o prestador
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPut("post/{postId}/aceitarproposta")]
        public async Task<IActionResult> AceitarProposta(int postId, [FromBody] AceitarPropostaBody dto)
        {
            var post = await _context.Posts
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound("Post não encontrado");

            if (post.Status != StatusPost.Aberto)
                return BadRequest($"Não é possível aceitar proposta: post está com status '{post.Status}'.");

            var prestador = await _context.Usuarios.FindAsync(dto.PrestadorId);
            if (prestador == null)
                return NotFound("Prestador não encontrado");

            // Atualiza o post
            post.Status = StatusPost.EmAndamento;
            post.PrestadorEmNegociacaoId = dto.PrestadorId;
            await _context.SaveChangesAsync();

            // Busca a sala correspondente para disparar o evento SignalR
            var sala = await _context.ChatRooms
                .FirstOrDefaultAsync(r =>
                    r.PostId == postId &&
                    r.ClienteId == post.UsuarioId &&
                    r.PrestadorId == dto.PrestadorId);

            if (sala != null)
            {
                await _hubContext.Clients.Group(sala.Id.ToString())
                    .SendAsync("PropostaAceita", new
                    {
                        postId,
                        mensagem = $"O cliente {post.Usuario.Nome} aceitou sua proposta! Serviço em andamento.",
                        novoStatus = post.Status.ToString()
                    });
            }

            return Ok(new
            {
                post.Id,
                post.Titulo,
                Status = post.Status.ToString(),
                post.PrestadorEmNegociacaoId,
                NomePrestador = prestador.Nome
            });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // PUT: api/chat/post/{postId}/concluir
        // Marca o serviço como Concluído.
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPut("post/{postId}/concluir")]
        public async Task<IActionResult> ConcluirServico(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound("Post não encontrado");

            if (post.Status != StatusPost.EmAndamento)
                return BadRequest("O serviço precisa estar Em Andamento para ser concluído.");

            post.Status = StatusPost.Concluido;
            post.PrestadorEmNegociacaoId = null;
            await _context.SaveChangesAsync();

            // Avisa todos na sala via SignalR
            var sala = await _context.ChatRooms
                .FirstOrDefaultAsync(r => r.PostId == postId);

            if (sala != null)
            {
                await _hubContext.Clients.Group(sala.Id.ToString())
                    .SendAsync("ServicoConcluido", new
                    {
                        postId,
                        mensagem = "O serviço foi marcado como Concluído!",
                        novoStatus = StatusPost.Concluido.ToString()
                    });
            }

            return Ok(new { post.Id, Status = post.Status.ToString() });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // PUT: api/chat/post/{postId}/abortar
        // Aborta a negociação: volta o post para Aberto e reinicia o prazo de 30 dias.
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPut("post/{postId}/abortar")]
        public async Task<IActionResult> AbortarNegociacao(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound("Post não encontrado");

            if (post.Status != StatusPost.EmAndamento)
                return BadRequest("O post precisa estar Em Andamento para ser abortado.");

            var prestadorAnteriorId = post.PrestadorEmNegociacaoId;

            // Volta para Aberto e reinicia o prazo
            post.Status = StatusPost.Aberto;
            post.PrestadorEmNegociacaoId = null;
            post.DataExpiracao = DateTime.UtcNow.AddDays(30);
            await _context.SaveChangesAsync();

            // Avisa todos na sala via SignalR
            var sala = await _context.ChatRooms
                .FirstOrDefaultAsync(r =>
                    r.PostId == postId &&
                    r.PrestadorId == prestadorAnteriorId);

            if (sala != null)
            {
                await _hubContext.Clients.Group(sala.Id.ToString())
                    .SendAsync("NegociacaoAbortada", new
                    {
                        postId,
                        mensagem = "A negociação foi encerrada. O anúncio voltou para Aberto.",
                        novoStatus = StatusPost.Aberto.ToString(),
                        novaDataExpiracao = post.DataExpiracao
                    });
            }

            return Ok(new
            {
                post.Id,
                Status = post.Status.ToString(),
                post.DataExpiracao
            });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // PUT: api/chat/room/confirmar
        // Registra a confirmação do usuário (cliente ou prestador).
        // Quando os dois confirmarem, transiciona o status do post automaticamente.
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpPut("room/confirmar")]
        public async Task<IActionResult> ConfirmarAcao([FromBody] ConfirmarAcaoBody cab)
        {
            var room = await _context.ChatRooms.Include(r => r.Post).FirstOrDefaultAsync(r => r.Id == cab.RoomId);
            if (room == null) return NotFound();

            bool isCliente = cab.UsuarioId == room.ClienteId;
            if (isCliente) room.ClienteConfirmou = true;
            else if (cab.UsuarioId == room.PrestadorId) room.PrestadorConfirmou = true;
            else return Forbid();

            string ator = isCliente ? "Cliente" : "Profissional";
            string acao = room.Post.Status == StatusPost.Aberto ? "aceitou" : "concluiu";

            var msg1 = new ChatMessage { ChatRoomId = room.Id, SenderId = cab.UsuarioId, SenderNome = "Sistema", Conteudo = $"✅ O {ator} {acao}", DataEnvio = DateTime.UtcNow };
            _context.ChatMessages.Add(msg1);

            bool ambosConfirmaram = room.ClienteConfirmou && room.PrestadorConfirmou;
            ChatMessage msg2 = null;

            if (ambosConfirmaram)
            {
                if (room.Post.Status == StatusPost.Aberto)
                {
                    room.Post.Status = StatusPost.EmAndamento;
                    room.Post.PrestadorEmNegociacaoId = room.PrestadorId;
                    msg2 = new ChatMessage { ChatRoomId = room.Id, SenderId = cab.UsuarioId, SenderNome = "Sistema", Conteudo = "🚀 Serviço em Andamento", DataEnvio = DateTime.UtcNow };
                }
                else if (room.Post.Status == StatusPost.EmAndamento)
                {
                    room.Post.Status = StatusPost.Concluido;
                    room.Post.PrestadorEmNegociacaoId = null;
                    msg2 = new ChatMessage { ChatRoomId = room.Id, SenderId = cab.UsuarioId, SenderNome = "Sistema", Conteudo = "🏁 Serviço concluído!", DataEnvio = DateTime.UtcNow };
                }
                if (msg2 != null) _context.ChatMessages.Add(msg2);
            }

            await _context.SaveChangesAsync();

            var group = _hubContext.Clients.Group(cab.RoomId.ToString());
            await group.SendAsync("StatusNegociacaoAtualizado", new { clienteConfirmou = room.ClienteConfirmou, prestadorConfirmou = room.PrestadorConfirmou, postStatus = room.Post.Status.ToString() });
            await group.SendAsync("ReceiveMessage", msg1);
            if (msg2 != null) await group.SendAsync("ReceiveMessage", msg2);

            if (ambosConfirmaram)
            {
                room.ClienteConfirmou = false; room.PrestadorConfirmou = false;
                await _context.SaveChangesAsync();
            }

            return Ok(new { AmbosConfirmaram = ambosConfirmaram, room.ClienteConfirmou, room.PrestadorConfirmou, PostStatus = room.Post.Status.ToString() });
        }
    }
}