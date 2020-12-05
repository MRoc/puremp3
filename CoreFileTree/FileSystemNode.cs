using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace CoreFileTree
{
    public class FileSystemNode : TreeNode
    {
        public FileSystemNode(string path, TreeNode parent)
            : base(parent)
        {
            Path = path;
        }

        public string Path
        {
            get;
            set;
        }
        public DirectoryInfo DirInfo
        {
            get
            {
                return new DirectoryInfo(Path);
            }
        }
        public override string ItemName
        {
            get
            {
                string result = new DirectoryInfo(Path).Name;

                if (result[result.Length - 1] == '\\')
                {
                    return result.Substring(0, result.Length - 1);
                }
                else
                {
                    return result;
                }
            }
        }
        public override string ItemPath
        {
            get
            {
                return new DirectoryInfo(Path).FullName;
            }
        }
    }
}
