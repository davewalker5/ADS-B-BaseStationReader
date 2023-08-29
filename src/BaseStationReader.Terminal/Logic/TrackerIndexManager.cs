using BaseStationReader.Terminal.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Terminal.Logic
{
    public class TrackerIndexManager : ITrackerIndexManager
    {
        private readonly Dictionary<string, int> _rowIndex = new();

        /// <summary>
        /// Add an aircraft ICAO address to the index associated with a given row number
        /// </summary>
        /// <param name="address"></param>
        /// <param name="rowNumber"></param>
        public void AddAircraft(string address, int rowNumber)
        {
            lock (_rowIndex)
            {
                if (!_rowIndex.ContainsKey(address))
                {
                    Shuffle(rowNumber, 1);
                    _rowIndex.Add(address, rowNumber);
                }
            }
        }

        /// <summary>
        /// Find an aircraft ICAO address in the index and return its row number
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int FindAircraft(string address)
        {
            int rowNumber = -1;

            lock (_rowIndex)
            {
                if (_rowIndex.ContainsKey(address))
                {
                    rowNumber = _rowIndex[address];
                }
            }

            return rowNumber;
        }

        /// <summary>
        /// Remove the entry for the specified ICAO address and return the row it was at before removal
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int RemoveAircraft(string address)
        {
            int rowNumber = -1;

            lock (_rowIndex)
            {
                if (_rowIndex.ContainsKey(address))
                {
                    rowNumber = _rowIndex[address];
                    _rowIndex.Remove(address);
                    Shuffle(rowNumber, -1);
                }
            }

            return rowNumber;
        }

        /// <summary>
        /// Shuffle the index for all rows *after* the one specified, either up or down. The assumption is that
        /// the caller will take out a lock to prevent concurrent attempts
        /// </summary>
        /// <param name="fromRow"></param>
        private void Shuffle(int fromRow, int increment)
        {
            foreach (var entry in _rowIndex)
            {
                if (entry.Value >= fromRow)
                {
                    _rowIndex[entry.Key] += increment;
                }
            }
        }
    }
}
