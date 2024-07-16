using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data.SqlTypes;
using TestTaskMessanger.Dbl.Data;
using TestTaskMessanger.Dbl.Data.Entities;
using TestTaskMessanger.Dbl.Exceptions;
using static System.Net.Mime.MediaTypeNames;

namespace TestTaskMessanger.Dbl.Repository
{
    public class MessangerRepository : IMessangerRepository
    {
        private readonly MessangerDbContext _dbContext;

        public MessangerRepository(MessangerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> AddMessageAsync(string username, string chat, string text)
        {
            var chatEntity = await GetChatAsync(chat);
            var userEntity = await GetUserAsync(username);

            if (chatEntity == null || userEntity == null)
                return false;

            await _dbContext.AddAsync(new MessageEntity
            {
                Chat = chatEntity,
                Sender = userEntity,
                SendingTime = DateTime.UtcNow,
                Text = text
            });

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateNewChat(string username, string password, string chat)
        {
            var userEntity = await GetUserByPassAsync(username, password);
            var chatEntity = await GetChatAsync(chat);

            if (chatEntity != null)
                return false;

            await _dbContext.AddAsync(new ChatEntity
            {
                Admin = userEntity,
                ChatName = chat,
            });

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<ChatEntity> GetChatAsync(string chat)
        {
            if (!_dbContext.Chats.Any())
                throw new NpgsqlException("There are no chats in the database");

            try
            {
                return await (from ch in _dbContext.Chats
                          where ch.ChatName == chat
                          select ch).FirstOrDefaultAsync() ?? throw new NotFoundException("Chat not Found");
            }
            catch (NotFoundException ex) { throw ex; }
        }

        public async Task<UserEntity> GetUserByPassAsync(string username, string password)
        {
            if (!await _dbContext.Users.AnyAsync())
                throw new NpgsqlException("There are no users in the database");

            try
            {
                return await (from user in _dbContext.Users
                              where user.Username == username && user.Password == password
                              select user).FirstOrDefaultAsync() ?? throw new NotFoundException("User not Found");
            }
            catch (NotFoundException ex) { throw ex; } 
        }

        public async Task<UserEntity> GetUserAsync(string username) =>
            await _dbContext.Users.FirstOrDefaultAsync(user => user.Username == username) ?? throw new NotFoundException("User not Found");

        public async Task<bool> RemoveChatAsync(string chat)
        {
            var chatEntity = await GetChatAsync(chat);

            if (chatEntity.ChatName != chat)
                return false;

            _dbContext.Remove(chatEntity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
