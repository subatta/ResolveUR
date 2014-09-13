// Guids.cs
// MUST match guids.h

using System;

namespace ResolveURVisualStudioPackage
{
    internal static class GuidList
    {
        public const string GuidResolveUrVisualStudioPackagePkgString = "637ba02c-3388-4e54-9051-3eea7c51b054";
        public const string GuidResolveUrVisualStudioPackageCmdSetString = "f78e295d-0aa2-4310-b512-ef6c575b1442";

        public static readonly Guid GuidResolveUrVisualStudioPackageCmdSet =
            new Guid(GuidResolveUrVisualStudioPackageCmdSetString);
    }
}