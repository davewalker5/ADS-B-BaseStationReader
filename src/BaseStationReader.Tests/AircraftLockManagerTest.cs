﻿using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseStationReader.Tests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class AircraftLockManagerTest
    {
        private BaseStationReaderDbContext? _context = null;
        private IAircraftWriter? _aircraftWriter = null;
        private IAircraftLockManager? _aircraftLocker = null;
        private const int TimeToLockMs = 600000;

        private const string Address = "406A3D";

        [TestInitialize]
        public void TestInitialise()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _aircraftWriter = new AircraftWriter(_context);
            _aircraftLocker = new AircraftLockManager(_aircraftWriter, TimeToLockMs);
        }

        [TestMethod]
        public async Task GetActiveAircraftTest()
        {
            var added = await _aircraftWriter!.WriteAsync(new Aircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            var active = await _aircraftLocker!.GetActiveAircraft(Address);
            Assert.IsNotNull(active);
            Assert.AreEqual(added.Id, active.Id);
        }

        [TestMethod]
        public async Task GetInactiveAircraftTest()
        {
            await _aircraftWriter!.WriteAsync(new Aircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            });

            var active = await _aircraftLocker!.GetActiveAircraft(Address);
            Assert.IsNull(active);
        }

        [TestMethod]
        public async Task InactiveAircraftIsLockedTest()
        {
            var aircraft = await _aircraftWriter!.WriteAsync(new Aircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            });

            Assert.IsTrue(aircraft.Id > 0);
            Assert.IsFalse(aircraft.Locked);

            var active = await _aircraftLocker!.GetActiveAircraft(Address);
            Assert.IsNull(active);

            var retrieved = await _aircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(aircraft.Id, retrieved.Id);
            Assert.IsTrue(aircraft.Locked);
        }

        [TestMethod]
        public async Task GetMissingAircraftTest()
        {
            var active = await _aircraftLocker!.GetActiveAircraft("000000");
            Assert.IsNull(active);
        }
    }
}
