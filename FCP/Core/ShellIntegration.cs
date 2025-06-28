using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace FCP.Core
{
    /// <summary>
    /// Manages the integration of the application with Windows Shell context menu.
    /// </summary>
    public static class ShellIntegration
    {
        private const string AppName = "FCPCompressor";
        private const string FileTypeProgId = "FCP.ArchiveFile";
        private const string FileExtension = ".fcp";

        /// <summary>
        /// Registers context menu entries for files, folders, and .fcp files.
        /// </summary>
        public static void Register()
        {
            if (!IsAdministrator())
            {
                RelaunchAsAdmin();
                return;
            }

            try
            {
                string appPath = Application.ExecutablePath;

                // 1. Register the custom file extension (.fcp)
                using (RegistryKey extKey = Registry.ClassesRoot.CreateSubKey(FileExtension))
                    extKey.SetValue("", FileTypeProgId);

                using (RegistryKey typeKey = Registry.ClassesRoot.CreateSubKey(FileTypeProgId))
                {
                    typeKey.SetValue("", "FCP Archive");

                    using (RegistryKey openCmd = typeKey.CreateSubKey(@"shell\open\command"))
                        openCmd.SetValue("", $"\"{appPath}\" \"%1\"");

                    using (RegistryKey decompress = typeKey.CreateSubKey($@"shell\{AppName}Decompress"))
                    {
                        decompress.SetValue("", "Decompress with FCP");
                        using (RegistryKey command = decompress.CreateSubKey("command"))
                            command.SetValue("", $"\"{appPath}\" \"%1\"");
                    }
                }

                // 2. Add "Compress with FCP" for all files (except .fcp)
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($@"*\shell\{AppName}Compress"))
                {
                    key.SetValue("", "Compress with FCP");
                    key.SetValue("AppliesTo", "System.FileExtension:<>\"" + FileExtension + "\"");

                    using (RegistryKey command = key.CreateSubKey("command"))
                        command.SetValue("", $"\"{appPath}\" \"%1\"");
                }

                // 3. Add "Compress with FCP" for folders
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($@"Directory\shell\{AppName}Compress"))
                {
                    key.SetValue("", "Compress with FCP");

                    using (RegistryKey command = key.CreateSubKey("command"))
                        command.SetValue("", $"\"{appPath}\" \"%1\"");
                }

                MessageBox.Show("Shell integration registered successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to register shell integration:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Unregisters all context menu entries.
        /// </summary>
        public static void Unregister()
        {
            if (!IsAdministrator())
            {
                RelaunchAsAdmin();
                return;
            }

            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(FileExtension, false);
                Registry.ClassesRoot.DeleteSubKeyTree(FileTypeProgId, false);
                Registry.ClassesRoot.DeleteSubKeyTree($@"*\shell\{AppName}Compress", false);
                Registry.ClassesRoot.DeleteSubKeyTree($@"Directory\shell\{AppName}Compress", false);

                MessageBox.Show("Shell integration unregistered successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to unregister shell integration:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void RelaunchAsAdmin()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Application.ExecutablePath,
                WorkingDirectory = Environment.CurrentDirectory,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                // User declined UAC
                return;
            }

            Application.Exit();
        }
    }
}
