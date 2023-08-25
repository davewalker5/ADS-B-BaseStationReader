using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerTimer
    {
        event EventHandler<EventArgs>? Tick;
        void Start();
        void Stop();
    }
}
