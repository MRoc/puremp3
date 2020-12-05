using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CoreControls;
using CoreControls.Threading;
using CoreDocument;
using ID3TagModel;
using CoreThreading;
using CoreControls.Commands;
using CoreControls.Preferences;
using System.Windows.Media.Animation;
using CoreControls.DragAndDrop;
using System.Text;
using CoreDocument.Text;
using System.Reflection;
using System.Windows.Controls.Primitives;
using PureMp3.Model;
using CoreVirtualDrive;
using PureMp3.Model.Batch;

namespace PureMp3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AllowDrop = true;
            DDManager = new DragAndDropManager(this);

            DataContextChanged += OnDataContextChanged;

            AddHandler(LoadedEvent, new RoutedEventHandler(OnLoaded));
            AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMove));

            CommandManager.AddPreviewCanExecuteHandler(this, new CanExecuteRoutedEventHandler(OnPreviewCanExecuteHandler));
            CommandManager.AddPreviewExecutedHandler(this, new ExecutedRoutedEventHandler(OnPreviewExecutedEvent));

            HelpManager.Instance.HelpRequested += OnInfoText;

            listviewFiles.ItemDoubleClicked += OnFileListItemDoubleClicked;
            libraryView.ItemDoubleClicked += OnLibraryViewDoubleClicked;

            DataContext = (App.Current as App).Doc;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                Document doc = e.OldValue as Document;
                doc.IsBatchActive.PropertyChanged -= OnIsBatchActiveChanged;
                doc.FileTreeModel.PropertyChanged -= OnFileTreeModelChanged;
                doc.Preferences.PrefsCommon.ShowHelpView.ItemT<DocObj<bool>>().PropertyChanged -= OnShowHelpViewChanged;
            }

            if (e.NewValue != null)
            {
                Document doc = e.NewValue as Document;
                doc.IsBatchActive.PropertyChanged += OnIsBatchActiveChanged;
                doc.FileTreeModel.PropertyChanged += OnFileTreeModelChanged;
                doc.Preferences.PrefsCommon.ShowHelpView.ItemT<DocObj<bool>>().PropertyChanged += OnShowHelpViewChanged;
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            HelpManager.Instance.MouseMove(this, e);

            HitTestResult result = VisualTreeHelper.HitTest(this, e.GetPosition(this));
            if (Object.ReferenceEquals(result, null))
            {
                return;
            }

            DependencyObject child = result.VisualHit;
            if (Object.ReferenceEquals(child, null))
            {
                return;
            }

            {
                bool foundContextMenu = WpfUtils.FindVisualParents<FrameworkElement>(child).Where(
                    n => n.ContextMenu != null).Count() > 0;

                labelHasContextMenu.Visibility = foundContextMenu
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            {
                DropTypes[] dropTypes = DDManager.DropTypeByPosition(e.GetPosition(this));
                bool foundDropTarget = dropTypes.Length > 0;

                StringBuilder sb = new StringBuilder();
                sb.Append("Drop ");

                foreach (var dropType in dropTypes)
                {
                    sb.Append(dropType.ToString());
                    sb.Append(" ");
                }

                labelHasDropTarget.Content = sb.ToString();
                labelHasDropTarget.Visibility = foundDropTarget
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void OnFileListItemDoubleClicked(object sender, EventArgs e)
        {
            Document.RequestPlaying(sender as TagModel);
        }
        private void OnLibraryViewDoubleClicked(object sender, EventArgs e)
        {
            Document.RequestPlaying(sender as string);
        }

        private void OnPreviewCanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Undo)
            {
                e.CanExecute = History.Instance.HasUndo;
                e.Handled = true;
            }
            else if (e.Command == ApplicationCommands.Redo)
            {
                e.CanExecute = History.Instance.HasRedo;
                e.Handled = true;
            }
        }
        private void OnPreviewExecutedEvent(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Undo)
            {
                if (History.Instance.HasUndo)
                {
                    History.Instance.Undo();
                }
                e.Handled = true;
            }
            else if (e.Command == ApplicationCommands.Redo)
            {
                if (History.Instance.HasRedo)
                {
                    History.Instance.Redo();
                }
                e.Handled = true;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Logger = new WpfConsoleLogger(batchConsole.OutputConsole, labelStatus);
            Logger.Verbose = Document.Preferences.PrefsCommon.Verbose.Value<bool>();

            EnableInfoView(Document.Preferences.PrefsCommon.ShowHelpView.Value<bool>());

            WorkerThreadPool.Instance.BeforeCallback += OnBeforeWorkerThread;
            WorkerThreadPool.Instance.AfterCallback += OnAfterWorkerThread;

            try
            {
                LoadPreferences();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            WorkerThreadPool.Instance.Exit();

            try
            {
                SavePreferences();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            base.OnClosing(e);
        }

        public Document Document
        {
            get
            {
                return DataContext as Document;
            }
        }
        public WpfConsoleLogger Logger
        {
            get;
            private set;
        }
        public DragAndDropManager DDManager
        {
            get;
            set;
        }

        private void OnIsBatchActiveChanged(object sender, EventArgs e)
        {
            Document.VisibleTab.Value = 1;
        }
        private void OnFileTreeModelChanged(object sender, EventArgs e)
        {
            if (Document.VisibleTab.Value >= 1 && Document.VisibleTab.Value <= 2)
            {
                // Force the operation to be enqueued in message queue
                // as this is triggered from mouse double click where
                // the mouse up event that comes after the double click
                // event re-sets the tab index.
                App.Current.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    Document.VisibleTab.Value = 0;
                }));
            }
        }
        private void OnShowHelpViewChanged(object sender, EventArgs e)
        {
            EnableInfoView(Document.Preferences.PrefsCommon.ShowHelpView.Value<bool>());
        }

        private void OnBeforeWorkerThread(IWork work)
        {
            EnableWindow(
                work.Type == IWorkType.Background,
                work.Type == IWorkType.AbortableLock);

            Document.IsWorkerThreadActive.Value = WorkerThreadPool.Instance.IsWorking;
        }
        private void OnAfterWorkerThread(IWork work)
        {
            EnableWindow(true, false);

            Document.IsWorkerThreadActive.Value = WorkerThreadPool.Instance.IsWorking;
        }

        private void OnInfoText(object sender, EventArgs e)
        {
            infoView.Text = sender != null ? sender.ToString() : "";
        }

        private void EnableWindow(bool value, bool showCancelButton)
        {
            mainGrid.IsEnabled = value;
            topGrid.IsEnabled = value;

            if (showCancelButton)
            {
                buttonCancel.Visibility = Visibility.Visible;
            }
            else
            {
                buttonCancel.Visibility = Visibility.Collapsed;
            }
        }
        private void EnableInfoView(bool enable)
        {
            infoViewBorder.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            fileTreeGrid.RowDefinitions.ElementAt(2).Height = new GridLength(enable ? 150 : 0);
        }

        public void SavePreferences()
        {
            AppPreferences.Instance.Set<double>("Window.Height", Height);
            AppPreferences.Instance.Set<double>("Window.Width", Width);
            AppPreferences.Instance.Set<double>("Window.Left", Left);
            AppPreferences.Instance.Set<double>("Window.Top", Top);
            AppPreferences.Instance.Set<double>("SplitterHorizontal", FirstColumn.ActualWidth);
            AppPreferences.Instance.Set<double>("SplitterVertical", FirstRow.ActualHeight);
            AppPreferences.Instance.Set<string>("FolderPath", Document.FileTreeModel.SelectedPathString());

            Preferences.SavePreferences(Document.Preferences);
        }
        public void LoadPreferences()
        {
            if (AppPreferences.Instance.HasKey("Window.Height"))
            {
                Height = AppPreferences.Instance.Get<double>("Window.Height", 800.0);
                Width = AppPreferences.Instance.Get<double>("Window.Height", 600.0);
                Left = AppPreferences.Instance.Get<double>("Window.Left", 0.0);
                Top = AppPreferences.Instance.Get<double>("Window.Top", 0.0);
            }

            if (AppPreferences.Instance.HasKey("SplitterHorizontal"))
            {
                FirstColumn.Width = new GridLength((int)
                    AppPreferences.Instance.Get<double>("SplitterHorizontal", 200.0));
            }
            if (AppPreferences.Instance.HasKey("SplitterVertical"))
            {
                FirstRow.Height = new GridLength((int)
                    AppPreferences.Instance.Get<double>("SplitterVertical", 200.0));
            }

            string path = AppPreferences.Instance.Get<string>("FolderPath", "");
            if (!String.IsNullOrEmpty(path))
            {
                Document.FileTreeModel.ExpandAndSelect(path, true);
            }
        }
    }
}
