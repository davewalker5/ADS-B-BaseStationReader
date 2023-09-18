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
        private readonly LiveViewModel _liveView = new LiveViewModel();
        private readonly DatabaseSearchModel _databaseSearch = new DatabaseSearchModel();

        public bool IsTracking { get { return _liveView.IsTracking; } }
        public ObservableCollection<Aircraft> TrackedAircraft { get {  return _liveView.TrackedAircraft; } }
        public BaseFilters? LiveViewFilters
        { 
            get { return _liveView.Filters; }
            set { _liveView.Filters = value; }
        }

        public ObservableCollection<Aircraft> SearchResults { get { return _databaseSearch.SearchResults; } }
        public DatabaseSearchCriteria? DatabaseSearchCriteria
        {
            get { return _databaseSearch.SearchCriteria; }
            set { _databaseSearch.SearchCriteria = value; }
        }

        public ApplicationSettings? Settings { get; set; }

        public ICommand ShowTrackingFiltersCommand { get; private set; }
        public Interaction<FiltersWindowViewModel,BaseFilters?> ShowFiltersDialog { get; private set; }

        public ICommand ShowDatabaseSearchCommand { get; private set; }
        public Interaction<DatabaseSearchWindowViewModel, DatabaseSearchCriteria?> ShowDatabaseSearchDialog { get; private set; }

        public ICommand ShowTrackingOptionsCommand { get; private set; }
        public Interaction<TrackingOptionsWindowViewModel, ApplicationSettings?> ShowTrackingOptionsDialog { get; private set; }

        public MainWindowViewModel()
        {
            // Wire up the tracking filters dialog
            ShowFiltersDialog = new Interaction<FiltersWindowViewModel, BaseFilters?>();
            ShowTrackingFiltersCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new FiltersWindowViewModel(LiveViewFilters);
                var result = await ShowFiltersDialog.Handle(dialogViewModel);
            });

            // Wire up the tracking options dialog
            ShowTrackingOptionsDialog = new Interaction<TrackingOptionsWindowViewModel, ApplicationSettings?>();
            ShowTrackingOptionsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new TrackingOptionsWindowViewModel(Settings);
                var result = await ShowTrackingOptionsDialog.Handle(dialogViewModel);
            });

            // Wire up the database search dialog
            ShowDatabaseSearchDialog = new Interaction<DatabaseSearchWindowViewModel, DatabaseSearchCriteria?>();
            ShowDatabaseSearchCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogViewModel = new DatabaseSearchWindowViewModel(DatabaseSearchCriteria);
                var result = await ShowDatabaseSearchDialog.Handle(dialogViewModel);
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
        public void Search()
            => _databaseSearch.Search();

        /// <summary>
        /// Export the current search results to the specified file
        /// </summary>
        /// <param name="filePath"></param>
        public void Export(string? filePath)
            => _databaseSearch.Export(filePath);
    }
}