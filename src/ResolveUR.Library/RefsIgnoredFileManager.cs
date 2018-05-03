namespace ResolveUR.Library
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using static System.String;

    class RefsIgnoredFileManager
    {
        const char IgnoreChar = '#';

        static readonly string TempPath = Path.GetTempPath();
        static readonly string RefsReviewFilePath = $@"{TempPath}\\refs.txt";
        static string _filePath;

        public RefsIgnoredFileManager(string filePath)
        {
            _filePath = filePath;
        }

        public string RefsIgnorePath => $"{Path.GetDirectoryName(_filePath)}\\.refsignored";

        public List<XmlNode> NodesToRemove { get; set; }

        /// <summary>
        ///     Writes references to be removed to a temp file inluding marking already ignored refs
        /// </summary>
        public void WriteRefsToFile()
        {
            var ignored = LoadIgnoredRefs();
            using (var sw = new StreamWriter(RefsReviewFilePath))
            {
                sw.WriteLine($"### The project \"{Path.GetFileNameWithoutExtension(_filePath)}\" is being resolved...");
                sw.WriteLine(
                    $"### Please Prefix Reference Line to exclude with ONE pound sign! If you've previously ignored any and those are found, they are pre-marked.");
                foreach (var xmlNode in NodesToRemove)
                {
                    var refIgnored = $"{IgnoreChar}{xmlNode.Attributes[0].Value}";
                    sw.WriteLine(ignored.Contains(refIgnored) ? refIgnored : xmlNode.Attributes[0].Value);
                }
            }
        }

        public void LaunchRefsFile()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = RefsReviewFilePath
                }
            };

            process.Start();
            process.WaitForExit();
        }

        public void ProcessRefsFromFile()
        {
            var refsSelectedToRemove = new List<string>();
            var refsIgnored = new List<string>();
            using (var sr = new StreamReader(RefsReviewFilePath))
            {
                string line;
                while (!IsNullOrWhiteSpace(line = sr.ReadLine()))
                {
                    if (line.StartsWith(new string(IgnoreChar, 3)))
                        continue;

                    if (line.StartsWith(IgnoreChar.ToString()))
                        refsIgnored.Add(line);
                    else
                        refsSelectedToRemove.Add(line);
                }
            }

            // append distinct ignored refs to .refsignored in project folder.
            var existingRefsIgnored = LoadIgnoredRefs();
            foreach (var existing in existingRefsIgnored)
                if (!refsIgnored.Contains(existing))
                    refsIgnored.Add(existing);
            WriteRefsIgnored(refsIgnored);

            var finalRefsToRemove = refsSelectedToRemove.Except(existingRefsIgnored).ToList();

            // trim final node list
            for (var i = 0; i < NodesToRemove.Count; i++)
                if (!finalRefsToRemove.Contains(NodesToRemove[i].Attributes[0].Value))
                    NodesToRemove[i] = null;

            NodesToRemove.RemoveAll(x => x == null);
        }

        List<string> LoadIgnoredRefs()
        {
            var refsIgnored = new List<string>();

            if (!File.Exists(RefsIgnorePath))
                return refsIgnored;

            using (var sr = new StreamReader(RefsIgnorePath))
            {
                string line = null;
                while (!IsNullOrWhiteSpace(line = sr.ReadLine()))
                    refsIgnored.Add(line);
            }

            return refsIgnored;
        }

        void WriteRefsIgnored(IEnumerable<string> list)
        {
            using (var sw = new StreamWriter(RefsIgnorePath))
            {
                foreach (var i in list)
                    sw.WriteLine(i);
            }
        }
    }
}