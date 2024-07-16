using TestTaskMessanger.DTO;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using TestTaskMessanger.Dbl.Repository;
using System.Data.SqlTypes;
using TestTaskMessanger.Dbl.Exceptions;
using Npgsql;
using TestTaskMessanger.Models;

namespace TestTaskMessanger.Hubs
{
    public class MessangerHub : Hub<IMessangerHub>
    {
        private readonly IMemoryCache _cache;
        private readonly IMessangerRepository _repository;
        private readonly ILogger<MessangerHub> _logger;

        private const string BOT_RECIEVER = "AdminBot";

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
                string? chat = (await _repository.GetChatAsync(userConnectionModel.Chat!)).ChatName;
                string? username = (await _repository.GetUserByPassAsync(userConnectionModel.Username!, userConnectionModel.Password!)).Username;

                if (_cache.TryGetValue(Context.ConnectionId, out string? oldChat))
                {
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You already in chat: {oldChat}");
                    return;
                }

                if (chat != null && username != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, chat!);
                    _cache.Set(Context.ConnectionId, chat);

                    await Clients.Group(chat!).ReceiveMessage(BOT_RECIEVER, $"{username} has been joined to chat: {chat}");
                } 
            }
            catch (SqlNotFilledException ex)
            {
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Problem with Messanger :(");
                _logger.LogCritical($"{ex.Message}" + "\n" + $"{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("Chat"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Chat {userConnectionModel.Chat} not found");
                if (ex.Message.Contains("User"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"User {userConnectionModel.Username} not found");
            }
        }

        public async Task LeaveChat(UserDisconectionModel userDisconectionModel)
        {
            try
            {
                var username = (await _repository.GetUserAsync(userDisconectionModel.Username!)).Username;

                if (_cache.TryGetValue(Context.ConnectionId, out string? chat))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, chat!);
                    _cache.Remove(Context.ConnectionId);

                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You left from chat: {chat}");
                    await Clients.Group(chat!).ReceiveMessage(BOT_RECIEVER, $"{username} has left the chat: {chat}");
                }
                else
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You're not in any chat rooms.");
            }
            catch (SqlNotFilledException ex)
            {
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Problem with Messanger :(");
                _logger.LogCritical($"{ex.Message}" + "\n" + $"{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("User"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"User {userDisconectionModel.Username} not found");
            }
        }

        public async Task Send(MessageModel sendingMessageDto)
        {
            try
            {
                string? username = (await _repository.GetUserAsync(sendingMessageDto.Username!)).Username;
                string? text = sendingMessageDto.Message;
                
                if (!_cache.TryGetValue(Context.ConnectionId, out string? chat))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You're not in any chat rooms.");

                if (await _repository.AddMessageAsync(username!, chat!, text!))
                {
                    await Clients.Group(chat!).ReceiveMessage(username!, text!);
                }     
                else
                    throw new NpgsqlException("New message dont be added to DB");
            }
            catch (NpgsqlException ex)
            {
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Problem with Messanger :(");
                _logger.LogCritical($"{ex.Message}" + "\n" + $"{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("User"))
                    await Clients.User(Context.UserIdentifier!).ReceiveMessage(BOT_RECIEVER, $"User {sendingMessageDto.Username} not found");
            }
        }
    }
}
