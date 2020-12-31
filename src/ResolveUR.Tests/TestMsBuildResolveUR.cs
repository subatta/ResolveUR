using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResolveUR.Library;
using System.Diagnostics;
using System.IO;

namespace ResolveUR.Tests
{
    [TestClass]
    public class TestMsBuildResolveUR
    {
        [TestMethod]
        public void TestFindMsBuildPath()
        {
            // Get the path
            var msBuildPath = MsBuildResolveUR.FindMsBuildPath();
            
            // Does that file exist?
            Assert.IsNotNull(msBuildPath);
            
            Debug.WriteLine(msBuildPath);

            Assert.IsTrue(File.Exists(msBuildPath));
        }
    }
}
