namespace BaseStationReader.BusinessLogic.Events
{
    public abstract class SubscriberNotifier
    {
        /// <summary>
        /// Fire-and-forget notification of subscribers to an event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        /// <param name="eventArgs"></param>
        public void NotifySubscribers<T>(object sender, EventHandler<T> handlers, T eventArgs) where T : EventArgs
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
                    catch (Exception)
                    {
                        // In principle, as the async notification isn't awaited so exceptions shouldn't bubble up but
                        // it's good practice to be defensive here
                    }
                }
            }
        }
    }
}