using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Configuration;
using BaseStationReader.Logic.Logging;
using BaseStationReader.UI.Models;
using BaseStationReader.UI.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace BaseStationReader.UI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private DispatcherTimer _timer = new DispatcherTimer();
        private ITrackerLogger? _logger = null;
        private ApplicationSettings? _settings = null;

        public MainWindow()
        {
            InitializeComponent();

            // Register the handlers for the dialogs
            this.WhenActivated(d => d(ViewModel!.ShowFiltersDialog.RegisterHandler(DoShowTrackingFiltersAsync)));
        }

        /// <summary>
        /// Handler called to initialise the window once it's fully loaded
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnLoaded(object? source, RoutedEventArgs e)
        {
            // Set the title, based on the version set in the project properties
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
            Title = $"Aircraft Database Viewer {info.FileVersion}";

            // Load the settings and configure the logger
            _settings = ConfigReader.Read("appsettings.json");
            _logger = new FileLogger();
            _logger.Initialise(_settings!.LogFile, _settings.MinimumLogLevel);

            // Configure the column titles and visibility
            ConfigureColumns(TrackedAircraftGrid);
            ConfigureColumns(DatabaseGrid);

            // Initialise the timer
            _timer.Interval = new TimeSpan(0, 0, 0, 0, _settings.RefreshInterval);
            _timer.Tick += OnTimerTick;

            // Get the view model from the data context and initialise the tracker
            var model = (MainWindowViewModel)DataContext!;
            model?.InitialiseTracker(_logger!, _settings!);
        }

        /// <summary>
        /// Configure column visibility on a data grid
        /// </summary>
        /// <param name="grid"></param>
        private void ConfigureColumns(DataGrid grid)
        {
            // Iterate over all the columns
            foreach (var column in grid.Columns)
            {
                // Find the corresponding column definition in the settings
                var definition = _settings!.Columns.Find(x => x.Property == column.Header.ToString());
                if (definition != null)
                {
                    // Found it, so apply the label
                    column.Header = definition.Label;
                }
                else
                {
                    // Not found, so hide the columns
                    column.IsVisible = false;
                }
            }
        }

        /// <summary>
        /// Handler to set the background colour of a row based on aircraft staleness
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnLoadingRow(object? source, DataGridRowEventArgs e)
        {
            var aircraft = e.Row.DataContext as Aircraft;
            if (aircraft != null)
            {
                switch (aircraft.Status)
                {
                    case TrackingStatus.Stale:
                        e.Row.Foreground = Brushes.Red;
                        break;
                    case TrackingStatus.Inactive:
                        e.Row.Foreground = Brushes.Yellow;
                        break;
                    default:
                        e.Row.Foreground = Brushes.White;
                        break;
                }
            }
        }

        /// <summary>
        /// Handler to refresh the display when the timer fires
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object? source, EventArgs e)
        {
            RefreshTrackedAircraftGrid();
        }

        /// <summary>
        /// Handler called to quit the application
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnExit(object source, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }

        /// <summary>
        /// Handler called to start tracking aircraft
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStartTracking(object source, RoutedEventArgs e)
        {
            // Check we're not already tracking
            if (!ViewModel!.IsTracking)
            {
                // Clear the current filters
                ViewModel!.LiveViewFilters = null;

                // Start tracking and perform an initial refresh
                ViewModel.StartTracking();
                _timer.Start();

                // Switch the state of the tracking menu options
                StartTrackingMenuItem.IsEnabled = false;
                StopTrackingMenuItem.IsEnabled = true;
                FilterLiveViewMenuItem.IsEnabled = true;
                ClearLiveViewFiltersMenuItem.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handler called to stop tracking aircraft
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStopTracking(object source, RoutedEventArgs e)
        {
            // Check we're currently tracking
            if (ViewModel!.IsTracking)
            {
                // Stop the timer and the tracker
                _timer.Stop();
                ViewModel.StopTracking();

                // Switch the state of the tracking menu options
                StartTrackingMenuItem.IsEnabled = true;
                StopTrackingMenuItem.IsEnabled = false;
                FilterLiveViewMenuItem.IsEnabled = false;
                ClearLiveViewFiltersMenuItem.IsEnabled = false;
            }
        }

        /// <summary>
        /// Handler to show the tracking filters dialog
        /// </summary>
        /// <param name="interaction"></param>
        /// <returns></returns>
        private async Task DoShowTrackingFiltersAsync(InteractionContext<FiltersWindowViewModel, SelectedFilters?> interaction)
        {
            // Create the dialog
            var dialog = new TrackingFiltersWindow();
            dialog.DataContext = interaction.Input;

            // Show the dialog and capture the results
            var result = await dialog.ShowDialog<SelectedFilters?>(this);
#pragma warning disable CS8604
            interaction.SetOutput(result);
#pragma warning restore CS8604

            // Capture the filters and refresh the tracked aircraft grid
            ViewModel!.LiveViewFilters = result;
            RefreshTrackedAircraftGrid();
        }

        /// <summary>
        /// Handler to clear the current live view filters
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnClearFilters(object source, RoutedEventArgs e)
        {
            ViewModel!.LiveViewFilters = null;
            RefreshTrackedAircraftGrid();
        }

        /// <summary>
        /// Refresh the aircraft tracking grid using the current filters
        /// </summary>
        private void RefreshTrackedAircraftGrid()
        {
            ViewModel!.RefreshTrackedAircraft();
            TrackedAircraftGrid.ItemsSource = ViewModel.TrackedAircraft;
        }
    }
}