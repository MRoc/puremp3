using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreFileTree
{
    class TreeNodeWalker : IEnumerator<TreeNode>, IEnumerable<TreeNode>
    {
        public TreeNodeWalker(TreeNode node)
        {
            StartMarker = node;
            Reset();
        }

        public TreeNode Current
        {
            get;
            private set;
        }
        object IEnumerator.Current
        {
            get
            {
                return (this as TreeNodeWalker).Current;
            }
        }

        public bool MoveNext()
        {
            TreeNode cur = Current;

            if (Current.IsExpanded && Current.Children.Count > 0)
            {
                Current = Current.Children[0];
            }
            else
            {
                if (!Object.ReferenceEquals(Next(Current), null))
                {
                    Current = Next(Current);
                }
                else
                {
                    Current = Next(Current.ParentNode);
                }
            }

            return !Object.ReferenceEquals(Current, null);
        }
        public void Reset()
        {
            Current = StartMarker;
        }

        public void Dispose()
        {
            StartMarker = null;
            Current = null;
        }

        private TreeNode Prev(TreeNode node)
        {
            if (!Object.ReferenceEquals(node.ParentNode, null))
            {
                int currentIndex = node.ParentNode.Children.IndexOf(node);

                if (currentIndex > 0)
                {
                    return node.ParentNode.Children[currentIndex - 1];
                }
            }

            return null;
        }
        private TreeNode Next(TreeNode node)
        {
            if (!Object.ReferenceEquals(node.ParentNode, null))
            {
                int currentIndex = node.ParentNode.Children.IndexOf(node);

                if (currentIndex < node.ParentNode.Children.Count - 1)
                {
                    return node.ParentNode.Children[currentIndex + 1];
                }
            }

            return null;
        }
        private TreeNode StartMarker
        {
            get;
            set;
        }

        IEnumerator<TreeNode> IEnumerable<TreeNode>.GetEnumerator()
        {
            return this;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
    }

    public static class TreeNodeWalkerExtensions
    {
        public static int IndexOf(this IEnumerable<TreeNode> seq, TreeNode item)
        {
            int counter = 0;

            foreach (var i in seq)
            {
                if (i == item)
                {
                    return counter;
                }
                else
                {
                    counter++;
                }
            }

            return -1;
        }
    }
}

