using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void MimeHelperTest()
        {
            string actual = FavoImgs.MimeHelper.GetFileExtension("image/jpeg");

            Assert.AreEqual(".jpg", actual);
        }
    }
}
