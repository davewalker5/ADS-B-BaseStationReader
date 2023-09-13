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
            ConfigureColumns(TrackedAircraftGrid);
            ConfigureColumns(DatabaseGrid);

            // Initialise the timer
            _timer.Interval = new TimeSpan(0, 0, 0, 0, _settings.RefreshInterval);
            _timer.Tick += OnTimerTick;

            // Set the interval text
            int refreshIntervalSeconds = _settings.RefreshInterval / 1000;
            RefreshInterval.Text = refreshIntervalSeconds.ToString();

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
                    model.StopTracking();
                    StartStop.Content = "Start";
                }
                else
                {
                    // Start tracking and perform an initial refresh
                    StartStop.Content = "Stop";
                    model.StartTracking();
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
        /// Handler to clear the live view filters, resetting them to their defaults
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnClearLiveFilters(object source, RoutedEventArgs e)
        {
            LiveAddressFilter.Text = "";
            LiveCallsignFilter.Text = "";
            LiveStatusFilter.SelectedIndex = 0;
            RefreshTrackedAircraftGrid();
        }

        /// <summary>
        /// Handler to clear the database search filters, resetting them to their defaults
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnClearDbFilters(object source, RoutedEventArgs e)
        {
            DbAddressFilter.Text = "";
            DbCallsignFilter.Text = "";
            DbStatusFilter.SelectedIndex = 0;
            DbFromDate.SelectedDate = null;
            DbToDate.SelectedDate = null;
            RefreshDatabaseGrid();
        }

        /// <summary>
        /// Handler to search the database using current filtering criteria
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnSearchDatabase(object source, RoutedEventArgs e)
        {
            RefreshDatabaseGrid();
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
                // Get the aircraft address and callsign filters
                var address = LiveAddressFilter.Text;
                var callsign = LiveCallsignFilter.Text;

                // Get the aircraft status filter
                var status = LiveStatusFilter.SelectedValue as string;
                if ((status != null) && status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    status = null;
                }

                // Refresh, filtering by the specified status
                model.RefreshTrackedAircraft(address, callsign, status);
                TrackedAircraftGrid.ItemsSource = model.TrackedAircraft;
            }
        }

        /// <summary>
        /// Refresh the database search grid using the current filters
        /// </summary>
        private void RefreshDatabaseGrid()
        {
            // Get the model from the data context
            var model = DataContext as MainWindowViewModel;
            if (model != null)
            {
                // Set a busy cursor
                var originalCursor = Cursor;
                Cursor = new Cursor(StandardCursorType.Wait);

                // Get the aircraft status filter
                var status = DbStatusFilter.SelectedValue as string;
                if ((status != null) && status.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    status = null;
                }

                // Get the from and to dates
                var from = GetDateFromDatePicker(DbFromDate);
                var to = GetDateFromDatePicker(DbToDate);

                // Perform the search and refresh the grid
                model.Search(DbAddressFilter.Text, DbCallsignFilter.Text, status, from, to);
                DatabaseGrid.ItemsSource = model.SearchResults;

                // Restore the cursor
                Cursor = originalCursor;
            }
        }

        /// <summary>
        /// Extract a date from a datepicker, ignoring time and timezone
        /// </summary>
        /// <param name="picker"></param>
        /// <returns></returns>
        private DateTime? GetDateFromDatePicker(DatePicker picker)
        {
            DateTime? date = null;

            // Check the picker has a selected date that has a value
            if ((picker.SelectedDate != null) && picker.SelectedDate.HasValue)
            {
                // Extract the year, month and day
                var year = picker.SelectedDate.Value.Year;
                var month = picker.SelectedDate.Value.Month;
                var day = picker.SelectedDate.Value.Day;

                // Create a new date from the extracted values
                date = new DateTime(year, month, day);
            }

            return date;
        }
    }
}