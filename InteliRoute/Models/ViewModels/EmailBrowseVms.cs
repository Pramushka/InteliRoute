using System.ComponentModel.DataAnnotations;
using InteliRoute.DAL.Entities;

namespace InteliRoute.Models.ViewModels;

public sealed class EmailListFiltersVm
{
    public string? Q { get; set; }
    public RouteStatus? Status { get; set; }
    public int? MailboxId { get; set; }

    [DataType(DataType.Date)] public DateTime? FromDate { get; set; }
    [DataType(DataType.Date)] public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class EmailRowVm
{
    public int Id { get; set; }
    public int MailboxId { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? Snippet { get; set; }
    public DateTime ReceivedUtc { get; set; }
    public RouteStatus RouteStatus { get; set; }
}

public sealed class EmailListVm
{
    public EmailListFiltersVm Filters { get; set; } = new();
    public List<EmailRowVm> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page => Filters.Page;
    public int PageSize => Filters.PageSize;
    public int PageCount => (int)Math.Ceiling((double)Math.Max(1, Total) / Math.Max(1, PageSize));

    // For filter dropdowns
    public List<(int Id, string Address)> Mailboxes { get; set; } = new();
}

public sealed class EmailDetailsVm
{
    public int Id { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? Snippet { get; set; }
    public string? BodyText { get; set; }
    public DateTime ReceivedUtc { get; set; }
    public RouteStatus RouteStatus { get; set; }
    public int? RoutedDepartmentId { get; set; }
    public string? RoutedEmail { get; set; }
    public string? ErrorMessage { get; set; }
}
