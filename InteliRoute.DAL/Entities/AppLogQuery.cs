using System;

namespace InteliRoute.DAL.Entities
{
    public sealed class AppLogQuery
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }           
        public string? Level { get; set; }             
        public int? TenantId { get; set; }
        public int? MailboxId { get; set; }
        public int? EmailId { get; set; }
        public bool? HasException { get; set; }          
        public string? Text { get; set; }               
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
