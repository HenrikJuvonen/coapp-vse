using System;

namespace CoApp.VisualStudio.Tools
{
    public static class GuidList
    {
        public const string guidVSPkgString = "5F058D4C-1DA8-4FA4-8FF7-CB039283F751";

        public const string guidVSConsoleCmdSetString = "B0E5EDF5-DC3C-4D7E-922E-02E7A6FFD50D";
        public const string guidVSDialogCmdSetString = "3B9792BD-7B73-42DF-804A-17BCB3CA67E6";
        public const string guidVSToolsGroupString = "6290196F-BA6A-4D72-9E80-433B2C32BAAF";
        public const string guidVSPackagesRestoreGroupString = "26999603-9B0C-468A-980D-EB1F55FB24B5";
        
        public static readonly Guid guidVSConsoleCmdSet = new Guid(guidVSConsoleCmdSetString);
        public static readonly Guid guidVSDialogCmdSet = new Guid(guidVSDialogCmdSetString);
        public static readonly Guid guidVSToolsGroupCmdSet = new Guid(guidVSToolsGroupString);
        public static readonly Guid guidVSPackagesRestoreCmdSet = new Guid(guidVSPackagesRestoreGroupString);
    };
}