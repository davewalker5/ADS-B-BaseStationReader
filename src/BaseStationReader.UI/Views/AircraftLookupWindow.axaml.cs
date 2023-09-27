using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using BaseStationReader.UI.ViewModels;
using ReactiveUI;
using System;

namespace BaseStationReader.UI;

public partial class AircraftLookupWindow : ReactiveWindow<AircraftLookupWindowViewModel>
{
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
    }

    /// <summary>
    /// Handler called to initiate a search
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void OnLookup(object source, RoutedEventArgs e)
    {
        // Store the current ICAO address
        ViewModel!.Address = Address.Text;

        // Search for the current ICAO address
        var details = ViewModel!.Search(Address.Text);
        if (details != null)
        {
            // Result is valid, so populate the text blocks with the aircraft details
            AirlineName.Text = details?.Airline?.Name ?? "";
            ManufacturerName.Text = details?.Model?.Manufacturer.Name ?? "";
            ModelName.Text = details?.Model?.Name ?? "";
            ModelIATA.Text = details?.Model?.IATA ?? "";
            ModelICAO.Text = details?.Model?.ICAO ?? "";
        }
    }
}