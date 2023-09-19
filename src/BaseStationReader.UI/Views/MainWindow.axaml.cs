using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace BaseStationReader.UI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private DispatcherTimer _timer = new DispatcherTimer();
        private ITrackerLogger? _logger = null;

        public MainWindow()
        {
            InitializeComponent();

            // Register the handlers for the dialogs
            this.WhenActivated(d => d(ViewModel!.ShowFiltersDialog.RegisterHandler(DoShowTrackingFiltersAsync)));
            this.WhenActivated(d => d(ViewModel!.ShowTrackingOptionsDialog.RegisterHandler(DoShowTrackingOptionsAsync)));
            this.WhenActivated(d => d(ViewModel!.ShowDatabaseSearchDialog.RegisterHandler(DoShowDatabaseSearchAsync)));
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
            ViewModel!.Settings = new TrackerConfigReader().Read("appsettings.json");
            _logger = new FileLogger();
            _logger.Initialise(ViewModel!.Settings!.LogFile, ViewModel!.Settings.MinimumLogLevel);

            // Configure the column titles and visibility
            ConfigureColumns(TrackedAircraftGrid);
            ConfigureColumns(DatabaseGrid);
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
                var definition = ViewModel!.Settings!.Columns.Find(x => x.Property == column.Header.ToString());
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
        /// Handler to set menu item availability when the selected tab changes
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTabChanged(object source, SelectionChangedEventArgs e)
        {
            var selectedTab = Tabs?.SelectedIndex;
            switch (selectedTab)
            {
                case 0:
                case 1:
                    TrackingMenu.IsEnabled = true;
                    DatabaseMenu.IsEnabled = false;
                    break;
                case 2:
                    TrackingMenu.IsEnabled = false;
                    DatabaseMenu.IsEnabled = true;
                    break;
                default:
                    break;
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
        /// Handler called to start tracking aircraft
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStartTracking(object source, RoutedEventArgs e)
        {
            // Check we're not already tracking
            if (!ViewModel!.IsTracking)
            {
                // Initialise the timer
                _timer.Interval = new TimeSpan(0, 0, 0, 0, ViewModel!.Settings!.RefreshInterval);
                _timer.Tick += OnTimerTick;

                // Clear the current filters
                ViewModel!.LiveViewFilters = null;

                // Start tracking and perform an initial refresh
                ViewModel!.InitialiseTracker(_logger!, ViewModel.Settings);
                ViewModel.StartTracking();
                _timer.Start();

                // Switch the state of the tracking menu options
                StartTrackingMenuItem.IsEnabled = false;
                StopTrackingMenuItem.IsEnabled = true;
                FilterLiveViewMenuItem.IsEnabled = true;
                ClearLiveViewFiltersMenuItem.IsEnabled = true;
                TrackingOptionsMenuItem.IsEnabled = false;
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
                TrackingOptionsMenuItem.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handler to show the tracking filters dialog
        /// </summary>
        /// <param name="interaction"></param>
        /// <returns></returns>
        private async Task DoShowTrackingFiltersAsync(InteractionContext<FiltersWindowViewModel, BaseFilters?> interaction)
        {
            // Create the dialog
            var dialog = new FiltersWindow();
            dialog.DataContext = interaction.Input;

            // Show the dialog and capture the results
            var result = await dialog.ShowDialog<BaseFilters?>(this);
#pragma warning disable CS8604
            interaction.SetOutput(result);
#pragma warning restore CS8604

            // Check we have a dialog result i.e. user didn't cancel
            if (result != null)
            {
                // Capture the filters and refresh the tracked aircraft grid
                ViewModel!.LiveViewFilters = result;
                RefreshTrackedAircraftGrid();
            }
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
        /// <summary>
        /// Handler to show the tracking options dialog
        /// </summary>
        /// <param name="interaction"></param>
        /// <returns></returns>
        private async Task DoShowTrackingOptionsAsync(InteractionContext<TrackingOptionsWindowViewModel, TrackerApplicationSettings?> interaction)
        {
            // Create the dialog
            var dialog = new TrackingOptionsWindow();
            dialog.DataContext = interaction.Input;

            // Show the dialog and capture the results
            var result = await dialog.ShowDialog<TrackerApplicationSettings?>(this);
#pragma warning disable CS8604
            interaction.SetOutput(result);
#pragma warning restore CS8604

            // Check we have a dialog result i.e. user didn't cancel
            if (result != null)
            {
                // TODO : Reconfigure the tracker
            }
        }

        /// <summary>
        /// Handler to show the database search dialog
        /// </summary>
        /// <param name="interaction"></param>
        /// <returns></returns>
        private async Task DoShowDatabaseSearchAsync(InteractionContext<DatabaseSearchWindowViewModel, DatabaseSearchCriteria?> interaction)
        {
            // Create the dialog
            var dialog = new DatabaseSearchWindow();
            dialog.DataContext = interaction.Input;

            // Show the dialog and capture the results
            var result = await dialog.ShowDialog<DatabaseSearchCriteria?>(this);
#pragma warning disable CS8604
            interaction.SetOutput(result);
#pragma warning restore CS8604

            // Check we have a dialog result i.e. user didn't cancel
            if (result != null)
            {
                // Capture the search critera and perform the search
                ViewModel!.DatabaseSearchCriteria = result;
                RefreshDatabaseGrid();
            }
        }

        /// <summary>
        /// Refresh the database search grid using the current filters
        /// </summary>
        private void RefreshDatabaseGrid()
        {
            // Set a busy cursor
            var originalCursor = Cursor;
            Cursor = new Cursor(StandardCursorType.Wait);

            // Perform the search and refresh the grid
            ViewModel!.Search();
            DatabaseGrid.ItemsSource = ViewModel.SearchResults;

            // Enable/disable the export menu item based on whether there's any data to export
            ExportMenuItem.IsEnabled = ViewModel.SearchResults.Count > 0;

            // Restore the cursor
            Cursor = originalCursor;
        }

        /// <summary>
        /// Handler to export database search results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnExport(object sender, RoutedEventArgs args)
        {
            // Set up the file types
            var xlsxFileType = new FilePickerFileType("Excel Workbook")
            {
                Patterns = new List<string> { "*.xlsx" }
            };

            var csvFileType = new FilePickerFileType("Comma-separated values (CSV)")
            {
                Patterns = new List<string> { "*.csv" }
            };

            // Open the file selection dialog
            var file = Task.Run(() => StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Database Search Results",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    xlsxFileType,
                    csvFileType
                }

            })).Result;

            // Check the dialog wasn't cancelled
            if (file != null)
            {
                // Set a busy cursor
                var originalCursor = Cursor;
                Cursor = new Cursor(StandardCursorType.Wait);

                // Export the current results to the specified file
                ViewModel!.Export(file.Path.LocalPath);

                // Restore the original cursor
                Cursor = originalCursor;
            }
        }
    }
}