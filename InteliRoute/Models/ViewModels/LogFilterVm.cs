using System;

namespace InteliRoute.Models.ViewModels
{
    public sealed class LogFilterVm
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Level { get; set; }           // "All", "Debug", "Information", ...
        public int? TenantId { get; set; }
        public int? MailboxId { get; set; }
        public int? EmailId { get; set; }
        public bool? HasException { get; set; }      // null any, true only-with, false only-without
        public string? Text { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
