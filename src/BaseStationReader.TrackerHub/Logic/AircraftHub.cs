using BaseStationReader.TrackerHub.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BaseStationReader.BusinessLogic.TrackerHub.Logic
{
    public class AircraftHub : Hub
    {
        private readonly IAircraftState _state;

        public AircraftHub(IAircraftState state)
            => _state = state;

        public override async Task OnConnectedAsync()
        {
            // Send a point-in-time snapshot so the client renders instantly
            await Clients.Caller.SendAsync("snapshot", _state.All());
            await base.OnConnectedAsync();
        }
    }
}