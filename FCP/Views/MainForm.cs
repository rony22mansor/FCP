using FCP.Controllers;
using FCP.Core;
using FCP.Helpers;
using FCP.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// This is the main code-behind file for your form.
// All the logic for button clicks and other events will go here.

namespace FCP
{
    public partial class MainForm : Form
    {
        private System.Threading.CancellationTokenSource _cancellationTokenSource;
        private System.Threading.ManualResetEventSlim _pauseEvent;
        private string _currentOpenArchivePath;
        private string _currentOperation;
        public MainForm(string[] args = null)
        {
            InitializeComponent();
            // Initially hide the password text box
            txtPassword.Visible = false;
            if (args != null && args.Length > 0)
            {
                string inputPath = args[0];

                if (File.Exists(inputPath))
                {
                    var info = new FileInfo(inputPath);
                    var item = new ListViewItem(info.Name);
                    item.SubItems.Add(FilesFoldersHelper.FormatBytes(info.Length));
                    item.SubItems.Add(info.FullName);
                    item.Tag = info.FullName;
                    fileListView.Items.Add(item);
                }
                else if (Directory.Exists(inputPath))
                {
                    FilesFoldersHelper.AddAllFilesFromFolder(inputPath, inputPath, fileListView);
                }
            }
        }

        private void btnRegisterShell_Click(object sender, EventArgs e)
        {
            ShellIntegration.Register();
        }

        private void btnUnregisterShell_Click(object sender, EventArgs e)
        {
            ShellIntegration.Unregister();
        }

        private void SetUIState(bool isProcessing, bool isArchiveOpen = false)
        {
            // Disable/enable controls based on whether an operation is running.
            mainToolStrip.Enabled = !isProcessing;
            groupBoxOptions.Enabled = !isProcessing;
            btnPauseResume.Enabled = isProcessing;
            btnCancel.Enabled = isProcessing;

            btnAddFiles.Enabled = !isArchiveOpen && !isProcessing;
            btnAddFolder.Enabled = !isArchiveOpen && !isProcessing;
            btnCompress.Enabled = !isArchiveOpen && !isProcessing;
            btnRemoveSelected.Enabled = !isArchiveOpen && !isProcessing;
            btnDecompress.Enabled = !isArchiveOpen && !isProcessing;

            if (!isProcessing)
            {
                // Reset UI elements after completion or cancellation.
                progressBar.Value = 0;
                lblCurrentActionValue.Text = "...";
                lblOutputPathValue.Text = "...";
                lblCurrentFile.Text = "...";
                btnPauseResume.Text = "Pause";
            }
        }

        // Event handler for the "Add Files" button
        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Files to Add";
                // Allow the user to select more than one file.
                dialog.Multiselect = true;
                dialog.Filter = "All files (*.*)|*.*";

                // Show the dialog and check if the user clicked "OK".
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Loop through each file path the user selected.
                    foreach (string filePath in dialog.FileNames)
                    {
                        // Get file information.
                        FileInfo fileInfo = new FileInfo(filePath);

                        // Create a new item for the ListView.
                        // The main item is the first column (File Name).
                        ListViewItem item = new ListViewItem(fileInfo.Name);

                        // Add "sub-items" for the other columns.
                        // Column 2: Size (formatted for readability).
                        item.SubItems.Add(FilesFoldersHelper.FormatBytes(fileInfo.Length));
                        // Column 3: The full path of the file.
                        item.SubItems.Add(fileInfo.FullName);
                        // THE FIX IS HERE: Ensure the Tag property is always set.
                        // The Tag stores the full path needed for reading the file.
                        item.Tag = fileInfo.FullName;
                        // Add the fully prepared item to the ListView.
                        fileListView.Items.Add(item);
                    }
                }
            }
        }

        // Event handler for the "Add Folder" button
        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            if (fileListView.Items.Count > 0)
            {
                foreach (ListViewItem Item in fileListView.Items)
                {
                    fileListView.Items.Remove(Item);
                }
            }
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder to add";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string rootPath = dialog.SelectedPath;
                    // Call the recursive helper method to scan the folder.
                    FilesFoldersHelper.AddAllFilesFromFolder(rootPath, rootPath, fileListView);
                }
            }
        }


        private void btnOpenArchive_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Open Archive";
                dialog.Filter = "FCP Archive (*.fcp)|*.fcp";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var reader = new ArchiveReader();
                        List<ArchiveEntry> entries = reader.ReadArchiveEntries(dialog.FileName);

                        // Clear the list before showing the archive contents
                        fileListView.Items.Clear();

                        foreach (var entry in entries)
                        {
                            ListViewItem item = new ListViewItem(Path.GetFileName(entry.RelativePath));
                            item.SubItems.Add(FilesFoldersHelper.FormatBytes(entry.OriginalSize));
                            item.SubItems.Add(entry.RelativePath);
                            item.Tag = entry;
                            fileListView.Items.Add(item);
                        }
                        // Set the application to "archive mode"
                        _currentOpenArchivePath = dialog.FileName;
                        SetUIState(false, isArchiveOpen: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open archive: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            // Check if there are any selected items to avoid errors.
            if (fileListView.SelectedItems.Count > 0)
            {
                // Loop through all selected items and remove them.
                // It's safe to use a foreach loop here because we are iterating
                // over a copy of the selected items, not the list itself.
                foreach (ListViewItem Item in fileListView.SelectedItems)
                {
                    fileListView.Items.Remove(Item);
                }
            }
            else
            {
                MessageBox.Show("Please select one or more files to remove.", "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Event handler for the "Remove All" button
        private void btnReset_Click(object sender, EventArgs e)
        {
            SetUIState(false, false);
            if (fileListView.Items.Count > 0)
            {
                foreach (ListViewItem Item in fileListView.Items)
                {
                    fileListView.Items.Remove(Item);
                }
            }
        }

        // Event handler for the "Compress" button
        private async void btnCompress_Click(object sender, EventArgs e)
        {
            if (fileListView.Items.Count == 0)
            {
                MessageBox.Show(
                    "Please add files to compress.",
                    "No Files",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "FCP Archive (*.fcp)|*.fcp";
                dialog.Title = "Save Archive As";
                if (dialog.ShowDialog() != DialogResult.OK) return;

                string outputArchivePath = dialog.FileName;
                var filesToArchive = new Dictionary<string, string>();

                foreach (ListViewItem item in fileListView.Items)
                {
                    filesToArchive[item.Tag.ToString()] = item.SubItems[2].Text;
                }

                CompressInterface algorithm = radioHuffman.Checked
                    ? (CompressInterface)new HuffmanAlgorithm()
                    : new ShannonFanoAlgorithm();

                var writer = new ArchiveWriter(algorithm);

                _cancellationTokenSource = new CancellationTokenSource();
                _pauseEvent = new ManualResetEventSlim(true);

                Progress<ProgressInfo> progress = new Progress<ProgressInfo>(report =>
                {
                    progressBar.Value = report.Percentage;
                    lblCurrentFile.Text = report.CurrentFile;
                });

                SetUIState(true);
                lblCurrentActionValue.Text = "Compressing...";

                _pauseEvent.Set();
                btnPauseResume.Text = "Pause";
                _currentOperation = "Compressing";
                try
                {

                    await Task.Run(() => writer.CreateArchive(filesToArchive, outputArchivePath, progress, _cancellationTokenSource.Token, _pauseEvent));

                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        lblCurrentActionValue.Text = "Operation was cancelled.";
                        if (File.Exists(outputArchivePath)) File.Delete(outputArchivePath);
                    }
                    else
                    {
                        byte[] archiveBytes = File.ReadAllBytes(outputArchivePath);

                        EncryptionHelper.HandleEncryptionAndWriteFile(
                            archiveBytes,
                            txtPassword.Text,
                            chkPassword.Checked,
                            outputArchivePath
                        );
                        lblCurrentActionValue.Text = "Completed!";
                        lblOutputPathValue.Text = outputArchivePath;

                        // Calculate and display compression ratio
                        long originalSize = filesToArchive.Keys.Sum(path => new FileInfo(path).Length);
                        long compressedSize = new FileInfo(outputArchivePath).Length;
                        double ratio = (double)compressedSize / originalSize;
                        lblCompressionRatioValue.Text = $"{ratio:P2}"; // Format as percentage

                        MessageBox.Show("Compression completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        fileListView.Items.Clear();
                        lblCompressionRatioValue.Text = "...";
                        lblOutputPathValue.Text = "...";
                        lblCurrentFile.Text = "...";
                        lblCurrentActionValue.Text = "...";
                    }
                }
                catch (OperationCanceledException)
                {
                    lblCurrentActionValue.Text = "Operation was cancelled.";
                    if (File.Exists(outputArchivePath)) File.Delete(outputArchivePath);
                }
                catch (Exception ex)
                {
                    //lblStatus.Text = "Error";
                    MessageBox.Show($"An error occurred during compression: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // 5. Reset UI state
                    SetUIState(false);
                    _cancellationTokenSource.Dispose();
                    _pauseEvent.Dispose();
                }
            }
        }


        private async void btnDecompressSelected_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentOpenArchivePath))
            {
                MessageBox.Show("This function can only be used after opening an archive.", "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (fileListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more files to decompress.", "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            var entriesToExtract = new List<ArchiveEntry>();
            foreach (ListViewItem item in fileListView.SelectedItems)
            {
                if (item.Tag is ArchiveEntry entry)
                {
                    entriesToExtract.Add(entry);
                }
            }

            if (entriesToExtract.Count == 0) return;

            string destinationDirectory;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select destination folder for extraction";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                destinationDirectory = dialog.SelectedPath;
            }

            var reader = new ArchiveReader();
            _cancellationTokenSource = new CancellationTokenSource();
            _pauseEvent = new ManualResetEventSlim(true);
            var progress = new Progress<ProgressInfo>(report =>
            {
                progressBar.Value = report.Percentage;
                lblCurrentFile.Text = report.CurrentFile;
            });

            SetUIState(true, isArchiveOpen: true);
            lblCurrentActionValue.Text = "Decompressing...";

            try
            {
                _currentOperation = "Decompressing";
                await Task.Run(() => reader.ExtractSelectedEntries(_currentOpenArchivePath, entriesToExtract, destinationDirectory,
                                                                   progress, _cancellationTokenSource.Token, _pauseEvent));

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    lblCurrentActionValue.Text = "Decompressing was cancelled.";
                }
                else
                {
                    lblCurrentActionValue.Text = "Decompressing Completed!";
                    lblOutputPathValue.Text = destinationDirectory;
                    MessageBox.Show("Decompressing of selected files completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (OperationCanceledException)
            {
                lblCurrentActionValue.Text = "Decompressing was cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during Decompressing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIState(false);
            }
        }

        // Event handler for the "Extract" button
        private async void btnDecompress_Click(object sender, EventArgs e)
        {
            string sourceArchivePath;
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Archive to Extract";
                dialog.Filter = "FCP Archive (*.fcp)|*.fcp";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                sourceArchivePath = dialog.FileName;
            }

            string destinationDirectory;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select destination folder for extraction";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                destinationDirectory = dialog.SelectedPath;
            }

            var reader = new ArchiveReader();
            _cancellationTokenSource = new CancellationTokenSource();
            _pauseEvent = new ManualResetEventSlim(true);
            var progress = new Progress<ProgressInfo>(report =>
            {
                progressBar.Value = report.Percentage;
                lblCurrentFile.Text = report.CurrentFile;
            });

            SetUIState(true);
            lblCurrentActionValue.Text = "Decompressing...";

            try
            {
                _currentOperation = "Decompressing";
                byte[] actualData = DecryptionHelper.HandleDecryptionIfNeeded(sourceArchivePath);

                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".fcp");
                File.WriteAllBytes(tempPath, actualData);

                await Task.Run(() => reader.ExtractArchive(tempPath, destinationDirectory,
                                                          progress, _cancellationTokenSource.Token, _pauseEvent));

                

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    lblCurrentActionValue.Text = "Decompressing was cancelled.";
                }
                else
                {
                    lblCurrentActionValue.Text = "Completed!";
                    lblOutputPathValue.Text = destinationDirectory;
                    MessageBox.Show("Decompressing completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                File.Delete(tempPath);
            }
            catch (OperationCanceledException)
            {
                lblCurrentActionValue.Text = "Decompressing was cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during Decompressing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIState(false);
            }
        }

        private async void btnDecompressFCP_Click(object sender, EventArgs e)
        {
            string sourceArchivePath = null;

            if (fileListView.SelectedItems.Count == 1)
            {
                var selectedItem = fileListView.SelectedItems[0];
                if (selectedItem.Tag is string fullPath && fullPath.EndsWith(".fcp", StringComparison.OrdinalIgnoreCase))
                {
                    sourceArchivePath = fullPath;
                }
            }

            if (sourceArchivePath == null)
            {
                MessageBox.Show("Please select a single .fcp archive from the list to decompress.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string destinationDirectory;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select destination folder for extraction";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                destinationDirectory = dialog.SelectedPath;
            }

            var reader = new ArchiveReader();
            _cancellationTokenSource = new CancellationTokenSource();
            _pauseEvent = new ManualResetEventSlim(true);
            var progress = new Progress<ProgressInfo>(report =>
            {
                progressBar.Value = report.Percentage;
                lblCurrentFile.Text = report.CurrentFile;
            });

            SetUIState(true);
            lblCurrentActionValue.Text = "Decompressing...";

            try
            {
                _currentOperation = "Decompressing";
                byte[] actualData = DecryptionHelper.HandleDecryptionIfNeeded(sourceArchivePath);

                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".fcp");
                File.WriteAllBytes(tempPath, actualData);

                await Task.Run(() => reader.ExtractArchive(tempPath, destinationDirectory,
                                                          progress, _cancellationTokenSource.Token, _pauseEvent));



                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    lblCurrentActionValue.Text = "Decompressing was cancelled.";
                }
                else
                {
                    lblCurrentActionValue.Text = "Completed!";
                    lblOutputPathValue.Text = destinationDirectory;
                    MessageBox.Show("Decompressing completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                File.Delete(tempPath);
            }
            catch (OperationCanceledException)
            {
                lblCurrentActionValue.Text = "Decompressing was cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during Decompressing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIState(false);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            MessageBox.Show("The Operation was canceled.", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void btnPauseResume_Click(object sender, EventArgs e)
        {
            if (_pauseEvent.IsSet)
            {
                _pauseEvent.Reset();
                btnPauseResume.Text = "Resume";
                lblCurrentActionValue.Text = _currentOperation + " Paused";
            }
            else
            {
                _pauseEvent.Set();
                btnPauseResume.Text = "Pause";
                lblCurrentActionValue.Text = _currentOperation + " Resumed";
            }
        }
        // Event handler for when the "Encrypt with Password" checkbox is changed
        private void chkPassword_CheckedChanged(object sender, EventArgs e)
        {
            // Show or hide the password a box based on the checkbox state.
            txtPassword.Visible = chkPassword.Checked;
        }




    }
}
