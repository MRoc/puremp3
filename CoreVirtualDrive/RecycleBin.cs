using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using System.IO;
using CoreUtils;
using System.Diagnostics;
using CoreLogging;
using CoreVirtualDrive.FileSystemOperations;

namespace CoreVirtualDrive
{
    public class RecycleBin
    {
        public static RecycleBin Instance
        {
            get
            {
                return instance;
            }
        }
        private RecycleBin()
        {
        }
        
        public string RootDir
        {
            get;
            set;
        }

        public void MoveToRecycleBin(string id)
        {
            CheckIsValid();

            string src = id;
            string dst;

            if (VirtualDrive.ExistsDirectory(id))
            {
                dst = Path.Combine(RootDir, RecycleBinNameDir());
                SafeOperations.MoveDirectory(src, dst);
            }
            else if (VirtualDrive.ExistsFile(id))
            {
                dst = Path.Combine(RootDir, RecycleBinNameFile());
                VirtualDrive.MoveFile(src, dst);
            }
            else
            {
                throw new Exception("\"" + id + "\" does not exist!");
            }

            recycledToSrc[dst] = src;
            srcToRecycled[src] = dst;
        }
        public void Restore(string id)
        {
            CheckIsValid();

            string recycled = srcToRecycled[id];
            string src = id;

            if (VirtualDrive.ExistsDirectory(recycled))
            {
                SafeOperations.MoveDirectory(recycled, src);
            }
            else if (VirtualDrive.ExistsFile(recycled))
            {
                VirtualDrive.MoveFile(recycled, src);
            }
            else
            {
                throw new Exception("\"" + id + "\" not found!");
            }

            recycledToSrc.Remove(recycled);
            srcToRecycled.Remove(src);
        }
        public void Restore()
        {
            CheckIsValid();

            foreach (var fileName in srcToRecycled.Keys.ToArray())
            {
                Restore(fileName);
            }
        }
        public void Clear()
        {
            CheckIsValid();

            foreach (var fileName in recycledToSrc.Keys)
            {
                if (VirtualDrive.ExistsDirectory(fileName))
                {
                    try
                    {
                        VirtualDrive.DeleteDirectory(fileName, true);
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (VirtualDrive.ExistsFile(fileName))
                {
                    VirtualDrive.DeleteFile(fileName);
                }
            }

            recycledToSrc.Clear();
            srcToRecycled.Clear();
        }
        public void DeleteContent()
        {
            foreach (var item in VirtualDrive.GetDirectories(RootDir))
            {
                VirtualDrive.DeleteDirectory(item, true);
            }
        }

        private void CheckIsValid()
        {
            if (String.IsNullOrEmpty(RootDir))
            {
                throw new Exception("Recycle bin requires a root directory!");
            }
            if (!VirtualDrive.ExistsDirectory(RootDir))
            {
                throw new Exception("Recycle bin root directory does not exist: \""
                    + RootDir + "\"");
            }
        }
        private string RecycleBinNameFile()
        {
            return VirtualDrive.GetFiles(RootDir, "*.*").Length.ToString() + ".trash";
        }
        private string RecycleBinNameDir()
        {
            return VirtualDrive.GetDirectories(RootDir).Length.ToString();
        }

        private Dictionary<string, string> recycledToSrc = new Dictionary<string, string>();
        private Dictionary<string, string> srcToRecycled = new Dictionary<string, string>();
        private static RecycleBin instance = new RecycleBin();
    }

    public class TestRecycleBin
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestRecycleBin));
        }

        public static void Init()
        {
            VirtualDrive.Store(Path.Combine(recycleBinFolder, "tmp.bin"), null);
            RecycleBin.Instance.RootDir = recycleBinFolder;
        }

        public static void Test_RecycleBin_MoveToRecycleBin_Files()
        {
            fileNames.ForEach(n => VirtualDrive.Store(VirtualDrive.VirtualFileName(n), null));

            fileNames.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            fileNames.ForEach(n => RecycleBin.Instance.MoveToRecycleBin(n));
            fileNames.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));
        }
        public static void Test_RecycleBin_Restore_Files()
        {
            fileNames.ForEach(n => VirtualDrive.Store(VirtualDrive.VirtualFileName(n), null));

            fileNames.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            fileNames.ForEach(n => RecycleBin.Instance.MoveToRecycleBin(n));
            fileNames.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));

            RecycleBin.Instance.Restore();

            fileNames.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
        }
        public static void Test_RecycleBin_Clear_Files()
        {
            fileNames.ForEach(n => VirtualDrive.Store(VirtualDrive.VirtualFileName(n), null));

            fileNames.ForEach(n => UnitTest.Test(VirtualDrive.ExistsFile(n)));
            fileNames.ForEach(n => RecycleBin.Instance.MoveToRecycleBin(n));
            fileNames.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));

            RecycleBin.Instance.Clear();
            RecycleBin.Instance.Restore();

            fileNames.ForEach(n => UnitTest.Test(!VirtualDrive.ExistsFile(n)));
        }

        public static void Exit()
        {
            VirtualDrive.DeleteDirectory(recycleBinFolder, true);
        }

        private static string recycleBinFolder = VirtualDrive.VirtualFileName(@"recycle");
        private static string[] fileNames = new string[]
        {
            VirtualDrive.VirtualFileName("t10.bin"),
            VirtualDrive.VirtualFileName("t11.bin"),
        };
    }
}
