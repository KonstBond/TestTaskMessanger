using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;
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

        public async Task<ChatEntity> GetChatAsync(string chatName)
        {
            if (!_dbContext.Chats.Any())
                throw new SqlNullValueException("There are no chats in the database");

            try
            {
                return await (from chat in _dbContext.Chats
                          where chat.ChatName == chatName
                          select chat).FirstOrDefaultAsync() ?? throw new NotFoundException("Chat not Found");
            }
            catch (NotFoundException ex) { throw ex; }
        }

        public async Task<UserEntity> GetUserAsync(string username, string password)
        {
            if (!_dbContext.Users.Any())
                throw new SqlNullValueException("There are no users in the database");

            try
            {
                return await (from user in _dbContext.Users
                              where user.Username == username && user.Password == password
                              select user).FirstOrDefaultAsync() ?? throw new NotFoundException("User not Found");
            }
            catch (NotFoundException ex) { throw ex; } 
        }
    }
}
