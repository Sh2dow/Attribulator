using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Attribulator.UI.Windows
{
    public class BackupModel
    {
        public string Name { get; set; }

        public override string ToString()
        {
            var time = DateTime.ParseExact(this.Name, "yyyy-MM-dd-H-m-ss", null);
            return time.ToString();
        }
    }

    public partial class RestoreBackupWindow : Window
    {
        public string ResultName { get; private set; }

        public RestoreBackupWindow(string backupFolder)
        {
            InitializeComponent();

            if (Directory.Exists(backupFolder))
            {
                var dirs = Directory.GetDirectories(backupFolder);
                if (dirs.Length > 0)
                {
                    var backups = dirs.Where(x => !x.Contains("SaveBackup"))
                        .Select(x => new BackupModel { Name = Path.GetFileName(x) })
                        .OrderByDescending(x => x.Name)
                        .ToList();

                    foreach (var backup in backups)
                    {
                        this.BackupsList.Items.Add(backup);
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.BackupsList.SelectedItem as BackupModel;
            if(selectedItem != null)
            {
                this.ResultName = selectedItem.Name;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Select backup to restore", "Restore backup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
