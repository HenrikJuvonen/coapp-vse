// Guids.cs
// MUST match guids.h
using System;

namespace CoApp.VsExtension
{
    public static class GuidList
    {
        public const string guidVsExtensionPkgString = "88b24e3f-acb7-4ee5-acd2-cb591ac6f77b";
        public const string guidVsExtensionCmdSetString = "9ac542c2-ba67-4b18-8a21-218af3f70b5c";
        public const string guidConsoleWindowPersistanceString = "4a183d63-ad84-4df0-9f1b-92d57c8a0601";

        public static readonly Guid guidVsExtensionCmdSet = new Guid(guidVsExtensionCmdSetString);
    };
}