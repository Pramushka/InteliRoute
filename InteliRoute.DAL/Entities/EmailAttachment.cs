using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Entities;

public class EmailAttachment
{
    public int Id { get; set; }
    public int EmailItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? StoragePath { get; set; }  // path or URL where saved

    public EmailItem? EmailItem { get; set; }
}
