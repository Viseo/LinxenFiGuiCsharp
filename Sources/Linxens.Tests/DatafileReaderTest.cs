using Linxens.Core.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Linxens.Tests
{
    [TestClass]
    public class DatafileReaderTest
    {
        [TestMethod]
        public void ShouldReadSampleFile()
        {
            DataFileService reader = new DataFileService();

            //var result = reader.ReadtxtFile("TestFiles\\RunningReel.txt");

            //Assert.AreEqual("4327", result.GetValue);
            //Assert.AreEqual("E100622", result.Emp);
        }
    }
}