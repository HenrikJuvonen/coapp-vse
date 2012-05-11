using System;
using System.Collections.Generic;

namespace CoGet.Runtime
{
    public interface IAssembly
    {
        string Name { get; }
        Version Version { get; }
        string PublicKeyToken { get; }
        string Culture { get; }
        IEnumerable<IAssembly> ReferencedAssemblies { get; }
    }
}
