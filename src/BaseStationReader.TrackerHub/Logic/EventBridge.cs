using System.Threading.Channels;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Interfaces.Hub;
using Microsoft.AspNetCore.SignalR;

namespace BaseStationReader.TrackerHub.Logic
{
    public class EventBridge : BackgroundService, IEventBridge
    {
        private readonly Channel<object> _channel = Channel.CreateBounded<object>(
            new BoundedChannelOptions(4096)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

        private readonly IHubContext<AircraftHub> _hub;

        public EventBridge(IHubContext<AircraftHub> hub)
        {
            _hub = hub;
        }

        /// <summary>
        /// Publish an incoming tracked aircraft event on the channel
        /// </summary>
        /// <param name="e"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public ValueTask PublishAsync(AircraftNotificationEventArgs e, CancellationToken token = default)
            => _channel.Writer.WriteAsync(e, token);

        /// <inheritdoc />
        public ValueTask PublishResetAsync(TrackingOptions options, CancellationToken token = default)
            => _channel.Writer.WriteAsync(options, token);

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
                while (reader.TryRead(out var message))
                {
                    if (message is TrackingOptions options)
                    {
                        await _hub.Clients.All.SendAsync("trackingReset", options, token);
                        continue;
                    }

                    var e = (AircraftNotificationEventArgs)message;
                    if (e.Aircraft != null)
                    {
                        var aircraft = TrackedAircraftDto.FromTrackedAircraft(e.Aircraft);
                        switch (e.NotificationType)
                        {
                            case AircraftNotificationType.Unknown:
                                break;
                            case AircraftNotificationType.Removed:
                                await _hub.Clients.All.SendAsync("aircraftRemoved", aircraft, token);
                                break;
                            default:
                                await _hub.Clients.All.SendAsync("aircraftUpdate", aircraft, token);
                                break;
                        }
                    }
                }
            }
        }
    }
}
