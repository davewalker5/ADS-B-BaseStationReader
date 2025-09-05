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
    private void OnLoaded(object source, RoutedEventArgs e)
    {
        Address.Text = ViewModel?.Address ?? "";
        if (!string.IsNullOrEmpty(Address.Text))
        {
            LookupAircraftAndFlightDetails();
        }
    }

    /// <summary>
    /// Handler called to initiate a search
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void OnLookup(object source, RoutedEventArgs e)
    {
        LookupAircraftAndFlightDetails();
    }

    /// <summary>
    /// Look up the aircraft and active flight details and populate the dialog with the results
    /// </summary>
    private void LookupAircraftAndFlightDetails()
    {
        // Set a busy cursor
        var originalCursor = Cursor;
        Cursor = new Cursor(StandardCursorType.Wait);

        // Search for the current ICAO address
        var aircraftDetails = ViewModel!.LookupAircraft(Address.Text);
        var flightDetails = ViewModel!.LookupActiveFlight(Address.Text);

        // Do we have valid aircraft details?
        if (aircraftDetails != null)
        {
            // Aircraft details are valid, so populate the text blocks with the aircraft details
            AirlineName.Text = aircraftDetails.Airline?.Name ?? DetailsNotAvailableText;
            ManufacturerName.Text = aircraftDetails.Model?.Manufacturer.Name ?? DetailsNotAvailableText;
            ModelName.Text = aircraftDetails.Model?.Name ?? DetailsNotAvailableText;
            ModelIATA.Text = aircraftDetails.Model?.IATA ?? DetailsNotAvailableText;
            ModelICAO.Text = aircraftDetails.Model?.ICAO ?? DetailsNotAvailableText;
        }
        else
        {
            // No details available, so set the default "not available" text
            AirlineName.Text = DetailsNotAvailableText;
            ManufacturerName.Text = DetailsNotAvailableText;
            ModelName.Text = DetailsNotAvailableText;
            ModelIATA.Text = DetailsNotAvailableText;
            ModelICAO.Text = DetailsNotAvailableText;
        }

        // Do we have valid flight details?
        if (aircraftDetails != null)
        {
            // Flight details are valid, so populate the text blocks with the flight details
            FlightNumber.Text = flightDetails?.FlightNumberIATA ?? DetailsNotAvailableText;
            DepartureIATA.Text = flightDetails?.DepartureAirportIATA ?? DetailsNotAvailableText;
            DestinationIATA.Text = flightDetails?.DestinationAirportIATA ?? DetailsNotAvailableText;
        }
        else
        {
            // No details available, so set the default "not available" text
            FlightNumber.Text = DetailsNotAvailableText;
            DepartureIATA.Text = DetailsNotAvailableText;
            DestinationIATA.Text = DetailsNotAvailableText;
        }

        // Restore the cursor
        Cursor = originalCursor;
    }
}