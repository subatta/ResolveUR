namespace ResolveUR
{
    using System;
    using System.IO;
    using Library;

    class ConsoleArgsResolveUR
    {
        public static ConsoleArgs Resolve(string[] args)
        {
            // at least solution path is required
            if (args == null || args.Length == 0)
                throw new ArgumentException("At least one argument, the solution or project file path, is required!");

            // 1st arg must be valid solution path 
            if (!File.Exists(args[0]))
                throw new ArgumentException("The first argument, solution or project file path, was not valid!");

            var filePath = args[0];

            // 2nd argument can be choice to resolve nuget packages or not.
            var isResolvePackage = args.Length >= 2 
                                    && string.Equals(args[1], "true", StringComparison.InvariantCultureIgnoreCase);

            // 3rd arg can be platform - x86 or x64
            var platform = string.Empty;
            if (args.Length >= 3 
                && (string.Equals(args[2], Constants.X86, StringComparison.InvariantCultureIgnoreCase) 
                    || string.Equals(args[2], Constants.X64, StringComparison.InvariantCultureIgnoreCase)))
                platform = args[2];

            return new ConsoleArgs
            {
                FilePath = filePath,
                ShouldResolveNugets = isResolvePackage,
                Platform = platform
            };
        }
    }
}