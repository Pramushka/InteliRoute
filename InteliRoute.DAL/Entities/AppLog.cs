using System.ComponentModel.DataAnnotations.Schema;

namespace InteliRoute.DAL.Entities
{
    [Table("app_logs")]
    public sealed class AppLog
    {
        [Column("Id")] 
        public long Id { get; set; }

        [Column("TimestampUtc")] 
        public DateTime Timestamp { get; set; }

        [Column("Level")] 
        public string Level { get; set; } = "";

        [Column("Message")] 
        public string Message { get; set; } = "";
        
        [Column("MessageTemplate")]
        public string? MessageTemplate { get; set; }
       
        [Column("Exception")] 
        public string? Exception { get; set; }
        
        [Column("Properties")] 
        public string? Properties { get; set; }
        
        [Column("Source")] 
        public string? Source { get; set; }
        
        [Column("TenantId")] 
        public int? TenantId { get; set; }
        
        [Column("UserName")] 
        public string? UserName { get; set; }
        
        [Column("LogLevel")] 
        public string? LogLevel { get; set; }

        // These do not exist as columns – keep them NotMapped if you added them for filtering:
        [NotMapped] public int? MailboxId { get; set; }
        [NotMapped] public int? EmailId { get; set; }
    }
}
