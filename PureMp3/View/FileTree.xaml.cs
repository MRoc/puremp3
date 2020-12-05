using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ID3.Processor;
using CoreControls.Commands;
using CoreFileTree;
using System.Reflection;

namespace PureMp3
{
    public partial class FileTree : TreeView
    {
        public FileTree()
        {
            InitializeComponent();

            SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(
                OnSelectedItemChanged);

            DataContextChanged += new DependencyPropertyChangedEventHandler(OnDataContextChanged);
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as FileTreeModel).ScrollToCallback -= ScrollTo;
            }
            if (e.NewValue != null)
            {
                (e.NewValue as FileTreeModel).ScrollToCallback += ScrollTo;
            }
        }

        public void ScrollTo(TreeNode node)
        {
            VirtualizingStackPanel vsp = typeof(ItemsControl).InvokeMember(
                "_itemsHost",
                BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic,
                null,
                this,
                null) as VirtualizingStackPanel;

            int rowIndex = node.RowIndex;
            int rowCount = (DataContext as FileTreeModel).RowCount;

            double scrollHeight = vsp.ScrollOwner.ScrollableHeight;
            double offset = scrollHeight * rowIndex / rowCount;

            vsp.SetVerticalOffset(offset);
        }

        private void OnSelectedItemChanged(
            object obj,
            RoutedPropertyChangedEventArgs<object> args)
        {
            if (SelectedItem is TreeNode)
            {
                FileTreeModel fileTreeModel = DataContext as FileTreeModel;
                TreeNode newSelectedItem = SelectedItem as TreeNode;
                TreeNode oldSelectedItem = fileTreeModel.SelectedTreeNode;

                if (!Object.ReferenceEquals(newSelectedItem, oldSelectedItem))
                {
                    fileTreeModel.SelectedTreeNode = newSelectedItem;
                }
            }
        }

        private void OnTreeNodeMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                DependencyObject c = sender as DependencyObject;
                ContentPresenter p = VisualTreeHelper.GetParent(c) as ContentPresenter;
                TreeNode node = p.DataContext as TreeNode;

                node.IsSelected = true;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (IsSupportedKey(e.Key))
            {
                FileTreeModel model = DataContext as FileTreeModel;
                TreeNode node = model.SearchTreeNode(MakeStringFromKey(e.Key));

                if (!Object.ReferenceEquals(node, null))
                {
                    node.IsSelected = true;
                    ScrollTo(node);
                }
            }
        }
        private bool IsSupportedKey(Key key)
        {
            return key >= Key.A && key <= Key.Z || key >= Key.D0 && key <= Key.D9;
        }
        private string MakeStringFromKey(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
            {
                return new string(new char[] { (char)('A' + (key - Key.A)) });
            }
            else if (key >= Key.D0 && key <= Key.D9)
            {
                return new string(new char[] { (char)('0' + (key - Key.D0)) });
            }
            else
            {
                return "";
            }            
        }
    }
}
