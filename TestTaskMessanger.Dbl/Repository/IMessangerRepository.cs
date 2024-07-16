using TestTaskMessanger.Dbl.Data.Entities;

namespace TestTaskMessanger.Dbl.Repository
{
    public interface IMessangerRepository
    {
        Task<UserEntity> GetUserByPassAsync(string username, string password);
        Task<UserEntity> GetUserAsync(string username);
        Task<ChatEntity> GetChatAsync(string chat);
        List<MemberEntity> GetAdminMembers();
        List<ChatEntity> GetAllChats();
        Task<bool> ClearAllMembers();
        Task<bool> AddMessageAsync(string username, string chat, string text);
        Task<bool> RemoveChatMemberAsync(string username, string chat);
        Task<bool> AddChatMemberAsync(string username, string chat);
    }
}