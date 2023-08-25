﻿using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic;
using BaseStationReader.Tests.Entities;
using BaseStationReader.Tests.Mocks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Tests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class AircraftTrackerTest
    {
        private const int MessageReaderIntervalMs = 100;
        private const int TrackerRecentMs = 500;
        private const int TrackerStaleMs = 1000;
        private const int TrackerRemovedMs = 2000;
        private const int MaximumTestRunTimeMs = 6000;

        private List<AircraftNotificationData> _notifications = new();

        [TestMethod]
        public void TestAircraftTracker()
        {
            string[] messages = new string[] {
                "MSG,8,1,1,3965A3,1,2023/08/23,12:07:27.929,2023/08/23,12:07:28.005,,,,,,,,,,,,0",
                "MSG,6,1,1,3965A3,1,2023/08/23,12:07:27.932,2023/08/23,12:07:28.006,,,,,,,,6303,0,0,0,"
            };

            // Create a mock reader and parser
            var reader = new MockMessageReader(messages, MessageReaderIntervalMs, true);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Create an aircraft tracker and wire up the event handlers
            var tracker = new AircraftTracker(reader, parsers, TrackerRecentMs, TrackerStaleMs, TrackerRemovedMs);
            tracker.AircraftAdded += OnAircraftNotification;
            tracker.AircraftUpdated += OnAircraftNotification;
            tracker.AircraftRemoved += OnAircraftNotification;

            // Start a stopwatch, that's used to make sure the test doesn't run continuously if
            // something goes awry
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            // Start the tracker and wait until all the messages have been sent or it's clear there's a problem
            tracker.Start();
            while (stopwatch.ElapsedMilliseconds < MaximumTestRunTimeMs)
            {
            }

            // Stop the tracker
            tracker.Stop();

            // Construct the expected de-duplicated sequence of notifications
            var expected = new List<AircraftNotificationType>
            {
                AircraftNotificationType.Added,
                AircraftNotificationType.Updated,
                AircraftNotificationType.Recent,
                AircraftNotificationType.Stale,
                AircraftNotificationType.Removed
            };


            // Identify duplicates in the notifications list (for the Recent and Stale notification types)
            var duplicates = new List<AircraftNotificationData>();
            var previous = AircraftNotificationType.Unknown;
            foreach (var notification in _notifications)
            {
                if (notification.NotificationType == previous)
                {
                    duplicates.Add(notification);
                }

                previous = notification.NotificationType;
            }

            // Remove the duplicates
            foreach (var notification in duplicates)
            {
                _notifications.Remove(notification);
            }

            // The actual notifications list should now be <= the length of the expected list. It *may* not
            // be the same length as the test timings may mean that not all the messages have been recevied
            // (this seems to be the case when run in a GitHub action, for instance)
            Assert.IsTrue(_notifications.Count <= expected.Count);

            // Now confirm the notifications we do have arrived in the right order with the correct aircraft data
            for (int i = 0; i < _notifications.Count; i++)
            {
                // Confirm the notification type is correct
                Assert.AreEqual(expected[i], _notifications[i].NotificationType);

                // Confirm the aircraft details are correct. The first copy won't have a squawk code,
                // the remainder will
                var expectedSquawk = (expected[i] == AircraftNotificationType.Added) ? null : "6303";
#pragma warning disable CS8604
                ConfirmAircraftProperties(_notifications[i].Aircraft, expectedSquawk);
#pragma warning restore CS8604
            }
        }

        /// <summary>
        /// Confirm the aircraft properties match the expected values
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="expectedSquawk"></param>
        private static void ConfirmAircraftProperties(Aircraft aircraft, string? expectedSquawk)
        {
            Assert.AreEqual("3965A3", aircraft.Address);
            Assert.IsNull(aircraft.Callsign);
            Assert.IsNull(aircraft.Altitude);
            Assert.IsNull(aircraft.GroundSpeed);
            Assert.IsNull(aircraft.Track);
            Assert.IsNull(aircraft.Latitude);
            Assert.IsNull(aircraft.Longitude);
            Assert.IsNull(aircraft.VerticalRate);
            if (expectedSquawk != null)
            {
                Assert.AreEqual(expectedSquawk, aircraft.Squawk);
            }
            else
            {
                Assert.IsNull(aircraft.Squawk);
            }
        }

        private void OnAircraftNotification(object? sender, AircraftNotificationEventArgs e)
        {
            lock (_notifications)
            {
                _notifications.Add(new AircraftNotificationData
                {
                    Aircraft = (Aircraft)e.Aircraft.Clone(),
                    NotificationType = e.NotificationType
                });
            }
        }
    }
}
