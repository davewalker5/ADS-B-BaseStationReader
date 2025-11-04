using System.Threading.Channels;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Entities;
using BaseStationReader.TrackerHub.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BaseStationReader.BusinessLogic.TrackerHub.Logic
{
    public class EventBridge : BackgroundService, IEventBridge
    {
        private readonly Channel<AircraftNotificationEventArgs> _channel = Channel.CreateBounded<AircraftNotificationEventArgs>(
            new BoundedChannelOptions(4096)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

        private readonly IHubContext<AircraftHub> _hub;
        private readonly ITrackerLogger _logger;

        public EventBridge(IHubContext<AircraftHub> hub, ITrackerLogger logger)
        {
            _hub = hub;
            _logger = logger;
        }

        /// <summary>
        /// Publish an incoming tracked aircraft event on the channel
        /// </summary>
        /// <param name="e"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public ValueTask PublishAsync(AircraftNotificationEventArgs e, CancellationToken token = default)
            => _channel.Writer.WriteAsync(e, token);

        /// <summary>
        /// Process pending events from the channel to the clients
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var reader = _channel.Reader;
            while (await reader.WaitToReadAsync(token))
            {
                while (reader.TryRead(out var e))
                {
                    if (e.Aircraft != null)
                    {
                        var aircraft = TrackedAircraftDto.FromTrackedAircraft(e.Aircraft);
                        switch (e.NotificationType)
                        {
                            case AircraftNotificationType.Unknown:
                                break;
                            case AircraftNotificationType.Removed:
                                _logger.LogMessage(Severity.Info, $"Handling removal message for aircraft {aircraft.Address}");
                                await _hub.Clients.All.SendAsync("aircraftRemoved", aircraft, token);
                                break;
                            default:
                                _logger.LogMessage(Severity.Verbose, $"Handling update message for aircraft {aircraft.Address}");
                                await _hub.Clients.All.SendAsync("aircraftUpdate", aircraft, token);
                                break;
                        }
                    }
                }
            }
        }
    }
}
