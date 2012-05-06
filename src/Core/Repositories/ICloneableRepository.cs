namespace CoGet
{
    public interface ICloneableRepository
    {
        IPackageRepository Clone();
    }
}
