using APIseverino.Data;
using APIseverino.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace APIseverino.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Chamado pelo cliente ao abrir a tela de chat.
        /// O roomId é o Id da ChatRoom (int convertido para string).
        /// </summary>
        public async Task JoinChat(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        /// <summary>
        /// Chamado pelo cliente para enviar uma mensagem.
        /// Salva no banco e retransmite para todos no grupo.
        /// </summary>
        public async Task SendMessage(string roomId, int senderId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (!int.TryParse(roomId, out int chatRoomId))
                return;

            // Valida sala e remetente
            var room = await _context.ChatRooms.FindAsync(chatRoomId);
            if (room == null)
                return;

            var sender = await _context.Usuarios.FindAsync(senderId);
            if (sender == null)
                return;

            // Persiste a mensagem
            var chatMessage = new ChatMessage
            {
                ChatRoomId = chatRoomId,
                SenderId = senderId,
                SenderNome = sender.Nome,
                Conteudo = message,
                DataEnvio = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Retransmite para todos os membros do grupo (incluindo o remetente)
            await Clients.Group(roomId).SendAsync("ReceiveMessage", new
            {
                chatMessage.Id,
                chatMessage.ChatRoomId,
                chatMessage.SenderId,
                chatMessage.SenderNome,
                chatMessage.Conteudo,
                chatMessage.DataEnvio
            });
        }

        /// <summary>
        /// Sai do grupo ao desconectar (automático pelo SignalR, mas pode ser chamado manualmente).
        /// </summary>
        public async Task LeaveChat(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        }
    }
}