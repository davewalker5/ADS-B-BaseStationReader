﻿// <auto-generated />
using System;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    [DbContext(typeof(BaseStationReaderDbContext))]
    [ExcludeFromCodeCoverage]
    partial class BaseStationReaderDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.10");

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
                        .HasColumnType("INTEGER");

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

                    b.ToTable("AIRCRAFT_POSITION", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
