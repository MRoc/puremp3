using System.IO;

namespace CoreVirtualDrive
{
    public interface IDrive
    {
        string Parent(string id);

        Stream OpenInStream(string id);
        Stream OpenOutStream(string id);

        string[] GetDirectories(string path);
        string[] GetFiles(string path, string pattern);

        bool ExistsFile(string id);
        bool ExistsDirectory(string id);

        FileAttributes DirectoryAttributes(string id);
        bool DriveIsReady(string id);

        void ClearDirectoryAttrributes(string id);
        void ClearFileAttrributes(string id);

        long FileLength(string id);

        void DeleteFile(string id);
        void DeleteDir(string id, bool recursive);

        void MoveFile(string src, string dst);
        void MoveDir(string src, string dst);

        void ReplaceFile(string src, string dst);

        void CopyFile(string src, string dst);
        void CopyDir(string src, string dst);

        void CreateDirectory(string dir);
    }
}
