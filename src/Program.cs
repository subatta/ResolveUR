
using System.IO;

namespace ReferenceResolution
{
    class ProjectReferences
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
                return;

            if (!File.Exists(args[0]))
                return;
            
            ResolveReferences.Resolve(args[0]);
        }
    }
} // namespace