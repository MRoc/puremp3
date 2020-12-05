using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreVirtualDrive
{
    public static class TestCoreVirtualDrive
    {
        public static void Run()
        {
            CoreVirtualDrive.TestVirtualPath.Tests();
            CoreVirtualDrive.TestFileWalker.Tests();
            CoreVirtualDrive.FileSystemOperations.TestSafeOperations.Tests();
            CoreVirtualDrive.TestVirtualFileTree.Tests();
            CoreVirtualDrive.TestID3VirtualDrive.Tests();
            CoreVirtualDrive.TestRecycleBin.Tests();
        }
    }
}
