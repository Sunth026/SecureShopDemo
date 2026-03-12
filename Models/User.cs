using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SecureShopDemo.Models;

[Index("Username", Name = "UQ__Users__536C85E465135389", IsUnique = true)]
public partial class User
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(20)]
    public string Role { get; set; } = null!;

    public bool IsLocked { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public string? Email { get; set; }
}
