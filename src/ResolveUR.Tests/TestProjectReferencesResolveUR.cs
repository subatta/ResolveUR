using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResolveUR.Library;

namespace ResolveUR.Tests
{
    [TestClass]
    public class TestProjectReferencesResolveUR
    {
        [TestMethod]
        public void TestGetXmlDocument()
        {
            string solutionFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string solutionPath = Path.Combine(solutionFolder, "ResolveUR.sln");
            ResolveUROptions resolveUrOptions = new ResolveUROptions {
                FilePath = solutionPath,
                ShouldResolvePackages = false,
                Platform = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
                MsBuilderPath = MsBuildResolveUR.FindMsBuildPath()
            };

            IResolve _resolveur;
            _resolveur = ResolveURFactory.GetResolver(
                resolveUrOptions,
                ResolveurHasBuildErrorsEvent,
                ResolveurProjectResolveCompleteEvent);

            // TODO: Why does Resolve delete? This should be a separate method!
            //_resolveur.Resolve();
        }

        static void ResolveurHasBuildErrorsEvent(string projectName)
        {
            Debug.WriteLine(projectName);
            Assert.Fail();
        }

        static void ResolveurProjectResolveCompleteEvent()
        {
            // Do nothing, this is a test!
        }
    }
}
