using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApplication1
{
    class ResolveReferences
    {

        public void Resolve(string solutionPath)
        {
            var projects = loadProjects(solutionPath);
            foreach (var project in projects)
            {
                var projectReferences = getReferenceInfo(project);
                foreach (var reference in projectReferences)
                {
                    var removedNode = removeReference(project, reference.ReferenceInclude);
                    if (isProjectNeedsReference(project))
                        addReference(project, removedNode);
                }
            }
        }

        private IEnumerable<string> loadProjects(string solutionPath)
        {
            var content = File.ReadAllText(solutionPath);
            var projReg = new Regex(
                "Project\\(\"\\{[\\w-]*\\}\"\\) = \"([\\w _]*.*)\", \"(.*\\.(cs|vcx|vb)proj)\""
                , RegexOptions.Compiled
            );
            var matches = projReg.Matches(content).Cast<Match>();
            var projects = matches.Select(x => x.Groups[2].Value).ToList();
            for (int i = 0; i < projects.Count; ++i)
            {
                if (!Path.IsPathRooted(projects[i]))
                    projects[i] = Path.Combine(Path.GetDirectoryName(solutionPath), projects[i]);
                projects[i] = Path.GetFullPath(projects[i]);
            }
            return projects;
        }

        private IEnumerable<ReferenceInfo> getReferenceInfo(string projectFile)
        {
            var fileName = projectFile;
            var xDoc = XDocument.Load(fileName);
            var ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

            //References "By DLL (file)"
            var list1 = (
                            from list in xDoc.Descendants(ns + "ItemGroup")
                            from item in list.Elements(ns + "Reference")
                            /* where item.Element(ns + "HintPath") != null */
                            /* optional */
                            select new ReferenceInfo
                            {
                                CsProjFileName = fileName,
                                ReferenceInclude = item.Attribute("Include").Value,
                                RefType = (item.Element(ns + "HintPath") == null) ? "CompiledDLLInGac" : "CompiledDLL",
                                HintPath = (item.Element(ns + "HintPath") == null) ? string.Empty : item.Element(ns + "HintPath").Value
                            }
                        ).ToList();

            //References "By Project"
            var list2 = (
                            from list in xDoc.Descendants(ns + "ItemGroup")
                            from item in list.Elements(ns + "ProjectReference")
                            where item.Element(ns + "Project") != null
                            select new ReferenceInfo
                            {
                                CsProjFileName = fileName,
                                ReferenceInclude = item.Attribute("Include").Value,
                                RefType = "ProjectReference",
                                ProjectGuid = item.Element(ns + "Project").Value
                            }
                        ).ToList();

            list1.AddRange(list2);

            return list1;
        }

        private XmlNode removeReference(string projectFile, string reference)
        {
            var doc = new XmlDocument();
            doc.Load(projectFile);
            var nodes = doc.SelectNodes(@"//*[local-name()='Reference']");
            XmlNode returnedNode = null;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Attributes["Include"].Value == reference)
                {
                    returnedNode = nodes[i];
                    nodes[i].ParentNode.RemoveChild(nodes[i]);
                    break;
                }
            }
            doc.Save(projectFile);
            return returnedNode;
        }

        private void addReference(string projectFile, XmlNode removedReferenceNode)
        {
            var doc = new XmlDocument();
            doc.Load(projectFile);
            var nodes = doc.SelectNodes(@"//*[local-name()='ItemGroup']");
            if (nodes.Count <= 0)
                return;
            // grab the itemgroup with Reference elements
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].ChildNodes[0].Name == "Reference")
                {
                    var importedNode = nodes[i].OwnerDocument.ImportNode(removedReferenceNode, true);
                    nodes[i].AppendChild(importedNode);
                    break;
                }
            }
            doc.Save(projectFile);
        }

        private bool isProjectNeedsReference(string project)
        {
            const string MsbuildExe = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
            const string LogFile = "buildlog.txt";
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = MsbuildExe;
            startInfo.Arguments = project + " /clp:ErrorsOnly /m /flp:logfile=" + LogFile + ";Verbosity=Quiet";

            try
            {
                using (var exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText("err.txt", ex.Message);
            }

            // open build log to check for errors
            var s = File.ReadAllText(LogFile);
            File.Delete(LogFile);

            // if we have a build error, the reference cannot be removed
            return s.Contains("error");
        }

        class ReferenceInfo
        {
            public string CsProjFileName;
            public string ReferenceInclude;
            public string RefType;
            public string HintPath;
            public string ProjectGuid;
            public override string ToString()
            {
                return string.Format("{0},{1},{2}", CsProjFileName, ReferenceInclude, RefType);
            }
        }
    }
}
