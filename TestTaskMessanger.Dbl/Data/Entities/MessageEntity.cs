using System.ComponentModel.DataAnnotations.Schema;

namespace TestTaskMessanger.Dbl.Data.Entities
{
    public class MessageEntity
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public ChatEntity? Chat { get; set; }
        public int SenderId { get; set; }
        public UserEntity? Sender { get; set; }
        public string? Text { get; set; }
        [Column("SendingTime", TypeName = "timestamp")]
        public DateTime SendingTime { get; set; }
    }
}
