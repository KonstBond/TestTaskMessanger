using TestTaskMessanger.DTO;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using TestTaskMessanger.Dbl.Repository;
using System.Data.SqlTypes;
using TestTaskMessanger.Dbl.Exceptions;
using Npgsql;
using TestTaskMessanger.Models;
using TestTaskMessanger.Utils;
using System;

namespace TestTaskMessanger.Hubs
{
    public class MessangerHub : Hub<IMessangerHub>
    {
        private readonly MesMemoryCache _cache;
        private readonly IMessangerRepository _repository;
        private readonly ILogger<MessangerHub> _logger;

        private const string BOT_RECIEVER = "AdminBot";

        public MessangerHub(MesMemoryCache cache, ILogger<MessangerHub> logger, IMessangerRepository repository)
        {
            _cache = cache;
            _logger = logger;
            _repository = repository;
        }

        public async Task CreateUser(UserModel userModel)
        {
            if (await _repository.CreateNewUserAsync(userModel.Username!, Cipner.Encode(userModel.Password!)))
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Hello in my app, [{userModel.Username}]");
            else
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"User with name: [{userModel.Username}] already exist");
        }
        public async Task JoinChat(UserConnectionModel userConnectionModel)
        {
            try
            {
                string? chat = (await _repository.GetChatAsync(userConnectionModel.Chat!)).ChatName;
                string? username = (await _repository.GetUserByPassAsync(
                    userConnectionModel.Username!,
                    Cipner.Encode(userConnectionModel.Password!)))
                    .Username;

                if (_cache.TryGetValue(Context.ConnectionId, out string? oldChat))
                {
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You already in chat: [{oldChat}]");
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, chat!);
                _cache.Set(Context.ConnectionId, chat);
                await Clients.Group(chat!).ReceiveMessage(BOT_RECIEVER, $"[{username}] has been joined to chat: [{chat}]");
            }
            catch (NpgsqlException ex)
            {
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Problem with Messanger :(");
                _logger.LogCritical($"{ex.Message}\n{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("Chat"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Chat [{userConnectionModel.Chat}] not found");
                if (ex.Message.Contains("User"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"User [{userConnectionModel.Username}] not found");
            }
        }
        public async Task LeaveChat(UserDisconectionModel userDisconectionModel)
        {
            try
            {
                string? username = (await _repository.GetUserAsync(userDisconectionModel.Username!)).Username;

                if (_cache.TryGetValue(Context.ConnectionId, out string? chat))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, chat!);
                    _cache.Remove(Context.ConnectionId);

                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You left from chat: [{chat}]");
                    await Clients.Group(chat!).ReceiveMessage(BOT_RECIEVER, $"[{username}] has left the chat: [{chat}]");
                }
                else
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You're not in any chat");
            }
            catch (SqlNotFilledException ex)
            {
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Problem with Messanger :(");
                _logger.LogCritical($"{ex.Message}\n{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("User"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"User [{userDisconectionModel.Username}] not found");
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
                _logger.LogCritical($"{ex.Message}\n{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("User"))
                    await Clients.User(Context.UserIdentifier!).ReceiveMessage(BOT_RECIEVER, $"User [{sendingMessageDto.Username}] not found");
            }
        }
        public async Task CreateChat(ChatModel chatModel)
        {
            try
            {
                string? username = (await _repository.GetUserAsync(chatModel.Username!)).Username;
                string password = Cipner.Encode(chatModel.Password!);

                if (_cache.TryGetValue(Context.ConnectionId, out string? chat))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, chat!);
                    _cache.Remove(Context.ConnectionId);

                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You left from chat: [{chat}]");
                    await Clients.Group(chat!).ReceiveMessage(BOT_RECIEVER, $"[{username}] has left the chat: [{chat}]");
                }

                if (await _repository.CreateNewChatAsync(username!, password, chatModel.Chat!))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, chatModel.Chat!);
                    _cache.Set(Context.ConnectionId, chatModel.Chat!);

                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You have created a new chat [{chatModel.Chat!}] and you are the admin of this chat");
                }
                else
                {
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Chat with name [{chatModel.Chat!}] already created");
                    return;
                }
            }
            catch (SqlNotFilledException ex)
            {
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Problem with Messanger :(");
                _logger.LogCritical($"{ex.Message}\n{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("User"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"User {chatModel.Username} not found");
            }
        }
        public async Task DeleteChat(ChatModel chatModel)
        {
            try
            {
                string password = Cipner.Encode(chatModel.Password!);
                var user = await _repository.GetUserByPassAsync(chatModel.Username!, password);
                var chat = await _repository.GetChatAsync(chatModel.Chat!);

                if (!chat.AdminId.Equals(user.Id))
                {
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"$You are not admin of chat: [{chat.ChatName!}]");
                    return;
                }    

                await Clients.Group(chat.ChatName!).ReceiveMessage(BOT_RECIEVER, $"Chat \"{chat.ChatName!}\" has been deleted by admin");

                foreach (var connectionId in _cache.GetKeys())
                {
                    if (_cache.TryGetValue(connectionId, out string? ch) && ch == chat.ChatName!)
                    {
                        await Groups.RemoveFromGroupAsync((string)connectionId, ch!);
                        _cache.Remove(connectionId);                        
                    }
                }

                if (await _repository.RemoveChatAsync(chat.ChatName!))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"You have deleted chat [{chat.ChatName!}]");
                else
                    throw new NpgsqlException("Chat not been deleted");

            }
            catch (NpgsqlException ex)
            {
                await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Problem with Messanger :(");
                _logger.LogCritical($"{ex.Message}\n{ex.StackTrace}");
            }
            catch (NotFoundException ex)
            {
                if (ex.Message.Contains("User"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"User [{chatModel.Username}] not found");
                if (ex.Message.Contains("Chat"))
                    await Clients.Caller.ReceiveMessage(BOT_RECIEVER, $"Chat [{chatModel.Chat}] not found");
            }
        }
    }
}
