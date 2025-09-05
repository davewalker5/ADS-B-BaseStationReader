using BaseStationReader.Entities.Tracking;
using BaseStationReader.UI.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace BaseStationReader.UI.ViewModels
{
    public class DatabaseSearchWindowViewModel : DatabaseSearchCriteria
    {
        public ObservableCollection<string> Statuses { get; private set; } = new();

        public ReactiveCommand<Unit, DatabaseSearchCriteria> SearchCommand { get; private set; }
        public ReactiveCommand<Unit, DatabaseSearchCriteria> CancelCommand { get; private set; }

        public DatabaseSearchWindowViewModel(DatabaseSearchCriteria initialValues)
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
            From = initialValues?.From;
            To = initialValues?.To;

            // Create a command that can be bound to the OK button on the dialog, that returns the selected
            // filter settings
#pragma warning disable CS8619
            SearchCommand = ReactiveCommand.Create(() =>
            {
                return this as DatabaseSearchCriteria;
            });
#pragma warning restore CS8619

            // Create a command that can be bound to the Cancel button on the dialog, that returns null
            CancelCommand = ReactiveCommand.Create(() => { return (DatabaseSearchCriteria)null; });
        }
    }
}
