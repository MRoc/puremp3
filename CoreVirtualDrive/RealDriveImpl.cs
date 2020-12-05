using System;
using System.IO;
using CoreUtils;

namespace CoreVirtualDrive
{
    class RealDriveImpl : IDrive
    {
        public string Parent(string id)
        {
            CheckIsNoVirtualDrive(id);

            return new DirectoryInfo(id).Parent.FullName;
        }

        public Stream OpenInStream(string id)
        {
            CheckIsNoVirtualDrive(id);

            return File.Open(id, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        public Stream OpenOutStream(string id)
        {
            CheckIsNoVirtualDrive(id);

            if (ExistsFile(id))
            {
                ClearFileAttrributes(id);
                return File.Open(id, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            }
            else
            {
                return File.Create(id, 1024, FileOptions.RandomAccess);
            }
        }

        public string[] GetFiles(string path, string pattern)
        {
            CheckIsNoVirtualDrive(path);

            return Directory.GetFiles(path, pattern);
        }
        public string[] GetDirectories(string path)
        {
            CheckIsNoVirtualDrive(path);

            return Directory.GetDirectories(path);
        }

        public bool ExistsFile(string id)
        {
            CheckIsNoVirtualDrive(id);

            return File.Exists(id);
        }
        public bool ExistsDirectory(string id)
        {
            CheckIsNoVirtualDrive(id);

            return Directory.Exists(id);
        }

        public FileAttributes DirectoryAttributes(string id)
        {
            CheckIsNoVirtualDrive(id);
            return new DirectoryInfo(id).Attributes;
        }
        public void ClearDirectoryAttrributes(string id)
        {
            CheckIsNoVirtualDrive(id);

            var currentDirInfo = new DirectoryInfo(id);
            currentDirInfo.Attributes = currentDirInfo.Attributes & ~FileAttributes.ReadOnly;
        }
        public void ClearFileAttrributes(string id)
        {
            CheckIsNoVirtualDrive(id);

            if (File.Exists(id))
            {
                File.SetAttributes(id, FileAttributes.Normal);
            }
        }
        public bool DriveIsReady(string id)
        {
            return new DriveInfo(id).IsReady;
        }

        public long FileLength(string id)
        {
            CheckIsNoVirtualDrive(id);

            return ExistsFile(id)
                ? new FileInfo(id).Length
                : 0;
        }

        public void DeleteFile(string id)
        {
            CheckIsNoVirtualDrive(id);

            ClearFileAttrributes(id);
            File.Delete(id);
        }
        public void DeleteDir(string id, bool recursive)
        {
            CheckIsNoVirtualDrive(id);

            ClearDirectoryAttrributes(id);

            Directory.Delete(id, recursive);
        }

        public void MoveFile(string src, string dst)
        {
            CheckIsNoVirtualDrive(src);
            CheckIsNoVirtualDrive(dst);

            ClearFileAttrributes(src);

            if (src != dst && src.Equals(dst, StringComparison.CurrentCultureIgnoreCase))
            {
                File.Move(src, src + ".tmp");
                File.Move(src + ".tmp", dst);
            }
            else
            {
                File.Move(src, dst);
            }
        }
        public void MoveDir(string src, string dst)
        {
            CheckIsNoVirtualDrive(src);
            CheckIsNoVirtualDrive(dst);

            string root0 = Path.GetPathRoot(src);
            string root1 = Path.GetPathRoot(dst);

            if (root0.ToLower() != root1.ToLower())
            {
                CopyAll(new DirectoryInfo(src), new DirectoryInfo(dst));
                Directory.Delete(src, true);
            }
            else
            {
                if (src != dst && src.Equals(dst, StringComparison.CurrentCultureIgnoreCase))
                {
                    Directory.Move(src, src + ".tmp");
                    Directory.Move(src + ".tmp", dst);
                }
                else
                {
                    Directory.Move(src, dst);
                }
            }
        }

        public void ReplaceFile(string src, string dst)
        {
            CheckIsNoVirtualDrive(src);
            CheckIsNoVirtualDrive(dst);

            ClearFileAttrributes(src);
            ClearFileAttrributes(dst);

            File.Replace(src, dst, null);
        }

        public void CopyFile(string src, string dst)
        {
            CheckIsNoVirtualDrive(src);
            CheckIsNoVirtualDrive(dst);

            File.Copy(src, dst);
        }
        public void CopyDir(string src, string dst)
        {
            CheckIsNoVirtualDrive(src);
            CheckIsNoVirtualDrive(dst);

            CopyAll(new DirectoryInfo(src), new DirectoryInfo(dst));
        }

        public void CreateDirectory(string dir)
        {
            CheckIsNoVirtualDrive(dir);
            Directory.CreateDirectory(dir);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        private static void CheckIsNoVirtualDrive(string id)
        {
            if (!String.IsNullOrEmpty(id)
                && id.StartsWith(VirtualDriveImpl.virtualDrive))
            {
                throw new Exception("This is NO virtual drive");
            }
        }
    }
}
