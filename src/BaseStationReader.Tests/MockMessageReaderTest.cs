using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Tests.Mocks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Tests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class MockMessageReaderTest
    {
        private readonly List<string> _received = new();

        [TestMethod]
        public async Task TestMockMessageReader()
        {
            string[] messages = new string[] {
                "MSG,8,1,1,3965A3,1,2023/08/23,12:07:27.929,2023/08/23,12:07:28.005,,,,,,,,,,,,0",
                "MSG,6,1,1,3965A3,1,2023/08/23,12:07:27.932,2023/08/23,12:07:28.006,,,,,,,,6303,0,0,0,",
                "MSG,7,1,1,407DCD,1,2023/08/23,12:12:35.113,2023/08/23,12:12:35.191,,18025,,,,,,,,,,"
            };

            // Create a mock reader and wire up the message read event
            var reader = new MockMessageReader(messages, 100, false);
            reader.MessageRead += OnMessageRead;

            // Start a stopwatch, that's used to make sure the test doesn't run continuously if
            // something goes awry
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            // Start the reader
            var tokenSource = new CancellationTokenSource();
            await reader.Start(tokenSource.Token);

            // Wait until all the messages have been sent or it's clear there's a problem
            while ((_received.Count < messages.Length) && (stopwatch.ElapsedMilliseconds <= 1000))
            {
            }

            // Stop the reader and the stopwatch
            tokenSource.Cancel();
            stopwatch.Stop();

            // Confirm the content of the received messages
            for (int i = 0; i < messages.Length; i++)
            {
                // We can't directly compare the input message string and the received string because the date and
                // time stamps will be updated on sending. Instead, split into fields and compare all except the
                // date and time fields
                var expected = messages[i].Split(",");
                var actual = _received[i].Split(",");
                Assert.AreEqual(expected.Length, actual.Length);

                // Build a list of fields excluded from the comparison
                var excluded = new List<int>
                {
                    (int)MessageField.DateGenerated,
                    (int)MessageField.TimeGenerated,
                    (int)MessageField.DateLogged,
                    (int)MessageField.TimeLogged
                };

                // Do a field-by-field comparison, exccluding the excluded fields
                for (int j = 0; j < expected.Length; j++)
                {
                    if (!excluded.Contains(j))
                    {
                        Assert.AreEqual(expected[j], actual[j]);
                    }
                }
            }
        }

        /// <summary>
        /// Handle message read events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageRead(object? sender, MessageReadEventArgs e)
        {
            lock (_received)
            {
                // Add the message text to the list of received messages
                if (!string.IsNullOrEmpty(e.Message))
                {
                    _received.Add(e.Message);
                }
            }
        }
    }
}
