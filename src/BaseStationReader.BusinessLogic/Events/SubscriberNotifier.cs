using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Events
{
    public abstract class SubscriberNotifier
    {
        protected ITrackerLogger Logger { get; private set; }

        public SubscriberNotifier(ITrackerLogger logger)
            => Logger = logger;

        /// <summary>
        /// Fire-and-forget notification of subscribers to an event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        /// <param name="eventArgs"></param>
        protected void NotifySubscribers<T>(object sender, EventHandler<T> handlers, T eventArgs) where T : EventArgs
        {
            // Get the invocation list for the event handlers
            var invocationList = handlers?.GetInvocationList();
            if (invocationList != null)
            {
                // Iterate over each of the delegates in the list
                foreach (var handler in invocationList)
                {
                    try
                    {
                        // Fire-and-forget subscriber notification
                        _ = Task.Run(() => ((EventHandler<T>)handler)?.Invoke(sender, eventArgs));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMessage(Severity.Error, ex.Message);
                        Logger.LogException(ex);
                    }
                }
            }
        }
    }
}