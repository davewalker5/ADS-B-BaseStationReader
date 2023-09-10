using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
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

            // Set the interval text
            var refreshIntervalSeconds = (int)(_settings.RefreshInterval / 1000);
            RefreshInterval.Text = refreshIntervalSeconds.ToString();

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
        /// Handler called to start/stop tracking aircraft
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStartStopTracking(object source, RoutedEventArgs e)
        {
            // Get the view model from the data context
            var model = DataContext as MainWindowViewModel;
            if (model != null)
            {
                if (model.IsTracking)
                {
                    // Stop the timer and the tracker
                    _timer.Stop();
                    model.Stop();
                    StartStop.Content = "Start";
                }
                else
                {
                    // Start tracking and perform an initial refresh
                    StartStop.Content = "Stop";
                    model.Start();
                    _timer.Start();
                }
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
        /// Handler to set the refresh interval when the interval text is updated
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnRefreshIntervalChanged(object? source, TextChangedEventArgs e)
        {
            // Get an integer interval, in seconds, from the refresh interval text box. If successful, and
            // the interval is valid, set the timer interval
            if (int.TryParse(RefreshInterval.Text, out int interval) && (interval > 0))
            {
                _timer.Interval = new TimeSpan(0, 0, interval);
            }
        }

        /// <summary>
        /// Handler to clear the current filters, resetting them to their defaults
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnClearFilters(object source, RoutedEventArgs e)
        {
            AddressFilter.Text = "";
            CallsignFilter.Text = "";
            StatusFilter.SelectedIndex = 0;
        }

        /// <summary>
        /// Refresh the aircraft tracking grid using the current filters
        /// </summary>
        private void RefreshTrackedAircraftGrid()
        {
            // Get the model from the data context
            var model = DataContext as MainWindowViewModel;
            if (model != null)
            {
                // Set a busy cursor
                var original = Cursor;
                Cursor = new Cursor(StandardCursorType.Wait);

                // Get the aircraft address and callsign filters
                var address = AddressFilter.Text;
                var callsign = CallsignFilter.Text;

                // Get the aircraft status filter
                var status = StatusFilter.SelectedValue as string;
                if ((status != null) && status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    status = null;
                }

                // Refresh, filtering by the specified status
                model.Refresh(address, callsign, status);
                TrackedAircraftGrid.ItemsSource = model.TrackedAircraft;

                // Restore the cursor
                Cursor = original;
            }
        }
    }
}