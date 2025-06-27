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

        // The starting position of this entry's compressed data within the archive stream.
        public long DataOffset { get; set; }
    }
}