using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CoreUtils;
using CoreTest;

namespace CoreVirtualDrive
{
    class VirtualDriveImpl : IDrive
    {
        public static readonly string virtualDrive = @"\\virtualdrive";
        public static FileInfo CreateVirtualFileInfo(string filename)
        {
            return new FileInfo(CreateVirtualFileName(filename));
        }
        public static string CreateVirtualFileName(string filename)
        {
            return Path.Combine(virtualDrive, filename);
        }

        public string Parent(string id)
        {
            CheckIsVirtualDrive(id);

            return new VirtualPath(id).Parent.ToString();
        }

        public Stream OpenInStream(string id)
        {
            CheckIsVirtualDrive(id);

            return new MemoryStream(fileTree.Load(new VirtualPath(id)));
        }
        public Stream OpenOutStream(string id)
        {
            CheckIsVirtualDrive(id);

            if (outStreamsToIdMap.ContainsValue(id))
            {
                throw new Exception("Can't open two out streams at the same time");
            }

            Stream result;

            if (ExistsFile(id))
            {
                result = new VirtualDriveMemoryOutStream(this, fileTree.Load(new VirtualPath(id)));
            }
            else
            {
                result = new VirtualDriveMemoryOutStream(this);
            }

            outStreamsToIdMap[result] = id;

            return result;
        }

        public string[] GetFiles(string path, string pattern)
        {
            CheckIsVirtualDrive(path);
            
            VirtualDirectory dir = fileTree.FindNode(new VirtualPath(path)) as VirtualDirectory;
            IEnumerable<VirtualFile> files = dir.Children.Where(n => n is VirtualFile).Select(n => n as VirtualFile);

            string regx = WildcardToRegex(path + pattern);

            return
                (from item in files
                 where System.Text.RegularExpressions.Regex.IsMatch(
                         item.Path.ToString(), regx, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                 orderby item.Path.ToString().GetAlphaNumericOrderToken()
                 select item.Path.ToString()).ToArray();
        }
        public string[] GetDirectories(string path)
        {
            CheckIsVirtualDrive(path);
            
            VirtualDirectory dir = fileTree.FindNode(new VirtualPath(path)) as VirtualDirectory;
            IEnumerable<VirtualDirectory> files = dir.Children.Where(n => n is VirtualDirectory).Select(n => n as VirtualDirectory);

            return
                (from item in files
                 orderby item.Path.ToString().GetAlphaNumericOrderToken()
                 select item.Path.ToString()).ToArray();
        }

        public bool ExistsFile(string id)
        {
            CheckIsVirtualDrive(id);

            VirtualFileSystemNode node = fileTree.FindNode(new VirtualPath(id));

            return !Object.ReferenceEquals(node, null) && node is VirtualFile;
        }
        public bool ExistsDirectory(string id)
        {
            CheckIsVirtualDrive(id);

            VirtualFileSystemNode node = fileTree.FindNode(new VirtualPath(id));

            return !Object.ReferenceEquals(node, null) && node is VirtualDirectory;
        }

        public FileAttributes DirectoryAttributes(string id)
        {
            return FileAttributes.Directory | FileAttributes.NotContentIndexed;
        }
        public void ClearDirectoryAttrributes(string id)
        {
            CheckIsVirtualDrive(id);
        }
        public void ClearFileAttrributes(string id)
        {
            CheckIsVirtualDrive(id);
        }
        public bool DriveIsReady(string id)
        {
            return true;
        }

        public long FileLength(string id)
        {
            CheckIsVirtualDrive(id);

            if (ExistsFile(id))
            {
                return fileTree.Load(new VirtualPath(id)).Length;
            }

            return 0;
        }

        public void DeleteFile(string id)
        {
            CheckIsVirtualDrive(id);

            if (!ExistsFile(id))
            {
                throw new Exception("Can't delete non-existing file");
            }

            fileTree.DeleteNode(new VirtualPath(id));
        }
        public void DeleteDir(string id, bool recursive)
        {
            CheckIsVirtualDrive(id);

            if (!ExistsDirectory(id))
            {
                throw new FileNotFoundException("\"" + id + "\" does not exist!");
            }
            if (!recursive && (GetFiles(id, "*.*").Length > 0 || GetDirectories(id).Length > 0))
            {
                throw new NotSupportedException("recursive==false");
            }

            fileTree.DeleteNode(new VirtualPath(id));
        }

        public void MoveFile(string src, string dst)
        {
            CheckIsVirtualDrive(src);
            CheckIsVirtualDrive(dst);

            byte[] content = fileTree.Load(new VirtualPath(src));
            fileTree.DeleteNode(new VirtualPath(src));
            fileTree.Store(new VirtualPath(dst), content);
        }
        public void MoveDir(string src, string dst)
        {
            CheckIsVirtualDrive(src);
            CheckIsVirtualDrive(dst);

            VirtualPath srcPath = new VirtualPath(src);
            VirtualPath dstPath = new VirtualPath(dst);

            VirtualDirectory srcDir = fileTree.FindNode(srcPath) as VirtualDirectory;
            fileTree.DeleteNode(srcPath);

            srcDir.Name = dstPath.Name;

            VirtualDirectory dstParent = fileTree.FindNode(dstPath.Parent) as VirtualDirectory;
            if (Object.ReferenceEquals(dstParent, null))
            {
                dstParent = fileTree.CreateDirectoryNodes(dstPath.Parent);
            }
            dstParent.Add(srcDir);
        }

        public void ReplaceFile(string src, string dst)
        {
            CheckIsVirtualDrive(src);
            CheckIsVirtualDrive(dst);

            if (src != dst)
            {
                VirtualPath srcPath = new VirtualPath(src);
                VirtualPath dstPath = new VirtualPath(dst);

                fileTree.Store(dstPath, fileTree.Load(srcPath));
                fileTree.DeleteNode(srcPath);
            }
        }

        public void CopyFile(string src, string dst)
        {
            CheckIsVirtualDrive(src);
            CheckIsVirtualDrive(dst);

            fileTree.Store(new VirtualPath(dst), fileTree.Load(new VirtualPath(src)));
        }
        public void CopyDir(string src, string dst)
        {
            CheckIsVirtualDrive(src);
            CheckIsVirtualDrive(dst);

            VirtualPath srcPath = new VirtualPath(src);
            VirtualPath dstPath = new VirtualPath(dst);

            VirtualDirectory srcNode = fileTree.FindNode(srcPath) as VirtualDirectory;
            VirtualDirectory dstNode = srcNode.Clone() as VirtualDirectory;
            dstNode.Name = dstPath.Name;

            VirtualDirectory dstParentNode = fileTree.FindNode(dstPath.Parent) as VirtualDirectory;
            dstParentNode.Add(dstNode);
        }

        public void CreateDirectory(string dir)
        {
            CheckIsVirtualDrive(dir);

            fileTree.CreateDirectoryNodes(new VirtualPath(dir));
        }

        public void Store(string id, byte[] data)
        {
            CheckIsVirtualDrive(id);
            fileTree.Store(new VirtualPath(id), data);
        }
        public void Clear()
        {
            Debug.Assert(outStreamsToIdMap.Count == 0);
            fileTree.Clear();
        }

        public static string WildcardToRegex(string pattern)
        {
            return ("^" +
                 pattern.Replace(@"\", @"\\")
                .Replace(".", @"\.")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace("*", "(.*)")
                .Replace("?", "(.{1,1})"));
        }

        private string IdByOutStream(Stream stream)
        {
            return outStreamsToIdMap[stream];
        }
        private void OnVirtualDriveMemoryOutStreamClosing(Stream stream)
        {
            outStreamsToIdMap.Remove(stream);
        }

        private class VirtualDriveMemoryOutStream : MemoryStream
        {
            public VirtualDriveMemoryOutStream(VirtualDriveImpl virtualDrive)
            {
                Drive = virtualDrive;
            }
            public VirtualDriveMemoryOutStream(VirtualDriveImpl virtualDrive, byte[] buffer)
                : base()
            {
                Write(buffer, 0, buffer.Length);
                Seek(0, SeekOrigin.Begin);
                Drive = virtualDrive;
            }
            public override void Close()
            {
                string id = Drive.IdByOutStream(this);

                byte[] buffer = GetBuffer();
                byte[] final = new byte[Length];
                Array.Copy(buffer, final, Length);

                Drive.Store(id, final);

                Drive.OnVirtualDriveMemoryOutStreamClosing(this);

                base.Close();
            }

            private VirtualDriveImpl Drive
            {
                get;
                set;
            }
        }

        private Dictionary<string, byte[]> store = new Dictionary<string, byte[]>();
        private Dictionary<Stream, string> outStreamsToIdMap = new Dictionary<Stream, string>();

        private static void CheckIsVirtualDrive(string id)
        {
            if (!id.StartsWith(virtualDrive))
            {
                throw new Exception("Not a virtual drive");
            }
        }

        private VirtualFileTree fileTree = new VirtualFileTree();
    }

    class VirtualFileTree
    {
        public VirtualFile Store(VirtualPath filePath, byte[] data)
        {
            VirtualPath dirPath = filePath.Parent;

            VirtualDirectory dir = CreateDirectoryNodes(dirPath);

            VirtualFile file = new VirtualFile(filePath.Parts.Last());
            file.Data = data;
            dir.Add(file);

            return file;
        }
        public byte[] Load(VirtualPath filePath)
        {
            return (FindNode(filePath) as VirtualFile).Data;
        }
        public void Clear()
        {
            root.Clear();
        }

        public VirtualFileSystemNode FindNode(VirtualPath filePath)
        {
            VirtualFileSystemNode cur = root;

            foreach (var item in filePath.Parts.Skip(1))
            {
                if (Object.ReferenceEquals(cur, null))
                {
                    break;
                }

                cur = (cur as VirtualDirectory).NodeByName(item);
            }

            return cur;
        }

        public VirtualDirectory CreateDirectoryNodes(VirtualPath path)
        {
            VirtualFileSystemNode cur = root;

            foreach (var part in path.Parts.Skip(1))
            {
                VirtualDirectory dir = cur as VirtualDirectory;
                VirtualDirectory sub = dir.NodeByName(part) as VirtualDirectory;

                if (Object.ReferenceEquals(sub, null))
                {
                    sub = new VirtualDirectory(part);
                    dir.Add(sub);
                }

                cur = sub;
            }

            return cur as VirtualDirectory;
        }
        public void DeleteNode(VirtualPath path)
        {
            VirtualFileSystemNode node = FindNode(path);
            VirtualDirectory parent = node.Parent;
            parent.Remove(node);
        }

        private VirtualDirectory root = new VirtualDirectory("virtualdrive");
    }
    abstract class VirtualFileSystemNode
    {
        public VirtualFileSystemNode(string name)
        {
            Name = name;
        }
        public VirtualDirectory Parent { get; set; }
        public string Name { get; set; }
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public VirtualPath Path
        {
            get
            {
                List<string> names = new List<string>();

                VirtualFileSystemNode node = this;
                while (!Object.ReferenceEquals(node, null))
                {
                    names.Insert(0, node.Name);
                    node = node.Parent;
                }

                return new VirtualPath(names.ToArray());
            }
        }
        public override string ToString()
        {
            return Name;
        }

        public abstract VirtualFileSystemNode Clone();
    }
    class VirtualFile : VirtualFileSystemNode
    {
        public VirtualFile(string name)
            : base(name)
        {
        }
        public byte[] Data { get; set; }

        public override VirtualFileSystemNode Clone()
        {
            VirtualFile result = new VirtualFile(Name);
            result.Data = Data.Clone() as byte[];
            return result;
        }
    }
    class VirtualDirectory : VirtualFileSystemNode
    {
        public VirtualDirectory(string name)
            : base(name)
        {
            Children = new HashSet<VirtualFileSystemNode>();
        }
        public void Add(VirtualFileSystemNode node)
        {
            VirtualFileSystemNode nodeOld = NodeByName(node.Name);
            if (!Object.ReferenceEquals(nodeOld, null))
            {
                Remove(nodeOld);
            }

            node.Parent = this;
            Children.Add(node);
        }
        public void Remove(VirtualFileSystemNode node)
        {
            Children.Remove(node);
            node.Parent = null;
        }
        public void Clear()
        {
            foreach (var node in Children.ToArray())
            {
                Remove(node);
            }
        }
        public VirtualFileSystemNode NodeByName(string name)
        {
            return Children.Where(n => n.Name == name).FirstOrDefault();
        }
        public HashSet<VirtualFileSystemNode> Children { get; private set; }

        public override VirtualFileSystemNode Clone()
        {
            VirtualDirectory result = new VirtualDirectory(Name);

            foreach (var item in Children)
            {
                result.Add(item.Clone());
            }

            return result;
        }
    }

    public class TestVirtualFileTree
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestVirtualFileTree));
        }

        public static void Test_VirtualFileTree_FindNode()
        {
            VirtualPath path0 = new VirtualPath(@"\\VirtualDrive\");

            VirtualFileTree tree = new VirtualFileTree();
            UnitTest.Test(!Object.ReferenceEquals(tree.FindNode(new VirtualPath(@"\\VirtualDrive\")), null));
            UnitTest.Test(Object.ReferenceEquals(tree.FindNode(new VirtualPath(@"\\VirtualDrive\notExisting")), null));
        }
        public static void Test_VirtualFileTree_CreateDirectoryNodes()
        {
            VirtualPath path = new VirtualPath(@"\\VirtualDrive\Folder0\Folder1\");

            VirtualFileTree tree = new VirtualFileTree();
            VirtualDirectory dir0 = tree.CreateDirectoryNodes(path);
            VirtualDirectory dir1 = tree.FindNode(path) as VirtualDirectory;

            UnitTest.Test(Object.ReferenceEquals(dir0, dir1));
        }
        public static void Test_VirtualFileTree_CreateDir_Store()
        {
            VirtualPath path = new VirtualPath(@"\\VirtualDrive\Folder0\Folder1\text.bin");

            VirtualFileTree tree = new VirtualFileTree();
            VirtualFile file0 = tree.Store(path, null);
            VirtualFile file1 = tree.FindNode(path) as VirtualFile;

            UnitTest.Test(Object.ReferenceEquals(file0, file1));
        }
    }
}
