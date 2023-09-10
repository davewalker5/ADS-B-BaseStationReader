using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Configuration;
using BaseStationReader.Logic.Logging;
using BaseStationReader.UI.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BaseStationReader.UI.Views
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer = new DispatcherTimer();
        private ITrackerLogger? _logger = null;
        private ApplicationSettings? _settings = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handler called to initialise the main window once it's fully loaded
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
            foreach (var column in TrackedAircraftGrid.Columns)
            {
                var definition = _settings!.Columns.FirstOrDefault(x => x.Property == column.Header.ToString());
                if (definition != null)
                {
                    column.Header = definition.Label;
                }
                else
                {
                    column.IsVisible = false;
                }
            }

            // Initialise the timer
            _timer.Interval = new TimeSpan(0, 0, 0, 0, _settings.RefreshInterval);
            _timer.Tick += OnTimerTick;

            // Get the view model from the data context and initialise the tracker
            var model = (MainWindowViewModel)DataContext!;
            model?.Initialise(_logger!, _settings!);
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
                        e.Row.Background = Brushes.Red;
                        e.Row.Foreground = Brushes.White;
                        break;
                    case TrackingStatus.Inactive:
                        e.Row.Background = Brushes.Yellow;
                        e.Row.Foreground = Brushes.Black;
                        break;
                    default:
                        e.Row.Background = Brushes.White;
                        e.Row.Foreground = Brushes.Black;
                        break;
                }
            }
        }

        /// <summary>
        /// Handler called to begin tracking aircraft
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStartTracking(object source, RoutedEventArgs e)
        {
            // Get the view model from the data context
            var model = DataContext as MainWindowViewModel;
            if (model != null)
            {
                // Start tracking and perform an initial refresh
                model.Start();
                _timer.Start();
            }
        }

        /// <summary>
        /// Handler called to stop tracking aircraft
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStopTracking(object source, RoutedEventArgs e)
        {
            // Stop the timer
            _timer.Stop();

            // Get the view model from the data context
            var model = DataContext as MainWindowViewModel;
            if (model != null)
            {
                // Stop the tracker
                model.Stop();
            }
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
        /// Handler to refresh the display when the timer fires
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object? source, EventArgs e)
        {
            RefreshTrackedAircraftGrid();
        }

        /// <summary>
        /// Handler to refresh the display when the status filter changes
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStatusFilterChanged(object? source, SelectionChangedEventArgs e)
        {
            RefreshTrackedAircraftGrid();
        }

        private void RefreshTrackedAircraftGrid()
        {
            var model = DataContext as MainWindowViewModel;
            if (model != null)
            {
                // Get the aircraft status filter
                var status = StatusFilter.SelectedValue as string;
                if ((status != null) && status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    status = null;
                }

                // Refresh, filtering by the specified status
                model.Refresh(status);
                TrackedAircraftGrid.ItemsSource = model.TrackedAircraft;
            }
        }
    }
}