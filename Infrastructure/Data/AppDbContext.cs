using System.Text.Json;
using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Anime> Animes => Set<Anime>();
    public DbSet<UserAnimeProgress> UserAnimeProgresses => Set<UserAnimeProgress>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EpisodeNotification> EpisodeNotifications => Set<EpisodeNotification>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.Property(user => user.Username).HasMaxLength(50).IsRequired();
            builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
            builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(user => user.AvatarUrl).HasMaxLength(1024);
            builder.Property(user => user.ExpoPushToken).HasMaxLength(256);

            builder.HasIndex(user => user.Username).IsUnique();
            builder.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            builder.HasIndex(token => token.TokenHash).IsUnique();
            builder.HasIndex(token => token.ExpiresAt);

            builder.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Anime>(builder =>
        {
            builder.Property(anime => anime.Title).HasMaxLength(512).IsRequired();
            builder.Property(anime => anime.TitleRussian).HasMaxLength(512);
            builder.Property(anime => anime.TitleEnglish).HasMaxLength(512);
            builder.Property(anime => anime.TitleJapanese).HasMaxLength(512);
            builder.Property(anime => anime.ImageUrl).HasMaxLength(1024);
            builder.Property(anime => anime.Season).HasMaxLength(64);

            builder.HasIndex(anime => anime.ShikimoriId).IsUnique().HasFilter("[ShikimoriId] IS NOT NULL");
            builder.HasIndex(anime => anime.Title);
            builder.HasIndex(anime => anime.Status);
            builder.HasIndex(anime => anime.UpdatedAt);

            var options = JsonOptions;
            var comparer = ListComparer;

            builder.Property(anime => anime.TitleSynonyms)
                .HasConversion(new ValueConverter<List<string>, string>(
                    value => JsonSerializer.Serialize(value, options),
                    value => JsonSerializer.Deserialize<List<string>>(value, options) ?? new List<string>()))
                .Metadata.SetValueComparer(comparer);

            builder.Property(anime => anime.Genres)
                .HasConversion(new ValueConverter<List<string>, string>(
                    value => JsonSerializer.Serialize(value, options),
                    value => JsonSerializer.Deserialize<List<string>>(value, options) ?? new List<string>()))
                .Metadata.SetValueComparer(comparer);

            builder.Property(anime => anime.Studios)
                .HasConversion(new ValueConverter<List<string>, string>(
                    value => JsonSerializer.Serialize(value, options),
                    value => JsonSerializer.Deserialize<List<string>>(value, options) ?? new List<string>()))
                .Metadata.SetValueComparer(comparer);
        });

        modelBuilder.Entity<UserAnimeProgress>(builder =>
        {
            builder.HasIndex(progress => new { progress.UserId, progress.AnimeId }).IsUnique();
            builder.HasIndex(progress => progress.ShikimoriId);

            builder.HasOne(progress => progress.User)
                .WithMany(user => user.AnimeProgresses)
                .HasForeignKey(progress => progress.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(progress => progress.Anime)
                .WithMany(anime => anime.UserProgresses)
                .HasForeignKey(progress => progress.AnimeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EpisodeNotification>(builder =>
        {
            builder.HasIndex(notification => new { notification.UserId, notification.AnimeId, notification.Episode }).IsUnique();
            builder.HasIndex(notification => new { notification.UserId, notification.IsRead });

            builder.HasOne(notification => notification.User)
                .WithMany()
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(notification => notification.Anime)
                .WithMany()
                .HasForeignKey(notification => notification.AnimeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(builder =>
        {
            builder.Property(review => review.Text).HasMaxLength(2000);
            builder.HasIndex(review => new { review.UserId, review.AnimeId }).IsUnique();
            builder.HasIndex(review => review.AnimeId);

            builder.HasOne(review => review.User)
                .WithMany(user => user.Reviews)
                .HasForeignKey(review => review.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(review => review.Anime)
                .WithMany(anime => anime.Reviews)
                .HasForeignKey(review => review.AnimeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static readonly JsonSerializerOptions JsonOptions = new();

    private static readonly ValueComparer<List<string>> ListComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        value => value.ToList());
}
