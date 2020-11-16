using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResolveUR.Library;

namespace Test.ResolveUR.Library
{
    [TestClass]
    public class TestMsBuildResolveUR
    {
        [TestMethod]
        public void TestFindMsBuildPath()
        {
            // Get the path
            var msBuildPath = MsBuildResolveUR.FindMsBuildPath();
            
            // Is that path valid?
            Assert.IsNotNull(msBuildPath);
            
            Debug.WriteLine(msBuildPath);

            Assert.IsTrue(File.Exists(msBuildPath));
        }
    }
}
