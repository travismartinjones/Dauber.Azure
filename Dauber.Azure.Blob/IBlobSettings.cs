using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dauber.Azure.Blob
{
    public interface IBlobSettings
    {
        string ConnectionString { get; }
    }
}
