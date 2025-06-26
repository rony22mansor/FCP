using FCP.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FCP.Controllers
{
    /// <summary>
    /// Handles reading a custom archive and extracting its contents.
    /// </summary>
    public class ArchiveReader
    {
        private readonly CompressInterface _huffman = new HuffmanAlgorithm();
        private readonly CompressInterface _shannonFano = new ShannonFanoAlgorithm();

        public List<ArchiveEntry> ReadArchiveEntries(string sourceArchivePath)
        {
            var entries = new List<ArchiveEntry>();
            using (FileStream archiveStream = new FileStream(sourceArchivePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(archiveStream))
            {
                // Read and validate magic number
                string magic = Encoding.UTF8.GetString(reader.ReadBytes(8));
                if (magic != "FCP_ARCH")
                {
                    throw new InvalidDataException("The selected file is not a valid FCP archive.");
                }

                // Read algorithm identifier (though we don't need it for just listing files)
                reader.ReadChar();

                int totalFiles = reader.ReadInt32();

                for (int i = 0; i < totalFiles; i++)
                {
                    // Read the header for the next file entry
                    string relativePath = reader.ReadString();
                    long originalSize = reader.ReadInt64();
                    long compressedSize = reader.ReadInt64();

                    entries.Add(new ArchiveEntry
                    {
                        RelativePath = relativePath,
                        OriginalSize = originalSize,
                        CompressedSize = compressedSize
                    });

                    // Skip the compressed data block to get to the next header
                    reader.BaseStream.Seek(compressedSize, SeekOrigin.Current);
                }
            }
            return entries;
        }

        /// <summary>
        /// Extracts all files from an archive to a specified destination directory.
        /// </summary>
        public void ExtractArchive(string sourceArchivePath, string destinationDirectory,
                                   IProgress<ProgressInfo> progress)
        {
            using (FileStream archiveStream = new FileStream(sourceArchivePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(archiveStream))
            {
                // --- Read Main Archive Header ---
                string magic = Encoding.UTF8.GetString(reader.ReadBytes(8));
                if (magic != "FCP_ARCH")
                {
                    throw new InvalidDataException("The selected file is not a valid FCP archive.");
                }

                char algoIdentifier = reader.ReadChar();
                CompressInterface selectedAlgorithm;

                if (algoIdentifier == 'H')
                {
                    selectedAlgorithm = _huffman;
                }
                else if (algoIdentifier == 'S')
                {
                    selectedAlgorithm = _shannonFano;
                }
                else
                {
                    throw new InvalidDataException("Archive contains an unknown compression algorithm identifier.");
                }

                int totalFiles = reader.ReadInt32();
                int filesProcessed = 0;

                // --- Read File Entries ---
                for (int i = 0; i < totalFiles; i++)
                {

                    string relativePath = reader.ReadString();
                    long originalSize = reader.ReadInt64();
                    long compressedSize = reader.ReadInt64();

                    filesProcessed++;
                    var report = new ProgressInfo
                    {
                        Percentage = (filesProcessed * 100) / totalFiles,
                        CurrentFile = $"Extracting: {Path.GetFileName(relativePath)}"
                    };
                    progress.Report(report);

                    byte[] compressedData = reader.ReadBytes((int)compressedSize);
                    byte[] decompressedData = selectedAlgorithm.Decompress(compressedData);

                    // **THE FIX IS HERE: Defensively handle improperly stored absolute paths.**
                    // If the path stored in the archive is absolute, this prevents writing files
                    // outside of the intended destination directory.
                    if (Path.IsPathRooted(relativePath))
                    {
                        // This makes the extraction work even with archives created by a buggy writer.
                        // In a production app, you might log this as a warning.
                        relativePath = Path.GetFileName(relativePath);
                    }

                    string destinationFilePath = Path.Combine(destinationDirectory, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));

                    File.WriteAllBytes(destinationFilePath, decompressedData);
                }
            }
        }
    }
}
