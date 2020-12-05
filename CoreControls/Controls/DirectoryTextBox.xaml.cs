using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CoreVirtualDrive;

namespace CoreControls.Controls
{
    public partial class DirectoryTextBox : UserControl
    {
        public DirectoryTextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DirectoryProperty = DependencyProperty.Register(
            "Directory",
            typeof(string),
            typeof(DirectoryTextBox),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDirectoryChanged));
        public string Directory
        {
            get
            {
                return (string)GetValue(DirectoryProperty);
            }
            set
            {
                SetValue(DirectoryProperty, value);
            }
        }
        private static void OnDirectoryChanged(
            DependencyObject source,
            DependencyPropertyChangedEventArgs e)
        {
            (source as DirectoryTextBox).Text = (string)e.NewValue;
        }

        public static readonly RoutedEvent DirectoryChangedEvent = EventManager.RegisterRoutedEvent(
            "DirectoryChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(DirectoryTextBox));
        public event RoutedEventHandler DirectoryChanged
        {
            add { AddHandler(DirectoryChangedEvent, value); }
            remove { RemoveHandler(DirectoryChangedEvent, value); }
        }

        public static readonly DependencyProperty ShowHiddenDirectoriesProperty = DependencyProperty.Register(
            "ShowHiddenDirectories",
            typeof(bool),
            typeof(DirectoryTextBox),
            new FrameworkPropertyMetadata(false, OnShowHiddenDirectoriesPropertyChanged));
        public bool ShowHiddenDirectories
        {
            get
            {
                return (bool)GetValue(ShowHiddenDirectoriesProperty);
            }
            set
            {
                SetValue(ShowHiddenDirectoriesProperty, value);
            }
        }
        private static void OnShowHiddenDirectoriesPropertyChanged(
            DependencyObject source,
            DependencyPropertyChangedEventArgs e)
        {
            (source as DirectoryTextBox).UpdateItems();
        }

        private Button buttonBrowse;
        private TextBox textBox;
        private Popup popup;
        private ListView listView;
        private Button ButtonBrowse
        {
            get
            {
                return buttonBrowse;
            }
        }
        private TextBox TextBoxView
        {
            get
            {
                return textBox;
            }
        }
        private Popup Popup
        {
            get
            {
                return popup;
            }
        }
        private ListView ItemList
        {
            get
            {
                return listView;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (!Object.ReferenceEquals(ButtonBrowse, null))
            {
                ButtonBrowse.Click -= new RoutedEventHandler(OnButtonBrowseClick);
            }
            if (!Object.ReferenceEquals(TextBoxView, null))
            {
                TextBoxView.PreviewKeyDown -= new KeyEventHandler(OnTextBoxViewPreviewKeyDown);
                TextBoxView.TextChanged -= new TextChangedEventHandler(OnTextBoxViewTextChanged);
                TextBoxView.LostFocus -= new RoutedEventHandler(OnTextBoxViewLostFocus);
            }
            if (!Object.ReferenceEquals(ItemList, null))
            {
                ItemList.PreviewMouseDown -= new MouseButtonEventHandler(OnItemListPreviewMouseDown);
                ItemList.PreviewMouseUp -= new MouseButtonEventHandler(OnItemListPreviewMouseUp);
                ItemList.SelectionChanged -= new SelectionChangedEventHandler(OnItemListSelectionChanged);
            }

            buttonBrowse = Template.FindName("PART_Button", this) as Button;
            textBox = Template.FindName("PART_TextBox", this) as TextBox;
            popup = Template.FindName("PART_Popup", this) as Popup;
            listView = Template.FindName("PART_ItemList", this) as ListView;

            if (!Object.ReferenceEquals(ButtonBrowse, null))
            {
                ButtonBrowse.Click += new RoutedEventHandler(OnButtonBrowseClick);
            }
            if (!Object.ReferenceEquals(TextBoxView, null))
            {
                TextBoxView.PreviewKeyDown += new KeyEventHandler(OnTextBoxViewPreviewKeyDown);
                TextBoxView.TextChanged += new TextChangedEventHandler(OnTextBoxViewTextChanged);
                TextBoxView.LostFocus += new RoutedEventHandler(OnTextBoxViewLostFocus);
            }
            if (!Object.ReferenceEquals(ItemList, null))
            {
                ItemList.PreviewMouseDown += new MouseButtonEventHandler(OnItemListPreviewMouseDown);
                ItemList.PreviewMouseUp += new MouseButtonEventHandler(OnItemListPreviewMouseUp);
                ItemList.SelectionChanged += new SelectionChangedEventHandler(OnItemListSelectionChanged);
            }

            if (!Object.ReferenceEquals(TextBoxView, null))
            {
                TextBoxView.Text = Directory;
            }

            ItemList.Focusable = false;
            Popup.Focusable = false;
        }

        private void OnButtonBrowseClick(object sender, RoutedEventArgs e)
        {
            string path = BrowseForDirectory();

            if (VirtualDrive.ExistsDirectory(path))
            {
                Text = path;
            }
        }
        private void OnTextBoxViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!Popup.IsOpen)
            {
                UpdateItems();
                ItemList.SelectedIndex = -1;
                OpenPopup();
            }
            else if (ItemList.Items.Count > 0)
            {
                if (e.Key == Key.Down)
                {
                    SelectNext();
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    SelectPrevious();
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab)
                {
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        SelectPreviousRoundRobin();
                    }
                    else
                    {
                        SelectNextRoundRobin();
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape || e.Key == Key.Return || e.Key == Key.Enter)
                {
                    if (Popup.IsOpen)
                    {
                        Popup.IsOpen = false;
                        e.Handled = true;
                    }
                }
            }
        }
        private void OnTextBoxViewTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!blockUpdateItems)
            {
                UpdateItems();
            }

            Directory = Text;

            if (VirtualDrive.ExistsDirectory(Directory))
            {
                RaiseEvent(new RoutedEventArgs(DirectoryChangedEvent, this));
            }
        }
        private void OnTextBoxViewLostFocus(object sender, RoutedEventArgs e)
        {
            if (Keyboard.FocusedElement == null
                || !IsVisualTreeAncestor(Keyboard.FocusedElement as DependencyObject, ItemList))
            {
                Popup.IsOpen = false;
            }
        }
        private void OnItemListPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                TextBlock tb = e.OriginalSource as TextBlock;
                if (tb != null)
                {
                    blockUpdateItems = true;
                    Text = tb.Text;
                    blockUpdateItems = false;
                }
            }
        }
        private void OnItemListPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                TextBoxView.Focus();
                UpdateItems();

                e.Handled = true;
            }
        }
        private void OnItemListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Object.ReferenceEquals(ItemList.SelectedItem, null))
            {
                blockUpdateItems = true;
                Text = ItemList.SelectedItem.ToString();
                blockUpdateItems = false;
            }
        }
        bool blockUpdateItems = false;

        private void SelectNext()
        {
            ItemList.SelectedIndex = Math.Min(ItemList.SelectedIndex + 1, ItemList.Items.Count - 1);
        }
        private void SelectPrevious()
        {
            ItemList.SelectedIndex = Math.Max(ItemList.SelectedIndex - 1, 0);
        }
        private void SelectNextRoundRobin()
        {
            if (ItemList.SelectedIndex + 1 == ItemList.Items.Count)
            {
                ItemList.SelectedIndex = 0;
            }
            else
            {
                SelectNext();
            }
        }
        private void SelectPreviousRoundRobin()
        {
            if (ItemList.SelectedIndex == 0)
            {
                ItemList.SelectedIndex = ItemList.Items.Count - 1;
            }
            else
            {
                SelectPrevious();
            }
        }

        private void OpenPopup()
        {
            Popup.IsOpen = ItemList.Items.Count > 0;
        }

        private string Text
        {
            get
            {
                return TextBoxView.Text;
            }
            set
            {
                if (!Object.ReferenceEquals(TextBoxView, null) && value != TextBoxView.Text)
                {
                    TextBoxView.Text = value;
                    TextBoxView.SelectionStart = Text.Length;
                    TextBoxView.SelectionLength = 0;
                }
            }
        }

        private static string BrowseForDirectory()
        {
            System.Windows.Forms.FolderBrowserDialog dialog =
                new System.Windows.Forms.FolderBrowserDialog();

            dialog.Description = "Select Directory...";

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            else
            {
                return null;
            }
        }

        private void UpdateItems()
        {
            IEnumerable<string> paths = DirectoryHelper.SuggestPaths(Text, ShowHiddenDirectories);

            ItemList.SelectedItem = null;

            ItemList.Items.Clear();
            foreach (var dir in paths)
            {
                ItemList.Items.Add(dir);
            }
        }

        private static bool IsVisualTreeAncestor(DependencyObject obj, DependencyObject maybeAncestor)
        {
            if (obj == null)
            {
                return false;
            }
            else if (obj == maybeAncestor)
            {
                return true;
            }
            else
            {
                return IsVisualTreeAncestor(VisualTreeHelper.GetParent(obj), maybeAncestor);
            }
        }
    }

    class DirectoryHelper
    {
        public static IEnumerable<string> SuggestPaths(string fullPath, bool showHiddenDirectories)
        {
            string basePath = BasePath(fullPath);

            List<string> result = new List<string>();

            if (VirtualDrive.ExistsDirectory(basePath))
            {
                try
                {
                    foreach (var dirName in VirtualDrive.GetDirectories(basePath))
                    {
                        bool isHidden = new DirectoryInfo(dirName).Attributes.HasFlag(FileAttributes.Hidden);
                        bool show = isHidden && showHiddenDirectories || !isHidden;

                        bool matches = Path.Combine(basePath, dirName).StartsWith(
                            fullPath.ToLower(), true, CultureInfo.CurrentUICulture);

                        if (show && matches)
                        {
                            result.Add(dirName);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
            else if (String.IsNullOrEmpty(basePath)
                || !basePath.Contains(new String(new char[] { Path.VolumeSeparatorChar })))
            {
                foreach (var dir in DriveInfo.GetDrives())
                {
                    result.Add(dir.Name);
                }
            }

            return result;
        }
        private static string BasePath(string fullPath)
        {
            string[] parts = fullPath.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                sb.Append(parts[i]);
                sb.Append(Path.DirectorySeparatorChar);

                if (i < parts.Length - 1
                    && !VirtualDrive.ExistsDirectory(sb.ToString() + Path.DirectorySeparatorChar + parts[i + 1]))
                {
                    break;
                }
            }

            return sb.ToString();
        }

        private static readonly char[] separators = new char[]
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };
        private static readonly string directorySeparatorChar =
            new String(new char[] { Path.DirectorySeparatorChar });
    }
}
