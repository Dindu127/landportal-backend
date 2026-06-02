using LandPortal.Api.Entities;
using LandPortal.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace LandPortal.Api.Data
{
    public class LandPortalDbContext : DbContext
    {
        // ... existing DbSets ...
        public DbSet<PropertyMedia> PropertyMedia { get; set; } = null!;
        public DbSet<ContactView> ContactViews { get; set; } = null!;
        public DbSet<PendingUnlock> PendingUnlocks { get; set; } = default!;

        public DbSet<ContactUnlockLog> ContactUnlockLogs { get; set; } = null!;



        // if controllers reference context.Media, add this alias:
        public DbSet<PropertyMedia> Media
        {
            get => PropertyMedia;
            set => PropertyMedia = value;
        }
        public LandPortalDbContext(DbContextOptions<LandPortalDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<EmailOtp> EmailOtps { get; set; } = null!;

       // public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();

        public DbSet<Property> Properties { get; set; } = null!;
        // public DbSet<PropertyMedia> PropertyMedia => Set<PropertyMedia>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");

                e.Property(u => u.Id).HasColumnName("id");
                e.Property(u => u.Email).HasColumnName("email");
                e.Property(u => u.PasswordHash).HasColumnName("password_hash");
                e.Property(u => u.FullName).HasColumnName("full_name");
                e.Property(u => u.Phone).HasColumnName("phone");
                e.Property(u => u.Role).HasColumnName("role");
                e.Property(u => u.IsActive).HasColumnName("is_active");
                e.Property(u => u.CreatedAt).HasColumnName("created_at");
                e.Property(u => u.PasswordResetOtp).HasColumnName("password_reset_otp");
                e.Property(u => u.PasswordResetExpiry).HasColumnName("password_reset_expiry");
                e.Property(u => u.PasswordResetAttempts).HasColumnName("password_reset_attempts");
                e.Property(u => u.ProfilePhotoUrl).HasColumnName("profile_photo_url");

                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Email).HasMaxLength(256).IsRequired();
                e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
                e.Property(u => u.Role).HasMaxLength(20).HasDefaultValue("User");
            });

            // Properties
            modelBuilder.Entity<Property>(e =>
            {
                e.ToTable("properties");

                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.OwnerId).HasColumnName("owner_id");
                e.Property(p => p.Title).HasColumnName("title");
                e.Property(p => p.Description).HasColumnName("description");
                e.Property(p => p.Price).HasColumnName("price");
                e.Property(p => p.City).HasColumnName("city");
                e.Property(p => p.Locality).HasColumnName("locality");
                e.Property(p => p.LandSize).HasColumnName("land_size");
                e.Property(p => p.Status).HasColumnName("status");
                e.Property(p => p.ListedAt).HasColumnName("listed_at");
                e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
                e.Property(p => p.CoverImageUrl).HasColumnName("cover_image_url");

                e.HasOne(p => p.Owner)
                 .WithMany(u => u.Properties)
                 .HasForeignKey(p => p.OwnerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.Property(p => p.Title).HasMaxLength(200).IsRequired();
                e.Property(p => p.Description).HasMaxLength(4000).IsRequired();
                e.Property(p => p.Price).HasPrecision(18, 2);
                e.Property(p => p.LandSize).HasPrecision(18, 2);
                e.Property(p => p.City).HasMaxLength(120).IsRequired();
                e.Property(p => p.Locality).HasMaxLength(200).IsRequired();

                e.HasIndex(p => new { p.City, p.Locality });
                e.HasIndex(p => p.Price);
                e.HasIndex(p => p.ListedAt);
                e.HasIndex(p => p.Status);
                e.HasIndex(p => p.OwnerId);
            });

            // PropertyMedia
            modelBuilder.Entity<PropertyMedia>(e =>
            {
                e.ToTable("property_media");

                e.Property(m => m.Id).HasColumnName("id");
                e.Property(m => m.PropertyId).HasColumnName("property_id");
                e.Property(m => m.Url).HasColumnName("url");
                e.Property(m => m.PublicUrl).HasColumnName("public_url");
                e.Property(m => m.ContentType).HasColumnName("content_type");
                e.Property(m => m.SortOrder).HasColumnName("sort_order");
                e.Property(m => m.IsCover).HasColumnName("is_cover");


                e.HasOne(m => m.Property)
                 .WithMany(p => p.Media)
                 .HasForeignKey(m => m.PropertyId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(m => m.Url).HasMaxLength(1024).IsRequired();
                e.Property(m => m.ContentType).HasMaxLength(100).IsRequired();

                e.HasIndex(m => new { m.PropertyId, m.SortOrder });
            });

            //modelBuilder.Entity<ContactUnlockLog>(b =>
            //{
            //    b.HasKey(x => x.Id);
            //    b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            //    b.Property(x => x.PaymentAmount).HasColumnType("decimal(18,2)");
            //    // add other configuration if needed
            //});

            modelBuilder.Entity<PendingUnlock>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OrderId).HasMaxLength(200); // ★ FIX: max length
                b.Property(x => x.Currency).HasMaxLength(10);
                b.Property(x => x.Status).HasMaxLength(50);
                b.Property(x => x.Notes).HasMaxLength(1000);
                b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<EmailOtp>(e =>
            {
                e.ToTable("EmailOtps");
                e.Property(x => x.Email).HasMaxLength(256).IsRequired();
                e.Property(x => x.Otp).HasMaxLength(10).IsRequired();
            });

        }
    }
}
