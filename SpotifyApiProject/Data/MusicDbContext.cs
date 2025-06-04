using Microsoft.EntityFrameworkCore;
using SpotifyTelegramBot.Models;

namespace SpotifyTelegramBot.Data
{
    public class MusicDbContext : DbContext
    {
        public MusicDbContext(DbContextOptions<MusicDbContext> options) : base(options) { }

        public DbSet<TrackResult> Tracks { get; set; }
        // Інші таблиці, якщо є
    }
}
