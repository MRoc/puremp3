using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using CoreDocument;
using System.Windows.Media;

namespace CoreControls.DragAndDrop
{
    public class DragAndDropManager
    {
        public DragAndDropManager(FrameworkElement root)
        {
            Root = root;

            root.PreviewDragEnter += new DragEventHandler(OnCanDrop);
            root.PreviewDragOver += new DragEventHandler(OnCanDrop);
            root.Drop += new DragEventHandler(OnDrop);
        }

        public DropTypes[] DropTypeByPosition(Point p)
        {
            DependencyObject child = VisualTreeHelper.HitTest(Root, p).VisualHit;

            IDropTargetProvider dropTarget = WpfUtils.FindVisualParents<FrameworkElement>(child)
                .Where(n => n.DataContext is IDropTargetProvider)
                .Select(n => n.DataContext as IDropTargetProvider).FirstOrDefault();

            if (Object.ReferenceEquals(dropTarget, null))
            {
                return new DropTypes[] { };
            }
            else
            {
                return dropTarget.DropTarget.SupportedTypes;
            }
        }

        private void OnCanDrop(object sender, DragEventArgs e)
        {
            if (CanDrop(e))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (CanDrop(e))
            {
                History.Instance.ExecuteInTransaction(
                    delegate()
                    {
                        DropTargetProvider(e).DropTarget.Drop(
                            (e.Data as System.Windows.DataObject).GetFileDropList()[0]);
                    },
                    History.Instance.NextFreeTransactionId(),
                    "Drop picture");
            }
        }

        private bool CanDrop(DragEventArgs e)
        {
            IDropTargetProvider dropTargetProvider = DropTargetProvider(e);

            if (dropTargetProvider != null)
            {
                return dropTargetProvider.DropTarget.AllowDrop(
                    (e.Data as System.Windows.DataObject).GetFileDropList()[0]);
            }

            return false;
        }
        private DropTypes DragEventArgsToDropTypes(DragEventArgs e)
        {
            if (e.Data is System.Windows.DataObject
                && (e.Data as System.Windows.DataObject).ContainsFileDropList())
            {
                string fileName = (e.Data as System.Windows.DataObject).GetFileDropList()[0];

                if (fileName.ToLower().EndsWith(".jpg") || fileName.ToLower().EndsWith(".png"))
                {
                    return DropTypes.Picture;
                }
            }

            return DropTypes.Unknown;
        }
        private IDropTargetProvider DropTargetProvider(DragEventArgs e)
        {
            Point p = e.GetPosition(Root);
            DropTypes type = DragEventArgsToDropTypes(e);
            DependencyObject child = VisualTreeHelper.HitTest(Root, p).VisualHit;

            return WpfUtils.FindVisualParents<FrameworkElement>(child)
                .Where(n => n.DataContext is IDropTargetProvider)
                .Where(n => (n.DataContext as IDropTargetProvider).DropTarget.SupportedTypes.Contains(type))
                .Select(n => n.DataContext as IDropTargetProvider).FirstOrDefault();
        }

        private FrameworkElement Root
        {
            get;
            set;
        }
    }
}
