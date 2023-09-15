using BaseStationReader.Entities.Tracking;
using BaseStationReader.UI.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace BaseStationReader.UI.ViewModels
{
    public class FiltersWindowViewModel : SelectedFilters
    {
        public ObservableCollection<string> Statuses { get; private set; } = new();

        public ReactiveCommand<Unit, SelectedFilters?> SelectFiltersCommand { get; private set; }
        public ReactiveCommand<Unit, SelectedFilters?> CancelFiltersCommand { get; private set; }

        public FiltersWindowViewModel(SelectedFilters? initialValues)
        {
            // Populate the list of available statuses
            Statuses.Add("All");
            Statuses.Add(TrackingStatus.Active.ToString());
            Statuses.Add(TrackingStatus.Inactive.ToString());
            Statuses.Add(TrackingStatus.Stale.ToString());

            // Populate from the initial values, if specified
            Address = initialValues?.Address ?? "";
            Callsign = initialValues?.Callsign ?? "";
            Status = initialValues?.Status ?? "";

            // Create a command that can be bound to the OK button on the dialog, that returns the selected
            // filter settings
#pragma warning disable CS8619
            SelectFiltersCommand = ReactiveCommand.Create(() =>
            {
                return this as SelectedFilters;
            });
#pragma warning restore CS8619

            // Create a command that can be bound to the Cancel button on the dialog, that returns null
            CancelFiltersCommand = ReactiveCommand.Create(() => { return (SelectedFilters?)null; });
        }
    }
}
