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
        public void ExtractArchive(
     string sourceArchivePath,
     string destinationDirectory,
     IProgress<ProgressInfo> progress,
     CancellationToken token,
     ManualResetEventSlim pauseEvent)
        {
            using (FileStream archiveStream = new FileStream(sourceArchivePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(archiveStream))
            {
                // قراءة الهيدر
                string magic = Encoding.UTF8.GetString(reader.ReadBytes(8));
                if (magic != "FCP_ARCH")
                {
                    throw new InvalidDataException("The selected file is not a valid FCP archive.");
                }

                char algoIdentifier = reader.ReadChar();
                CompressInterface selectedAlgorithm;

                if (algoIdentifier == 'H')
                {
                    selectedAlgorithm = new HuffmanAlgorithm();
                }
                else
                {
                    selectedAlgorithm = new ShannonFanoAlgorithm();
                }

                int totalFiles = reader.ReadInt32();
                int filesProcessed = 0;

                for (int i = 0; i < totalFiles; i++)
                {
                    // تحقق من الإلغاء بدون رمي استثناء
                    if (token.IsCancellationRequested)
                    {
                        progress?.Report(new ProgressInfo
                        {
                            Percentage = (filesProcessed * 100) / totalFiles,
                            CurrentFile = "Operation canceled by user."
                        });
                        return; // خروج هادئ من الدالة
                    }

                    // تحقق من الإيقاف المؤقت
                    pauseEvent.Wait();

                    string relativePath = reader.ReadString();
                    long originalSize = reader.ReadInt64();
                    long compressedSize = reader.ReadInt64();

                    byte[] compressedData = reader.ReadBytes((int)compressedSize);
                    byte[] decompressedData = selectedAlgorithm.Decompress(compressedData);

                    string destinationFilePath = Path.Combine(destinationDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                    File.WriteAllBytes(destinationFilePath, decompressedData);

                    filesProcessed++;
                    progress?.Report(new ProgressInfo
                    {
                        Percentage = (filesProcessed * 100) / totalFiles,
                        CurrentFile = $"{Path.GetFileName(relativePath)} ...Done!"
                    });
                }
            }
        }

    }


  }
