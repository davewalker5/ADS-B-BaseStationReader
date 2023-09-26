using BaseStationReader.Entities.Lookup;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Data
{
    [ExcludeFromCodeCoverage]
    public partial class BaseStationReaderDbContext : DbContext
    {
        public virtual DbSet<Aircraft> Aircraft { get; set; }
        public virtual DbSet<AircraftPosition> AircraftPositions { get; set; }
        public virtual DbSet<AircraftModel> AircraftModels { get; set; }
        public virtual DbSet<Manufacturer> Manufacturers { get; set; }
        public virtual DbSet<WakeTurbulenceCategory> WakeTurbulenceCategories { get; set; }

        public BaseStationReaderDbContext(DbContextOptions<BaseStationReaderDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Initialise the aircraft tracker model
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Aircraft>(entity =>
            {
                entity.ToTable("AIRCRAFT");

                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.HasMany(e => e.Positions)
                    .WithOne(e => e.Aircraft)
                    .HasForeignKey(e => e.AircraftId);

                entity.Property(e => e.Address).HasColumnName("Address");
                entity.Property(e => e.Callsign).HasColumnName("Callsign");
                entity.Property(e => e.Altitude).HasColumnName("Altitude");
                entity.Property(e => e.GroundSpeed).HasColumnName("GroundSpeed");
                entity.Property(e => e.Track).HasColumnName("Track");
                entity.Property(e => e.Latitude).HasColumnName("Latitude");
                entity.Property(e => e.Longitude).HasColumnName("Longitude");
                entity.Property(e => e.VerticalRate).HasColumnName("VerticalRate");
                entity.Property(e => e.Squawk).HasColumnName("Squawk");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.Messages).HasColumnName("Messages");
                entity.Property(e => e.Distance).HasColumnName("Distance");

                entity.Property(e => e.FirstSeen)
                    .IsRequired()
                    .HasColumnName("FirstSeen")
                    .HasColumnType("DATETIME");

                entity.Property(e => e.LastSeen)
                    .IsRequired()
                    .HasColumnName("LastSeen")
                    .HasColumnType("DATETIME");
            });

            modelBuilder.Entity<AircraftPosition>(entity =>
            {
                modelBuilder.Entity<AircraftPosition>().Ignore(e => e.Address);

                entity.ToTable("AIRCRAFT_POSITION");

                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.AircraftId)
                    .IsRequired()
                    .HasColumnName("AircraftId");

                entity.Property(e => e.Latitude)
                    .IsRequired()
                    .HasColumnName("Latitude");

                entity.Property(e => e.Longitude)
                    .IsRequired()
                    .HasColumnName("Longitude");

                entity.Property(e => e.Timestamp)
                    .IsRequired()
                    .HasColumnName("Timestamp")
                    .HasColumnType("DATETIME");

                entity.Property(e => e.Distance).HasColumnName("Distance");
            });

            modelBuilder.Entity<WakeTurbulenceCategory>(entity =>
            {
                entity.ToTable("WAKE_TURBULENCE_CATEGORY");

                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.HasMany(e => e.Models)
                    .WithOne(e => e.WakeTurbulenceCategory)
                    .HasForeignKey(e => e.WakeTurbulenceCategoryId);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasColumnName("Category");

                entity.Property(e => e.Meaning)
                    .IsRequired()
                    .HasColumnName("Meaning");
            });

            modelBuilder.Entity<Manufacturer>(entity =>
            {
                entity.ToTable("MANUFACTURER");

                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.HasMany(e => e.Models)
                    .WithOne(e => e.Manufacturer)
                    .HasForeignKey(e => e.ManufacturerId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("Name");
            });

            modelBuilder.Entity<AircraftModel>(entity =>
            {
                entity.ToTable("AIRCRAFT_MODEL");

                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.IATA).HasColumnName("IATA");
                entity.Property(e => e.ICAO).HasColumnName("ICAO");
                entity.Property(e => e.Name).HasColumnName("Name");
            });
        }
    }
}
