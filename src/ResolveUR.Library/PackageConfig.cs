using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace ResolveUR.Library
{
    class PackageConfig
    {
        
        IDictionary<string, XmlNode> _packages;
        XmlDocument packageConfigDocument = null;
        HashSet<XmlNode> _packagesToKeep = new HashSet<XmlNode>();

        public string PackageConfigPath
        {
            get;
            set;
        }
        public string FilePath
        {
            get;
            set;
        }
        public void LoadPackagesIfAny()
        {
            // an entry in package config maps to hint path under reference node as follows:
            // Entry : <package id="CsvHelper" version="2.7.0" targetFramework="net45" />
            // Hint path : <HintPath>..\packages\CsvHelper.2.7.0\lib\net40-client\CsvHelper.dll</HintPath>

            // Folder in HintPath CsvHelper.2.7.0 is derived by concatenation of Id and Version attributes of Entry

            // map versioned library CsvHelper.2.7.0 to package entries in this method
            if (!File.Exists(PackageConfigPath) || packageConfigDocument != null)
                return;

            packageConfigDocument = new XmlDocument();
            packageConfigDocument.Load(PackageConfigPath);

            const string PackageNode = @"//*[local-name()='package']";
            var packageNodes = packageConfigDocument.SelectNodes(PackageNode);
            if (packageNodes.Count > 0)
                _packages = new Dictionary<string, XmlNode>();
            foreach (XmlNode node in packageNodes)
            {
                _packages.Add(node.Attributes["id"].Value + "." + node.Attributes["version"].Value, node);
            }

            // when references are cleaned up later, store package nodes that match hint paths for references that are ok to keep
            // at the conclusion of cleanup, rewrite packages config with saved package nodes to keep

        }

        public void CopyPackageToKeep(XmlNode referenceNode)
        {
            if (referenceNode.ChildNodes.Count == 0) return;

            var hintPath = getHintPath(referenceNode);
            if (string.IsNullOrWhiteSpace(hintPath)) return;
            foreach (var package in _packages)
            {
                if (hintPath.Contains(package.Key) && !_packagesToKeep.Contains(package.Value))
                {
                    _packagesToKeep.Add(package.Value);
                    break;
                }
            }
        }
        public void RemoveUnusedPackage(XmlNode referenceNode)
        {
            if (referenceNode.ChildNodes.Count == 0) return;

            var hintPath = getHintPath(referenceNode);
            foreach (var package in _packages)
            {
                if (hintPath.Contains(package.Key))
                {
                    var packagePath = hintPath.Substring(0, hintPath.IndexOf(package.Key)) + package.Key;
                    packagePath = Path.Combine(Path.GetDirectoryName(FilePath), packagePath);
                    try
                    {
                        Directory.Delete(packagePath, true);
                    }
                    catch (Exception) 
                    {
                        // don't bother 
                    }
                    break;
                }
            }
        }
        private string getHintPath(XmlNode referenceNode)
        {
            var node = referenceNode
                        .ChildNodes
                        .OfType<XmlNode>()
                        .FirstOrDefault(x => x.Name == "HintPath");
            if (node == null) return string.Empty;
                        
            return node.InnerXml;
        }

        public void UpdatePackageConfig()
        {
            if (_packagesToKeep.Count == 0) return;

            packageConfigDocument.DocumentElement.RemoveAll();
            foreach (var package in _packagesToKeep)
                packageConfigDocument.DocumentElement.AppendChild(package);

            packageConfigDocument.Save(PackageConfigPath);
            _packagesToKeep.Clear();
        }

    }
}
