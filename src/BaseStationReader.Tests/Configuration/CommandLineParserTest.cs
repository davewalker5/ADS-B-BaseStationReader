using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Exceptions;
using BaseStationReader.BusinessLogic.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Tests.Configuration
{
    [ExcludeFromCodeCoverage]
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
            string[] args = new string[]{ "--host", "192.168.0.98", "--port", "30003" };
            _parser?.Parse(args);

            var values = _parser?.GetValues(CommandLineOptionType.Host);
            Assert.IsNotNull(values);
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual("192.168.0.98", values.First());

            values = _parser?.GetValues(CommandLineOptionType.Port);
            Assert.IsNotNull(values);
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual("30003", values.First());
        }

        [TestMethod]
        public void ValidUsingShortNamesTest()
        {
            string[] args = new string[] { "-h", "192.168.0.98", "-p", "30003" };
            _parser?.Parse(args);

            var values = _parser?.GetValues(CommandLineOptionType.Host);
            Assert.IsNotNull(values);
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual("192.168.0.98", values.First());

            values = _parser?.GetValues(CommandLineOptionType.Port);
            Assert.IsNotNull(values);
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual("30003", values.First());
        }

        [TestMethod]
        [ExpectedException(typeof(MissingMandatoryOptionException))]
        public void MissingMandatoryFailsTest()
        {
            string[] args = new string[] { "-p", "30003" };
            _parser?.Parse(args);
        }

        [TestMethod]
        [ExpectedException(typeof(TooFewValuesException))]
        public void TooFewArgumentsFailsTest()
        {
            string[] args = new string[] { "-h", "-p", "30003" };
            _parser?.Parse(args);
        }

        [TestMethod]
        [ExpectedException(typeof(TooManyValuesException))]
        public void TooManyArgumentsFailsTest()
        {
            string[] args = new string[] { "-h", "192.168.0.98", "127.0.0.1", "-p", "30003" };
            _parser?.Parse(args);
        }

        [TestMethod]
        [ExpectedException(typeof(UnrecognisedCommandLineOptionException))]
        public void UnrecognisedOptionNameFailsTest()
        {
            string[] args = new string[] { "--oops", "192.168.0.98", "-p", "30003" };
            _parser?.Parse(args);
        }

        [TestMethod]
        [ExpectedException(typeof(UnrecognisedCommandLineOptionException))]
        public void UnrecognisedOptionShortNameFailsTest()
        {
            string[] args = new string[] { "-o", "192.168.0.98", "-p", "30003" };
            _parser?.Parse(args);
        }

        [TestMethod]
        [ExpectedException(typeof(MalformedCommandLineException))]
        public void MalformedCommandLineFailsTest()
        {
            string[] args = new string[] { "192.168.0.98", "-p", "30003" };
            _parser?.Parse(args);
        }

        [TestMethod]
        [ExpectedException(typeof(DuplicateOptionException))]
        public void DuplicateOptionTypeFailsTest()
        {
            _parser?.Add(CommandLineOptionType.Host, true, "--other-host", "-oh", "Duplicate option type", 1, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(DuplicateOptionException))]
        public void DuplicateOptionNameFailsTest()
        {
            _parser?.Add(CommandLineOptionType.ApplicationTimeout, true, "--host", "-oh", "Duplicate option name", 1, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(DuplicateOptionException))]
        public void DuplicateOptionShortNameFailsTest()
        {
            _parser?.Add(CommandLineOptionType.ApplicationTimeout, true, "--other-host", "-h", "Duplicate option short name", 1, 1);
        }
    }
}
