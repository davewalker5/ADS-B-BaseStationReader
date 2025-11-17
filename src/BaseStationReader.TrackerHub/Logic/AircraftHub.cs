using BaseStationReader.Interfaces.Tracking;
using Microsoft.AspNetCore.SignalR;

namespace BaseStationReader.BusinessLogic.TrackerHub.Logic
{
    public class AircraftHub : Hub
    {
        private ITrackerController _controller;

        public AircraftHub(ITrackerController controller)
            => _controller = controller;

        public override async Task OnConnectedAsync()
        {
            // Send a point-in-time snapshot so the client renders instantly
            await Clients.Caller.SendAsync("snapshot", _controller.State);

            // Send the tracking parameters
            await Clients.Caller.SendAsync("trackingOptions", _controller.TrackingOptions);
            await base.OnConnectedAsync();
        }
    }
}