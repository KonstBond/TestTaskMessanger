using Microsoft.EntityFrameworkCore;
using Npgsql;
using TestTaskMessanger.Dbl.Data;
using TestTaskMessanger.Dbl.Data.Entities;
using TestTaskMessanger.Dbl.Exceptions;

namespace TestTaskMessanger.Dbl.Repository
{
    public class MessangerRepository : IMessangerRepository
    {
        private readonly MessangerDbContext _dbContext;

        public MessangerRepository(MessangerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CreateNewUserAsync(string username, string password)
        {
            if (await _dbContext.Users.FirstOrDefaultAsync(us => us.Username == username) != null)
                return false;

            _dbContext.Add(new UserEntity
            {
                Username = username,
                Password = password
            });
            _dbContext.SaveChanges();
            return true;
        }

        public async Task<bool> AddMessageAsync(string username, string chat, string text)
        {
            var chatEntity = await GetChatAsync(chat);
            var userEntity = await GetUserAsync(username);

            if (chatEntity == null || userEntity == null)
                return false;

            _dbContext.Add(new MessageEntity
            {
                Chat = chatEntity,
                Sender = userEntity,
                SendingTime = DateTime.UtcNow,
                Text = text
            });

            _dbContext.SaveChanges();
            return true;
        }

        public async Task<bool> CreateNewChatAsync(string username, string password, string chat)
        {
            var userEntity = await GetUserByPassAsync(username, password);

            if (await _dbContext.Chats.FirstOrDefaultAsync(ch => ch.ChatName == chat) != null)
                return false;

            _dbContext.Add(new ChatEntity
            {
                Admin = userEntity,
                ChatName = chat,
            });

            _dbContext.SaveChanges();
            return true;
        }

        public async Task<ChatEntity> GetChatAsync(string chat)
        {
            if (!await _dbContext.Chats.AnyAsync())
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
            _dbContext.SaveChanges();
            return true;
        }
    }
}
