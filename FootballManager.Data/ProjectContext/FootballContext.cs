using FootballManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data
{
    public class FootballContext : DbContext
    {
        public FootballContext(DbContextOptions<FootballContext> options) : base(options) { }

        public DbSet<Club> Clubs => Set<Club>();
        public DbSet<Footballer> Footballers => Set<Footballer>();
        public DbSet<Tournament> Tournaments => Set<Tournament>();
        public DbSet<TournamentClub> TournamentClubs => Set<TournamentClub>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Transfer> Transfers => Set<Transfer>();
        public DbSet<SeasonSummary> SeasonSummaries => Set<SeasonSummary>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- CLUB ----
            modelBuilder.Entity<Club>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                b.Property(x => x.Money)
                    .HasColumnType("decimal(18,2)");
            });

            // ---- FOOTBALLER ----
            modelBuilder.Entity<Footballer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                b.HasOne(x => x.Club)
                    .WithMany(c => c.Footballers)
                    .HasForeignKey(x => x.ClubId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---- TOURNAMENT ----
            modelBuilder.Entity<Tournament>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                b.Property(x => x.RewardByRank)
                    .HasColumnType("text");
            });

            // ---- TOURNAMENT-CLUB (Join table) ----
            modelBuilder.Entity<TournamentClub>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(tc => tc.Tournament)
                    .WithMany(t => t.TournamentClubs)
                    .HasForeignKey(tc => tc.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(tc => tc.Club)
                    .WithMany(c => c.TournamentClubs)
                    .HasForeignKey(tc => tc.ClubId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---- MATCH ----
            modelBuilder.Entity<Match>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(m => m.Tournament)
                    .WithMany(t => t.Matches)
                    .HasForeignKey(m => m.TournamentId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(m => m.HomeClub)
                    .WithMany(c => c.HomeMatches)
                    .HasForeignKey(m => m.HomeClubId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(m => m.AwayClub)
                    .WithMany(c => c.AwayMatches)
                    .HasForeignKey(m => m.AwayClubId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(m => m.MVPFootballer)
                    .WithMany()
                    .HasForeignKey(m => m.MVPFootballerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---- TRANSFER ----
            modelBuilder.Entity<Transfer>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(t => t.Footballer)
                    .WithMany(f => f.Transfers)
                    .HasForeignKey(t => t.FootballerId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(t => t.FromClub)
                    .WithMany()
                    .HasForeignKey(t => t.FromClubId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(t => t.ToClub)
                    .WithMany()
                    .HasForeignKey(t => t.ToClubId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.Property(x => x.TransferFee)
                    .HasColumnType("decimal(18,2)");
            });

            // ---- SEASON SUMMARY ----
            modelBuilder.Entity<SeasonSummary>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(s => s.Tournament)
                    .WithMany()
                    .HasForeignKey(s => s.TournamentId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(s => s.ChampionClub)
                    .WithMany()
                    .HasForeignKey(s => s.ChampionClubId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(s => s.TopScorer)
                    .WithMany()
                    .HasForeignKey(s => s.TopScorerId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(s => s.MVPFootballer)
                    .WithMany()
                    .HasForeignKey(s => s.MVPFootballerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
