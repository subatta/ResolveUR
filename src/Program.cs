using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ConsoleApplication1;

namespace ConsoleApplication1
{
    class ProjectReferences
    {
        public static void Main()
        {
            var c = new ResolveReferences();
            c.Resolve(@"D:\Programming\CodeLib\ActiveProjects\KeePassMenu\KPM\KPM.sln");
        }
    }
} // namespace