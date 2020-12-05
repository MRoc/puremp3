using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CoreVirtualDrive.FileSystemOperations
{
    public interface IFileSystemOperation
    {
        void Do();
        void Undo();
    }
    internal class DirectoryCreate : IFileSystemOperation
    {
        public string Dir { get; set; }

        public void Do()
        {
            VirtualDrive.CreateDirectory(Dir);
        }
        public void Undo()
        {
            //System.Threading.Thread.Sleep(100);
            VirtualDrive.DeleteDirectory(Dir, true);
        }

        public override string ToString()
        {
            return "Create directory \"" + Dir + "\"";
        }
    }
    internal class DirectoryDelete : IFileSystemOperation
    {
        public string Dir { get; set; }

        public void Do()
        {
            //System.Threading.Thread.Sleep(100);
            VirtualDrive.DeleteDirectory(Dir, true);
        }
        public void Undo()
        {
            VirtualDrive.CreateDirectory(Dir);
        }

        public override string ToString()
        {
            return "Delete directory \"" + Dir + "\"";
        }
    }
    internal class FileMove : IFileSystemOperation
    {
        public string Src { get; set; }
        public string Dst { get; set; }

        public void Do()
        {
            VirtualDrive.MoveFile(Src, Dst);
        }
        public void Undo()
        {
            VirtualDrive.MoveFile(Dst, Src);
        }

        public override string ToString()
        {
            return "Move file \"" + Src + "\" to \"" + Dst + "\"";
        }
    }
    internal class FileCopy : IFileSystemOperation
    {
        public string Src { get; set; }
        public string Dst { get; set; }

        public void Do()
        {
            VirtualDrive.CopyFile(Src, Dst);
        }
        public void Undo()
        {
            VirtualDrive.DeleteFile(Dst);
        }

        public override string ToString()
        {
            return "Copy file \"" + Src + "\" to \"" + Dst + "\"";
        }
    }
    internal class FileAttributeHelper
    {
        public static void ClearAttributes(string currentDir)
        {
            if (VirtualDrive.ExistsDirectory(currentDir))
            {
                VirtualDrive.ClearDirectoryAttributes(currentDir);

                foreach (string dir in VirtualDrive.GetDirectories(currentDir))
                {
                    ClearAttributes(dir);
                }
                foreach (string file in VirtualDrive.GetFiles(currentDir, "*.*"))
                {
                    VirtualDrive.ClearFileAttributes(file);
                }
            }
        }
    }
}
