using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CoreDocument;

namespace CoreFileTree
{
    public class TreeNode : DocBase
    {
        private TreeNode parent;
        private bool isExpanded;
        private bool isSelected;

        private readonly ObservableCollection<TreeNode> children
            = new ObservableCollection<TreeNode>();
        
        public static readonly TreeNode DummyChild = new TreeNode();
        
        protected TreeNode()
        {
        }
        protected TreeNode(TreeNode parent)
        {
            ParentNode = parent;
            RootNode.AddNode(this, DummyChild);
        }

        public ObservableCollection<TreeNode> Children
        {
            get { return children; }
        }
        public IEnumerable<TreeNode> ChildrenRecursive
        {
            get
            {
                if (this == DummyChild)
                {
                    throw new Exception("Can't call ChildrenRecursive on DummyChild!");
                }

                List<TreeNode> result = new List<TreeNode>();

                result.Add(this);

                foreach (var item in Children)
                {
                    if (item != DummyChild)
                    {
                        result.AddRange(item.ChildrenRecursive);
                    }
                }

                return result;
            }
        }

        public bool HasDummyChild
        {
            get
            {
                return this.Children.Count == 1 && this.Children[0] == DummyChild;
            }
        }
        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                if (value == isExpanded)
                {
                    return;
                }

                isExpanded = value;
                NotifyPropertyChanged(this, m => m.IsExpanded);

                if (isExpanded && parent != null)
                {
                    parent.IsExpanded = true;
                }

                if (value)
                {
                    if (this.HasDummyChild)
                    {
                        RootNode.RemoveNode(this, DummyChild);
                        LoadChildren();
                    }
                }
                else
                {
                    RootNode.ClearNode(this);
                    RootNode.AddNode(this, DummyChild);
                }
            }
        }
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    NotifyPropertyChanged(this, m => m.IsSelected);
                }
            }
        }

        protected virtual void LoadChildren()
        {
        }
        public virtual void Refresh()
        {
            foreach (var child in Children)
            {
                child.Refresh();
            }
        }

        public virtual string ItemName
        {
            get { return ""; }
        }
        public virtual string ItemPath
        {        
            get { return ""; }
        }

        public TreeNode ParentNode
        {
            get { return parent; }
            set { parent = value ; }
        }
        public FileTreeModel RootNode
        {
            get
            {
                var item = this;

                while (!Object.ReferenceEquals(item.ParentNode, null))
                {
                    item = item.ParentNode;
                }

                return item as FileTreeModel;
            }
        }

        public int RowIndex
        {
            get
            {
                return new TreeNodeWalker(RootNode).IndexOf(this);
            }
        }

        public override string ToString()
        {
            return ItemName + " (" + ItemPath + ")";
        }

        public ObservableCollection<ICommand> Commands
        {
            get
            {
                return RootNode.CommandsForNode(this);
            }
        }
    }
}
