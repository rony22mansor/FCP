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

                // **THE FIX IS HERE: Read the algorithm identifier.**
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

                    

                    byte[] compressedData = reader.ReadBytes((int)compressedSize);

                    // **Use the explicitly selected algorithm.**
                    byte[] decompressedData = selectedAlgorithm.Decompress(compressedData);

                    string destinationFilePath = Path.Combine(destinationDirectory, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));

                    File.WriteAllBytes(destinationFilePath, decompressedData);

                    filesProcessed++;
                    var report = new ProgressInfo
                    {
                        Percentage = (filesProcessed * 100) / totalFiles,
                        CurrentFile = $"{Path.GetFileName(relativePath)} ...Done!"
                    };
                    progress.Report(report);
                }
            }
        }
    }
}
