// Guids.cs
// MUST match guids.h
using System;

namespace CoGet.Tools
{
    public static class GuidList
    {
        public const string guidCoGetPkgString = "F7D0E7A3-C60B-422A-BFAE-CEED36ADE7D2";

        public const string guidCoGetConsoleCmdSetString = "1E8A55F6-C18D-407F-91C8-94B02AE1CED6";
        public const string guidCoGetDialogCmdSetString = "25fd982b-8cae-4cbd-a440-e03ffccde106";
        public const string guidCoGetToolsGroupString = "C0D88179-5D25-4982-BFE6-EC5FD59AC103";
        public const string guidCoGetPackagesRestoreGroupString = "B4B288EF-D5B7-4669-9D6A-ACD644F90AC8";

        // any project system that wants to load CoGet when its project opens needs to activate a UI context with this GUID
        public const string guidAutoLoadCoGetString = "65B1D035-27A5-4BBA-BAB9-5F61C1E2BC4A";

        public static readonly Guid guidCoGetConsoleCmdSet = new Guid(guidCoGetConsoleCmdSetString);
        public static readonly Guid guidCoGetDialogCmdSet = new Guid(guidCoGetDialogCmdSetString);
        public static readonly Guid guidCoGetToolsGroupCmdSet = new Guid(guidCoGetToolsGroupString);
        public static readonly Guid guidCoGetPackagesRestoreCmdSet = new Guid(guidCoGetPackagesRestoreGroupString);
    };
}