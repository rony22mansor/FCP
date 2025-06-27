using FCP.Controllers;
using FCP.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FCP
{
    public static class Constants
    {
        public static String OurSignature = "FCP_ARCH";
        public static char HuffmanAlgorithmCode = 'H';
        public static char ShannonFanoAlgorithmCode = 'S';
    }
}

//private async void btnCompress_Click(object sender, EventArgs e)
//{
//    if (fileListView.Items.Count == 0)
//    {
//        MessageBox.Show("Please add files to compress.", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//        return;
//    }

//    // 1. Prompt user for save location
//    using (SaveFileDialog dialog = new SaveFileDialog())
//    {
//        dialog.Filter = "FCP Archive (*.fcp)|*.fcp";
//        dialog.Title = "Save Archive As";
//        if (dialog.ShowDialog() != DialogResult.OK) return;

//        // 2. Prepare for compression
//        string outputArchivePath = dialog.FileName;
//        var filesToArchive = new Dictionary<string, string>();
//        foreach (ListViewItem item in fileListView.Items)
//        {
//            // Key: Full path, Value: Relative path
//            filesToArchive[item.Tag.ToString()] = item.SubItems[2].Text;
//        }

//        CompressInterface algorithm = radioHuffman.Checked ? (CompressInterface)new HuffmanAlgorithm() : new ShannonFanoAlgorithm();
//        var writer = new ArchiveWriter(algorithm);

//        _cancellationTokenSource = new System.Threading.CancellationTokenSource();

//        Progress<ProgressInfo> progress = new Progress<ProgressInfo>(report =>
//        {
//            // This code runs on the UI thread.
//            progressBar.Value = report.Percentage;
//            lblCurrentFile.Text = report.CurrentFile;
//        });

//        SetUIState(true);
//        lblCurrentActionValue.Text = "Compressing...";

//        try
//        {
//            // 3. Run the compression on a background thread
//            await Task.Run(() => writer.CreateArchive(filesToArchive, outputArchivePath, progress), _cancellationTokenSource.Token);

//            // 4. Update UI on completion
//            //lblStatus.Text = "Ready";
//            lblCurrentActionValue.Text = "Completed!";
//            lblOutputPathValue.Text = outputArchivePath;

//            // Calculate and display compression ratio
//            long originalSize = filesToArchive.Keys.Sum(path => new FileInfo(path).Length);
//            long compressedSize = new FileInfo(outputArchivePath).Length;
//            double ratio = (double)compressedSize / originalSize;
//            lblCompressionRatioValue.Text = $"{ratio:P2}"; // Format as percentage

//            MessageBox.Show("Compression completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

//            fileListView.Items.Clear();
//            lblCompressionRatioValue.Text = "...";
//            lblOutputPathValue.Text = "...";
//            lblCurrentFile.Text = "...";
//            lblCurrentActionValue.Text = "...";
//        }
//        catch (OperationCanceledException)
//        {
//            lblCurrentActionValue.Text = "Operation was canceled.";
//            // Clean up partially created file
//            if (File.Exists(outputArchivePath)) File.Delete(outputArchivePath);
//        }
//        catch (Exception ex)
//        {
//            //lblStatus.Text = "Error";
//            MessageBox.Show($"An error occurred during compression: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//        finally
//        {
//            // 5. Reset UI state
//            SetUIState(false);
//            _cancellationTokenSource.Dispose();
//        }
//    }
//}