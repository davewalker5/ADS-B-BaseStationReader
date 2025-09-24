using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.Lookup.Logic
{
    internal abstract class CommandHandlerBase
    {
        protected LookupToolApplicationSettings Settings { get; private set; }
        protected LookupToolCommandLineParser Parser { get; private set; }
        protected ITrackerLogger Logger { get; private set; }
        protected BaseStationReaderDbContext Context { get; private set; }

        public CommandHandlerBase(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context)
        {
            Settings = settings;
            Parser = parser;
            Logger = logger;
            Context = context;
        }

        /// <summary>
        /// Handle the command
        /// </summary>
        /// <returns></returns>
#pragma warning disable CS1998
        public virtual async Task Handle()
        {
        }
#pragma warning restore CS1998
    }
}