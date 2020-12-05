using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLogging;
using CoreTest;
using CoreUtils;

namespace CoreVirtualDrive.FileSystemOperations
{
    public class SafeOperations
    {
        public static void MoveDirectory(string src, string dst)
        {
            FileAttributeHelper.ClearAttributes(src);
            Execute(MoveDirectoryOperations(src, dst).ToArray());
        }
        public static void CopyDirectory(string src, string dst)
        {
            Execute(CopyDirectoryOperations(src, dst).ToArray());
        }
        public static void MoveFile(string src, string dst)
        {
            Execute(MoveFileOperations(src, dst).ToArray());
        }
        public static void CopyFile(string src, string dst)
        {
            Execute(CopyFileOperations(src, dst).ToArray());
        }

        internal static IEnumerable<IFileSystemOperation> MoveDirectoryOperations(string src, string dst)
        {
            EnsureDirectoryExists(src);
            EnsureDirectoryNotExists(dst);

            List<IFileSystemOperation> operations = new List<IFileSystemOperation>();

            FileWalker fileWalker = new FileWalker(src);

            operations.Add(Factory.CreateDirectoryCreate(dst));

            VirtualPath srcRootPath = new VirtualPath(src);
            VirtualPath dstRootPath = new VirtualPath(dst);
            EnsurePathExists(dstRootPath.Parent, operations);

            foreach (var dir in fileWalker.Directories.Reverse().Skip(1))
            {
                VirtualPath srcDirPath = new VirtualPath(dir);
                VirtualPath dstDirPath = new VirtualPath(
                    dstRootPath.Parts,
                    srcDirPath.Parts.Skip(srcRootPath.Parts.Count()));

                operations.Add(Factory.CreateDirectoryCreate(dstDirPath.ToString()));
            }
            foreach (var file in fileWalker.Files)
            {
                VirtualPath srcFilePath = new VirtualPath(file);
                VirtualPath dstFilePath = new VirtualPath(
                    dstRootPath.Parts,
                    srcFilePath.Parts.Skip(srcRootPath.Parts.Count()));

                operations.Add(Factory.CreateFileMove(file, dstFilePath.ToString()));
            }
            foreach (var dir in fileWalker.Directories)
            {
                operations.Add(Factory.CreateDirectoryDelete(dir));
            }

            return operations;
        }
        internal static IEnumerable<IFileSystemOperation> CopyDirectoryOperations(string src, string dst)
        {
            EnsureDirectoryExists(src);
            EnsureDirectoryNotExists(dst);

            List<IFileSystemOperation> operations = new List<IFileSystemOperation>();

            FileWalker fileWalker = new FileWalker(src);

            operations.Add(Factory.CreateDirectoryCreate(dst));

            VirtualPath srcRootPath = new VirtualPath(src);
            VirtualPath dstRootPath = new VirtualPath(dst);
            EnsurePathExists(dstRootPath.Parent, operations);

            foreach (var dir in fileWalker.Directories.Reverse().Skip(1))
            {
                VirtualPath srcDirPath = new VirtualPath(dir);
                VirtualPath dstDirPath = new VirtualPath(
                    dstRootPath.Parts,
                    srcDirPath.Parts.Skip(srcRootPath.Parts.Count()));

                operations.Add(Factory.CreateDirectoryCreate(dstDirPath.ToString()));
            }
            foreach (var file in fileWalker.Files)
            {
                VirtualPath srcFilePath = new VirtualPath(file);
                VirtualPath dstFilePath = new VirtualPath(
                    dstRootPath.Parts,
                    srcFilePath.Parts.Skip(srcRootPath.Parts.Count()));

                operations.Add(Factory.CreateFileCopy(file, dstFilePath.ToString()));
            }

            return operations;
        }
        internal static IEnumerable<IFileSystemOperation> CopyFileOperations(string src, string dst)
        {
            EnsureFileExists(src);
            EnsureFileNotExists(dst);

            List<IFileSystemOperation> operations = new List<IFileSystemOperation>();

            EnsurePathExists(new VirtualPath(dst).Parent, operations);
            operations.Add(Factory.CreateFileCopy(src, dst));

            return operations;
        }
        internal static IEnumerable<IFileSystemOperation> MoveFileOperations(string src, string dst)
        {
            EnsureFileExists(src);
            EnsureFileNotExists(dst);

            List<IFileSystemOperation> operations = new List<IFileSystemOperation>();

            EnsurePathExists(new VirtualPath(dst).Parent, operations);
            operations.Add(Factory.CreateFileMove(src, dst));

            return operations;
        }

        internal static void Execute(IFileSystemOperation[] operations)
        {
            for (int i = 0; i < operations.Length; i++)
            {
                try
                {
                    Logger.WriteLine(Tokens.InfoVerbose, "  " + operations[i].ToString());

                    operations[i].Do();
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(Tokens.Exception, ex);

                    for (int j = i - 1; j >= 0; j--)
                    {
                        Logger.WriteLine(Tokens.InfoVerbose, "  " + operations[j].ToString());
                        operations[j].Undo();
                    }

                    throw ex;
                }
            }
        }

        internal static void EnsurePathExists(VirtualPath path, IList<IFileSystemOperation> operations)
        {
            for (int i = 0; i < path.Parts.Count(); i++)
            {
                string parent = path.PartialPath(i + 1).ToString();

                if (!VirtualDrive.ExistsDirectory(parent))
                {
                    operations.Add(Factory.CreateDirectoryCreate(parent));
                }
            }
        }

        private static void EnsureDirectoryExists(string dir)
        {
            if (!VirtualDrive.ExistsDirectory(dir))
            {
                throw new Exception("\"" + dir + "\" does not exist!");
            }
        }
        private static void EnsureDirectoryNotExists(string dir)
        {
            if (VirtualDrive.ExistsDirectory(dir))
            {
                throw new Exception("\"" + dir + "\" already exists!");
            }
        }
        private static void EnsureFileExists(string file)
        {
            if (!VirtualDrive.ExistsFile(file))
            {
                throw new Exception("\"" + file + "\" does not exist!");
            }
        }
        private static void EnsureFileNotExists(string file)
        {
            if (VirtualDrive.ExistsFile(file))
            {
                throw new Exception("\"" + file + "\" already exists!");
            }
        }
    }

    public class TestSafeOperations
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestSafeOperations));
        }
        
        public static void Test_RelativeFileMove_MoveDirectoryOperations()
        {
            string[] srcFiles =
            {
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder0\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder0\folder0\file0.bin"),
            };
            string[] dstFiles =
            {
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder1\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder1\folder0\file0.bin"),
            };
            srcFiles.ForEach(n => VirtualDrive.Store(VirtualDrive.VirtualFileName(n), null));

            IEnumerable<IFileSystemOperation> operations = SafeOperations.MoveDirectoryOperations(
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder0"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder1"));

            IEnumerable<string> expected = new string[]
            {
                "Create directory \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder1\"",
                "Create directory \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder1\\folder0\"",
                "Move file \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder0\\folder0\\file0.bin\" to \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder1\\folder0\\file0.bin\"",
                "Move file \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder0\\file0.bin\" to \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder1\\file0.bin\"",
                "Delete directory \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder0\\folder0\"",
                "Delete directory \"\\\\virtualdrive\\Test_RelativeFileMove_Move\\folder0\""
            };

            UnitTest.Test(operations.Select(n => n.ToString()).SequenceEqual(expected));

            srcFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));

            foreach (var operation in operations)
            {
                operation.Do();
            }

            srcFiles.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));

            foreach (var operation in operations.Reverse())
            {
                operation.Undo();
            }

            srcFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName("Test_RelativeFileMove_Move"), true);
        }
        public static void Test_RelativeFileMove_MoveDirectory()
        {
            string[] srcFiles =
            {
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder0\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder0\folder0\file0.bin"),
            };
            string[] dstFiles =
            {
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder1\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder1\folder0\file0.bin"),
            };
            srcFiles.ForEach(n => VirtualDrive.Store(VirtualDrive.VirtualFileName(n), null));

            srcFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));

            SafeOperations.MoveDirectory(
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder0"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileMove_Move\folder1"));

            srcFiles.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName("Test_RelativeFileMove_Move"), true);
        }
        
        public static void Test_RelativeFileCopy_CopyDirectoryOperations()
        {
            string[] srcFiles =
            {
                VirtualDrive.VirtualFileName(@"Test_RelativeFileCopy_Copy\folder0\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileCopy_Copy\folder0\folder0\file0.bin"),
            };
            string[] dstFiles =
            {
                VirtualDrive.VirtualFileName(@"Test_RelativeFileCopy_Copy\folder1\file0.bin"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileCopy_Copy\folder1\folder0\file0.bin"),
            };
            srcFiles.ForEach(n => VirtualDrive.Store(VirtualDrive.VirtualFileName(n), null));

            IEnumerable<IFileSystemOperation> operations = SafeOperations.CopyDirectoryOperations(
                VirtualDrive.VirtualFileName(@"Test_RelativeFileCopy_Copy\folder0"),
                VirtualDrive.VirtualFileName(@"Test_RelativeFileCopy_Copy\folder1"));

            IEnumerable<string> expected = new string[]
            {
                "Create directory \"\\\\virtualdrive\\Test_RelativeFileCopy_Copy\\folder1\"",
                "Create directory \"\\\\virtualdrive\\Test_RelativeFileCopy_Copy\\folder1\\folder0\"",
                "Copy file \"\\\\virtualdrive\\Test_RelativeFileCopy_Copy\\folder0\\folder0\\file0.bin\" to \"\\\\virtualdrive\\Test_RelativeFileCopy_Copy\\folder1\\folder0\\file0.bin\"",
                "Copy file \"\\\\virtualdrive\\Test_RelativeFileCopy_Copy\\folder0\\file0.bin\" to \"\\\\virtualdrive\\Test_RelativeFileCopy_Copy\\folder1\\file0.bin\"",
            };

            UnitTest.Test(operations.Select(n => n.ToString()).SequenceEqual(expected));

            srcFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));

            foreach (var operation in operations)
            {
                operation.Do();
            }

            srcFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));

            foreach (var operation in operations.Reverse())
            {
                operation.Undo();
            }

            srcFiles.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            dstFiles.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName("Test_RelativeFileCopy_Copy"), true);
        }
    }
}
