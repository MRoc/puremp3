using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreVirtualDrive.FileSystemOperations
{
    internal class Factory
    {
        internal static IFileSystemOperation CreateDirectoryCreate(string dir)
        {
            DirectoryCreate result = new DirectoryCreate();
            result.Dir = dir;
            return result;
        }
        internal static IFileSystemOperation CreateDirectoryDelete(string dir)
        {
            DirectoryDelete result = new DirectoryDelete();
            result.Dir = dir;
            return result;
        }
        internal static IFileSystemOperation CreateFileMove(string src, string dst)
        {
            FileMove result = new FileMove();
            result.Src = src;
            result.Dst = dst;
            return result;
        }
        internal static IFileSystemOperation CreateFileCopy(string src, string dst)
        {
            FileCopy result = new FileCopy();
            result.Src = src;
            result.Dst = dst;
            return result;
        }
    }
}
