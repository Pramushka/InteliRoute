using System;
using System.Collections.Generic;

namespace InteliRoute.Models.ViewModels
{
    public sealed class LogsIndexVm
    {
        public LogFilterVm Filter { get; set; } = new();
        public List<LogRowVm> Rows { get; set; } = new();

        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PageCount => (int)Math.Ceiling(Total / (double)PageSize);

        public bool IsSuperAdmin { get; set; }
        public List<TenantChoiceVm> Tenants { get; set; } = new();
    }

    public sealed class LogRowVm
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "";
        public string? Source { get; set; }
        public string Message { get; set; } = "";
        public int? TenantId { get; set; }
        public int? MailboxId { get; set; }
        public int? EmailId { get; set; }
        public bool HasException { get; set; }
    }

    public sealed class TenantChoiceVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
