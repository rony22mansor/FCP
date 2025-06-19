using FCP.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
            // Use an OpenFileDialog to let the user select multiple files.
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

                        // Add the fully prepared item to the ListView.
                        fileListView.Items.Add(item);
                    }
                }
            }
        }

        // Event handler for the "Add Folder" button
        private void btnAddFolder_Click(object sender, EventArgs e)
        {
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
            // Check if there are any selected items to avoid errors.
            if (fileListView.Items.Count > 0)
            {
                // Loop through all selected items and remove them.
                // It's safe to use a foreach loop here because we are iterating
                // over a copy of the selected items, not the list itself.
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
        private void btnCompress_Click(object sender, EventArgs e)
        {
            // TODO: This is where the main compression logic will be triggered.
            // 1. Get the list of files/folders from the ListView.
            // 2. Ask the user for a save location for the archive.
            // 3. Get the selected algorithm (Huffman/Shannon-Fano).
            // 4. If password protection is enabled, get the password.
            // 5. Start the compression on a background thread (using async/await and a CancellationTokenSource).
            // 6. Update the progress bar and status labels.
            MessageBox.Show("Compress button clicked!");
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


    }
}
