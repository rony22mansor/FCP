using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FCP.Helpers
{
    public static class FilesFoldersHelper
    {
        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (dblSByte >= 1024 && i < suffixes.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return String.Format("{0:0.##} {1}", dblSByte, suffixes[i]);
        }

        public static void AddAllFilesFromFolder(string currentFolderPath, string rootPath, ListView fileListView)
        {
            try
            {
                // 1. Process all files in the current folder.
                foreach (string filePath in Directory.GetFiles(currentFolderPath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    // Calculate the path relative to the root folder.
                    string relativePath = filePath.Replace(rootPath, "").TrimStart(Path.DirectorySeparatorChar);

                    ListViewItem item = new ListViewItem(fileInfo.Name);
                    item.SubItems.Add(FormatBytes(fileInfo.Length));
                    item.SubItems.Add(relativePath);
                    item.Tag = fileInfo.FullName; // Store the full path for reading the file.

                    fileListView.Items.Add(item);
                }

                // 2. Recursively call this method for each sub-folder.
                foreach (string directoryPath in Directory.GetDirectories(currentFolderPath))
                {
                    AddAllFilesFromFolder(directoryPath, rootPath, fileListView);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding a folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
