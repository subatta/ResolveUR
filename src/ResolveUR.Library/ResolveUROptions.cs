namespace ResolveUR.Library
{
    public class ResolveUROptions
    {
        public string FilePath { get; set; }
        public string MsBuilderPath { get; set; }
        public string Platform { get; set; }
        public bool ShouldResolvePackages { get; set; }
    }
}