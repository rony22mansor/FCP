using FCP.Controllers;
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
        // Flag to track the paused state of an operation
        private bool isPaused = false;
        private System.Threading.CancellationTokenSource _cancellationTokenSource;
        public MainForm()
        {
            InitializeComponent();
            // Initially hide the password text box
            txtPassword.Visible = false;

        }

        // Event handler for the "Add Files" button
        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            if (fileListView.Items.Count > 0)
            {
                foreach (ListViewItem Item in fileListView.Items)
                {
                    fileListView.Items.Remove(Item);
                }
            }
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


        // Event handler for the "Remove Selected" button
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
        private void btnRemoveAll_Click(object sender, EventArgs e)
        {
            if (fileListView.Items.Count > 0)
            {
                foreach (ListViewItem Item in fileListView.Items)
                {
                    fileListView.Items.Remove(Item);
                }
            }
            else
            {
                MessageBox.Show("Please add one or more files to remove.", "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _cancellationTokenSource = new System.Threading.CancellationTokenSource();

                Progress<ProgressInfo> progress = new Progress<ProgressInfo>(report =>
                {
                    progressBar.Value = report.Percentage;
                    lblCurrentFile.Text = report.CurrentFile;
                });

                SetUIState(true);
                lblCurrentActionValue.Text = "Compressing...";

                try
                {
                    await Task.Run(
                        () => writer.CreateArchive(filesToArchive, outputArchivePath, progress),
                        _cancellationTokenSource.Token
                    );

                    byte[] archiveBytes = File.ReadAllBytes(outputArchivePath);

                    if (chkPassword.Checked && !string.IsNullOrWhiteSpace(txtPassword.Text))
                    {
                        // مشفّر
                        byte[] encryptedBytes = EncryptionHelper.EncryptWithPassword(archiveBytes, txtPassword.Text);

                        // إضافة علم يدل على التشفير
                        byte[] withFlag = new byte[1 + encryptedBytes.Length];
                        withFlag[0] = (byte)'E';
                        Buffer.BlockCopy(encryptedBytes, 0, withFlag, 1, encryptedBytes.Length);
                        File.WriteAllBytes(outputArchivePath, withFlag);

                        MessageBox.Show(
                            "The archive was successfully encrypted with your password.",
                            "Encryption Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        // إضافة علم يدل على عدم التشفير
                        byte[] withFlag = new byte[1 + archiveBytes.Length];
                        withFlag[0] = (byte)'U';
                        Buffer.BlockCopy(archiveBytes, 0, withFlag, 1, archiveBytes.Length);
                        File.WriteAllBytes(outputArchivePath, withFlag);
                    }

                    // الحساب والعرض
                    long originalSize = filesToArchive.Keys.Sum(path => new FileInfo(path).Length);
                    long compressedSize = new FileInfo(outputArchivePath).Length;
                    double ratio = (double)compressedSize / originalSize;
                    lblCompressionRatioValue.Text = $"{ratio:P2}";

                    lblCurrentActionValue.Text = "Completed!";
                    lblOutputPathValue.Text = outputArchivePath;

                    MessageBox.Show(
                        "Compression completed successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // إعادة ضبط القيم
                    lblCurrentFile.Text = "...";
                    lblCurrentActionValue.Text = "...";
                }
                catch (OperationCanceledException)
                {
                    lblCurrentActionValue.Text = "Operation was canceled.";
                    if (File.Exists(outputArchivePath)) File.Delete(outputArchivePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"An error occurred during compression: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                finally
                {
                    SetUIState(false);
                    _cancellationTokenSource.Dispose();
                }
            }
        }



        // Event handler for the "Extract" button
        private async void btnExtract_Click(object sender, EventArgs e)
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

            var progress = new Progress<ProgressInfo>(report =>
            {
                progressBar.Value = report.Percentage;
                lblCurrentActionValue.Text = report.CurrentFile;
            });

            SetUIState(true);
            lblCurrentActionValue.Text = "Preparing to extract...";

            try
            {
                byte[] archiveBytes = File.ReadAllBytes(sourceArchivePath);

                // قراءة العلم
                char encryptionFlag = (char)archiveBytes[0];
                byte[] actualData = archiveBytes.Skip(1).ToArray();

                if (encryptionFlag == 'E')
                {
                    var passwordDialog = new PasswordPromptForm();
                    var result = passwordDialog.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(passwordDialog.Password))
                    {
                        actualData = DecryptionHelper.DecryptWithPassword(actualData, passwordDialog.Password);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Password is required to decrypt this archive.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        return;
                    }
                }

                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".fcp");
                File.WriteAllBytes(tempPath, actualData);

                lblCurrentActionValue.Text = "Decompressing...";
                await Task.Run(() => reader.ExtractArchive(tempPath, destinationDirectory, progress));
                File.Delete(tempPath); // تنظيف الملف المؤقت

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    lblCurrentActionValue.Text = "Extraction was cancelled.";
                }
                else
                {
                    lblCurrentActionValue.Text = "Extraction Completed!";
                    lblOutputPathValue.Text = destinationDirectory;

                    MessageBox.Show(
                        "Extraction completed successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (OperationCanceledException)
            {
                lblCurrentActionValue.Text = "Extraction was cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred during extraction: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                SetUIState(false);
                _cancellationTokenSource.Dispose();
            }
        }



        // Event handler for the "Cancel" button in the status panel
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Console.WriteLine("dadasdasdsad");
            // Signal the CancellationTokenSource to cancel the operation.
            if (_cancellationTokenSource != null)
            {
                Console.WriteLine("============");
                _cancellationTokenSource.Cancel();
            }
        }

        // Event handler for the combined Pause/Resume button
        private void btnPauseResume_Click(object sender, EventArgs e)
        {
            isPaused = !isPaused; // Toggle the state

            if (isPaused)
            {
                btnPauseResume.Text = "Resume";
                // TODO: Implement logic to pause the operation.
                // This could involve using a ManualResetEventSlim to block the background thread.
                MessageBox.Show("Operation Paused!");
            }
            else
            {
                btnPauseResume.Text = "Pause";
                // TODO: Implement logic to resume the operation.
                // This would signal the ManualResetEventSlim to unblock the background thread.
                MessageBox.Show("Operation Resumed!");
            }
        }

        // Event handler for when the "Encrypt with Password" checkbox is changed
        private void chkPassword_CheckedChanged(object sender, EventArgs e)
        {
            // Show or hide the password a box based on the checkbox state.
            txtPassword.Visible = chkPassword.Checked;
        }




        private void SetUIState(bool isProcessing)
        {
            // Disable/enable controls based on whether an operation is running.
            mainToolStrip.Enabled = !isProcessing;
            groupBoxOptions.Enabled = !isProcessing;
            btnPauseResume.Enabled = isProcessing;
            btnCancel.Enabled = isProcessing;

            if (!isProcessing)
            {
                // Reset UI elements after completion or cancellation.
                progressBar.Value = 0;
                lblCurrentActionValue.Text = "...";
                btnPauseResume.Text = "Pause";
            }
        }

    }
}
