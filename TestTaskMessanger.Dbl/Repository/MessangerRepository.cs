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

        public List<MemberEntity> GetAdminMembers()
        {
            if (!_dbContext.Members.Any())
                throw new SqlNullValueException("There are no members in the database");

            return (from member in _dbContext.Members
                    where member.isAdmin == true
                    select member).ToList();
        }

        public List<ChatEntity> GetAllChats()
        {
            if (!_dbContext.Chats.Any())
                throw new SqlNullValueException("There are no chats in the database");

            return (from chat in _dbContext.Chats
                    select chat).ToList();
        }

        public async Task<bool> ClearAllMembers()
        {
            bool isCompleted = Task.Run(async () =>
            {
                var members = await _dbContext.Members.ToListAsync();
                _dbContext.Members.RemoveRange(members);
            }).IsCompletedSuccessfully;
            await _dbContext.SaveChangesAsync();
            return isCompleted;
        }
            

        public async Task<bool> AddMessageAsync(string username, string chat, string text)
        {
            ChatEntity? chatEntity = await GetChatAsync(chat);
            UserEntity? userEntity = await GetUserAsync(username);

            bool isCompleted = _dbContext.AddAsync(new MessageEntity
            {
                Chat = chatEntity,
                Sender = userEntity,
                SendingTime = DateTime.UtcNow,
                Text = text
            }).IsCompletedSuccessfully;

            await _dbContext.SaveChangesAsync();
            return isCompleted;
        }

        public async Task<bool> AddChatMemberAsync(string username, string chat)
        {
            ChatEntity? chatEntity = await GetChatAsync(chat);
            UserEntity? userEntity = await GetUserAsync(username);

            MemberEntity? memberEntity = await _dbContext.Members.FirstOrDefaultAsync(
                member => member.UserId == userEntity.Id && member.ChatId == chatEntity.Id);
            
            if(memberEntity == null)
            {
                bool isCompleted = _dbContext.Members.AddAsync(new MemberEntity 
                {
                    Chat = chatEntity,
                    User = userEntity,
                    isAdmin = userEntity.Id == chatEntity.AdminId
                }).IsCompletedSuccessfully;
                await _dbContext.SaveChangesAsync();
                return isCompleted;
            }
            else
                return false;
        }

        public async Task<bool> RemoveChatMemberAsync(string username, string chat)
        {
            ChatEntity? chatEntity = await GetChatAsync(chat);
            UserEntity? userEntity = await GetUserAsync(username);

            MemberEntity memberEntity = 
                await _dbContext.Members.FindAsync(new[] { chatEntity.Id, userEntity.Id }) 
                ?? throw new NotFoundException("Member not Found");

            bool isCompleted = Task.Run(() => _dbContext.Members.Remove(memberEntity)).IsCompletedSuccessfully;
            await _dbContext.SaveChangesAsync();

            return isCompleted;
        }

        public async Task<ChatEntity> GetAsync(string chat)
        {
            if (!_dbContext.Chats.Any())
                throw new SqlNullValueException("There are no chats in the database");

            try
            {
                return await (from ch in _dbContext.Chats
                              where ch.ChatName == chat
                              select ch).FirstOrDefaultAsync() ?? throw new NotFoundException("Chat not Found");
            }
            catch (NotFoundException ex) { throw ex; }
        }

        public async Task<ChatEntity> GetChatAsync(string chat)
        {
            if (!_dbContext.Chats.Any())
                throw new SqlNullValueException("There are no chats in the database");

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
                throw new SqlNullValueException("There are no users in the database");

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
    }
}
