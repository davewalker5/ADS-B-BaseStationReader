using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using BaseStationReader.UI.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;

namespace BaseStationReader.UI.Views
{
    public partial class DatabaseSearchWindow : ReactiveWindow<DatabaseSearchWindowViewModel>
    {
        public DatabaseSearchWindow()
        {
            InitializeComponent();

            // Register the dialog button handlers
            this.WhenActivated(a => a(ViewModel!.SearchCommand.Subscribe(Close)));
            this.WhenActivated(a => a(ViewModel!.CancelCommand.Subscribe(Close)));
        }

        /// <summary>
        /// Handler called to initialise the window once it's fully loaded
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnLoaded(object source, RoutedEventArgs e)
        {
            Address.Text = ViewModel?.Address ?? "";
            Callsign.Text = ViewModel?.Callsign ?? "";
            StatusFilter.SelectedValue = ViewModel?.Status ?? "";
            FromDate.SelectedDate = ViewModel?.From;
            ToDate.SelectedDate = ViewModel?.To;
        }

        /// <summary>
        /// Handler to capture changes to the address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAddressKeyUp(object sender, KeyEventArgs e)
        {
            ViewModel!.Address = Address.Text ?? "";
        }

        /// <summary>
        /// Handler to capture changes to the callsign
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCallsignKeyUp(object sender, KeyEventArgs e)
        {
            ViewModel!.Callsign = Callsign.Text ?? "";
        }

        /// <summary>
        /// Handler to capture changes in the selected status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStatusChanged(object sender, SelectionChangedEventArgs e)
        {
            var status = StatusFilter.SelectedValue as string;
            ViewModel!.Status = status ?? "";
        }

        /// <summary>
        /// Handler to capture the date from the "from" date picker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFromDateChanged(object sender, DatePickerSelectedValueChangedEventArgs e)
        {
            ViewModel!.From = GetDateFromOffset(e.NewDate);
            Debug.Print(ViewModel.From.ToString() ?? "");
        }

        /// <summary>
        /// Handler to capture the date from the "to" date picker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnToDateChanged(object sender, DatePickerSelectedValueChangedEventArgs e)
        {
            ViewModel!.From = GetDateFromOffset(e.NewDate);
            Debug.Print(ViewModel.To.ToString() ?? "");
        }

        /// <summary>
        /// Extract a date from a date time offser, ignoring time and timezone
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private DateTime? GetDateFromOffset(DateTimeOffset? offset)
        {
            DateTime? date = null;

            // Check the picker has a selected date that has a value
            if ((offset != null) && offset.HasValue)
            {
                // Extract the year, month and day
                var year = offset.Value.Year;
                var month = offset.Value.Month;
                var day = offset.Value.Day;

                // Create a new date from the extracted values
                date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
            }

            return date;
        }
    }
}