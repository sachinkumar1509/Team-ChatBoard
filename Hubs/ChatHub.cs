using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
namespace ChatBoard.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<int, string> _connections = new ConcurrentDictionary<int, string>();
        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().Session.GetInt32("UserId");
            if (userId != null)
            {
                _connections[userId.Value] = Context.ConnectionId;
            }
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var userId = _connections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId != 0)
                _connections.TryRemove(userId, out _);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessageToUser(int receiverId, string message, int senderId)
        {
            if (_connections.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message);
            }
        }
        // Helper for controller
        public static string GetConnectionId(int userId)
        {
            _connections.TryGetValue(userId, out string connectionId);
            return connectionId;
        }
    }
}