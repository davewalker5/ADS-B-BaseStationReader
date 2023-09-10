using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Expressions;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Database;
using BaseStationReader.Logic.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BaseStationReader.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static ITrackerWrapper? _wrapper = null;

        public ObservableCollection<Aircraft> TrackedAircraft { get; private set; } = new();
        public ObservableCollection<string> Statuses { get; private set; } = new();

        public MainWindowViewModel()
        {
            Statuses.Add("All");
            Statuses.Add(TrackingStatus.Active.ToString());
            Statuses.Add(TrackingStatus.Inactive.ToString());
            Statuses.Add(TrackingStatus.Stale.ToString());
        }

        /// <summary>
        /// Initialise the tracker
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        public void Initialise(ITrackerLogger logger, ApplicationSettings settings)
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
        /// <param name="status"></param>
        public void Refresh(string? status)
        {
            List<Aircraft> aircraft;

            // Build the filtering expression, if needed
            var builder = new ExpressionBuilder<Aircraft>();
            if ((status != null) && Enum.TryParse<TrackingStatus>(status, out TrackingStatus statusEnumValue))
            {
                builder.Add("Status", TrackerFilterOperator.Equals, statusEnumValue);
            }

            // Build the filter expression. If there is one, use it to filter the collection of aircraft used
            // to refresh the grid. Otherwise, just use all the current tracked aircraft
            var filter = builder.Build();
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