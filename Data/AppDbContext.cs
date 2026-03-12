using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Models;

namespace SecureShopDemo.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<LoginAttemptLog> LoginAttemptLogs { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductComment> ProductComments { get; set; }
    public virtual DbSet<UploadedFileLog> UploadedFileLogs { get; set; }
    public virtual DbSet<User> Users { get; set; }

    public DbSet<AttackLog> AttackLogs { get; set; }
    public DbSet<OtpEntry> OtpCodes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=SUNTH\\SQLEXPRESS;Database=SecureShopDemoDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginAttemptLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LoginAtt__3214EC07C3BDCD31");

            entity.Property(e => e.AttemptTime).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC0731C100BE");

            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC075977BBF3");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC073DFAEF48");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProductComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductC__3214EC0749CC1355");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductComments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductComments_Products");
        });

        modelBuilder.Entity<UploadedFileLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Uploaded__3214EC0728212933");

            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07A309A18E");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Role).HasDefaultValue("User");
            entity.Property(e => e.Email).HasMaxLength(100);
        });

        modelBuilder.Entity<AttackLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IpAddress).HasMaxLength(100);
            entity.Property(e => e.AttackType).HasMaxLength(100);
            entity.Property(e => e.Path).HasMaxLength(200);
            entity.Property(e => e.Method).HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<OtpEntry>(entity =>
        {
            entity.ToTable("OtpCodes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.OtpCode)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.ExpiredAt);

            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}