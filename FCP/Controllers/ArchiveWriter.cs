using FCP.Models;
using System;
using System.Collections.Generic;
using System.IO;
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

        public void CreateArchive(
            Dictionary<string, string> filesToArchive,
            string outputArchivePath,
            IProgress<ProgressInfo> progress,
            CancellationToken token,
            ManualResetEventSlim pauseEvent)
        {
            using (FileStream archiveStream = new FileStream(outputArchivePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(archiveStream))
            {
                writer.Write(Encoding.UTF8.GetBytes("FCP_ARCH"));
                char algoIdentifier = (_algorithm is HuffmanAlgorithm) ? 'H' : 'S';
                writer.Write(algoIdentifier);
                writer.Write(filesToArchive.Count);

                int filesProcessed = 0;
                int totalFiles = filesToArchive.Count;

                foreach (var fileEntry in filesToArchive)
                {
                    // 1. فحص الإلغاء أولاً
                    token.ThrowIfCancellationRequested();

                    // 2. حلقة الإيقاف المؤقت المحسنة
                    while (!pauseEvent.IsSet)
                    {
                        progress?.Report(new ProgressInfo
                        {
                            Percentage = (filesProcessed * 100) / totalFiles,
                            CurrentFile = "Paused..."
                        });

                        // انتظار قصير مع فحص الإلغاء
                        if (token.WaitHandle.WaitOne(200)) // زمن انتظار محسن (200ms)
                        {
                            token.ThrowIfCancellationRequested();
                        }
                    }

                    // 3. فحص الإلغاء مرة أخرى بعد الخروج من حلقة الإيقاف
                    token.ThrowIfCancellationRequested();

                    string sourcePath = fileEntry.Key;
                    string relativePath = fileEntry.Value;

                    byte[] originalData = File.ReadAllBytes(sourcePath);
                    byte[] compressedData = _algorithm.Compress(originalData);

                    var entry = new ArchiveEntry
                    {
                        RelativePath = relativePath,
                        OriginalSize = originalData.Length,
                        CompressedSize = compressedData.Length
                    };

                    WriteEntry(writer, entry, compressedData);

                    filesProcessed++;
                    progress?.Report(new ProgressInfo
                    {
                        Percentage = (filesProcessed * 100) / totalFiles,
                        CurrentFile = $"{Path.GetFileName(sourcePath)} ...Done!"
                    });
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