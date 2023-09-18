using BaseStationReader.Entities.Config;
using ReactiveUI;
using System.Reactive;

namespace BaseStationReader.UI.ViewModels
{
    public class TrackingOptionsWindowViewModel : ViewModelBase
    {
        public ApplicationSettings Settings { get; private set; } = new();
        public ReactiveCommand<Unit, ApplicationSettings?> SelectTrackingOptionsCommand { get; private set; }
        public ReactiveCommand<Unit, ApplicationSettings?> CancelTrackingOptionsCommand { get; private set; }

        public TrackingOptionsWindowViewModel(ApplicationSettings initialValues)
        {
            // Populate the initial values
            Settings = initialValues;

            // Create a command that can be bound to the OK button on the dialog, that returns the selected
            // options
#pragma warning disable CS8619
            SelectTrackingOptionsCommand = ReactiveCommand.Create(() =>
            {
                return Settings;
            });
#pragma warning restore CS8619

            // Create a command that can be bound to the Cancel button on the dialog, that returns null
            CancelTrackingOptionsCommand = ReactiveCommand.Create(() => { return (ApplicationSettings?)null; });
        }
    }
}
