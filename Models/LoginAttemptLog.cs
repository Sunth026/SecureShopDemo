using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecureShopDemo.Models;

public partial class LoginAttemptLog
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(100)]
    public string IpAddress { get; set; } = null!;

    public bool IsSuccess { get; set; }

    public DateTime AttemptTime { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
