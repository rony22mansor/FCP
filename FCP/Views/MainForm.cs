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

            // Wire up the form's Resize event to our handler method.
            this.Resize += new System.EventHandler(this.MainForm_Resize);

            // Call once at startup to set initial column sizes.
            ResizeListViewColumns();
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
                MessageBox.Show("Please add files to compress.", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Prompt user for save location
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "FCP Archive (*.fcp)|*.fcp";
                dialog.Title = "Save Archive As";
                if (dialog.ShowDialog() != DialogResult.OK) return;

                // 2. Prepare for compression
                string outputArchivePath = dialog.FileName;
                var filesToArchive = new Dictionary<string, string>();
                foreach (ListViewItem item in fileListView.Items)
                {
                    // Key: Full path, Value: Relative path
                    filesToArchive[item.Tag.ToString()] = item.SubItems[2].Text;
                }

                CompressInterface algorithm = radioHuffman.Checked ? (CompressInterface)new HuffmanAlgorithm() : new ShannonFanoAlgorithm();
                var writer = new ArchiveWriter(algorithm);

                _cancellationTokenSource = new System.Threading.CancellationTokenSource();

                SetUIState(true);
                //lblStatus.Text = "Compressing...";
                lblCurrentActionValue.Text = "Preparing...";

                try
                {
                    // 3. Run the compression on a background thread
                    await Task.Run(() => writer.CreateArchive(filesToArchive, outputArchivePath), _cancellationTokenSource.Token);

                    // 4. Update UI on completion
                    //lblStatus.Text = "Ready";
                    lblCurrentActionValue.Text = "Completed!";
                    lblOutputPathValue.Text = outputArchivePath;

                    // Calculate and display compression ratio
                    long originalSize = filesToArchive.Keys.Sum(path => new FileInfo(path).Length);
                    long compressedSize = new FileInfo(outputArchivePath).Length;
                    double ratio = (double)compressedSize / originalSize;
                    lblCompressionRatioValue.Text = $"{ratio:P2}"; // Format as percentage

                    MessageBox.Show("Compression completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    //lblStatus.Text = "Cancelled";
                    lblCurrentActionValue.Text = "Operation was cancelled.";
                    // Clean up partially created file
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
                }
            }
        }

        // Event handler for the "Extract" button
        private void btnExtract_Click(object sender, EventArgs e)
        {
            // TODO: This is where the main extraction logic will be triggered.
            // 1. Ask the user to select an archive file.
            // 2. Display the archive contents in the ListView.
            // 3. Ask the user for an extraction destination folder.
            // 4. If the archive is encrypted, prompt for the password.
            // 5. Start extraction on a background thread.
            MessageBox.Show("Extract button clicked!");
        }

        // Event handler for the "Cancel" button in the status panel
        private void btnCancel_Click(object sender, EventArgs e)
        {
            // TODO: Implement logic to cancel the ongoing operation.
            // This will typically involve calling .Cancel() on a CancellationTokenSource.
            MessageBox.Show("Cancel button clicked!");
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


        private void MainForm_Resize(object sender, EventArgs e)
        {
            ResizeListViewColumns();
        }

        /// <summary>
        /// Adjusts the ListView column widths to be proportional to the control's width.
        /// </summary>
        private void ResizeListViewColumns()
        {

            // Get the total client width of the ListView, subtracting a little for the vertical scrollbar
            int totalWidth = fileListView.ClientSize.Width;

            // Set column widths proportionally
            colFileName.Width = (int)(totalWidth * 0.30); // 30% for File Name
            colSize.Width = (int)(totalWidth * 0.15);     // 15% for Size
            colPath.Width = totalWidth - colFileName.Width - colSize.Width; // The rest for Path
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
