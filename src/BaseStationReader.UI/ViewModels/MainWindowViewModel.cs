using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using System;
using System.Collections.ObjectModel;

namespace BaseStationReader.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private LiveViewViewModel _liveView = new LiveViewViewModel();
        private DatabaseSearchViewModel _databaseSearch = new DatabaseSearchViewModel();

        public ObservableCollection<string> Statuses { get; private set; } = new();
        public ObservableCollection<Aircraft> TrackedAircraft { get {  return _liveView.TrackedAircraft; } }
        public bool IsTracking { get { return _liveView.IsTracking; } }

        public ObservableCollection<Aircraft> SearchResults { get {  return _databaseSearch.SearchResults; } }

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
        public void InitialiseTracker(ITrackerLogger logger, ApplicationSettings settings)
            => _liveView.Initialise(logger, settings);

        /// <summary>
        /// Start the tracker
        /// </summary>
        public void StartTracking()
            => _liveView.Start();

        /// <summary>
        /// Stop the tracker
        /// </summary>
        public void StopTracking()
            => _liveView.Stop();

        /// <summary>
        /// Refresh the tracked aircraft collection
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="status"></param>
        public void RefreshTrackedAircraft(string? address, string? callsign, string? status)
            => _liveView.Refresh(address, callsign, status);


        /// <summary>
        /// Search the database for records matching the specified filtering criteria
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="status"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void Search(string? address, string? callsign, string? status, DateTime? from, DateTime? to)
            => _databaseSearch.Search(address, callsign, status, from, to);
    }
}