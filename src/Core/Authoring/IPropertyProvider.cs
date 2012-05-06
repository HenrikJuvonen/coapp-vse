namespace CoGet
{
    public interface IPropertyProvider
    {
        dynamic GetPropertyValue(string propertyName);
    }
}
