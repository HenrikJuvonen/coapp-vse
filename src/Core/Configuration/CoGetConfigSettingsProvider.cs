namespace CoGet
{
    public sealed class CoGetConfigSettingsProvider : ISettingsProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification="This type is immutable.")]
        public static readonly CoGetConfigSettingsProvider Default = new CoGetConfigSettingsProvider();

        private CoGetConfigSettingsProvider()
        {
        }

        public ISettings LoadUserSettings()
        {
            return Settings.LoadDefaultSettings();
        }
    }
}