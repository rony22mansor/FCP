using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCP.Models
{
    public interface CompressInterface
    {
        /// <summary>
        /// Compresses a byte array.
        /// </summary>
        /// <param name="data">The raw byte data to compress.</param>
        /// <returns>A byte array containing the compressed data and any necessary header information.</returns>
        byte[] Compress(byte[] data);

        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="compressedData">The compressed data, including the header.</param>
        /// <returns>The original, decompressed byte array.</returns>
        byte[] Decompress(byte[] compressedData);
    }
}
