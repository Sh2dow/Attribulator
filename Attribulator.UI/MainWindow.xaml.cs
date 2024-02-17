using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.ModScript.API;
using Attribulator.UI;
using Attribulator.UI.PropertyGrid;
using Attribulator.UI.Windows;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.Instance = this;

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

        private void Command_Open(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
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
            else
            {
                MessageBox.Show("Please select game profile first", "No game profile selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Open(string exePath)
        {
            this.gameExe = exePath;
            this.gameFolder = System.IO.Path.GetDirectoryName(exePath);
            this.StatusLabel.Content = this.gameFolder;

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

        private void Command_Save(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.database != null)
            {
                try
                {
                    this.Backup("SaveBackup");

                    var profile = this.GetProfile();
                    foreach (var file in this.files)
                    {
                        file.Group = "GLOBAL";
                    }

                    profile.SaveFiles(this.database, this.gameFolder, this.files);
                    this.StatusLabel.Content = "Saved";
                    MainWindow.UnsavedChanges = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error saving file", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.Restore(Path.Combine(this.gameFolder, "GLOBAL", "AttribulatorBackups", "SaveBackup"));
                }
            }
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
                    var childNode = new CollectionTreeViewItem(child, node);
                    childNode.ContextMenu = this.collectionContextMenu;
                    node.Items.Add(childNode);
                    PopulateTreeNode(child, childNode);
                }
            }
        }

        public void PopulateTreeView()
        {
            this.TreeView.Items.Clear();
            this.Tabs.Items.Clear();
            this.currentClass = null;
            this.currentCollection = null;

            var classes = this.database.Classes.OrderBy(x => x.Name);
            foreach (var cls in classes)
            {
                var classNode = new ClassTreeViewItem(cls, null);
                classNode.ContextMenu = this.classContextMenu;

                var collections = this.database.RowManager.EnumerateCollections(cls.Name).OrderBy(x => x.Name);
                foreach (var collection in collections)
                {
                    var childNode = new CollectionTreeViewItem(collection, classNode);
                    childNode.ContextMenu = this.collectionContextMenu;
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
                this.currentCollection = treeViewItem;
                this.currentClass = null;
            }
            else
            {
                this.currentCollection = null;
                this.currentClass = e.Source as ClassTreeViewItem;
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection;

                if (!this.Tabs.Items.Cast<TabItem>().Any(x => x.Header as string == collection.ShortPath))
                {
                    var ti = new TabItem();
                    ti.Header = collection.ShortPath;
                    ti.Content = new MainGrid(collection);
                    this.Tabs.Items.Add(ti);
                    this.Tabs.SelectedIndex = this.Tabs.Items.Count - 1;
                }
                else
                {
                    for (int i = 0; i < this.Tabs.Items.Count; i++)
                    {
                        var tabItem = this.Tabs.Items[i] as TabItem;
                        if(tabItem.Header as string == collection.ShortPath)
                        {
                            this.Tabs.SelectedIndex = i; 
                            break;
                        }
                    }
                }
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
            this.StatusLabel.Content = "No game exe selected";

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
            this.collectionToCopy = null;
            this.currentClass = null;
            this.currentCollection = null;
            MainWindow.UnsavedChanges = false;
            this.TreeView.Items.Clear();
            this.Tabs.Items.Clear();
            BaseModScriptCommand.ClearCache();
        }

        private void Command_RunGame(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(this.gameExe))
                {
                    Process.Start(new ProcessStartInfo(this.gameExe) { WorkingDirectory = this.gameFolder });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to run the game", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            HashManager.Save();
            this.settings.Root.Srcipt = this.ScriptEditor.Text;
            this.settings.Save();
        }

        private void Command_Reload(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.gameExe))
            {
                var gameExe = this.gameExe;
                this.CloseGame();
                this.Open(gameExe);
                this.StatusLabel.Content = "Reloaded";
            }
        }

        private void MenuItem_CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            if (this.files != null && this.files.Any() && !string.IsNullOrEmpty(this.gameFolder))
            {
                var name = DateTime.Now.ToString("yyyy-MM-dd-H-m-ss");
                this.Backup(name);
                this.StatusLabel.Content = $"Created backup: {name}";
            }
        }

        private void Backup(string folderName)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to backup", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        string backup = dirs.Where(x => !x.Contains("SaveBackup")).OrderByDescending(x => Path.GetFileName(x)).FirstOrDefault();
                        if (!string.IsNullOrEmpty(backup))
                        {
                            this.Restore(backup);
                            this.StatusLabel.Content = $"Restored backup: {backup}";
                            return;
                        }
                    }
                }

                MessageBox.Show("No backups found", "Restore backup", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Restore(string folderName)
        {
            try
            {
                var files = Directory.GetFiles(folderName);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(this.gameFolder, "GLOBAL", fileName), true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to restore backup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Command_ExecuteAll(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.database != null && this.ScriptEditor.Text.Length > 0)
            {
                var lines = this.ScriptEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                this.ExecuteScript(lines);
                this.StatusLabel.Content = "Executed script";
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
            catch (Exception e)
            {
                errors.Add(e.Message);
            }

            if (errors.Count > 0)
            {
                new ScriptErrorWindow(errors).ShowDialog();
            }

            MainWindow.UnsavedChanges = true;
            this.PopulateTreeView();
        }

        public bool ExecuteScriptInternal(params string[] lines)
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
                    return false;
                }
            }

            MainWindow.UnsavedChanges = true;
            return true;
        }

        private void Command_Import(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
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
                        this.StatusLabel.Content = $"Imported script: {dialog.FileName}";
                    }
                }
            }
        }

        private void Command_Export(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            using (var dialog = new Forms.SaveFileDialog())
            {
                dialog.Filter = "ModScript|*.nfsms";
                dialog.Title = "Export ModScript file";

                Forms.DialogResult result = dialog.ShowDialog();

                if (result == Forms.DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, this.ScriptEditor.Text);
                    this.StatusLabel.Content = $"Exported script: {dialog.FileName}";
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
            if (this.ScriptEditor.Text.Length == 0 || this.ScriptEditor.Text.EndsWith(Environment.NewLine))
            {
                this.ScriptEditor.Text += line;
            }
            else
            {
                this.ScriptEditor.Text += Environment.NewLine + line;
            }

            this.ScriptScroll.ScrollToBottom();
        }

        private Image CreateImageSource(string name)
        {
            var img = new Image();
            img.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/{name}"));
            return img;
        }

        private void Command_TreeAdd(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            NewNodeNameWindow newNodeWindow = null;
            if (this.currentClass != null)
            {
                newNodeWindow = new NewNodeNameWindow(this.currentClass.Class.Name, this.currentClass.Class.Name);
            }

            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection;
                newNodeWindow = new NewNodeNameWindow(collection.Name, $"{collection.Class.Name} {collection.Name}");
            }

            if (newNodeWindow != null)
            {
                if (newNodeWindow.ShowDialog().Value)
                {
                    this.PopulateTreeView();
                    this.StatusLabel.Content = "Added node";
                }
            }
        }

        private void Command_ChangeVault(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null && this.currentCollection.Collection.Class.Name == "gameplay")
            {
                if (new ChangeVaultWindow(this.currentCollection.Collection).ShowDialog().Value)
                {
                    this.GetSelectedGrid()?.Draw();
                    this.StatusLabel.Content = "Changed vault";
                }
            }
        }

        private void Command_TreeDelete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection;
                string command = $"delete_node {collection.Class.Name} {collection.Name}";
                if (this.ExecuteScriptInternal(command))
                {
                    this.AddScriptLine(command);
                    this.StatusLabel.Content = $"Deleted node: {collection.Name}";
                    var parentNode = this.currentCollection.ParentNode;
                    parentNode.Items.Remove(this.currentCollection);
                }
            }
        }

        private void Command_TreeCopy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                this.collectionToCopy = this.currentCollection;
                this.cutNode = false;
            }
        }

        private void Command_TreeCut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                this.collectionToCopy = this.currentCollection;
                this.cutNode = true;
            }
        }

        private void Command_TreeRename(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection;
                if (new CollectionRenameWindow(collection).ShowDialog().Value)
                {
                    this.currentCollection.SetName(collection.Name);
                    this.StatusLabel.Content = "Renamed node";
                    var tab = this.GetSelectedTab();
                    if (tab != null)
                    {
                        (tab.Content as MainGrid).Draw();
                        tab.Header = collection.ShortPath;
                    }
                }
            }
        }

        private void Command_TreeEdit(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                new EditFieldsWindow(this.currentCollection.Collection).ShowDialog();
                this.GetSelectedGrid()?.Draw();
            }
        }

        private void Command_TreePaste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.collectionToCopy != null)
            {
                if (this.currentClass != null)
                {
                    var collection = this.collectionToCopy.Collection;
                    string command;
                    if (this.cutNode)
                    {
                        command = $"move_node {this.currentClass.Class.Name} {collection.Name}";
                    }
                    else
                    {
                        command = $"copy_node {this.currentClass.Class.Name} {collection.Name} {collection.Name}_copy";
                    }
                    this.collectionToCopy = null;
                    this.ExecuteScriptInternal(new[] { command });
                    this.AddScriptLine(command);
                    this.PopulateTreeView();
                }

                if (this.currentCollection != null)
                {
                    var collection = this.collectionToCopy.Collection;
                    string command;
                    if (this.cutNode)
                    {
                        command = $"move_node {collection.Class.Name} {collection.Name} {this.currentCollection.HeaderName}";
                    }
                    else
                    {
                        command = $"copy_node {collection.Class.Name} {collection.Name} {this.currentCollection.HeaderName} {collection.Name}_copy";
                    }

                    this.ExecuteScriptInternal(new[] { command });
                    this.AddScriptLine(command);
                    this.PopulateTreeView();
                }
            }
        }

        private void CreateClassContextMenu()
        {
            var contextMenu = this.classContextMenu;
            contextMenu.Items.Clear();

            var menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Icon = this.CreateImageSource("Add.png");
            menuItem.Click += (s, e) => this.Command_TreeAdd(null, null);
            menuItem.InputGestureText = "Ctrl+A";
            contextMenu.Items.Add(menuItem);

            if (this.collectionToCopy != null && this.collectionToCopy.Collection.Class == this.currentClass.Class)
            {
                contextMenu.Items.Add(new Separator());

                menuItem = new MenuItem();
                menuItem.Header = $"Paste ({this.collectionToCopy.HeaderName})";
                menuItem.Icon = this.CreateImageSource("Paste.png");
                menuItem.Click += (s, e) => this.Command_TreePaste(null, null);
                menuItem.InputGestureText = "Ctrl+V";
                contextMenu.Items.Add(menuItem);
            }
        }

        private void CreateCollectionContextMenu()
        {
            var contextMenu = this.collectionContextMenu;
            contextMenu.Items.Clear();

            var menuItem = new MenuItem();
            menuItem.Header = "Open in new tab";
            menuItem.Click += (s, e) => this.TreeView_MouseDoubleClick(null, null);
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Icon = this.CreateImageSource("Add.png");
            menuItem.Click += (s, e) => this.Command_TreeAdd(null, null);
            menuItem.InputGestureText = "Ctrl+A";
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Delete";
            menuItem.Icon = this.CreateImageSource("Delete.png");
            menuItem.Click += (s, e) => this.Command_TreeDelete(null, null);
            menuItem.InputGestureText = "Ctrl+D";
            contextMenu.Items.Add(menuItem);

            contextMenu.Items.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = "Copy";
            menuItem.Icon = this.CreateImageSource("Copy.png");
            menuItem.Click += (s, e) => this.Command_TreeCopy(null, null);
            menuItem.InputGestureText = "Ctrl+C";
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Cut";
            menuItem.Icon = this.CreateImageSource("Cut.png");
            menuItem.Click += (s, e) => this.Command_TreeCut(null, null);
            menuItem.InputGestureText = "Ctrl+X";
            contextMenu.Items.Add(menuItem);

            if (this.collectionToCopy != null && this.collectionToCopy.Collection.Class == this.currentCollection.Collection.Class)
            {
                menuItem = new MenuItem();
                menuItem.Header = $"Paste ({this.collectionToCopy.HeaderName})";
                menuItem.Icon = this.CreateImageSource("Paste.png");
                menuItem.Click += (s, e) => this.Command_TreePaste(null, null);
                menuItem.InputGestureText = "Ctrl+V";
                contextMenu.Items.Add(menuItem);
            }

            contextMenu.Items.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = "Edit fields";
            menuItem.Icon = this.CreateImageSource("Properties.png");
            menuItem.Click += (s, e) => this.Command_TreeEdit(null, null);
            menuItem.InputGestureText = "Ctrl+Z";
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Rename";
            menuItem.Icon = this.CreateImageSource("Rename.png");
            menuItem.Click += (s, e) => this.Command_TreeRename(null, null);
            menuItem.InputGestureText = "Ctrl+R";
            contextMenu.Items.Add(menuItem);

            if (this.currentCollection.Collection.Class.Name == "gameplay")
            {
                contextMenu.Items.Add(new Separator());

                menuItem = new MenuItem();
                menuItem.Header = "Change vault";
                menuItem.Icon = this.CreateImageSource("Settings.png");
                menuItem.Click += (s, e) => this.Command_ChangeVault(null, null);
                menuItem.InputGestureText = "Ctrl+W";
                contextMenu.Items.Add(menuItem);
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var source = e.Source;
            if (source is TreeHeader header)
            {
                source = header.Parent;
            }

            if (source is ClassTreeViewItem)
            {
                this.currentClass = (ClassTreeViewItem)source;
                this.currentCollection = null;
                this.CreateClassContextMenu();
            }

            if (source is CollectionTreeViewItem)
            {
                this.currentCollection = (CollectionTreeViewItem)source;
                this.currentClass = null;
                this.CreateCollectionContextMenu();
            }
        }

        private void MenuItem_Raider_Click(object sender, RoutedEventArgs e)
        {
            new RaiderWindow().ShowDialog();
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var stackPanel = button.Parent as StackPanel;
            this.RemoveTab(stackPanel);
        }

        private void TabHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                this.RemoveTab(sender as StackPanel);
            }
        }

        private void RemoveTab(StackPanel stackPanel)
        {
            var header = stackPanel.DataContext as string;
            var toRemove = this.Tabs.Items.Cast<TabItem>().First(x => x.Header as string == header);
            this.Tabs.Items.Remove(toRemove);
        }

        private MainGrid GetSelectedGrid()
        {
            if (this.currentCollection != null)
            {
                return this.Tabs.Items.Cast<TabItem>().Select(x => x.Content as MainGrid)
                     .FirstOrDefault(x => x.Collection == this.currentCollection.Collection);
            }

            return null;
        }

        public MainGrid EditGrid => this.Tabs.SelectedContent as MainGrid;

        private TabItem GetSelectedTab()
        {
            if (this.currentCollection != null)
            {
                return this.Tabs.Items.Cast<TabItem>()
                     .FirstOrDefault(x => (x.Content as MainGrid).Collection == this.currentCollection.Collection);
            }

            return null;
        }
    }
}
