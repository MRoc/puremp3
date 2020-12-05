using System.IO;
using System.Linq;
using CoreVirtualDrive;
using System.Collections.Generic;
using CoreUtils;

namespace CoreFileTree
{
    public class VolumeItem : DirectoryItem 
    {
        public VolumeItem(string path, TreeNode parent)
            : base(path, parent)
        {
        }
    }
}
