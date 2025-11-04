using Microsoft.AspNetCore.SignalR;

namespace BaseStationReader.BusinessLogic.TrackerHub.Logic
{
    public class AircraftHub : Hub
    {
        public override async Task OnConnectedAsync()
            => await base.OnConnectedAsync();
    }
}