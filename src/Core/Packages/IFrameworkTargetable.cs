using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CoGet
{
    public interface IFrameworkTargetable
    {
        IEnumerable<FrameworkName> SupportedFrameworks { get; }
    }
}
