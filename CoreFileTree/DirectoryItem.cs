using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CoreUtils;
using CoreVirtualDrive;
using System.Diagnostics;

namespace CoreFileTree
{
    public class DirectoryItem : FileSystemNode
    {
        public DirectoryItem(string path, TreeNode parent)
            : base(path, parent)
        {
        }

        protected override void LoadChildren()
        {
            foreach (string dir in Directories())
            {
                RootNode.AddNode(this, new DirectoryItem(dir, this));
            }
        }
        public override void Refresh()
        {
            if (!IsExpanded)
                return;

            List<string> oldDirs = (from item in Children select (item as DirectoryItem).Path).ToList();
            List<string> newDirs = (from item in Directories() select item).ToList();

            List<string> toAdd = newDirs.Where(n => !oldDirs.Contains(n)).ToList();
            List<string> toRemove = oldDirs.Where(n => !newDirs.Contains(n)).ToList();

            foreach (var dir in toAdd)
            {
                Add(dir);
            }
            foreach (var dir in toRemove)
            {
                Remove(dir);
            }

            base.Refresh();
        }

        private IEnumerable<string> Directories()
        {
            string[] dirs = VirtualDrive.GetDirectories(Path);
            foreach (string dir in dirs)
            {
                if ((VirtualDrive.DirectoryAttributes(dir) & FileAttributes.Hidden) == 0)
                {
                    yield return dir;
                }
            }
        }

        private void Add(string dirStr)
        {
            DirectoryInfo dir = new DirectoryInfo(dirStr);

            IEnumerable<string> names =
                from item
                in Children
                select (item as DirectoryItem).DirInfo.Name;

            int index = names.AlphaNumericInsertPosition(dir.Name);

            RootNode.AddNode(this, new DirectoryItem(dir.FullName, this), index);
        }
        private void Remove(string dirStr)
        {
            DirectoryInfo dir = new DirectoryInfo(dirStr);

            foreach (var child in Children)
            {
                if ((child as DirectoryItem).DirInfo.FullName == dir.FullName)
                {
                    RootNode.RemoveNode(this, child);
                    break;
                }
            }
        }
    }
}
