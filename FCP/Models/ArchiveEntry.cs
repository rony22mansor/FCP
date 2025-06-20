using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCP.Models
{
    public class ArchiveEntry
    {
        // The path of the file relative to the root of the archive.
        public string RelativePath { get; set; }

        // The original, uncompressed size of the file in bytes.
        public long OriginalSize { get; set; }

        // The size of the compressed data block in the archive, in bytes.
        public long CompressedSize { get; set; }
    }
}
