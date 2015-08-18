using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using Awesomium.Core;
using STM_View.Properties;
using Application = System.Windows.Application;

namespace STM_View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static bool Stop { get; set; }

        public MainWindow()
        {
            var session = WebCore.CreateWebSession(@"SessionDataPath", WebPreferences.Default);
            
            InitializeComponent();
            webControl.WebSession = session;

            GetTreeview();
        }

        private void GetTreeview()
        {
            if (string.IsNullOrEmpty(Settings.Default.Path)) return;

            var di = new DirectoryInfo(Settings.Default.Path);

            foreach (
                var item in
                    di.GetDirectories()
                        .Select(directoryInfo => new TreeViewItem {Tag = directoryInfo, Header = directoryInfo.Name})
                )
            {
                item.Items.Add("*");
                Trw_Products.Items.Add(item);
            }
        }

        private void LaunchOnGitHub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/mazanuj/");
        }

        private void Trw_Products_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem) e.OriginalSource;
            item.Items.Clear();
            DirectoryInfo dir;

            try
            {
                var tag = item.Tag as DriveInfo;
                if (tag != null)
                {
                    var drive = tag;
                    dir = drive.RootDirectory;
                }
                else dir = (DirectoryInfo) item.Tag;
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                foreach (
                    var newItem in
                        dir.GetDirectories().Select(subDir => new TreeViewItem {Tag = subDir, Header = subDir.Name}))
                {
                    newItem.Items.Add("*");
                    item.Items.Add(newItem);
                }

                foreach (
                    var newItem  in
                        dir.GetFiles().Select(subFile => new TreeViewItem {Tag = subFile, Header = subFile.Name}))
                    item.Items.Add(newItem);
            }
            catch
            {
            }
        }

        private void EventSetter_OnHandler(object sender, RoutedEventArgs e)
        {
            var path = Settings.Default.Path;
            var item = e.OriginalSource as TreeViewItem;

            if (item == null || (!(item.Header as string)?.EndsWith(".html") ?? true))
                return;

            string lastPath = $@"\{item.Header}";
            while (true)
            {
                dynamic parent = ItemsControl.ItemsControlFromItemContainer(item);
                if (parent != null)
                {
                    if (!string.IsNullOrEmpty(parent.Name))
                        break;

                    var header = parent.Header;
                    lastPath = $@"\{header}{lastPath}";

                    item = parent as TreeViewItem;
                }
                else return;
            }
            path += lastPath;

            webControl.Source = new Uri(path);
        }

        private async void ButtonSearch_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Regex.IsMatch(SearchBox.Text, @"\w"))
                return;

            SearchButton.IsEnabled = false;
            Stop = false;

            var text = SearchBox.Text;

            var count = 0;
            await
                new DirectoryInfo(Settings.Default.Path).GetFiles("*", SearchOption.AllDirectories)
                    .ForEachAsync(Settings.Default.ThreadNums, async file =>
                    {
                        if (Stop)
                            return;

                        var tr = File.ReadAllText(file.FullName);
                        if (tr.Contains(text.ToLower()))
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                SearchCount.Content = (++count).ToString();
                                OpenItems(file.FullName);
                            }));
                    });
            SearchButton.IsEnabled = true;
        }

        private void OpenItems(string path)
        {
            var parents = Regex.Match(path, @"(?<=Forum\\).+").Value.Split('\\').ToList();

            var items = Trw_Products.Items;

            for (var i = 0; i < parents.Count; i++)
            {
                var item = items.Cast<TreeViewItem>().First(x => x.Header.ToString() == parents[i]);
                item.IsExpanded = true;

                if (i + 1 == parents.Count)
                {
                    item.Background = Brushes.Red;
                    return;
                }

                items = item.Items;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            SearchCount.Content = string.Empty;
            ExpandSubContainers(Trw_Products);
            SearchBox.Text = string.Empty;
        }

        private static void ExpandSubContainers(ItemsControl parentContainer)
        {
            foreach (
                var currentContainer in
                    parentContainer.Items.Cast<object>()
                        .Select(item => parentContainer.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem)
                        .Where(currentContainer => currentContainer != null && currentContainer.Items.Count > 0))
            {
                // Expand the current item. 
                currentContainer.IsExpanded = false;
                currentContainer.Background = Brushes.White;
                if (currentContainer.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    // If the sub containers of current item is not ready, we need to wait until 
                    // they are generated. 
                    currentContainer.ItemContainerGenerator.StatusChanged += delegate
                    {
                        ExpandSubContainers(currentContainer);
                    };
                }
                else
                {
                    // If the sub containers of current item is ready, we can directly go to the next 
                    // iteration to expand them. 
                    ExpandSubContainers(currentContainer);
                }
            }
        }

        private void ButtonStop_OnClick(object sender, RoutedEventArgs e)
        {
            Stop = true;
        }

        private void ButtonGetDir_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                Settings.Default.Path = dialog.SelectedPath;
        }

        private void ButtonRenew_OnClick(object sender, RoutedEventArgs e)
        {
            GetTreeview();
        }
    }
}