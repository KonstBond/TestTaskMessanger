using TestTaskMessanger.Dbl.Data.Entities;

namespace TestTaskMessanger.Dbl.Repository
{
    public interface IMessangerRepository
    {
        Task<UserEntity> GetUserByPassAsync(string username, string password);
        Task<UserEntity> GetUserAsync(string username);
        Task<ChatEntity> GetChatAsync(string chat);
        Task<bool> AddMessageAsync(string username, string chat, string text);
        Task<bool> CreateNewChat(string username, string password, string chat);
        Task<bool> RemoveChatAsync(string chat);
    }
}