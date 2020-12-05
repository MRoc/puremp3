using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using CoreUtils;

namespace CoreVirtualDrive
{
    public class FileWalker
    {
        public FileWalker(string fileOrDirectory)
        {
            List<string> files = new List<string>();
            List<string> directories = new List<string>();

            Walk(fileOrDirectory, files, directories);

            Files = files;
            Directories = directories;
        }

        public IEnumerable<string> Files { get; private set; }
        public IEnumerable<string> Directories { get; private set; }

        private static void Walk(string dirOrFile, List<string> files, List<string> dirs)
        {
            if (VirtualDrive.ExistsFile(dirOrFile))
            {
                files.Add(dirOrFile);
            }
            else if (VirtualDrive.ExistsDirectory(dirOrFile))
            {
                foreach (var item in VirtualDrive.GetDirectories(dirOrFile))
                {
                    Walk(item, files, dirs);
                }
                foreach (var item in VirtualDrive.GetFiles(dirOrFile, "*.*"))
                {
                    Walk(item, files, dirs);
                }

                dirs.Add(dirOrFile);
            }
            else
            {
                throw new Exception("Unknown");
            }
        }
    }

    public class TestFileWalker
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestFileWalker));
        }
       
        public static void Test_FileWalker()
        {
            string[] files =
            {
                VirtualDrive.VirtualFileName(@"Test_FileWalker\folder0\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_FileWalker\folder0\file1.bin"),
                VirtualDrive.VirtualFileName(@"Test_FileWalker\folder0\folder0\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_FileWalker\folder0\folder0\file1.bin"),
                VirtualDrive.VirtualFileName(@"Test_FileWalker\folder1\file2.bin"),
            };

            string[] expected = 
            {
                "\\\\virtualdrive\\Test_FileWalker\\folder0\\folder0\\file0.bin",
                "\\\\virtualdrive\\Test_FileWalker\\folder0\\folder0\\file1.bin",
                "\\\\virtualdrive\\Test_FileWalker\\folder0\\file0.bin",
                "\\\\virtualdrive\\Test_FileWalker\\folder0\\file1.bin",
                "\\\\virtualdrive\\Test_FileWalker\\folder1\\file2.bin",
                "\\\\virtualdrive\\Test_FileWalker\\folder0\\folder0",
                "\\\\virtualdrive\\Test_FileWalker\\folder0",
                "\\\\virtualdrive\\Test_FileWalker\\folder1",
                "\\\\virtualdrive\\Test_FileWalker",
            };

            files.ForEach(n => VirtualDrive.Store(VirtualDrive.VirtualFileName(n), null));

            FileWalker walker = new FileWalker(VirtualDrive.VirtualFileName("Test_FileWalker"));

            UnitTest.Test(walker.Files.Concat(walker.Directories).SequenceEqual(expected));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName("Test_FileWalker"), true);
        }
    }
}
