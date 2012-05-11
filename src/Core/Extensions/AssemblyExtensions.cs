using System.Reflection;

namespace CoGet
{
    public static class AssemblyExtensions
    {
        public static AssemblyName GetNameSafe(this Assembly assembly)
        {
            return new AssemblyName(assembly.FullName);
        }
    }
}
