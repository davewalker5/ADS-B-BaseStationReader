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
        private readonly TimeSpan _refreshInterval;

        public EventBridge(IHubContext<AircraftHub> hub, IConfiguration configuration)
        {
            _hub = hub;
            var refreshInterval = configuration.GetValue<int?>("ApplicationSettings:RefreshInterval") ?? 1000;
            _refreshInterval = TimeSpan.FromMilliseconds(Math.Max(100, refreshInterval));
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
                // Open one collection window, then publish only the final state received for each
                // aircraft. This prevents a slow client from replaying obsolete intermediate states.
                await Task.Delay(_refreshInterval, token);
                var pendingAircraft = new Dictionary<string, AircraftNotificationEventArgs>(
                    StringComparer.OrdinalIgnoreCase);
                TrackingOptions pendingReset = null;

                while (reader.TryRead(out var message))
                {
                    if (message is TrackingOptions options)
                    {
                        pendingReset = options;
                        pendingAircraft.Clear();
                        continue;
                    }

                    var e = (AircraftNotificationEventArgs)message;
                    if (e.Aircraft != null)
                    {
                        pendingAircraft[e.Aircraft.Address] = e;
                    }
                }

                if (pendingReset is not null)
                {
                    await _hub.Clients.All.SendAsync("trackingReset", pendingReset, token);
                }

                foreach (var e in pendingAircraft.Values)
                {
                    var aircraft = TrackedAircraftDto.FromTrackedAircraft(e.Aircraft);
                    if (e.NotificationType == AircraftNotificationType.Removed)
                    {
                        await _hub.Clients.All.SendAsync("aircraftRemoved", aircraft, token);
                    }
                    else if (e.NotificationType != AircraftNotificationType.Unknown)
                    {
                        await _hub.Clients.All.SendAsync("aircraftUpdate", aircraft, token);
                    }
                }
            }
        }
    }
}
