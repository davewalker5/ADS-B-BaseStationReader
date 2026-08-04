using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Hub;
using Microsoft.AspNetCore.SignalR;

namespace BaseStationReader.TrackerHub.Logic
{
    public class AircraftHub : Hub
    {
        private readonly ITrackerController _controller;

        /// <summary>
        /// Initialises a hub connection backed by the current tracker controller.
        /// </summary>
        /// <param name="controller">The running tracker controller.</param>
        public AircraftHub(ITrackerController controller)
            => _controller = controller;

        /// <summary>
        /// Sends the legacy snapshot and tracking options messages when a client connects.
        /// </summary>
        /// <returns>A task that completes after the initial state has been sent.</returns>
        public override async Task OnConnectedAsync()
        {
            // Send a point-in-time snapshot so the client renders instantly
            await Clients.Caller.SendAsync("snapshot", _controller.State);

            // Send the tracking parameters
            await Clients.Caller.SendAsync("trackingOptions", _controller.TrackingOptions);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Returns an authoritative point-in-time snapshot of all currently tracked aircraft.
        /// </summary>
        /// <returns>The current aircraft collection.</returns>
        public IReadOnlyCollection<TrackedAircraftDto> GetCurrentAircraft()
        {
            // Materialise the controller projection so SignalR serialises one consistent snapshot.
            return _controller.State.ToArray();
        }

        /// <summary>
        /// Returns the options for the active tracking profile.
        /// </summary>
        /// <returns>The current tracking options.</returns>
        public TrackingOptions GetTrackingOptions()
        {
            // Read the immutable options projection from the running controller.
            return _controller.TrackingOptions;
        }
    }
}
