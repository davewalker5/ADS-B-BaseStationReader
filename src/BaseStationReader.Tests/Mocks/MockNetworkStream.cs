using System.Text;

namespace BaseStationReader.Tests.Mocks
{
    internal class MockNetworkStream
    {
        private readonly byte[] _buffer;
        private long _offset = 0;

        public int ReadTimeout { get; set; }

        public MockNetworkStream(byte[] buffer)
            => _buffer = buffer;

        /// <summary>
        /// Read the next line from the buffer
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> ReadLineAsync(CancellationToken token)
        {
            // Read the next line from the buffer and if it's not blank then return it
            var line = ReadNextLineFromBuffer();
            if (!string.IsNullOrEmpty(line))
            {
                return line;
            }

            // Nothing left in the buffer so we block until either cancellation is requested or
            // the read timeout expires 
            await Task.Delay(ReadTimeout, token);
            return null;
        }

        /// <summary>
        /// Read up to the next newline, or the end of the buffer, and return the result as a string
        /// </summary>
        /// <returns></returns>
        private string ReadNextLineFromBuffer()
        {
            var builder = new StringBuilder();
            char character = default;

            // Loop until we hit a newline or there's nothing left
            while ((character != '\n') && (_offset < _buffer.Length))
            {
                // Get the next byte as a character and move on to the next position
                character = (char)_buffer[_offset];
                _offset++;

                // If this isn't a newline, append it
                if (character != '\n')
                {
                    builder.Append(character);
                }
            }

            // Return the contents of the builder as a string
            return builder.ToString();
        }
    }
}