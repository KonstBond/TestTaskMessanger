using TestTaskMessanger.Dbl.Data.Entities;

namespace TestTaskMessanger.Dbl.Repository
{
    public interface IMessangerRepository
    {
        Task<UserEntity> GetUserAsync(string username, string password);
        Task<ChatEntity> GetChatAsync(string chatName);
    }
}