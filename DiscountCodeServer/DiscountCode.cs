using LiteDB;

namespace DiscountCodeServer
{
    public class DiscountCode
    {
        [BsonId]
        public string Code { get; set; } = default!;

        public bool Used { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAt { get; set; }   
    }

}
