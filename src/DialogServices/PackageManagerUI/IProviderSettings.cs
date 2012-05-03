
namespace CoApp.VsExtension.Dialog.PackageManagerUI
{
    public interface IProviderSettings
    {
        int SelectedProvider { get; set; }
        bool IncludePrereleasePackages { get; set; }
    }
}