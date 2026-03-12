using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecureShopDemo.Models;

public partial class ProductComment
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(1000)]
    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? AttachmentPath { get; set; }

    public string? OriginalFileName { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("ProductComments")]
    public virtual Product Product { get; set; } = null!;
}
