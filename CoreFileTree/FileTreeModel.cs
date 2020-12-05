using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using CoreControls.Commands;
using CoreThreading;
using CoreVirtualDrive;
using CoreDocument.Text;
using CoreUtils;

namespace CoreFileTree
{
    public class FileTreeModel : TreeNode
    {
        public event Func<TreeNode, ObservableCollection<ICommand>> CommandsProvider;

        public FileTreeModel()
        {
            IsExpanded = true;

            foreach (var drive in VirtualDrive.GetDrives())
            {
                AddNode(this, new VolumeItem(drive, this));
            }
            
            WorkerThreadPool.Instance.StartWork(new FileTreeRefreshThread(this, newAddedTreeNodes));
        }

        public void AddNode(TreeNode parent, TreeNode child)
        {
            parent.Children.Add(child);

            if (child != DummyChild)
            {
                lock (newAddedTreeNodes)
                {
                    newAddedTreeNodes.AddRange(child.ChildrenRecursive);
                }
            }
        }
        public void AddNode(TreeNode parent, TreeNode child, int index)
        {
            parent.Children.Insert(index, child);

            if (child != DummyChild)
            {
                lock (newAddedTreeNodes)
                {
                    newAddedTreeNodes.AddRange(child.ChildrenRecursive);
                }
            }
        }
        public void RemoveNode(TreeNode parent, TreeNode child)
        {
            parent.Children.Remove(child);

            if (child != DummyChild)
            {
                lock (newAddedTreeNodes)
                {
                    foreach (var item in child.ChildrenRecursive)
                    {
                        newAddedTreeNodes.Remove(item);
                        item.Parent = null;
                    }
                }
            }
        }
        public void ClearNode(TreeNode parent)
        {
            while (parent.Children.Count > 0)
            {
                RemoveNode(parent, parent.Children[0]);
            }
        }

        public delegate void RequestScrollTo(TreeNode node);
        public RequestScrollTo ScrollToCallback
        {
            get;
            set;
        }
        public void ScrollTo(TreeNode node)
        {
            if (ScrollToCallback != null)
            {
                ScrollToCallback(node);
            }
        }

        public ObservableCollection<ICommand> CommandsForNode(TreeNode node)
        {
            if (CommandsProvider != null)
            {
                return CommandsProvider(node);
            }
            else
            {
                return new ObservableCollection<ICommand>();
            }
        }

        public TreeNode SelectedTreeNode
        {
            get
            {
                return selectedTreeNode;
            }
            set
            {
                if (!Object.ReferenceEquals(selectedTreeNode, value))
                {
                    if (!Object.ReferenceEquals(selectedTreeNode, null))
                    {
                        selectedTreeNode.IsSelected = false;
                    }

                    selectedTreeNode = value;
                    NotifyPropertyChanged(this, m => m.SelectedTreeNode);

                    if (!Object.ReferenceEquals(selectedTreeNode, null))
                    {
                        selectedTreeNode.IsSelected = true;
                    }
                }
            }
        }
        public void ExpandAndSelect(string path, bool scrollTo)
        {
            SelectedTreeNode = null;

            if (!Object.ReferenceEquals(path, null))
            {
                ObservableCollection<TreeNode> curChildren = Children;
                TreeNode lastTreeNode = null;

                string[] items = VirtualDrive.Split(path);
                for (int i = 0; i < items.Length; i++)
                {
                    foreach (var child in curChildren)
                    {
                        if (child.ItemName.ToLower().Equals(items[i].ToLower()))
                        {
                            if (i < items.Length - 1)
                            {
                                child.IsExpanded = true;
                            }
                            curChildren = child.Children;
                            lastTreeNode = child;
                        }
                    }
                }

                if (!Object.ReferenceEquals(lastTreeNode, null))
                {
                    lastTreeNode.IsSelected = true;

                    if (scrollTo)
                    {
                        ScrollTo(lastTreeNode);
                    }
                }
            }
        }
        public IEnumerable<TreeNode> SelectedPath()
        {
            var pathBuilder = new List<TreeNode>();

            var selectedTreeNode = SelectedTreeNode;
            while (selectedTreeNode != null && selectedTreeNode.ItemName.Length > 0)
            {
                pathBuilder.Insert(0, selectedTreeNode);
                selectedTreeNode = selectedTreeNode.ParentNode;
            }

            return pathBuilder;
        }
        public string SelectedPathString()
        {
            var result = new StringBuilder();

            foreach (var treeNode in SelectedPath())
            {
                string itemName = treeNode.ItemName;

                result.Append(itemName);

                if (!itemName.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    result.Append(Path.DirectorySeparatorChar);
                }
            }

            return result.ToString();
        }

        public TreeNode SearchTreeNode(string text)
        {
            TreeNode node = null;

            if (!Object.ReferenceEquals(SelectedTreeNode, null))
            {
                node = new TreeNodeWalker(SelectedTreeNode).Where(
                    n => n.ItemName.StartsWith(text, true, null)).FirstOrDefault();
            }

            if (Object.ReferenceEquals(node, null))
            {
                node = new TreeNodeWalker(this).Where(
                    n => n.ItemName.StartsWith(text, true, null)).FirstOrDefault();
            }

            return node;
        }

        public int RowCount
        {
            get
            {
                return new TreeNodeWalker(RootNode).Count();
            }
        }

        public ICommand RefreshCommand
        {
            get
            {
                return new CallbackCommand(
                    () => Refresh(),
                    new LocalizedText("FileTreeModelRefresh"),
                    new LocalizedText("FileTreeModelRefreshHelp"));
            }
        }

        public override void Refresh()
        {
            if (!IsExpanded)
                return;

            List<string> oldDrives = (from item in Children select (item as VolumeItem).Path).ToList();
            List<string> newDrives = (from item in VirtualDrive.GetDrives() select item).ToList();

            List<string> toAdd = newDrives.Where(n => !oldDrives.Contains(n)).ToList();
            List<string> toRemove = oldDrives.Where(n => !newDrives.Contains(n)).ToList();

            foreach (var drive in toAdd)
            {
                Add(drive);
            }

            foreach (var drive in toRemove)
            {
                Remove(drive);
            }

            base.Refresh();
        }
        private void Add(string driveStr)
        {
            DriveInfo drive = new DriveInfo(driveStr);

            IEnumerable<string> names =
                from item
                in Children
                select (item as VolumeItem).Path;
            
            int index = names.AlphaNumericInsertPosition(drive.Name);

            RootNode.AddNode(this, new VolumeItem(drive.ToString(), this), index);
        }
        private void Remove(string driveStr)
        {
            DriveInfo drive = new DriveInfo(driveStr);

            foreach (var child in Children)
            {
                if ((child as VolumeItem).Path == drive.ToString())
                {
                    RootNode.RemoveNode(this, child);
                    break;
                }
            }
        }

        public override string ToString()
        {
            return "FileTreeModel";
        }

        private class FileTreeRefreshThread : IWork
        {
            public FileTreeRefreshThread(FileTreeModel root, List<TreeNode> incomingNodes)
            {
                Root = root;
                IncomingNodes = incomingNodes;
            }

            public void Before()
            {
                Console.WriteLine("{0}: Starting", GetType().Name);
            }
            public void Run()
            {
                //return;
                while (!Abort)
                {
                    TreeNode node = TryDequeueNode();

                    if (Object.ReferenceEquals(node, null))
                    {
                        Thread.Sleep(50);
                    }
                    else
                    {
                        RemoveChildrenIfFolderEmpty(node);
                        Thread.Sleep(5);
                    }
                }
            }
            public void After()
            {
                Console.WriteLine("{0}: Stopped", GetType().Name);
            }

            public IWorkType Type
            {
                get
                {
                    return IWorkType.Invisible;
                }
            }
            public bool Abort
            {
                get;
                set;
            }

            private FileTreeModel Root
            {
                get;
                set;
            }
            private List<TreeNode> IncomingNodes
            {
                get;
                set;
            }

            private TreeNode TryDequeueNode()
            {
                TreeNode node = null;

                lock (IncomingNodes)
                {
                    if (IncomingNodes.Count > 0)
                    {
                        node = IncomingNodes[0];
                        IncomingNodes.RemoveAt(0);
                    }
                }

                return node;
            }
            private void RemoveChildrenIfFolderEmpty(TreeNode node)
            {
                try
                {
                    if (node is VolumeItem && !VirtualDrive.DriveIsReady(node.ItemPath)
                        || VirtualDrive.GetDirectories(node.ItemPath).Length == 0)
                    {
                        WorkerThreadPool.Instance.InvokingThread.BeginInvokeLowPrio(
                            delegate()
                            {
                                Root.ClearNode(node);
                            });
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private TreeNode selectedTreeNode;
        private List<TreeNode> newAddedTreeNodes = new List<TreeNode>();
    }
}
