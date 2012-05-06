namespace CoApp.VsExtension.VisualStudio
{
    public interface IVsProjectSystem : IProjectSystem
    {
        string UniqueName { get; }
    }
}