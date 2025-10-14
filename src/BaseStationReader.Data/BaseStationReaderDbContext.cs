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
        public virtual DbSet<Aircraft> Aircraft { get; set; }
        public virtual DbSet<Model> Models { get; set; }
        public virtual DbSet<Manufacturer> Manufacturers { get; set; }
        public virtual DbSet<Sighting> Sightings { get; set; }
        public virtual DbSet<FlightNumberMapping> FlightNumberMappings { get; set; }

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
                    .HasColumnName("LookupTimestamp")
                    .HasColumnType("DATETIME");

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
                entity.ToTable("POSITION");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Address).IsRequired().HasColumnName("Address");
                entity.Property(e => e.Altitude).HasColumnName("Altitude");
                entity.Property(e => e.Latitude).HasColumnName("Latitude");
                entity.Property(e => e.Longitude).HasColumnName("Longitude");
                entity.Property(e => e.Distance).HasColumnName("Distance");
                entity.Property(e => e.Timestamp).IsRequired().HasColumnName("Timestamp").HasColumnType("DATETIME");

                entity.HasOne(e => e.Aircraft)
                    .WithMany()
                    .HasForeignKey(e => e.AircraftId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Airline>(entity =>
            {
                entity.ToTable("AIRLINE");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasColumnName("Name");
                entity.Property(e => e.ICAO).HasColumnName("ICAO");
                entity.Property(e => e.IATA).HasColumnName("IATA");
            });

            modelBuilder.Entity<Flight>(entity =>
            {
                entity.ToTable("FLIGHT");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Number).IsRequired().HasColumnName("Number");
                entity.Property(e => e.ICAO).HasColumnName("ICAO");
                entity.Property(e => e.IATA).IsRequired().HasColumnName("IATA");
                entity.Property(e => e.Embarkation).IsRequired().HasColumnName("Embarkation");
                entity.Property(e => e.Destination).IsRequired().HasColumnName("Destination");

                entity.HasOne(e => e.Airline)
                    .WithMany()
                    .HasForeignKey(e => e.AirlineId)
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

                entity.HasOne(e => e.Model)
                    .WithMany()
                    .HasForeignKey(e => e.ModelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Model>(entity =>
            {
                entity.ToTable("MODEL");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.ICAO).HasColumnName("ICAO");
                entity.Property(e => e.IATA).HasColumnName("IATA");

                entity.HasOne(e => e.Manufacturer)
                    .WithMany()
                    .HasForeignKey(e => e.ManufacturerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Manufacturer>(entity =>
            {
                entity.ToTable("MANUFACTURER");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasColumnName("Name");
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

            modelBuilder.Entity<FlightNumberMapping>(entity =>
            {
                entity.ToTable("FLIGHT_NUMBER_MAPPING");

                entity.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
                entity.Property(e => e.AirlineICAO).HasColumnName("AirlineICAO");
                entity.Property(e => e.AirlineIATA).HasColumnName("AirlineIATA");
                entity.Property(e => e.AirlineName).HasColumnName("AirlineName");
                entity.Property(e => e.AirportICAO).HasColumnName("AirportICAO");
                entity.Property(e => e.AirportIATA).HasColumnName("AirportIATA");
                entity.Property(e => e.AirportName).HasColumnName("AirportName");
                entity.Property(e => e.AirportType).IsRequired().HasColumnName("AirportType");
                entity.Property(e => e.Embarkation).IsRequired().HasColumnName("Embarkation");
                entity.Property(e => e.Destination).IsRequired().HasColumnName("Destination");
                entity.Property(e => e.Callsign).IsRequired().HasColumnName("Callsign");
                entity.Property(e => e.FileName).IsRequired().HasColumnName("Filename");
            });
        }
    }
}
