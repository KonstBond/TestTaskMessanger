using TestTaskMessanger.DTO;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using TestTaskMessanger.Hubs;
using TestTaskMessanger.Dbl.Repository;
using System.Data.SqlTypes;
using TestTaskMessanger.Dbl.Exceptions;

namespace Messanger.Hubs
{
    public class MessangerHub : Hub<IMessangerHub>
    {
        private readonly IMemoryCache _cache;
        private readonly IMessangerRepository _repository;
        private readonly ILogger<MessangerHub> _logger;

        public MessangerHub(IMemoryCache cache, ILogger<MessangerHub> logger, IMessangerRepository repository)
        {
            _cache = cache;
            _logger = logger;
            _repository = repository;
        }

        public async Task JoinChat(UserConnectionModel userConnectionModel)
        {
            try
            {
                await _repository.GetChatAsync(userConnectionModel.Chat);
                await _repository.GetUserAsync(userConnectionModel.Username, userConnectionModel.Password);
            }
            catch (SqlNotFilledException ex)
            {
                await Clients.User(Context.UserIdentifier).ReceiveMessage("AdminBot", $"Problem with Messanger :(");

                _logger.LogCritical($"{ex.Message}" + "\n" + $"{ex.StackTrace}");
                return;
            }
            catch(NotFoundException ex)
            {
                if (ex.Message.Contains("Chat"))
                    await Clients.User(Context.UserIdentifier).ReceiveMessage("AdminBot", $"Chat {userConnectionModel.Chat} not found");
                if (ex.Message.Contains("User"))
                    await Clients.User(Context.UserIdentifier).ReceiveMessage("AdminBot", $"User {userConnectionModel.Username} not found");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, userConnectionModel.Chat);
            _cache.Set(Context.ConnectionId, userConnectionModel.Chat);
            _cache.Set(Context.User.Identities, true);
            await Clients.Group(userConnectionModel.Chat).ReceiveMessage(userConnectionModel.Username, "has been joined");
            _logger.LogInformation($"{userConnectionModel.Username} has been joinded to chat: {userConnectionModel.Chat}");
        }

        public async Task Send(MessageModel sendingMessageDto)
        {
            string chat = _cache.Get(Context.ConnectionId)?.ToString()!;
            _cache.TryGetValue<bool>(Context.User.Identities, out bool inChat);
            try
            {
                if (inChat)
                {
                    await Clients.Group(chat).ReceiveMessage(sendingMessageDto.Username, sendingMessageDto.Message);
                }
                else
                {
                    await Clients.User(sendingMessageDto.Username).ReceiveMessage("AdminBot", "");
                }
                
            }
            catch (ArgumentNullException)
            {
                _logger.LogInformation($"User {sendingMessageDto.Username} not joined to chat");
            }
        }
    }
}
