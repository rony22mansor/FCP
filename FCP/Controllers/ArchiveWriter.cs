using FCP.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCP.Controllers
{
    public class ArchiveWriter
    {
        private readonly CompressInterface _algorithm;

        public ArchiveWriter(CompressInterface algorithm)
        {
            _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        }

        /// <summary>
        /// Creates an archive from a list of file paths.
        /// </summary>
        /// <param name="filesToArchive">A dictionary where Key is the full path to the source file
        /// and Value is the relative path to store in the archive.</param>
        /// <param name="outputArchivePath">The path where the final archive file will be saved.</param>
        public void CreateArchive(Dictionary<string, string> filesToArchive, string outputArchivePath)
        {
            using (FileStream archiveStream = new FileStream(outputArchivePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(archiveStream))
            {
                // --- Main Archive Header ---
                // Write a "magic number" to identify our file type.
                writer.Write(Encoding.UTF8.GetBytes("FCP_ARCH"));
                // Write the number of files that will be in this archive.
                writer.Write(filesToArchive.Count);

                // --- File Entries ---
                foreach (var fileEntry in filesToArchive)
                {
                    string sourcePath = fileEntry.Key;
                    string relativePath = fileEntry.Value;

                    // 1. Read the original file data.
                    byte[] originalData = File.ReadAllBytes(sourcePath);

                    // 2. Compress the data using the selected algorithm.
                    byte[] compressedData = _algorithm.Compress(originalData);

                    // If compression fails or makes the file larger, store it uncompressed.
                    // For this project, we'll assume compression is always beneficial.
                    // A real-world app would add a flag here to indicate if the block is compressed.
                    if (compressedData == null)
                    {
                        // For simplicity, we'll skip files that can't be compressed.
                        // A more robust implementation might store them uncompressed.
                        continue;
                    }

                    // 3. Create the metadata header for this specific file.
                    var entry = new ArchiveEntry
                    {
                        RelativePath = relativePath,
                        OriginalSize = originalData.Length,
                        CompressedSize = compressedData.Length
                    };

                    // 4. Write the file's header and data to the archive.
                    WriteEntry(writer, entry, compressedData);
                }
            }
        }

        private void WriteEntry(BinaryWriter writer, ArchiveEntry entry, byte[] data)
        {
            // --- File Entry Header ---
            writer.Write(entry.RelativePath);
            writer.Write(entry.OriginalSize);
            writer.Write(entry.CompressedSize);

            // --- File Data ---
            writer.Write(data);
        }
    }
}
