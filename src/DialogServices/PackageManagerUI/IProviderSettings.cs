
namespace CoGet.Dialog.PackageManagerUI
{
    public interface IProviderSettings
    {
        int SelectedProvider { get; set; }
        bool IncludePrereleasePackages { get; set; }
    }
}