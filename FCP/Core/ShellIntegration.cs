using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace FCP.Core
{
    public static class ShellIntegration
    {
        private const string AppName = "FCPCompressor";
        private const string FileTypeProgId = "FCP.ArchiveFile";
        private const string FileExtension = ".fcp";

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

                // 1. Register the custom .fcp extension
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

                // 2. Add "Compress with FCP" for all FILES
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($@"*\shell\{AppName}Compress"))
                {
                    key.SetValue("", "Compress with FCP");
                    // Removes restriction to allow all file types (not just non-.fcp)
                    using (RegistryKey command = key.CreateSubKey("command"))
                        command.SetValue("", $"\"{appPath}\" \"%1\"");
                }

                // 3. Add "Compress with FCP" for FOLDERS
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
                return; // User cancelled UAC
            }

            Application.Exit();
        }
    }
}
