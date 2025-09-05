using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.UI.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;

namespace BaseStationReader.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Model underlying the live view
        /// </summary>
        private readonly LiveViewModel _liveView = new LiveViewModel();

        /// <summary>
        /// Model underlying the database search view
        /// </summary>
        private readonly DatabaseSearchModel _databaseSearch = new DatabaseSearchModel();

        /// <summary>
        /// Application settings
        /// </summary>
        public TrackerApplicationSettings Settings { get; set; }

        /// <summary>
        /// Logging provider
        /// </summary>
        public ITrackerLogger Logger { get; set; }

        /// <summary>
        /// True if the tracker is actively tracking
        /// </summary>
        public bool IsTracking { get { return _liveView.IsTracking; } }

        /// <summary>
        /// Collection of currently tracked aircraft
        /// </summary>
        public ObservableCollection<Aircraft> TrackedAircraft { get {  return _liveView.TrackedAircraft; } }

        /// <summary>
        /// Filtering criteria for the live view
        /// </summary>
        public BaseFilters LiveViewFilters
        { 
            get { return _liveView.Filters; }
            set { _liveView.Filters = value; }
        }

        /// <summary>
        /// Collection of database search results
        /// </summary>
        public ObservableCollection<Aircraft> SearchResults { get { return _databaseSearch.SearchResults; } }

        /// <summary>
        /// Database search criteria
        /// </summary>
        public DatabaseSearchCriteria DatabaseSearchCriteria
        {
            get { return _databaseSearch.SearchCriteria; }
            set { _databaseSearch.SearchCriteria = value; }
        }

        /// <summary>
        /// Aircraft lookup criteria
        /// </summary>
        public AircraftLookupCriteria AircraftLookupCriteria { get; set; }

        /// <summary>
        /// Command to show the live view filtering dialog
        /// </summary>
        public ICommand ShowTrackingFiltersCommand { get; private set; }

        /// <summary>
        /// Interaction to show the live view filtering dialog
        /// </summary>
        public Interaction<FiltersWindowViewModel,BaseFilters> ShowFiltersDialog { get; private set; }

        /// <summary>
        /// Command to show the aircraft lookup dialog
        /// </summary>
        public ICommand ShowAircraftLookupCommand { get; private set; }

        /// <summary>
        /// Interaction to show the aircraft lookup dialog
        /// </summary>
        public Interaction<AircraftLookupWindowViewModel, AircraftLookupCriteria> ShowAircraftLookupDialog { get; private set; }

        /// <summary>
        /// Command to show the tracking options dialog
        /// </summary>
        public ICommand ShowTrackingOptionsCommand { get; private set; }

        /// <summary>
        /// Interaction to show the tracking options dialog
        /// </summary>
        public Interaction<TrackingOptionsWindowViewModel, TrackerApplicationSettings> ShowTrackingOptionsDialog { get; private set; }

        /// <summary>
        /// Command to show the database search dialog
        /// </summary>
        public ICommand ShowDatabaseSearchCommand { get; private set; }

        /// <summary>
        /// Interaction to show the database search dialog
        /// </summary>
        public Interaction<DatabaseSearchWindowViewModel, DatabaseSearchCriteria> ShowDatabaseSearchDialog { get; private set; }

        public MainWindowViewModel()
        {
            // Wire up the tracking filters dialog
            ShowFiltersDialog = new Interaction<FiltersWindowViewModel, BaseFilters>();
            ShowTrackingFiltersCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new FiltersWindowViewModel(LiveViewFilters);
                var result = await ShowFiltersDialog.Handle(dialogViewModel);
                return result;
            });

            // Wire up the aircraft lookup dialog
            ShowAircraftLookupDialog = new Interaction<AircraftLookupWindowViewModel, AircraftLookupCriteria>();
            ShowAircraftLookupCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new AircraftLookupWindowViewModel(Logger!, Settings!, AircraftLookupCriteria);
                var result = await ShowAircraftLookupDialog.Handle(dialogViewModel);
                return result;
            });

            // Wire up the tracking options dialog
            ShowTrackingOptionsDialog = new Interaction<TrackingOptionsWindowViewModel, TrackerApplicationSettings>();
            ShowTrackingOptionsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new TrackingOptionsWindowViewModel(Settings!);
                var result = await ShowTrackingOptionsDialog.Handle(dialogViewModel);
                return result;
            });

            // Wire up the database search dialog
            ShowDatabaseSearchDialog = new Interaction<DatabaseSearchWindowViewModel, DatabaseSearchCriteria>();
            ShowDatabaseSearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new DatabaseSearchWindowViewModel(DatabaseSearchCriteria);
                var result = await ShowDatabaseSearchDialog.Handle(dialogViewModel);
                return result;
            });
        }

        /// <summary>
        /// Initialise the tracker
        /// </summary>
        public void InitialiseTracker()
            => _liveView.Initialise(Logger!, Settings!);

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
        public void Search()
            => _databaseSearch.Search();

        /// <summary>
        /// Export the current search results to the specified file
        /// </summary>
        /// <param name="filePath"></param>
        public void Export(string filePath)
            => _databaseSearch.Export(filePath);
    }
}