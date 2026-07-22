using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Data
{
    [ExcludeFromCodeCoverage]
    public partial class BaseStationReaderDbContext : DbContext
    {
        public virtual DbSet<TrackedAircraft> TrackedAircraft { get; set; }
        public virtual DbSet<AircraftPosition> Positions { get; set; }
        public virtual DbSet<Flight> Flights { get; set; }
        public virtual DbSet<Airline> Airlines { get; set; }
        public virtual DbSet<Airport> Airports { get; set; }
        public virtual DbSet<Aircraft> Aircraft { get; set; }
        public virtual DbSet<Model> Models { get; set; }
        public virtual DbSet<Manufacturer> Manufacturers { get; set; }
        public virtual DbSet<Sighting> Sightings { get; set; }
        public virtual DbSet<ExcludedAddress> ExcludedAddresses { get; set; }
        public virtual DbSet<ExcludedCallsign> ExcludedCallsigns { get; set; }
        public virtual DbSet<ApiLogEntry> ApiLogEntries { get; set; }
        public virtual DbSet<Provenance> Provenance { get; set; }

        public BaseStationReaderDbContext(DbContextOptions<BaseStationReaderDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Truncate the specified table and reset its identity counter
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task TruncateTable(string tableName)
        {
            // With a SQLite back-end we have no choice but to use ExecuteSqlRawAsync in this context, so
            // suppress the warnings about idempotence and SQL injection risks
#pragma warning disable EF1002
            await Database.ExecuteSqlRawAsync($"DELETE FROM {tableName};");
            await Database.ExecuteSqlRawAsync($"DELETE FROM sqlite_sequence WHERE name = '{tableName}';");
#pragma warning restore EF1002
        }

        /// <summary>
        /// Clear down aircraft tracking data while leaving aircraft details and airlines intact
        /// </summary>
        /// <returns></returns>
        public async Task ClearDown()
        {
            await TruncateTable("POSITION");
            await TruncateTable("TRACKED_AIRCRAFT");
        }

        /// <summary>
        /// Initialise the aircraft tracker model
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrackedAircraft>(entity =>
            {
                entity.ToTable("TRACKED_AIRCRAFT");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Address).IsRequired().HasColumnName("Address");
                entity.Property(e => e.Callsign).HasColumnName("Callsign");
                entity.Property(e => e.Altitude).HasColumnType("REAL").HasColumnName("Altitude");
                entity.Property(e => e.GroundSpeed).HasColumnType("REAL").HasColumnName("GroundSpeed");
                entity.Property(e => e.Track).HasColumnType("REAL").HasColumnName("Track");
                entity.Property(e => e.Latitude).HasColumnType("REAL").HasColumnName("Latitude");
                entity.Property(e => e.Longitude).HasColumnType("REAL").HasColumnName("Longitude");
                entity.Property(e => e.VerticalRate).HasColumnType("REAL").HasColumnName("VerticalRate");
                entity.Property(e => e.Squawk).HasColumnName("Squawk");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.Messages).HasColumnName("Messages");
                entity.Property(e => e.Distance).HasColumnType("REAL").HasColumnName("Distance");

                entity.Property(e => e.FirstSeen)
                    .IsRequired()
                    .HasColumnName("FirstSeen")
                    .HasColumnType("DATETIME");

                entity.Property(e => e.LastSeen)
                    .IsRequired()
                    .HasColumnName("LastSeen")
                    .HasColumnType("DATETIME");

                // Support the bounded historical-browser filters without changing writer ownership.
                entity.HasIndex(e => e.Address);
                entity.HasIndex(e => e.Callsign);
                entity.HasIndex(e => e.FirstSeen);
                entity.HasIndex(e => e.LastSeen);
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<AircraftPosition>(entity =>
            {
                entity.ToTable("POSITION");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Address).IsRequired().HasColumnName("Address");
                entity.Property(e => e.Altitude).HasColumnType("REAL").HasColumnName("Altitude");
                entity.Property(e => e.Latitude).HasColumnType("REAL").HasColumnName("Latitude");
                entity.Property(e => e.Longitude).HasColumnType("REAL").HasColumnName("Longitude");
                entity.Property(e => e.Distance).HasColumnType("REAL").HasColumnName("Distance");
                entity.Property(e => e.Timestamp).IsRequired().HasColumnName("Timestamp").HasColumnType("DATETIME");

                entity.HasOne(e => e.Aircraft)
                    .WithMany()
                    .HasForeignKey(e => e.AircraftId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Speed ordering and boundary lookups within one tracking session.
                entity.HasIndex(e => new { e.AircraftId, e.Timestamp });
            });

            modelBuilder.Entity<Airline>(entity =>
            {
                entity.ToTable("AIRLINE");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasColumnName("Name");
                entity.Property(e => e.ICAO).HasColumnName("ICAO");
                entity.Property(e => e.IATA).HasColumnName("IATA");
                entity.Property(e => e.ProvenanceId).IsRequired().HasColumnName("ProvenanceId");

                entity.HasOne(e => e.Provenance)
                    .WithMany()
                    .HasForeignKey(e => e.ProvenanceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Airport>(entity =>
            {
                entity.ToTable("AIRPORT");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasColumnName("Name");
                entity.Property(e => e.ICAO).IsRequired().HasColumnName("ICAO");
                entity.Property(e => e.IATA).IsRequired().HasColumnName("IATA");
                entity.Property(e => e.Latitude).HasColumnType("REAL").HasColumnName("Latitude");
                entity.Property(e => e.Longitude).HasColumnType("REAL").HasColumnName("Longitude");
                entity.Property(e => e.ProvenanceId).IsRequired().HasColumnName("ProvenanceId");

                entity.HasOne(e => e.Provenance)
                    .WithMany()
                    .HasForeignKey(e => e.ProvenanceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Flight>(entity =>
            {
                entity.ToTable("FLIGHT");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.ICAO).HasColumnName("ICAO");
                entity.Property(e => e.IATA).IsRequired().HasColumnName("IATA");
                entity.Property(e => e.Callsign).IsRequired().HasColumnName("Callsign");
                entity.Property(e => e.AirlineId).IsRequired().HasColumnName("AirlineId");
                entity.Property(e => e.OriginAirportId).IsRequired().HasColumnName("OriginAirportId");
                entity.Property(e => e.DestinationAirportId).IsRequired().HasColumnName("DestinationAirportId");
                entity.Property(e => e.ProvenanceId).IsRequired().HasColumnName("ProvenanceId");

                entity.HasOne(e => e.Airline)
                    .WithMany()
                    .HasForeignKey(e => e.AirlineId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OriginAirport)
                    .WithMany()
                    .HasForeignKey(e => e.OriginAirportId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DestinationAirport)
                    .WithMany()
                    .HasForeignKey(e => e.DestinationAirportId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Provenance)
                    .WithMany()
                    .HasForeignKey(e => e.ProvenanceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Aircraft>(entity =>
            {
                entity.ToTable("AIRCRAFT");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Address).IsRequired().HasColumnName("Address");
                entity.Property(e => e.Registration).IsRequired().HasColumnName("Registration");
                entity.Property(e => e.Manufactured).HasColumnName("Manufactured");
                entity.Property(e => e.Age).HasColumnName("Age");
                entity.Property(e => e.ProvenanceId).IsRequired().HasColumnName("ProvenanceId");

                entity.HasOne(e => e.Model)
                    .WithMany()
                    .HasForeignKey(e => e.ModelId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Provenance)
                    .WithMany()
                    .HasForeignKey(e => e.ProvenanceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Model>(entity =>
            {
                entity.ToTable("MODEL");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.ICAO).HasColumnName("ICAO");
                entity.Property(e => e.IATA).HasColumnName("IATA");
                entity.Property(e => e.ProvenanceId).IsRequired().HasColumnName("ProvenanceId");

                entity.HasOne(e => e.Manufacturer)
                    .WithMany()
                    .HasForeignKey(e => e.ManufacturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Provenance)
                    .WithMany()
                    .HasForeignKey(e => e.ProvenanceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Manufacturer>(entity =>
            {
                entity.ToTable("MANUFACTURER");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasColumnName("Name");
                entity.Property(e => e.ProvenanceId).IsRequired().HasColumnName("ProvenanceId");

                entity.HasOne(e => e.Provenance)
                    .WithMany()
                    .HasForeignKey(e => e.ProvenanceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Provenance>(entity =>
            {
                entity.ToTable("PROVENANCE");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.SourceRef).IsRequired().HasColumnName("SourceRef");
                entity.Property(e => e.Source).IsRequired().HasColumnName("Source");
                entity.Property(e => e.SourceUrl).IsRequired().HasColumnName("SourceUrl");
                entity.Property(e => e.SourceDataset).IsRequired().HasColumnName("SourceDataset");
                entity.Property(e => e.SourceVersion).IsRequired().HasColumnName("SourceVersion");
                entity.Property(e => e.Licence).IsRequired().HasColumnName("Licence");

                entity.HasIndex(e => e.SourceRef).IsUnique();
            });

            modelBuilder.Entity<Sighting>(entity =>
            {
                entity.ToTable("SIGHTING");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.AircraftId).IsRequired().HasColumnName("AircraftId");
                entity.Property(e => e.FlightId).IsRequired().HasColumnName("FlightId");
                entity.Property(e => e.Timestamp).IsRequired().HasColumnName("Timestamp").HasColumnType("DATETIME");

                entity.HasOne(e => e.Aircraft)
                    .WithMany()
                    .HasForeignKey(e => e.AircraftId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Flight)
                    .WithMany()
                    .HasForeignKey(e => e.FlightId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExcludedAddress>(entity =>
            {
                entity.ToTable("EXCLUDED_ADDRESS");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Address).IsRequired().HasColumnName("Address");
            });

            modelBuilder.Entity<ExcludedCallsign>(entity =>
            {
                entity.ToTable("EXCLUDED_CALLSIGN");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Callsign).IsRequired().HasColumnName("Callsign");
            });

            modelBuilder.Entity<ApiLogEntry>(entity =>
            {
                entity.ToTable("API_LOG");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Service).IsRequired().HasColumnName("Service");
                entity.Property(e => e.Endpoint).IsRequired().HasColumnName("Endpoint");
                entity.Property(e => e.Url).IsRequired().HasColumnName("Url");
                entity.Property(e => e.Property).IsRequired().HasColumnName("Property");
                entity.Property(e => e.PropertyValue).HasColumnName("PropertyValue");
                entity.Property(e => e.Timestamp).IsRequired().HasColumnName("Timestamp").HasColumnType("DATETIME");
            });
        }
    }
}
