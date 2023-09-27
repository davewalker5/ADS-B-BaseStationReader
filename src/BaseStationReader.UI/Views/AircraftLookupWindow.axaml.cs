using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using BaseStationReader.UI.ViewModels;
using ReactiveUI;
using System;

namespace BaseStationReader.UI;

public partial class AircraftLookupWindow : ReactiveWindow<AircraftLookupWindowViewModel>
{
    private const string DetailsNotAvailableText = "Not available";

    public AircraftLookupWindow()
    {
        InitializeComponent();

        // Register the dialog button handlers
        this.WhenActivated(a => a(ViewModel!.CloseCommand.Subscribe(Close)));
    }

    /// <summary>
    /// Handler called to initialise the window once it's fully loaded
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void OnLoaded(object? source, RoutedEventArgs e)
    {
        Address.Text = ViewModel?.Address ?? "";
        if (!string.IsNullOrEmpty(Address.Text))
        {
            LookupAircraftDetails();
        }
    }

    /// <summary>
    /// Handler called to initiate a search
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void OnLookup(object source, RoutedEventArgs e)
    {
        LookupAircraftDetails();
    }

    /// <summary>
    /// Look up the aircraft details and populate the dialog with the results
    /// </summary>
    private void LookupAircraftDetails()
    {
        // Search for the current ICAO address
        var originalCursor = Cursor;
        Cursor = new Cursor(StandardCursorType.Wait);
        var details = ViewModel!.Search(Address.Text);
        Cursor = originalCursor;

        // Check we have some valid details
        if (details != null)
        {
            // Result is valid, so populate the text blocks with the aircraft details
            AirlineName.Text = details?.Airline?.Name ?? DetailsNotAvailableText;
            ManufacturerName.Text = details?.Model?.Manufacturer.Name ?? DetailsNotAvailableText;
            ModelName.Text = details?.Model?.Name ?? DetailsNotAvailableText;
            ModelIATA.Text = details?.Model?.IATA ?? DetailsNotAvailableText;
            ModelICAO.Text = details?.Model?.ICAO ?? DetailsNotAvailableText;
        }
        else
        {
            // No details availables, so set the default "not available" text
            AirlineName.Text = DetailsNotAvailableText;
            ManufacturerName.Text = DetailsNotAvailableText;
            ModelName.Text = DetailsNotAvailableText;
            ModelIATA.Text = DetailsNotAvailableText;
            ModelICAO.Text = DetailsNotAvailableText;
        }
    }
}