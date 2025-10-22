using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class DatabaseManagementFactoryTest
    {
        [TestMethod]
        public void GetContextTest()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var factory = new DatabaseManagementFactory(null, context, 0, 0);
            var retrieved = factory.Context<BaseStationReaderDbContext>();

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(context, retrieved);
        }
    }
}