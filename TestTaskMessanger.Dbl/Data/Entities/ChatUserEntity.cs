namespace TestTaskMessanger.Dbl.Data.Entities
{
    public class Member
    {
        public int ChatId { get; set; }
        public ChatEntity? Chat { get; set; }
        public int UserId { get; set; }
        public UserEntity? User { get; set; }
        public bool isAdmin { get; set; }
    }
}
