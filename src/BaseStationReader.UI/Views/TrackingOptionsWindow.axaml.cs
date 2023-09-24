using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using BaseStationReader.UI.ViewModels;
using ReactiveUI;
using System;

namespace BaseStationReader.UI;

public partial class TrackingOptionsWindow : ReactiveWindow<TrackingOptionsWindowViewModel>
{
    public TrackingOptionsWindow()
    {
        InitializeComponent();

        // Register the dialog button handlers
        this.WhenActivated(a => a(ViewModel!.SelectTrackingOptionsCommand.Subscribe(Close)));
        this.WhenActivated(a => a(ViewModel!.CancelTrackingOptionsCommand.Subscribe(Close)));
    }

    /// <summary>
    /// Handler called to initialise the window once it's fully loaded
    /// </summary>
    /// <param name="source"></param>
    /// <param name="e"></param>
    private void OnLoaded(object? source, RoutedEventArgs e)
    {
        Host.Text = ViewModel?.Settings?.Host ?? "";
        Port.Text = ViewModel?.Settings?.Port.ToString() ?? "";
        SocketReadTimeout.Text = ViewModel?.Settings?.SocketReadTimeout.ToString() ?? "";
        TimeToInactive.Text = ViewModel?.Settings?.TimeToRecent.ToString() ?? "";
        TimeToStale.Text = ViewModel?.Settings?.TimeToStale.ToString() ?? "";
        TimeToRemoval.Text = ViewModel?.Settings?.TimeToRemoval.ToString() ?? "";
        TimeToLocked.Text = ViewModel?.Settings?.TimeToLock.ToString() ?? "";
        RefreshInterval.Text = ViewModel?.Settings?.RefreshInterval.ToString() ?? "";
        ReceiverLatitude.Text = ViewModel?.Settings?.ReceiverLatitude?.ToString("N6") ?? "";
        ReceiverLongitude.Text = ViewModel?.Settings?.ReceiverLongitude?.ToString("N6") ?? "";
    }

    /// <summary>
    /// Handler to capture changes to the host
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnHostKeyUp(object sender, KeyEventArgs e)
    {
        ViewModel!.Settings.Host = Host.Text ?? "";
    }

    /// <summary>
    /// Handler to capture changes to the port
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnPortKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(Port.Text, out int port))
        {
            ViewModel!.Settings.Port = port;
        }
    }

    /// <summary>
    /// Handler to capture changes to the socket read timeout
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnSocketReadTimeoutKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(SocketReadTimeout.Text, out int readTimeout))
        {
            ViewModel!.Settings.SocketReadTimeout = readTimeout;
        }
    }

    /// <summary>
    /// Handler to capture changes to the inactive timeout
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnTimeToInactiveKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(TimeToInactive.Text, out int timeout))
        {
            ViewModel!.Settings.TimeToRecent = timeout;
        }
    }

    /// <summary>
    /// Handler to capture changes to the stale timeout
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnTimeToStaleKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(TimeToInactive.Text, out int timeout))
        {
            ViewModel!.Settings.TimeToStale = timeout;
        }
    }

    /// <summary>
    /// Handler to capture changes to the removal timeout
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnTimeToRemovalKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(TimeToRemoval.Text, out int timeout))
        {
            ViewModel!.Settings.TimeToRemoval = timeout;
        }
    }

    /// <summary>
    /// Handler to capture changes to the locked timeout
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnTimeToLockedKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(TimeToLocked.Text, out int timeout))
        {
            ViewModel!.Settings.TimeToLock = timeout;
        }
    }

    /// <summary>
    /// Handler to capture changes to the refresh interval
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnRefreshIntervalKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(RefreshInterval.Text, out int interval))
        {
            ViewModel!.Settings.RefreshInterval = interval;
        }
    }

    /// <summary>
    /// Handler to capture changes to the receiver latitude
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnReceiverLatitudeKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(ReceiverLatitude.Text, out int latitude))
        {
            ViewModel!.Settings.ReceiverLatitude = latitude;
        }
    }

    /// <summary>
    /// Handler to capture changes to the receiver longitude
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnReceiverLongitudeKeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(ReceiverLongitude.Text, out int longitude))
        {
            ViewModel!.Settings.ReceiverLongitude = longitude;
        }
    }
}