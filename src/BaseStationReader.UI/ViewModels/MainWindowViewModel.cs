using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Tracking;
using System.Collections.ObjectModel;
using System.Linq;

namespace BaseStationReader.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static ITrackerWrapper? _wrapper = null;

        public ObservableCollection<Aircraft> TrackedAircraft { get; private set; } = new();

        /// <summary>
        /// Initialise the tracker
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        public void Initialise(ITrackerLogger logger, ApplicationSettings settings)
        {
            _wrapper = new TrackerWrapper(logger, settings);
            _wrapper.Initialise();
        }

        /// <summary>
        /// Start the tracker
        /// </summary>
        public void Start()
            => _wrapper!.Start();

        /// <summary>
        /// Stop the tracker
        /// </summary>
        public void Stop()
            => _wrapper!.Stop();

        /// <summary>
        /// Refresh the tracked aircraft collection
        /// </summary>
        public void Refresh()
        {
            var aircraft = _wrapper!.TrackedAircraft.Values.ToList();
            TrackedAircraft = new ObservableCollection<Aircraft>(aircraft);
        }
    }
}