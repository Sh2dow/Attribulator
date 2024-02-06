using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.ModScript.API;
using Attribulator.UI;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using Forms = System.Windows.Forms;

namespace AttribulatorUI
{
    public partial class MainWindow : Window
    {
        public static bool UnsavedChanges = false;

        private string gameExe;
        private string gameFolder;

        private IServiceProvider serviceProvider;
        private IModScriptService modScriptService;

        private Database database;
        private IEnumerable<LoadedFile> files;

        private List<MenuItem> gameMenuItems = new List<MenuItem>();

        private Settings settings;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // Setup
                var services = new ServiceCollection();
                var loaders = Program.GetPluginLoaders();

                // Register services
                services.AddSingleton<IProfileService, ProfileServiceImpl>();
                services.AddSingleton<IStorageFormatService, StorageFormatServiceImpl>();
                services.AddSingleton<IPluginService, PluginServiceImpl>();

                var plugins = Program.ConfigurePlugins(services, loaders);
                serviceProvider = services.BuildServiceProvider();

                Program.LoadProfiles(services, serviceProvider);
                Program.LoadStorageFormats(services, serviceProvider);
                Program.LoadPlugins(plugins, serviceProvider);

                this.modScriptService = this.serviceProvider.GetRequiredService<IModScriptService>();

                this.settings = new Settings();
                this.ScriptEditor.Text = this.settings.Root.Srcipt;

                this.PopulateGameMenuItems();

                this.CheckSelectedGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        private void PopulateGameMenuItems()
        {
            var games = this.settings.Root.Games;
            foreach (var game in games)
            {
                var gameMenuItem = new MenuItem();
                gameMenuItem.Header = game.Header;
                gameMenuItem.Tag = game;
                gameMenuItem.IsChecked = game.Selected;
                gameMenuItem.Click += MenuItem_Game_Click;
                this.GamesMenuItem.Items.Add(gameMenuItem);
                this.gameMenuItems.Add(gameMenuItem);
            }
        }

        private void CheckSelectedGame()
        {
            var selectedGame = this.settings.Root.SelectedGame;
            if (selectedGame != null && !string.IsNullOrWhiteSpace(selectedGame.ExePath))
            {
                this.Open(selectedGame.ExePath);
            }
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            if (this.gameMenuItems.Any(x => x.IsChecked))
            {
                using (var dialog = new Forms.OpenFileDialog())
                {
                    dialog.Filter = "Game executable|*.exe";
                    dialog.Title = "Open game executable";

                    Forms.DialogResult result = dialog.ShowDialog();

                    if (result == Forms.DialogResult.OK)
                    {
                        this.Open(dialog.FileName);
                    }
                }
            }
        }

        private void Open(string exePath)
        {
            this.gameExe = exePath;
            this.gameFolder = System.IO.Path.GetDirectoryName(exePath);
            this.GameFolderLabel.Content = this.gameFolder;

            try
            {
                var profile = this.GetProfile();
                this.database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
                this.files = profile.LoadFiles(database, this.gameFolder + "\\GLOBAL");
                this.database.CompleteLoad();

                this.PopulateTreeView();
                this.settings.Root.SelectedGame.ExePath = this.gameExe;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading game profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            if (this.database != null)
            {
                var profile = this.GetProfile();
                foreach (var file in files)
                {
                    file.Group = "GLOBAL";
                }

                profile.SaveFiles(database, this.gameFolder, files);
            }

            MainWindow.UnsavedChanges = false;
        }

        private IProfile GetProfile()
        {
            var ProfileName = this.DetectGame();
            return serviceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
        }

        private string DetectGame()
        {
            var selectedGame = this.settings.Root.Games.FirstOrDefault(x => x.Selected);
            return selectedGame?.Profile;
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsUnsaved())
            {
                this.Close();
            }
        }

        private bool IsUnsaved()
        {
            if (MainWindow.UnsavedChanges)
            {
                var result = MessageBox.Show("There are unsaved changes. Do you really want to proceed?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.No;
            }

            return false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.IsUnsaved())
            {
                e.Cancel = true;
            }
        }

        void PopulateTreeNode(VltCollection collection, TreeViewItem node)
        {
            if (collection.Children.Count > 0)
            {
                foreach (var child in collection.Children.OrderBy(x => x.Name))
                {
                    var childNode = new CollectionTreeViewItem(child);
                    node.Items.Add(childNode);
                    PopulateTreeNode(child, childNode);
                }
            }

            node.Header = collection.Name;
        }

        private void PopulateTreeView()
        {
            this.TreeView.Items.Clear();
            this.EditGrid.Children.Clear();

            foreach (var cls in this.database.Classes.OrderBy(x => x.Name))
            {
                var node = new TreeViewItem();
                node.Header = cls.Name;

                var collections = this.database.RowManager.GetFlattenedCollections(cls.Name).OrderBy(x => x.Name);
                foreach (var collection in collections)
                {
                    if (collection.Parent == null)
                    {
                        var childNode = new CollectionTreeViewItem(collection);
                        node.Items.Add(childNode);
                        PopulateTreeNode(collection, childNode);
                    }
                }

                this.TreeView.Items.Add(node);
            }
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.Source as CollectionTreeViewItem;
            if (treeViewItem != null)
            {
                var collection = treeViewItem.Collection;
                this.EditGrid.Display(collection);
            }
            else
            {
                this.EditGrid.Children.Clear();
            }
        }

        private void MenuItem_Game_Click(object sender, RoutedEventArgs e)
        {
            var senderItem = sender as MenuItem;
            if (senderItem.IsChecked)
            {
                return;
            }

            if (this.IsUnsaved())
            {
                return;
            }

            foreach (var item in gameMenuItems)
            {
                item.IsChecked = false;
                (item.Tag as GameSettings).Selected = false;
            }

            senderItem.IsChecked = true;
            (senderItem.Tag as GameSettings).Selected = true;
            this.GameFolderLabel.Content = "No game exe selected";

            this.CloseGame();

            this.CheckSelectedGame();
        }

        private void CloseGame()
        {
            this.gameExe = null;
            this.gameFolder = null;
            this.database = null;
            this.files = null;
            MainWindow.UnsavedChanges = false;
            this.TreeView.Items.Clear();
            this.EditGrid.Children.Clear();
        }

        private void MenuItemGameRun_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.gameExe))
            {
                Process.Start(new ProcessStartInfo(this.gameExe) { WorkingDirectory = this.gameFolder });
            }
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.settings.Root.Srcipt = this.ScriptEditor.Text;
            this.settings.Save();
        }

        private void MenuItem_Reload_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.gameExe))
            {
                var gameExe = this.gameExe;
                this.CloseGame();
                this.Open(gameExe);
            }
        }

        private void MenuItem_CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            if (this.files != null && this.files.Any() && !string.IsNullOrEmpty(this.gameFolder))
            {
                var targetPath = Path.Combine(this.gameFolder, "GLOBAL", "AttribulatorBackups", DateTime.Now.ToString("yyyy-MM-dd-H-m-ss"));
                Directory.CreateDirectory(targetPath);

                foreach (var file in this.files)
                {
                    var extensions = new[] { ".bin", ".lzc" };
                    foreach (var extension in extensions)
                    {
                        var original = Path.Combine(this.gameFolder, "GLOBAL", file.Name + extension);
                        if (File.Exists(original))
                        {
                            File.Copy(original, Path.Combine(targetPath, file.Name + extension));
                        }
                    }
                }
            }
        }

        private void MenuItem_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.gameFolder))
            {
                var backupsDir = Path.Combine(this.gameFolder, "GLOBAL", "AttribulatorBackups");
                if (Directory.Exists(backupsDir))
                {
                    var dirs = Directory.GetDirectories(backupsDir);
                    if (dirs.Length > 0)
                    {
                        string backup = dirs.OrderByDescending(x => Path.GetFileName(x)).First();
                        var files = Directory.GetFiles(backup);
                        foreach (var file in files)
                        {
                            var fileName = Path.GetFileName(file);
                            File.Copy(file, Path.Combine(this.gameFolder, "GLOBAL", fileName), true);
                        }
                    }
                }
            }
        }

        private void MenuItem_Script_ExeculteAll_Click(object sender, RoutedEventArgs args)
        {
            if (this.database != null && this.ScriptEditor.Text.Length > 0)
            {
                var lines = this.ScriptEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                this.ExecuteScript(lines);
            }
        }

        private void ExecuteScript(IEnumerable<string> lines)
        {
            var errors = new List<string>();

            var modScriptDatabase = new DatabaseHelper(this.database);
            foreach (var command in this.modScriptService.ParseCommands(lines))
            {
                try
                {
                    command.Execute(modScriptDatabase);
                }
                catch (Exception e)
                {
                    errors.Add(e.Message);
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors.Take(5)), "Failed to execute script", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            this.PopulateTreeView();
        }

        private void MenuItem_ImportModScript_Click(object sender, RoutedEventArgs e)
        {
            if (this.database != null)
            {
                using (var dialog = new Forms.OpenFileDialog())
                {
                    dialog.Filter = "ModScript|*.nfsms";
                    dialog.Title = "Import ModScript file";

                    Forms.DialogResult result = dialog.ShowDialog();

                    if (result == Forms.DialogResult.OK)
                    {
                        var importWindow = new ImportModScriptWindow(dialog.FileName);
                        importWindow.ShowDialog();
                        var resultScript = importWindow.ResultScript;
                        this.ExecuteScript(resultScript);
                    }
                }
            }
        }

        private void MenuItem_ScriptsClear_Click(object sender, RoutedEventArgs e)
        {
            this.ScriptEditor.Clear();
        }
    }
}
