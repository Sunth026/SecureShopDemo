using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecureShopDemo.Models;

public partial class UploadedFileLog
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(255)]
    public string OriginalFileName { get; set; } = null!;

    [StringLength(20)]
    public string Extension { get; set; } = null!;

    public long Size { get; set; }

    public bool IsSafe { get; set; }

    [StringLength(500)]
    public string? ScanMessage { get; set; }

    [StringLength(50)]
    public string? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }
}
