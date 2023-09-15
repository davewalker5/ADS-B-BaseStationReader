using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.UI.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;

namespace BaseStationReader.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly LiveViewModel _liveView = new LiveViewModel();
        private readonly DatabaseSearchModel _databaseSearch = new DatabaseSearchModel();

        public bool IsTracking { get { return _liveView.IsTracking; } }
        public ObservableCollection<Aircraft> TrackedAircraft { get {  return _liveView.TrackedAircraft; } }
        public SelectedFilters? LiveViewFilters
        { 
            get { return _liveView.Filters; }
            set { _liveView.Filters = value; }
        }

        public ObservableCollection<Aircraft> SearchResults { get { return _databaseSearch.SearchResults; } }

        public ICommand ShowTrackingFiltersCommand { get; private set; }
        public Interaction<FiltersWindowViewModel,SelectedFilters?> ShowFiltersDialog { get; private set; }

        public MainWindowViewModel()
        {
            // Wire up the tracking filters dialog
            ShowFiltersDialog = new Interaction<FiltersWindowViewModel, SelectedFilters?>();
            ShowTrackingFiltersCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new FiltersWindowViewModel(LiveViewFilters);
                var result = await ShowFiltersDialog.Handle(dialogViewModel);
            });

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
        public void RefreshTrackedAircraft()
            => _liveView.Refresh();


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