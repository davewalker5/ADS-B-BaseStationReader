﻿// <auto-generated />
using System;
using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    [DbContext(typeof(BaseStationReaderDbContext))]
    [Migration("20230926123432_AircraftModelLookup")]
    partial class AircraftModelLookup
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.10");

            modelBuilder.Entity("BaseStationReader.Entities.Lookup.Airline", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<string>("IATA")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("IATA");

                    b.Property<string>("ICAO")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("ICAO");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("Name");

                    b.HasKey("Id");

                    b.ToTable("AIRLINE", (string)null);
                });

            modelBuilder.Entity("BaseStationReader.Entities.Lookup.Manufacturer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("Name");

                    b.HasKey("Id");

                    b.ToTable("MANUFACTURER", (string)null);
                });

            modelBuilder.Entity("BaseStationReader.Entities.Lookup.Model", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<string>("IATA")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("IATA");

                    b.Property<string>("ICAO")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("ICAO");

                    b.Property<int>("ManufacturerId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("Name");

                    b.HasKey("Id");

                    b.HasIndex("ManufacturerId");

                    b.ToTable("MODEL", (string)null);
                });

            modelBuilder.Entity("BaseStationReader.Entities.Tracking.Aircraft", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("Address");

                    b.Property<decimal?>("Altitude")
                        .HasColumnType("TEXT")
                        .HasColumnName("Altitude");

                    b.Property<string>("Callsign")
                        .HasColumnType("TEXT")
                        .HasColumnName("Callsign");

                    b.Property<double?>("Distance")
                        .HasColumnType("REAL")
                        .HasColumnName("Distance");

                    b.Property<DateTime>("FirstSeen")
                        .HasColumnType("DATETIME")
                        .HasColumnName("FirstSeen");

                    b.Property<decimal?>("GroundSpeed")
                        .HasColumnType("TEXT")
                        .HasColumnName("GroundSpeed");

                    b.Property<DateTime>("LastSeen")
                        .HasColumnType("DATETIME")
                        .HasColumnName("LastSeen");

                    b.Property<decimal?>("Latitude")
                        .HasColumnType("TEXT")
                        .HasColumnName("Latitude");

                    b.Property<decimal?>("Longitude")
                        .HasColumnType("TEXT")
                        .HasColumnName("Longitude");

                    b.Property<int>("Messages")
                        .HasColumnType("INTEGER")
                        .HasColumnName("Messages");

                    b.Property<string>("Squawk")
                        .HasColumnType("TEXT")
                        .HasColumnName("Squawk");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER")
                        .HasColumnName("Status");

                    b.Property<decimal?>("Track")
                        .HasColumnType("TEXT")
                        .HasColumnName("Track");

                    b.Property<decimal?>("VerticalRate")
                        .HasColumnType("TEXT")
                        .HasColumnName("VerticalRate");

                    b.HasKey("Id");

                    b.ToTable("AIRCRAFT", (string)null);
                });

            modelBuilder.Entity("BaseStationReader.Entities.Tracking.AircraftPosition", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("Id");

                    b.Property<int>("AircraftId")
                        .HasColumnType("INTEGER")
                        .HasColumnName("AircraftId");

                    b.Property<decimal>("Altitude")
                        .HasColumnType("TEXT");

                    b.Property<double?>("Distance")
                        .HasColumnType("REAL")
                        .HasColumnName("Distance");

                    b.Property<decimal>("Latitude")
                        .HasColumnType("TEXT")
                        .HasColumnName("Latitude");

                    b.Property<decimal>("Longitude")
                        .HasColumnType("TEXT")
                        .HasColumnName("Longitude");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("DATETIME")
                        .HasColumnName("Timestamp");

                    b.HasKey("Id");

                    b.HasIndex("AircraftId");

                    b.ToTable("AIRCRAFT_POSITION", (string)null);
                });

            modelBuilder.Entity("BaseStationReader.Entities.Lookup.Model", b =>
                {
                    b.HasOne("BaseStationReader.Entities.Lookup.Manufacturer", "Manufacturer")
                        .WithMany("Models")
                        .HasForeignKey("ManufacturerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Manufacturer");
                });

            modelBuilder.Entity("BaseStationReader.Entities.Tracking.AircraftPosition", b =>
                {
                    b.HasOne("BaseStationReader.Entities.Tracking.Aircraft", "Aircraft")
                        .WithMany("Positions")
                        .HasForeignKey("AircraftId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Aircraft");
                });

            modelBuilder.Entity("BaseStationReader.Entities.Lookup.Manufacturer", b =>
                {
                    b.Navigation("Models");
                });

            modelBuilder.Entity("BaseStationReader.Entities.Tracking.Aircraft", b =>
                {
                    b.Navigation("Positions");
                });
#pragma warning restore 612, 618
        }
    }
}