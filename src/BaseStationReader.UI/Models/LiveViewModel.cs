﻿using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Expressions;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Database;
using BaseStationReader.Logic.Tracking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace BaseStationReader.UI.Models
{
    public class LiveViewModel
    {
        private ITrackerWrapper? _wrapper = null;

        public ObservableCollection<Aircraft> TrackedAircraft { get; private set; } = new();
        public bool IsTracking { get { return _wrapper != null && _wrapper.IsTracking; } }
        public BaseFilters? Filters { get; set; }

        /// <summary>
        /// Initialise the tracker
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        public void Initialise(ITrackerLogger logger, TrackerApplicationSettings settings)
        {
            _wrapper = new TrackerWrapper(logger, settings);
            _wrapper.Initialise();
        }

        /// <summary>
        /// Start the tracker
        /// </summary>
        public void Start()
            => _wrapper!.Start();

        /// <summary>
        /// Stop the tracker
        /// </summary>
        public void Stop()
            => _wrapper!.Stop();

        /// <summary>
        /// Refresh the tracked aircraft collection
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="status"></param>
        public void Refresh()
        {
            // Build the filtering expression, if needed
            var builder = new ExpressionBuilder<Aircraft>();
            if (!string.IsNullOrEmpty(Filters?.Address))
            {
                builder.Add("Address", TrackerFilterOperator.Equals, Filters.Address.ToUpper());
            }

            if (!string.IsNullOrEmpty(Filters?.Callsign))
            {
                builder.Add("Callsign", TrackerFilterOperator.Equals, Filters.Callsign.ToUpper());
            }

            if (!string.IsNullOrEmpty(Filters?.Status) && Enum.TryParse(Filters.Status, out TrackingStatus statusEnumValue))
            {
                builder.Add("Status", TrackerFilterOperator.Equals, statusEnumValue);
            }

            // Build the filter expression. If there is one, use it to filter the collection of aircraft used
            // to refresh the grid. Otherwise, just use all the current tracked aircraft
            var filter = builder.Build();
            List<Aircraft> aircraft;
            if (filter != null)
            {
                aircraft = _wrapper!.TrackedAircraft.Values.AsQueryable().Where(filter).ToList();
            }
            else
            {
                aircraft = _wrapper!.TrackedAircraft.Values.ToList();
            }

            // Update the observable collection from the filtered aircraft list
            TrackedAircraft = new ObservableCollection<Aircraft>(aircraft);
        }
    }
}
