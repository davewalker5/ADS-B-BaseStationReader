using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Exceptions;
using BaseStationReader.BusinessLogic.Configuration;

namespace BaseStationReader.Tests.Configuration
{
    [TestClass]
    public class CommandLineParserTest
    {
        private CommandLineParser _parser;

        [TestInitialize]
        public void TestInitialise()
        {
            _parser = new CommandLineParser();
            _parser.Add(CommandLineOptionType.Host, true, "--host", "-h", "Host to connect to for data stream", 1, 1);
            _parser.Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to connect to for data stream", 1, 1);
        }

        [TestMethod]
        public void ValidUsingNamesTest()
        {
            string[] args = ["--host", "192.168.0.98", "--port", "30003"];
            _parser?.Parse(args);

            var values = _parser?.GetValues(CommandLineOptionType.Host);
            Assert.IsNotNull(values);
            Assert.HasCount(1, values);
            Assert.AreEqual("192.168.0.98", values.First());

            values = _parser?.GetValues(CommandLineOptionType.Port);
            Assert.IsNotNull(values);
            Assert.HasCount(1, values);
            Assert.AreEqual("30003", values.First());
        }

        [TestMethod]
        public void ValidUsingShortNamesTest()
        {
            string[] args = ["-h", "192.168.0.98", "-p", "30003"];
            _parser?.Parse(args);

            var values = _parser?.GetValues(CommandLineOptionType.Host);
            Assert.IsNotNull(values);
            Assert.HasCount(1, values);
            Assert.AreEqual("192.168.0.98", values.First());

            values = _parser?.GetValues(CommandLineOptionType.Port);
            Assert.IsNotNull(values);
            Assert.HasCount(1, values);
            Assert.AreEqual("30003", values.First());
        }

        [TestMethod]
        public void MissingMandatoryFailsTest()
        {
            string[] args = ["-p", "30003"];
            Assert.Throws<MissingMandatoryOptionException>(() => { _parser?.Parse(args); });
        }

        [TestMethod]
        public void TooFewArgumentsFailsTest()
        {
            string[] args = ["-h", "-p", "30003"];
            Assert.Throws<TooFewValuesException>(() => { _parser?.Parse(args); });
        }

        [TestMethod]
        public void TooManyArgumentsFailsTest()
        {
            string[] args = ["-h", "192.168.0.98", "127.0.0.1", "-p", "30003"];
            Assert.Throws<TooManyValuesException>(() => { _parser?.Parse(args); });
        }

        [TestMethod]
        public void UnrecognisedOptionNameFailsTest()
        {
            string[] args = ["--oops", "192.168.0.98", "-p", "30003"];
            Assert.Throws<UnrecognisedCommandLineOptionException>(() => { _parser?.Parse(args); });
        }

        [TestMethod]
        public void UnrecognisedOptionShortNameFailsTest()
        {
            string[] args = ["-o", "192.168.0.98", "-p", "30003"];
            Assert.Throws<UnrecognisedCommandLineOptionException>(() => { _parser?.Parse(args); });
        }

        [TestMethod]
        public void MalformedCommandLineFailsTest()
        {
            string[] args = ["192.168.0.98", "-p", "30003"];
            Assert.Throws<MalformedCommandLineException>(() => { _parser?.Parse(args); });
        }

        [TestMethod]
        public void DuplicateOptionTypeFailsTest()
        {
            Assert.Throws<DuplicateOptionException>(() =>
            {
                _parser?.Add(CommandLineOptionType.Host, true, "--other-host", "-oh", "Duplicate option type", 1, 1);
            });
        }

        [TestMethod]
        public void DuplicateOptionNameFailsTest()
        {
            Assert.Throws<DuplicateOptionException>(() =>
            {
                _parser?.Add(CommandLineOptionType.ApplicationTimeout, true, "--host", "-oh", "Duplicate option name", 1, 1);
            });
        }

        [TestMethod]
        public void DuplicateOptionShortNameFailsTest()
        {
            Assert.Throws<DuplicateOptionException>(() =>
            {
                _parser?.Add(CommandLineOptionType.ApplicationTimeout, true, "--other-host", "-h", "Duplicate option short name", 1, 1);
            });
        }
    }
}
