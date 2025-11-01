using ChatBoard.Hubs;
using ChatBoard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
namespace ChatBoard.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatBoardContext _context;
        private readonly IHubContext<ChatHub> _chatHub;
        public ChatController(ChatBoardContext context, IHubContext<ChatHub> chatHub)
        {
            _context = context;
            _chatHub = chatHub;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    ViewBag.LoggedInUserName = user.DisplayName;
                    ViewBag.LoggedInUserImage = string.IsNullOrEmpty(user.ProfileImage) ? "/images/default-user.png" : user.ProfileImage;
                }
            }
            base.OnActionExecuting(context);
        }
        public IActionResult Index()
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return RedirectToAction("Login", "Account");

            // 1️⃣ One-to-one chat users
            var directChatUsers = _context.Users
                .Where(u => _context.Chats.Any(c =>
                    (c.SenderId == currentUserId && c.ReceiverId == u.UserId) ||
                    (c.ReceiverId == currentUserId && c.SenderId == u.UserId)))
                .Select(u => new ChatUserViewModel
                {
                    UserId = u.UserId,
                    DisplayName = u.DisplayName,
                    EmpCode = u.EmpCode,
                    Email = u.Email,
                    ProfileImage = string.IsNullOrEmpty(u.ProfileImage) ? "/images/default-user.png" : u.ProfileImage,
                    IsGroup = false
                })
                .ToList();

            // 2️⃣ Group chat users
            var groupIds = _context.ChatGroupMembers
                .Where(m => m.UserId == currentUserId)
                .Select(m => m.GroupId)
                .Distinct()
                .ToList();

            var groupUsers = _context.ChatGroups
                .Where(g => groupIds.Contains(g.GroupId))
                .Select(g => new ChatUserViewModel
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    GroupImage = "/images/default-user.png",
                    IsGroup = true
                })
                .ToList();

            // 3️⃣ Combine
            var allUsers = directChatUsers.Union(groupUsers).ToList();

            return View(allUsers);
        }
        [HttpGet]
        public IActionResult GetUsersByName(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Json(new List<User>());
            var users = _context.Users
                                .Where(u => u.DisplayName.ToLower().StartsWith(query.ToLower()))
                                .Select(u => new
                                {
                                    u.DisplayName,
                                    Email = u.Email,
                                    EmpCode = u.EmpCode,
                                    UserId = u.UserId,
                                    ProfileImage = string.IsNullOrEmpty(u.ProfileImage) ? "/images/default-user.png" : u.ProfileImage
                                })
                                .ToList();
            return Json(users);
        }
        [HttpGet]
        public IActionResult GetChatHistory(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Json(new { error = "Username required" });
            var receiver = _context.Users.FirstOrDefault(u => u.EmpCode == username || u.Username == username);
            if (receiver == null)
                return Json(new { error = "User not found" });
            int? senderId = HttpContext.Session.GetInt32("UserId");
            if (senderId == null)
                return Json(new { error = "Logged-in user not found" });
            int receiverId = receiver.UserId;
            var messages = _context.Chats
                .Where(c => (c.SenderId == senderId && c.ReceiverId == receiverId) ||
                            (c.SenderId == receiverId && c.ReceiverId == senderId))
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    senderName = c.SenderId == senderId ? "You" : _context.Users.FirstOrDefault(u => u.UserId == c.SenderId).DisplayName,
                    isSender = c.SenderId == senderId,
                    messageType = c.MessageType,
                    text = c.Message,
                    filePath = c.FilePath,
                    createdAt = c.CreatedAt
                }).ToList();
            return Json(messages);
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
                return BadRequest(new { error = "Message cannot be empty" });
            var sender = _context.Users.FirstOrDefault(u => u.UserId == model.SenderId);
            if (sender == null)
                return BadRequest(new { error = "Sender not found" });
            var chat = new Chat
            {
                SenderId = model.SenderId,
                ReceiverId = model.ReceiverId,
                Message = model.Message,
                MessageType = "text",
                CreatedAt = DateTime.Now
            };
            _context.Chats.Add(chat);
            _context.SaveChanges();
            var connectionId = ChatHub.GetConnectionId(model.ReceiverId);
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _chatHub.Clients.Client(connectionId)
                    .SendAsync("ReceiveMessage", sender.DisplayName, model.Message);
            }
            return Json(chat);
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, int senderId, int receiverId)
        {
            if (file != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine("wwwroot/uploads", fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }
                var chat = new Chat
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    MessageType = "file",
                    FilePath = "/uploads/" + fileName,
                    CreatedAt = DateTime.Now
                };
                _context.Chats.Add(chat);
                _context.SaveChanges();
                // Send file to recipient via SignalR
                var connectionId = ChatHub.GetConnectionId(receiverId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    //await _chatHub.Clients.Client(connectionId)
                    //    .SendAsync("ReceiveMessage", "You", chat.FilePath); // Use sender name if needed
                    await _chatHub.Clients.Client(connectionId)
                .SendAsync("ReceiveMessage", _context.Users.FirstOrDefault(u => u.UserId == senderId)?.DisplayName ?? "User", chat.FilePath);
                    //.SendAsync("ReceiveMessage", sender.DisplayName, chat.FilePath);

                }
                return Json(chat);
            }
            return BadRequest();
        }
        [HttpGet]
        public IActionResult GetChatHistoryPaged(string username, int skip = 0, int take = 20)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(username))
                return Json(new { error = "Username required" });

            var receiver = _context.Users.FirstOrDefault(u => u.EmpCode == username || u.Username == username);
            if (receiver == null)
                return Json(new { error = "User not found" });

            int? senderId = HttpContext.Session.GetInt32("UserId");
            if (senderId == null)
                return Json(new { error = "Logged-in user not found" });

            int receiverId = receiver.UserId;

            // Fetch the last 'take' messages (newest first, then reverse to oldest-first order)
            var messages = _context.Chats
                .Where(c => (c.SenderId == senderId && c.ReceiverId == receiverId) ||
                            (c.SenderId == receiverId && c.ReceiverId == senderId))
                .OrderByDescending(c => c.CreatedAt) // newest first
                .Skip(skip)
                .Take(take)
                .AsEnumerable() // switch to in-memory for reversing
                .Reverse() // show oldest first in display
                .Select(c => new
                {
                    c.ChatId,
                    senderName = c.SenderId == senderId ? "You" :
                                 _context.Users.FirstOrDefault(u => u.UserId == c.SenderId)?.DisplayName ?? "Unknown",
                    isSender = c.SenderId == senderId,
                    messageType = c.MessageType,
                    text = c.Message,
                    filePath = c.FilePath,
                    createdAt = c.CreatedAt
                })
                .ToList();

            return Json(messages);
        }
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            var users = _context.Users
                                //.Where(u => u.UserId != currentUserId)
                                .Select(u => new { u.UserId, u.DisplayName, u.EmpCode })
                                .ToList();
            return Json(users);
        }
        [HttpPost]
        public IActionResult CreateGroup([FromBody] CreateGroupModel model)
        {
            try
            {
                int? currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId == null)
                    return BadRequest(new { error = "User not logged in" });

                if (model == null || string.IsNullOrEmpty(model.GroupName) || model.MemberIds == null || model.MemberIds.Count == 0)
                    return BadRequest(new { error = "Invalid group data" });

                // ✅ Create new group
                var group = new ChatGroup
                {
                    GroupName = model.GroupName,
                    CreatedBy = currentUserId.Value,
                    CreatedAt = DateTime.Now
                };
                _context.ChatGroups.Add(group);
                _context.SaveChanges();

                // ✅ Add selected members
                foreach (var userId in model.MemberIds)
                {
                    _context.ChatGroupMembers.Add(new ChatGroupMember
                    {
                        GroupId = group.GroupId,
                        UserId = userId,
                        JoinedAt = DateTime.Now
                    });
                }

                // ✅ Add creator to group
                _context.ChatGroupMembers.Add(new ChatGroupMember
                {
                    GroupId = group.GroupId,
                    UserId = currentUserId.Value,
                    JoinedAt = DateTime.Now
                });

                _context.SaveChanges();

                // ✅ Fetch creator's name (for message text)
                var creator = _context.Users.FirstOrDefault(u => u.UserId == currentUserId.Value);
                string creatorName = creator != null ? creator.Username : "Someone";

                // ✅ Create group creation message
                string systemMessage = $"Group '{group.GroupName}' was created by {creatorName}.";

                // ✅ Add message to chat history table (so all members can see it)
                var chatMessage = new ChatMessage
                {
                    GroupId = group.GroupId,
                    SenderId = currentUserId.Value,
                    MessageText = systemMessage,
                    SentAt = DateTime.Now,
                    IsSystemMessage = true
                };

                _context.ChatMessages.Add(chatMessage);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    groupId = group.GroupId,
                    message = "Group created successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SendGroupMessage([FromBody] SendGroupMessageModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.MessageText))
                return BadRequest(new { error = "Message cannot be empty" });

            var sender = _context.Users.FirstOrDefault(u => u.UserId == model.SenderId);
            if (sender == null) return BadRequest(new { error = "Sender not found" });

            var group = _context.ChatGroups.FirstOrDefault(g => g.GroupId == model.GroupId);
            if (group == null) return BadRequest(new { error = "Group not found" });

            // Save group message
            var chatMessage = new ChatMessage
            {
                GroupId = model.GroupId,
                SenderId = model.SenderId,
                MessageText = model.MessageText,
                SentAt = DateTime.Now,
                IsSystemMessage = false
            };
            _context.ChatMessages.Add(chatMessage);
            _context.SaveChanges();

            // Send to group members via SignalR
            var memberIds = _context.ChatGroupMembers
                                .Where(m => m.GroupId == model.GroupId && m.UserId != model.SenderId)
                                .Select(m => m.UserId)
                                .ToList();

            foreach (var memberId in memberIds)
            {
                var connectionId = ChatHub.GetConnectionId(memberId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _chatHub.Clients.Client(connectionId)
                        .SendAsync("ReceiveMessage", sender.DisplayName, model.MessageText, chatMessage.MessageId, false);
                }
            }

            return Json(new { ChatId = chatMessage.MessageId, MessageText = chatMessage.MessageText });
        }
        //[HttpGet]
        //public IActionResult GetGroupChatHistory(int groupId)
        //{
        //    try
        //    {
        //        var messages = _context.ChatMessages
        //            .Where(m => m.GroupId == groupId)
        //            .OrderBy(m => m.SentAt)
        //            .Select(m => new
        //            {
        //                chatId = m.MessageId,
        //                senderName = m.SenderId == HttpContext.Session.GetInt32("UserId") ? "You" :
        //                             _context.Users.FirstOrDefault(u => u.UserId == m.SenderId).DisplayName,
        //                text = m.MessageText,
        //                isSender = m.SenderId == HttpContext.Session.GetInt32("UserId")
        //            }).ToList();

        //        return Json(messages);
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}
        [HttpGet]
        public IActionResult GetGroupChatHistory(int groupId, int skip = 0, int take = 20)
        {
            try
            {
                int? currentUserId = HttpContext.Session.GetInt32("UserId");

                var messages = (from m in _context.ChatMessages
                                join u in _context.Users on m.SenderId equals u.UserId into userJoin
                                from user in userJoin.DefaultIfEmpty()
                                where m.GroupId == groupId
                                orderby m.SentAt
                                select new
                                {
                                    m.MessageId,
                                    m.SenderId,
                                    DisplayName = user != null ? user.DisplayName : null,
                                    m.MessageText,
                                    m.SentAt,
                                    m.MessageType, // ✅ include message type
                                    m.FilePath     // ✅ include file path (if you have a column)
                                })
                                .Skip(skip)
                                .Take(take)
                                .AsEnumerable() // ✅ switch to C# evaluation
                                .Select(m => new
                                {
                                    chatId = m.MessageId,
                                    senderName = m.SenderId == currentUserId ? "You" : (m.DisplayName ?? "Unknown"),
                                    text = m.MessageText ?? string.Empty,
                                    isSender = m.SenderId == currentUserId,
                                    messageType = string.IsNullOrEmpty(m.MessageType) ? "text" : m.MessageType, // ✅ always return messageType
                                    filePath = m.FilePath // ✅ file link
                                })
                                .ToList();

                return Json(messages);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //[HttpGet]
        //public IActionResult GetGroupChatHistory(int groupId)
        //{
        //    try
        //    {
        //        int? currentUserId = HttpContext.Session.GetInt32("UserId");

        //        var messages = (from m in _context.ChatMessages
        //                        join u in _context.Users on m.SenderId equals u.UserId into userJoin
        //                        from user in userJoin.DefaultIfEmpty()
        //                        where m.GroupId == groupId
        //                        orderby m.SentAt
        //                        select new
        //                        {
        //                            chatId = m.MessageId,
        //                            senderName = m.SenderId == currentUserId ? "You" : (user.DisplayName ?? "Unknown"),
        //                            text = m.MessageText ?? string.Empty,
        //                            isSender = m.SenderId == currentUserId
        //                        }).ToList();

        //        return Json(messages);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> UploadGroupFile(IFormFile file, int senderId, int groupId)
        {
            try
            {
                if (file == null || groupId <= 0)
                    return BadRequest(new { error = "Invalid file or group." });

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine("wwwroot/uploads", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                var chatMessage = new ChatMessage
                {
                    GroupId = groupId,
                    SenderId = senderId,
                    MessageText = null,
                    SentAt = DateTime.Now,
                    IsSystemMessage = false,
                    MessageType = "file",
                    FilePath = "/uploads/" + fileName
                };

                _context.ChatMessages.Add(chatMessage);
                _context.SaveChanges();

                // Notify all members except sender
                var memberIds = _context.ChatGroupMembers
                    .Where(m => m.GroupId == groupId && m.UserId != senderId)
                    .Select(m => m.UserId)
                    .ToList();

                foreach (var memberId in memberIds)
                {
                    var connectionId = ChatHub.GetConnectionId(memberId);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _chatHub.Clients.Client(connectionId)
                            .SendAsync("ReceiveMessage",
                                _context.Users.FirstOrDefault(u => u.UserId == senderId)?.DisplayName ?? "User",
                                "/uploads/" + fileName,
                                chatMessage.MessageId,
                                false,
                                "file"
                            );
                    }
                }

                return Json(new
                {
                    chatMessage.MessageId,
                    chatMessage.FilePath,
                    chatMessage.MessageType
                });
            }
            catch (Exception ex)
            {

                throw;
            }
        }

    }
}