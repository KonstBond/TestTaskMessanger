namespace TestTaskMessanger.Dbl.Data.Entities
{
    public class ChatEntity
    {
        public int Id { get; set; }
        public string? ChatName { get; set; }
        public int AdminId { get; set; }
        public UserEntity? Admin { get; set; }
    }
}
