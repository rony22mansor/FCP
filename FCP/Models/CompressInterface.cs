using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FCP.Models
{
    public interface CompressInterface
    {
        byte[] Compress(byte[] data, CancellationToken token, ManualResetEventSlim pauseEvent);

        byte[] Decompress(byte[] compressedData, CancellationToken token, ManualResetEventSlim pauseEvent);
    }
}
