using System.Threading;

namespace FCP.Models
{
    public interface CompressInterface
    {
        byte[] Compress(byte[] data, CancellationToken token, ManualResetEventSlim pauseEvent);

        byte[] Decompress(byte[] compressedData, CancellationToken token, ManualResetEventSlim pauseEvent);
    }
}