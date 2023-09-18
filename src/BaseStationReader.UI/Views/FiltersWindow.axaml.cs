using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using BaseStationReader.UI.ViewModels;
using ReactiveUI;
using System;

namespace BaseStationReader.UI.Views
{
    public partial class FiltersWindow : ReactiveWindow<FiltersWindowViewModel>
    {
        public FiltersWindow()
        {
            InitializeComponent();

            // Register the dialog button handlers
            this.WhenActivated(a => a(ViewModel!.SelectFiltersCommand.Subscribe(Close)));
            this.WhenActivated(a => a(ViewModel!.CancelFiltersCommand.Subscribe(Close)));
        }

        /// <summary>
        /// Handler called to initialise the window once it's fully loaded
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnLoaded(object? source, RoutedEventArgs e)
        {
            Address.Text = ViewModel?.Address ?? "";
            Callsign.Text = ViewModel?.Callsign ?? "";
            StatusFilter.SelectedValue = ViewModel?.Status ?? "";
        }

        /// <summary>
        /// Handler to capture changes to the address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnAddressKeyUp(object sender, KeyEventArgs e)
        {
            ViewModel!.Address = Address.Text ?? "";
        }

        /// <summary>
        /// Handler to capture changes to the callsign
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnCallsignKeyUp(object sender, KeyEventArgs e)
        {
            ViewModel!.Callsign = Callsign.Text ?? "";
        }

        /// <summary>
        /// Handler to capture changes in the selected status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnStatusChanged(object sender, SelectionChangedEventArgs e)
        {
            var status = StatusFilter.SelectedValue as string;
            ViewModel!.Status = status ?? "";
        }
    }
}
