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
        /// <param name="progress">An object to report progress back to the UI.</param>
        public void CreateArchive(Dictionary<string, string> filesToArchive, string outputArchivePath,
                                  IProgress<ProgressInfo> progress)
        {
            using (FileStream archiveStream = new FileStream(outputArchivePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(archiveStream))
            {
                // --- Main Archive Header ---
                // 1. Magic Number
                writer.Write(Encoding.UTF8.GetBytes("FCP_ARCH"));

                // 2. Algorithm Identifier (THE FIX IS HERE)
                char algoIdentifier = (_algorithm is HuffmanAlgorithm) ? 'H' : 'S';
                writer.Write(algoIdentifier);

                // 3. File Count
                writer.Write(filesToArchive.Count);

                int filesProcessed = 0;
                int totalFiles = filesToArchive.Count;

                // --- File Entries ---
                foreach (var fileEntry in filesToArchive)
                {

                    string sourcePath = fileEntry.Key;
                    string relativePath = fileEntry.Value;

                    filesProcessed++;
                    var report = new ProgressInfo
                    {
                        Percentage = (filesProcessed * 100) / totalFiles,
                        CurrentFile = $"{Path.GetFileName(sourcePath)} ...Done!"
                    };
                    progress.Report(report);

                    byte[] originalData = File.ReadAllBytes(sourcePath);
                    byte[] compressedData = _algorithm.Compress(originalData);

                    if (compressedData == null) continue;

                    var entry = new ArchiveEntry
                    {
                        RelativePath = relativePath,
                        OriginalSize = originalData.Length,
                        CompressedSize = compressedData.Length
                    };
                   
                    WriteEntry(writer, entry, compressedData);
                }
            }
        }

        private void WriteEntry(BinaryWriter writer, ArchiveEntry entry, byte[] data)
        {
            writer.Write(entry.RelativePath);
            writer.Write(entry.OriginalSize);
            writer.Write(entry.CompressedSize);
            writer.Write(data);
        }
    }
}
