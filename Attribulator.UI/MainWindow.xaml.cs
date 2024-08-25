using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.API.Services;
using Attribulator.CLI;
using Attribulator.CLI.Services;
using Attribulator.ModScript.API;
using Attribulator.UI;
using Attribulator.UI.PropertyGrid;
using Attribulator.UI.Windows;
using FramePFX.Themes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private string gameGlobalFolder;
        private string backupsFolder;

        private IServiceProvider serviceProvider;
        private IModScriptService modScriptService;

        private Database database;
        private DatabaseHelper modScriptDatabase;
        private IEnumerable<LoadedFile> files;

        private List<MenuItem> gameMenuItems;

        private Settings settings;

        public static MainWindow Instance { get; private set; }

        public SearchResult Search { get; private set; }

        private TreeViewItem currentClass = null;
        private TreeViewItem currentCollection = null;
        private TreeViewItem collectionToCopy = null;
        private ContextMenu classContextMenu = null;
        private ContextMenu collectionContextMenu = null;
        private bool cutNode = false;

        private bool folderMode = false;

        public MainWindow()
        {
            InitializeComponent();

            this.settings = new Settings();
            this.Search = new SearchResult();
            this.Search.Settings = this.settings.Root.Search;

            ThemesController.SetTheme(settings.Root.Theme);
        }

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

                this.ScriptEditor.Text = this.settings.Root.Srcipt;

                this.PopulateGameMenuItems();
                this.CreateWelcomeScreen();

                this.CheckSelectedGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var windowSettings = this.settings.Root.Window;
            if (windowSettings == null)
            {
                this.settings.Root.Window = new WindowSettings
                {
                    Top = this.Top,
                    Left = this.Left,
                    Height = this.Height,
                    Width = this.Width,
                    Maximized = false
                };
            }
            else
            {
                this.Top = windowSettings.Top;
                this.Left = windowSettings.Left;
                this.Height = windowSettings.Height;
                this.Width = windowSettings.Width;
                if (windowSettings.Maximized)
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
        }

        private void SetTitle()
        {
            this.Title = $"OGVI v1.3 by ARCHIE";
            var selectedGame = this.settings.Root.SelectedGame;
            if (selectedGame != null)
            {
                this.Title += " | Need for Speed: " + selectedGame.Header;
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

            this.SetTitle();
        }

        private void Command_Open(object sender, ExecutedRoutedEventArgs e)
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

        private void MenuItem_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (this.gameMenuItems.Any(x => x.IsChecked))
            {
                using (var dialog = new Forms.FolderBrowserDialog())
                {
                    var result = dialog.ShowDialog();
                    if (result == Forms.DialogResult.OK)
                    {
                        this.gameFolder = dialog.SelectedPath;
                        this.gameGlobalFolder = dialog.SelectedPath;
                        this.OpenFolder();
                        this.folderMode = true;
                        this.gameExe = null;
                        this.backupsFolder = Path.Combine(this.gameFolder, "AttribulatorBackups");
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
            this.gameFolder = Path.GetDirectoryName(exePath);
            this.gameGlobalFolder = Path.Combine(this.gameFolder, "GLOBAL");
            this.backupsFolder = Path.Combine(this.gameFolder, "GLOBAL", "AttribulatorBackups");
            this.OpenFolder();
            this.folderMode = false;
        }

        private void OpenFolder()
        {
            this.StatusLabel.Content = this.gameFolder;

            try
            {
                var profile = this.GetProfile();
                this.database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
                this.files = profile.LoadFiles(database, this.gameGlobalFolder);
                this.database.CompleteLoad();
                this.modScriptDatabase = new DatabaseHelper(this.database, this.files);

                this.PopulateTreeView();
                if (!this.folderMode)
                {
                    this.settings.Root.SelectedGame.ExePath = this.gameExe;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading game profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Command_Save(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.database != null)
            {
                try
                {
                    // If there are no backups, create one just in case
                    Directory.CreateDirectory(this.backupsFolder);
                    var backups = Directory.GetDirectories(this.backupsFolder);
                    if (backups.Length == 0)
                    {
                        this.MenuItem_CreateBackup_Click(null, null);
                    }

                    this.Backup("SaveBackup");

                    var profile = this.GetProfile();
                    foreach (var file in this.files)
                    {
                        file.Group = "";
                    }

                    profile.SaveFiles(this.database, this.gameGlobalFolder, this.files);
                    this.StatusLabel.Content = "Saved";
                    MainWindow.UnsavedChanges = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error saving file", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.Restore(Path.Combine(this.gameGlobalFolder, "AttribulatorBackups", "SaveBackup"));
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
                foreach (var childCollection in collection.Children.OrderBy(x => x.Name))
                {
                    var childNode = new TreeViewItem
                    {
                        Tag = new CollectionTag(childCollection, node),
                        ContextMenu = this.collectionContextMenu,
                        Header = childCollection.Name
                    };

                    node.Items.Add(childNode);
                    PopulateTreeNode(childCollection, childNode);
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
                var classNode = new TreeViewItem
                {
                    Tag = new ClassTag(cls, this.TreeView),
                    ContextMenu = this.classContextMenu,
                    Header = cls.Name
                };

                var collections = this.database.RowManager.EnumerateCollections(cls.Name).OrderBy(x => x.Name);
                foreach (var collection in collections)
                {
                    var childNode = new TreeViewItem
                    {
                        Tag = new CollectionTag(collection, classNode),
                        ContextMenu = this.collectionContextMenu,
                        Header = collection.Name
                    };

                    classNode.Items.Add(childNode);
                    PopulateTreeNode(collection, childNode);
                }

                this.TreeView.Items.Add(classNode);
            }
        }

        private void SetCurrent(TreeViewItem treeViewItem, bool context)
        {
            if (treeViewItem != null)
            {
                this.currentClass = null;
                this.currentCollection = null;

                if (treeViewItem.Tag is CollectionTag)
                {
                    this.currentCollection = treeViewItem;
                    if (context)
                    {
                        this.CreateCollectionContextMenu();
                    }
                }

                if (treeViewItem.Tag is ClassTag)
                {
                    this.currentClass = treeViewItem;
                    if (context)
                    {
                        this.CreateClassContextMenu();
                    }
                }
            }
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            this.SetCurrent(e.Source as TreeViewItem, false);
        }

        private void OpenNewTab()
        {
            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection();

                if (!this.Tabs.Items.Cast<TabItem>().Any(x => (x.Header as TabHeader).Text == collection.ShortPath))
                {
                    var ti = new TabItem();
                    ti.Header = new TabHeader(ti, collection.ShortPath);
                    ti.Content = new MainGrid(collection);
                    this.Tabs.Items.Add(ti);
                    this.Tabs.SelectedIndex = this.Tabs.Items.Count - 1;
                }
                else
                {
                    for (int i = 0; i < this.Tabs.Items.Count; i++)
                    {
                        var tabItem = this.Tabs.Items[i] as TabItem;
                        if ((tabItem.Header as TabHeader).Text == collection.ShortPath)
                        {
                            this.Tabs.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.settings.Root.OpenCollectionByDoubleClick)
            {
                this.OpenNewTab();
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
            this.CreateWelcomeScreen();
            this.CheckSelectedGame();
        }

        private void CloseGame()
        {
            this.gameExe = null;
            this.gameFolder = null;
            this.gameGlobalFolder = null;
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

        private void Command_RunGame(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.folderMode)
            {
                MessageBox.Show("No game exe selected", "Unable to run the game", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (!string.IsNullOrEmpty(this.gameExe))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(this.gameExe) { WorkingDirectory = this.gameFolder });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Failed to run the game", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

            var windowSettings = this.settings.Root.Window;
            windowSettings.Maximized = this.WindowState == WindowState.Maximized;
            if (this.WindowState == WindowState.Normal)
            {
                windowSettings.Top = this.Top;
                windowSettings.Left = this.Left;
                windowSettings.Height = this.Height;
                windowSettings.Width = this.Width;
            }

            this.settings.Save();
        }

        private void Command_Reload(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.folderMode)
            {
                this.CloseGame();
                this.OpenFolder();
            }
            else
            {
                var gameExe = this.gameExe;
                this.CloseGame();
                this.Open(gameExe);
            }
            this.StatusLabel.Content = "Reloaded";
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
                var targetPath = Path.Combine(this.backupsFolder, folderName);
                Directory.CreateDirectory(targetPath);

                foreach (var file in this.files)
                {
                    var extensions = new[] { ".bin", ".lzc" };
                    foreach (var extension in extensions)
                    {
                        var original = Path.Combine(this.gameGlobalFolder, file.Name + extension);
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

        private void MenuItem_RestoreBackupSpecific_Click(object sender, RoutedEventArgs e)
        {
            var restoreWnd = new RestoreBackupWindow(this.backupsFolder);
            if (restoreWnd.ShowDialog().Value)
            {
                var backupName = restoreWnd.ResultName;
                this.Restore(Path.Combine(this.backupsFolder, backupName));
            }
        }

        private void MenuItem_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.gameFolder))
            {
                if (Directory.Exists(this.backupsFolder))
                {
                    var dirs = Directory.GetDirectories(this.backupsFolder);
                    if (dirs.Length > 0)
                    {
                        string backup = dirs.Where(x => !x.Contains("SaveBackup")).OrderByDescending(x => Path.GetFileName(x)).FirstOrDefault();
                        if (!string.IsNullOrEmpty(backup))
                        {
                            this.Restore(backup);
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
                    File.Copy(file, Path.Combine(this.gameGlobalFolder, fileName), true);
                }

                this.StatusLabel.Content = $"Restored backup: {folderName}";
                var result = MessageBox.Show("Backup successfully restored, do you want to reload database?", "Backup restored", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    this.Command_Reload(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to restore backup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Command_ExecuteAll(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.database != null && this.ScriptEditor.Text.Length > 0)
            {
                var lines = this.ScriptEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                List<CommandModel> commands = new List<CommandModel>();
                for (int i = 0; i < lines.Length; i++)
                {
                    commands.Add(new CommandModel { Line = lines[i], LineNumber = i + 1 });
                }

                this.ExecuteScript(commands);
                this.StatusLabel.Content = "Executed script";
            }
        }

        private void ExecuteScript(IEnumerable<CommandModel> commandModels)
        {
            var errors = new List<ScriptErrorItem>();

            foreach (var commandModel in commandModels)
            {
                try
                {
                    var command = this.modScriptService.ParseCommand(commandModel.Line, commandModel.LineNumber);
                    command.Execute(this.modScriptDatabase);
                }
                catch (Exception e)
                {
                    errors.Add(new ScriptErrorItem(e.Message, commandModel.File, commandModel.LineNumber, commandModel.Line));
                }
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

        private void Command_Import(object sender, ExecutedRoutedEventArgs e)
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
                        if (importWindow.ShowDialog().Value)
                        {
                            var resultScript = importWindow.ResultScript;
                            this.ExecuteScript(resultScript);
                            this.StatusLabel.Content = $"Imported script: {dialog.FileName}";
                        }
                    }
                }
            }
        }

        private void Command_Export(object sender, ExecutedRoutedEventArgs e)
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

        private ImageSource CreateImageSource(string name)
        {
            return new BitmapImage(new Uri($"pack://application:,,,/Resources/{name}"));
        }

        private Image CreateImage(string name)
        {
            var img = new Image();
            img.Source = this.CreateImageSource(name);
            return img;
        }

        private VltCollection HandleAddCommand(TreeViewItem parent, string nodeName)
        {
            string className;
            if (parent.Class() != null)
            {
                className = parent.Class().Name;
            }
            else
            {
                className = parent.Collection().Class.Name;
            }

            var newCollection = this.database.RowManager.FindCollectionByName(className, nodeName);
            var newNode = new TreeViewItem
            {
                Tag = new CollectionTag(newCollection, parent),
                Header = nodeName,
                ContextMenu = this.collectionContextMenu
            };
            parent.Items.Add(newNode);
            newNode.Select();

            return newCollection;
        }

        private void Command_TreeAdd(object sender, ExecutedRoutedEventArgs e)
        {
            string parent = null;
            string title = null;

            if (this.currentClass != null)
            {
                var className = this.currentClass.Class().Name;
                parent = className;
                title = className;
            }

            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection();
                var className = collection.Class.Name;
                parent = $"{className} {collection.Name}";
                title = collection.Name;
            }

            if (parent != null)
            {
                var newNodeWindow = new NewNodeNameWindow(title, parent, this.CreateImageSource("Add.png"));
                if (newNodeWindow.ShowDialog().Value)
                {
                    var newCollection = this.HandleAddCommand(this.currentCollection, newNodeWindow.ResultName);
                    this.StatusLabel.Content = $"Added node: {newCollection.ShortPath}";
                }
            }
        }

		private SolidColorBrush _searchHighlight;
		private SolidColorBrush SearchHighlight
		{
			get
			{
				if (this._searchHighlight == null)
				{
					this._searchHighlight = (SolidColorBrush)FindResource("ABrush.Search.Highlight");
				}

				return this._searchHighlight;
			}
		}

		private SolidColorBrush _searchParentHighlight;
		private SolidColorBrush SearchParentHighlight
		{
			get
			{
				if (this._searchParentHighlight == null)
				{
					this._searchParentHighlight = (SolidColorBrush)FindResource("ABrush.Search.Parent.Highlight");
				}

				return this._searchParentHighlight;
			}
		}

        private void HighlightParent(TreeViewItem item)
        {
            if (item != null)
            {
                this.HighlightParent(item.GetParent() as TreeViewItem);

                var solidBrush = item.Background as SolidColorBrush;
                if (solidBrush.Color.A == 0)
                {
                    item.Background = SearchParentHighlight;
                }
            }
        }

        private void Find(TreeViewItem item)
        {
            item.Background = Brushes.Transparent;

            foreach (TreeViewItem subItem in item.Items)
            {
                this.Find(subItem);
            }

            var search = this.settings.Root.Search;
            if (search.NodeEnabled)
            {
                if (!item.Header().Contains(search.NodeText, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }
            }

            var properties = item.Collection().GetData();
            if (search.FieldEnabled)
            {
                bool fieldFound = false;
                foreach (var property in properties)
                {
                    if (property.Key.Contains(search.FieldText, StringComparison.InvariantCultureIgnoreCase))
                    {
                        fieldFound = true;
                        break;
                    }
                }

                if (!fieldFound)
                {
                    return;
                }
            }

            if (search.ValueEnabled)
            {
                bool valueFound = false;
                foreach (var property in properties)
                {
                    if (property.Value.ToString().Contains(search.ValueText, StringComparison.InvariantCultureIgnoreCase))
                    {
                        valueFound = true;
                        break;
                    }
                }

                if (!valueFound)
                {
                    return;
                }
            }

            item.Background = this.SearchHighlight;
            this.HighlightParent(item);
			this.Search.Found++;
        }

        private void RedrawTabs()
        {
            foreach (TabItem item in this.Tabs.Items)
            {
                (item.Content as MainGrid).Draw();
            }
        }

        public void Find()
        {
            if (this.TreeView.Items.Count != 0)
            {
                this.ClearSearch();

                var search = this.settings.Root.Search;
                if (search.NodeEnabled || search.FieldEnabled || search.ValueEnabled)
                {
                    foreach (TreeViewItem item in this.TreeView.Items)
                    {
                        foreach (TreeViewItem subItem in item.Items)
                        {
                            this.Find(subItem);
                        }
                    }
                }

                this.Search.Executed = true;
                this.RedrawTabs();
            }
        }

        public void FindNext()
        {
			if (this.Search.Executed && this.Search.Found > 0)
            {
                var selectedItem = this.TreeView.SelectedItem as TreeViewItem;
                if (selectedItem == null)
                {
                    selectedItem = this.TreeView.Items[0] as TreeViewItem;
                }

                while (true)
                {
                    selectedItem = selectedItem.GetNextItem();
                    if (selectedItem == null)
                    {
                        selectedItem = this.TreeView.Items[0] as TreeViewItem;
                    }

                    var solidBrush = selectedItem.Background as SolidColorBrush;
                    if (solidBrush.Color == this.SearchHighlight.Color)
                    {
                        selectedItem.IsSelected = true;
                        selectedItem.BringIntoView();
                        break;
                    }
                }
            }
        }

        private void Command_Find(object sender, ExecutedRoutedEventArgs e)
        {
            var searchWindow = new SearchWindow(this.settings);
            searchWindow.Owner = this;
            searchWindow.Show();
        }

        private void Command_FindNext(object sender, ExecutedRoutedEventArgs e)
        {
            this.FindNext();
        }

        private void ClearSearch(TreeViewItem item)
        {
            foreach (TreeViewItem subItem in item.Items)
            {
                this.ClearSearch(subItem);
            }

            item.Background = Brushes.Transparent;
        }

        public void ClearSearch()
        {
            if (this.Search.Executed)
            {
                foreach (TreeViewItem item in this.TreeView.Items)
                {
                    this.ClearSearch(item);
                }

                this.Search.Executed = false;
				this.Search.Found = 0;
                this.RedrawTabs();
            }
        }

        private void MenuItem_SearchClear_Click(object sender, RoutedEventArgs e)
        {
            this.ClearSearch();
        }

        private void Command_ChangeVault(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null && this.currentCollection.Collection().Class.Name == "gameplay")
            {
                if (new ChangeVaultWindow(this.currentCollection.Collection(), this.CreateImageSource("Settings.png")).ShowDialog().Value)
                {
                    this.GetSelectedGrid()?.Draw();
                    this.StatusLabel.Content = "Changed vault";
                }
            }
        }

        private void Command_TreeDelete(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection();
                string command = $"delete_node {collection.Class.Name} {collection.Name}";
                if (this.ExecuteScriptInternal(command))
                {
                    this.AddScriptLine(command);
                    this.StatusLabel.Content = $"Deleted node: {collection.Name}";
                    var parentNode = this.currentCollection.GetParent();
                    parentNode.Items.Remove(this.currentCollection);
                }
            }
        }

        private void Command_TreeCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                this.collectionToCopy = this.currentCollection;
                this.cutNode = false;
            }
        }

        private void Command_TreeCut(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                this.collectionToCopy = this.currentCollection;
                this.cutNode = true;
            }
        }

        private void Command_TreeRename(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                var collection = this.currentCollection.Collection();
                if (new CollectionRenameWindow(collection, this.CreateImageSource("Rename.png")).ShowDialog().Value)
                {
                    this.currentCollection.Header = collection.Name;
                    this.StatusLabel.Content = "Renamed node";
                    var tab = this.GetTab(collection);
                    if (tab != null)
                    {
                        (tab.Content as MainGrid).Draw();
                        (tab.Header as TabHeader).SetText(collection.ShortPath);
                    }
                }
            }
        }

        private void Command_TreeEdit(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.currentCollection != null)
            {
                if (new EditFieldsWindow(this.currentCollection.Collection()).ShowDialog().Value)
                {
                    this.RedrawTabs();
                }
            }
        }

        private void Command_TreePaste(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.collectionToCopy != null)
            {
                string command = null;
                TreeViewItem newParentItem = null;
                if (this.currentClass != null)
                {
                    command = $"{this.currentClass.Class().Name} {this.collectionToCopy.Collection().Name}";
                    newParentItem = this.currentClass;
                }

                if (this.currentCollection != null)
                {
                    var collection = this.collectionToCopy.Collection();
                    command = $"{collection.Class.Name} {this.collectionToCopy.Collection().Name} {this.currentCollection.Collection().Name}";
                    newParentItem = this.currentCollection;
                }

                if (command != null)
                {
                    if (this.cutNode)
                    {
                        command = $"move_node {command}";
                        if (this.ExecuteScriptInternal(new[] { command }))
                        {
                            var oldParentItem = this.collectionToCopy.GetParent();
                            oldParentItem.Items.Remove(this.collectionToCopy);
                            newParentItem.Items.Add(this.collectionToCopy);
                            this.collectionToCopy.SetParent(newParentItem);
                            this.collectionToCopy.Select();

                            this.AddScriptLine(command);
                            this.StatusLabel.Content = $"Moved node: {this.collectionToCopy.Collection().ShortPath}";
                            this.collectionToCopy = null;
                        }
                    }
                    else
                    {
                        var copyWindow = new CopyNodeWindow(this.CreateImageSource("Paste.png"), this.collectionToCopy.Collection().Name, command);
                        if (copyWindow.ShowDialog().Value)
                        {
                            var newCollection = this.HandleAddCommand(newParentItem, copyWindow.Result);
                            this.StatusLabel.Content = $"Copied node: {newCollection.ShortPath}";
                        }
                    }
                }
            }
        }

        private void CreateClassContextMenu()
        {
            var contextMenu = this.classContextMenu;
            contextMenu.Items.Clear();

            var menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Icon = this.CreateImage("Add.png");
            menuItem.Click += (s, e) => this.Command_TreeAdd(null, null);
            menuItem.InputGestureText = "Ctrl+A";
            contextMenu.Items.Add(menuItem);

            if (this.collectionToCopy != null && this.collectionToCopy.Collection().Class == this.currentClass.Class())
            {
                contextMenu.Items.Add(new Separator());

                menuItem = new MenuItem();
                menuItem.Header = $"Paste ({this.collectionToCopy.Header})";
                menuItem.Icon = this.CreateImage("Paste.png");
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
            menuItem.Click += (s, e) => this.OpenNewTab();
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Add";
            menuItem.Icon = this.CreateImage("Add.png");
            menuItem.Click += (s, e) => this.Command_TreeAdd(null, null);
            menuItem.InputGestureText = "Ctrl+A";
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Delete";
            menuItem.Icon = this.CreateImage("Delete.png");
            menuItem.Click += (s, e) => this.Command_TreeDelete(null, null);
            menuItem.InputGestureText = "Ctrl+D";
            contextMenu.Items.Add(menuItem);

            contextMenu.Items.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = "Copy";
            menuItem.Icon = this.CreateImage("Copy.png");
            menuItem.Click += (s, e) => this.Command_TreeCopy(null, null);
            menuItem.InputGestureText = "Ctrl+C";
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Cut";
            menuItem.Icon = this.CreateImage("Cut.png");
            menuItem.Click += (s, e) => this.Command_TreeCut(null, null);
            menuItem.InputGestureText = "Ctrl+X";
            contextMenu.Items.Add(menuItem);

            if (this.collectionToCopy != null && this.collectionToCopy.Collection().Class == this.currentCollection.Collection().Class)
            {
                menuItem = new MenuItem();
                menuItem.Header = $"Paste ({this.collectionToCopy.Header})";
                menuItem.Icon = this.CreateImage("Paste.png");
                menuItem.Click += (s, e) => this.Command_TreePaste(null, null);
                menuItem.InputGestureText = "Ctrl+V";
                contextMenu.Items.Add(menuItem);
            }

            contextMenu.Items.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = "Edit fields";
            menuItem.Icon = this.CreateImage("Properties.png");
            menuItem.Click += (s, e) => this.Command_TreeEdit(null, null);
            menuItem.InputGestureText = "Ctrl+Z";
            contextMenu.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Rename";
            menuItem.Icon = this.CreateImage("Rename.png");
            menuItem.Click += (s, e) => this.Command_TreeRename(null, null);
            menuItem.InputGestureText = "Ctrl+R";
            contextMenu.Items.Add(menuItem);

            if (this.currentCollection.Collection().Class.Name == "gameplay")
            {
                contextMenu.Items.Add(new Separator());

                menuItem = new MenuItem();
                menuItem.Header = "Change vault";
                menuItem.Icon = this.CreateImage("Settings.png");
                menuItem.Click += (s, e) => this.Command_ChangeVault(null, null);
                menuItem.InputGestureText = "Ctrl+W";
                contextMenu.Items.Add(menuItem);
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.SetCurrent(e.Source as TreeViewItem, true);
        }

        private void MenuItem_Hasher_Click(object sender, RoutedEventArgs e)
        {
            var raider = new HasherWindow();
            raider.Owner = this;
            raider.Show();
        }

        private void MenuItem_Raider_Click(object sender, RoutedEventArgs e)
        {
            var raider = new RaiderWindow();
            raider.Owner = this;
            raider.Show();
        }

        public void RemoveTab(TabItem tabItem)
        {
            this.Tabs.Items.Remove(tabItem);
        }

        private MainGrid GetSelectedGrid()
        {
            if (this.currentCollection != null)
            {
                return this.Tabs.Items.Cast<TabItem>().Select(x => x.Content as MainGrid)
                     .FirstOrDefault(x => x.Collection == this.currentCollection.Collection());
            }

            return null;
        }

        public MainGrid EditGrid => this.Tabs.SelectedContent as MainGrid;

        private TabItem GetTab(VltCollection collection)
        {
            return this.Tabs.Items.Cast<TabItem>()
                 .FirstOrDefault(x => (x.Content as MainGrid).Collection == collection);
        }

        private void MenuItem_Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindows(this.settings).ShowDialog();
        }

        private void CreateWelcomeScreen()
        {
            if (this.settings.Root.ShowWelcomeTab)
            {
                this.Tabs.Items.Add(new TabItem
                {
                    Content = new WelcomeScreen(),
                    Header = new TabHeader(null, "Welcome")
                });

                this.Tabs.SelectedIndex = 0;
            }
        }

        private void Collapse(TreeViewItem item)
        {
            foreach (TreeViewItem subItem in item.Items)
            {
                this.Collapse(subItem);
            }

            item.IsExpanded = false;
        }

        private void MenuItem_Collapse_Click(object sender, RoutedEventArgs e)
        {
            foreach (TreeViewItem item in this.TreeView.Items)
            {
                this.Collapse(item);
            }
        }
    }
}
