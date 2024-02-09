using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.ModScript.API;
using Attribulator.UI;
using Microsoft.Extensions.DependencyInjection;
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
        private DatabaseHelper modScriptDatabase;
        private IEnumerable<LoadedFile> files;

        private List<MenuItem> gameMenuItems;

        private Settings settings;

        public static MainWindow Instance { get; private set; }

        private ClassTreeViewItem currentClass = null;
        private CollectionTreeViewItem currentCollection = null;
        private CollectionTreeViewItem collectionToCopy = null;
        private ContextMenu classContextMenu = null;
        private ContextMenu collectionContextMenu = null;
        private bool cutNode = false;

        public MainWindow()
        {
            MainWindow.Instance = this;

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

                this.classContextMenu = new ContextMenu();
                this.collectionContextMenu = new ContextMenu();

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
            this.gameMenuItems = new List<MenuItem>();
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
                this.modScriptDatabase = new DatabaseHelper(this.database);

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
                this.Backup("SaveBackup");

                var profile = this.GetProfile();
                foreach (var file in files)
                {
                    file.Group = "GLOBAL";
                }

                try
                {
                    profile.SaveFiles(database, this.gameFolder, files);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error saving file", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.Restore(Path.Combine(this.gameFolder, "GLOBAL", "AttribulatorBackups", "SaveBackup"));
                }
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

        private void PopulateTreeNode(VltCollection collection, TreeViewItem node)
        {
            if (collection.Children.Count > 0)
            {
                foreach (var child in collection.Children.OrderBy(x => x.Name))
                {
                    var childNode = new CollectionTreeViewItem(child);
                    childNode.ContextMenu = this.collectionContextMenu;
                    node.Items.Add(childNode);
                    PopulateTreeNode(child, childNode);
                }
            }
        }

        public void PopulateTreeView()
        {
            this.TreeView.Items.Clear();
            this.EditGrid.Children.Clear();

            var classes = this.database.Classes.OrderBy(x => x.Name);
            foreach (var cls in classes)
            {
                var classNode = new ClassTreeViewItem(cls);
                classNode.ContextMenu = this.classContextMenu;

                var collections = this.database.RowManager.EnumerateCollections(cls.Name).OrderBy(x => x.Name);
                foreach (var collection in collections)
                {
                    var childNode = new CollectionTreeViewItem(collection);
                    childNode.ContextMenu = this.collectionContextMenu; ;
                    classNode.Items.Add(childNode);
                    PopulateTreeNode(collection, childNode);
                }

                this.TreeView.Items.Add(classNode);
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
            this.modScriptDatabase = null;
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
                this.Backup(DateTime.Now.ToString("yyyy-MM-dd-H-m-ss"));
            }
        }

        private void Backup(string folderName)
        {
            var targetPath = Path.Combine(this.gameFolder, "GLOBAL", "AttribulatorBackups", folderName);
            Directory.CreateDirectory(targetPath);

            foreach (var file in this.files)
            {
                var extensions = new[] { ".bin", ".lzc" };
                foreach (var extension in extensions)
                {
                    var original = Path.Combine(this.gameFolder, "GLOBAL", file.Name + extension);
                    if (File.Exists(original))
                    {
                        File.Copy(original, Path.Combine(targetPath, file.Name + extension), true);
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
                        string backup = dirs.Where(x => x != "SaveBackup").OrderByDescending(x => Path.GetFileName(x)).First();
                        this.Restore(backup);
                    }
                }
            }
        }

        private void Restore(string folderName)
        {
            var files = Directory.GetFiles(folderName);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(this.gameFolder, "GLOBAL", fileName), true);
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

            try
            {
                foreach (var command in this.modScriptService.ParseCommands(lines))
                {
                    try
                    {
                        command.Execute(this.modScriptDatabase);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e.Message);
                    }
                }
            }
            catch(Exception e)
            {
                errors.Add(e.Message);
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors.Take(5)), "Failed to execute script", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            this.PopulateTreeView();
        }

        public void ExecuteScriptInternal(IEnumerable<string> lines)
        {
            foreach (var command in this.modScriptService.ParseCommands(lines))
            {
                try
                {
                    command.Execute(this.modScriptDatabase);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error executing script", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            MainWindow.UnsavedChanges = true;
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

        private void MenuItem_Export_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new Forms.SaveFileDialog())
            {
                dialog.Filter = "ModScript|*.nfsms";
                dialog.Title = "Import ModScript file";

                Forms.DialogResult result = dialog.ShowDialog();

                if (result == Forms.DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, this.ScriptEditor.Text);
                }
            }
        }

        private void MenuItem_ScriptsClear_Click(object sender, RoutedEventArgs e)
        {
            this.ScriptEditor.Clear();
        }

        public void AddScriptLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                this.AddScriptLine(line);
            }
        }

        public void AddScriptLine(string line)
        {
            this.ScriptEditor.Text += "\n" + line;
        }

        private void CreateClassContextMenu()
        {
            var contextMenu = this.classContextMenu;

            var menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Click += (s, e) =>
            {
                var newNodeWindow = new NewNodeNameWindow(this.currentClass.Class.Name);
                newNodeWindow.ShowDialog();
                var result = newNodeWindow.Result;
                if (!string.IsNullOrEmpty(result))
                {
                    string command = $"add_node {this.currentClass.Class.Name} {result}";
                    this.ExecuteScriptInternal(new[] { command });
                    this.AddScriptLine(command);
                    this.PopulateTreeView();
                }
            };
            contextMenu.Items.Add(menuItem);

            if (this.collectionToCopy != null && this.collectionToCopy.Collection.Class == this.currentCollection.Collection.Class)
            {
                contextMenu.Items.Add(new Separator());

                menuItem = new MenuItem();
                menuItem.Header = $"Paste ({this.collectionToCopy.Header})";
                menuItem.Click += (s, e) =>
                {
                    var collection = this.collectionToCopy.Collection;
                    string command;
                    if (this.cutNode)
                    {
                        command = $"move_node {collection.Class.Name} {collection.Name}";
                    }
                    else
                    {
                        command = $"copy_node {collection.Class.Name} {collection.Name} {collection.Name}_copy";
                    }
                    this.collectionToCopy = null;
                    this.ExecuteScriptInternal(new[] { command });
                    this.AddScriptLine(command);
                    this.PopulateTreeView();
                };
                contextMenu.Items.Add(menuItem);
            }
        }

        private void CreateCollectionContextMenu()
        {
            var contextMenu = this.collectionContextMenu;
            contextMenu.Items.Clear();

            var menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Click += (s, e) =>
            {
                var collection = this.currentCollection.Collection;
                var newNodeWindow = new NewNodeNameWindow(collection.Name);
                newNodeWindow.ShowDialog();
                var result = newNodeWindow.Result;
                if (!string.IsNullOrEmpty(result))
                {
                    string command = $"add_node {collection.Class.Name} {collection.Name} {result}";
                    this.ExecuteScriptInternal(new[] { command });
                    this.AddScriptLine(command);
                    this.PopulateTreeView();
                }
            };
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Delete";
            menuItem.Click += (s, e) =>
            {
                var collection = this.currentCollection.Collection;
                string command = $"delete_node {collection.Class.Name} {collection.Name}";
                this.ExecuteScriptInternal(new[] { command });
                this.AddScriptLine(command);
                this.PopulateTreeView();
            };
            contextMenu.Items.Add(menuItem);

            contextMenu.Items.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = "Copy";
            menuItem.Click += (s, e) =>
            {
                this.collectionToCopy = this.currentCollection;
                this.cutNode = false;
            };
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Cut";
            menuItem.Click += (s, e) =>
            {
                this.collectionToCopy = this.currentCollection;
                this.cutNode = true;
            };
            contextMenu.Items.Add(menuItem);

            if (this.collectionToCopy != null && this.collectionToCopy.Collection.Class == this.currentCollection.Collection.Class)
            {
                menuItem = new MenuItem();
                menuItem.Header = $"Paste ({this.collectionToCopy.Header})";
                menuItem.Click += (s, e) =>
                {
                    var collection = this.collectionToCopy.Collection;
                    string command;
                    if (this.cutNode)
                    {
                        command = $"move_node {collection.Class.Name} {collection.Name} {this.currentCollection.Header}";
                    }
                    else
                    {
                        command = $"copy_node {collection.Class.Name} {collection.Name} {this.currentCollection.Header} {collection.Name}_copy";
                    }

                    this.collectionToCopy = null;
                    this.ExecuteScriptInternal(new[] { command });
                    this.AddScriptLine(command);
                    this.PopulateTreeView();
                };
                contextMenu.Items.Add(menuItem);
            }

            contextMenu.Items.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = "Edit fields";
            menuItem.Click += (s, e) =>
            {
                new EditFieldsWindow(this.currentCollection.Collection).ShowDialog();
            };
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Rename";
            menuItem.Click += (s, e) =>
            {
                var collection = this.currentCollection.Collection;
                new CollectionRenameWindow(collection).ShowDialog();
                this.currentCollection.Header = collection.Name;
            };
            contextMenu.Items.Add(menuItem);
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.Source is ClassTreeViewItem)
            {
                this.currentClass = (ClassTreeViewItem)e.Source;
                this.CreateClassContextMenu();
            }

            if (e.Source is CollectionTreeViewItem)
            {
                this.currentCollection = (CollectionTreeViewItem)e.Source;
                this.CreateCollectionContextMenu();
            }
        }

        private void MenuItem_Raider_Click(object sender, RoutedEventArgs e)
        {
            new RaiderWindow().ShowDialog();
        }
    }
}
