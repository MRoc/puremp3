using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CoreTest;
using CoreUtils;

namespace CoreVirtualDrive
{
    public class VirtualDrive
    {
        private static VirtualDriveImpl virtualDriveImpl = new VirtualDriveImpl();
        private static RealDriveImpl realDriveImpl = new RealDriveImpl();
        
        public static FileInfo CreateVirtualFileInfo(string filename)
        {
            return new FileInfo(VirtualFileName(filename));
        }
        public static string VirtualFileName(string filename)
        {
            return Path.Combine(VirtualDriveImpl.virtualDrive, filename);
        }
        public static string VirtualPrefix()
        {
            return VirtualDriveImpl.virtualDrive;
        }
        public static string FileName(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return null;
            }

            string[] parts = VirtualDrive.Split(id);

            if (parts.Length > 0)
            {
                return parts[parts.Length - 1];
            }
            else
            {
                return "";
            }
        }

        public static string Parent(string id)
        {
            return IsVirtual(id)
                ? virtualDriveImpl.Parent(id)
                : realDriveImpl.Parent(id);
        }
        public static string[] GetDrives()
        {
            return (
                from item
                in DriveInfo.GetDrives()
                select item.ToString()).ToArray();
        }
        public static string[] GetFiles(string path, string pattern)
        {
            return (from i
                    in CurrentDrive(path).GetFiles(path, pattern)
                    orderby i.GetAlphaNumericOrderToken()
                    select i).ToArray();
        }
        public static string[] GetDirectories(string path)
        {
            return (from i
                    in CurrentDrive(path).GetDirectories(path)
                    orderby i.GetAlphaNumericOrderToken()
                    select i).ToArray();
        }
        public static Stream OpenInStream(string id)
        {
            return CurrentDrive(id).OpenInStream(id);
        }
        public static Stream OpenOutStream(string id)
        {
            LockFile(id, AccessObserver.AccessType.Write);

            Stream result = new VirtualDriveStream(
                CurrentDrive(id).OpenOutStream(id),
                OnStreamClosing);

            outStreamStore.Open(id, result);

            return result;
        }
        public static bool ExistsFile(string id)
        {
            return CurrentDrive(id).ExistsFile(id);
        }
        public static bool ExistsDirectory(string id)
        {
            return CurrentDrive(id).ExistsDirectory(id);
        }
        public static FileAttributes DirectoryAttributes(string id)
        {
            return CurrentDrive(id).DirectoryAttributes(id);
        }
        public static void ClearDirectoryAttributes(string id)
        {
            CurrentDrive(id).ClearDirectoryAttrributes(id);
        }
        public static void ClearFileAttributes(string id)
        {
            CurrentDrive(id).ClearFileAttrributes(id);
        }
        public static bool DriveIsReady(string id)
        {
            return CurrentDrive(id).DriveIsReady(id);
        }
        public static long FileLength(string id)
        {
            return CurrentDrive(id).FileLength(id);
        }
        public static void DeleteFile(string id)
        {
            CurrentDrive(id).DeleteFile(id);
        }
        public static void DeleteDirectory(string id, bool recursive)
        {
            CurrentDrive(id).DeleteDir(id, recursive);
        }
        public static void MoveFile(string src, string dst)
        {
            VirtualDrive.LockFile(src, AccessObserver.AccessType.Move);

            try
            {
                CurrentDrive(src).MoveFile(src, dst);
            }
            catch (Exception ex)
            {
                VirtualDrive.FreeFile(src, AccessObserver.AccessType.Move);
                throw ex;
            }

            VirtualDrive.FreeFile(src, dst);
        }
        public static void MoveDirectory(string src, string dst)
        {
            VirtualDrive.LockFile(src, AccessObserver.AccessType.Move);

            try
            {
                CurrentDrive(src).MoveDir(src, dst);
            }
            catch (Exception ex)
            {
                VirtualDrive.FreeFile(src, AccessObserver.AccessType.Move);
                throw ex;
            }

            VirtualDrive.FreeFile(src, dst);
        }
        public static void ReplaceFile(string src, string dst)
        {
            using (VirtualDriveLock curLock = new VirtualDriveLock(dst, AccessObserver.AccessType.Write))
            {
                CurrentDrive(src).ReplaceFile(src, dst);
            }
        }
        public static void CopyFile(string src, string dst)
        {
            CurrentDrive(src).CopyFile(src, dst);
        }
        public static void CopyDirectory(string src, string dst)
        {
            CurrentDrive(src).CopyDir(src, dst);
        }

        public static void CreateDirectory(string dir)
        {
            CurrentDrive(dir).CreateDirectory(dir);
        }

        public static void Store(string id, byte[] data)
        {
            using (Stream s = VirtualDrive.OpenOutStream(id))
            {
                if (!Object.ReferenceEquals(data, null))
                {
                    s.Write(data, 0, data.Length);
                }
            }
        }
        public static byte[] Load(string id)
        {
            using (Stream s = VirtualDrive.OpenInStream(id))
            {
                byte[] data = new byte[VirtualDrive.FileLength(id)];
                s.Read(data, 0, data.Length);
                return data;
            }
        }
        public static void Clear()
        {
            virtualDriveImpl.Clear();
        }

        private static void OnStreamClosing(Stream stream)
        {
            string id = outStreamStore.IdByStream(stream);

            outStreamStore.Close(id, stream);
            FreeFile(id, AccessObserver.AccessType.Write);
        }

        public static AccessObserver ObserverLockExclusive
        {
            get
            {
                return accessObserverOpen;
            }
        }
        public static AccessObserver ObserverFreeShared
        {
            get
            {
                return accessObserverClose;
            }
        }

        public static void LockFile(string id, AccessObserver.AccessType type)
        {
            if (idLockCounter.Lock(id))
            {
                accessObserverOpen.CallAction(id, AccessObserver.AccessRequest.LockExclusive, type);
            }
        }
        public static void FreeFile(string id, AccessObserver.AccessType type)
        {
            if (idLockCounter.Free(id))
            {
                accessObserverClose.CallAction(id, AccessObserver.AccessRequest.FreeShared, type);
            }
        }
        public static void FreeFile(string id, string newId)
        {
            if (idLockCounter.Free(id))
            {
                accessObserverClose.CallAction(
                    id, newId, AccessObserver.AccessRequest.FreeShared, AccessObserver.AccessType.Move);
            }
        }

        private static IdLockCounter idLockCounter = new IdLockCounter();
        private static OutStreamStore outStreamStore = new OutStreamStore();
        private static AccessObserver accessObserverOpen = new AccessObserver();
        private static AccessObserver accessObserverClose = new AccessObserver();

        private static bool IsVirtual(string id)
        {
            return !String.IsNullOrEmpty(id)
                && id.StartsWith(VirtualDriveImpl.virtualDrive);
        }
        private static IDrive CurrentDrive(string id)
        {
            return IsVirtual(id)
                ? (virtualDriveImpl as IDrive)
                : (realDriveImpl as IDrive);
        }

        public static string BuildString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(idLockCounter.ToString());
            result.Append(accessObserverOpen.ToString());
            result.Append(accessObserverClose.ToString());

            return result.ToString();            
        }

        public static string[] Split(string id)
        {
            return id.Split(directorySeparators, StringSplitOptions.RemoveEmptyEntries);
        }
        private static readonly char[] directorySeparators =
            new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
    }

    public class VirtualDriveLock : IDisposable
    {
        public VirtualDriveLock(string id, AccessObserver.AccessType type)
        {
            Id = id;
            Type = type;

            VirtualDrive.LockFile(Id, Type);
        }
        public void Dispose()
        {
            VirtualDrive.FreeFile(Id, Type);
        }

        private string Id
        {
            get;
            set;
        }
        public AccessObserver.AccessType Type
        {
            get;
            private set;
        }
    }

    public class AccessObserver
    {
        public enum AccessRequest
        {
            LockExclusive,
            FreeShared
        }
        public enum AccessType
        {
            Write,
            //Delete,
            Move,
            //Replace
        }
        public class AccessObserverEventArgs : EventArgs
        {
            public AccessObserverEventArgs(
                string affectedId,
                string observedId,
                AccessRequest request,
                AccessType type)
            {
                AffectedId = affectedId;
                ObservedId = observedId;
                NewObservedId = observedId;
                Request = request;
                Type = type;
            }
            public AccessObserverEventArgs(
                string affectedId,
                string observedId,
                string newObservedId,
                AccessRequest request,
                AccessType type)
            {
                AffectedId = affectedId;
                ObservedId = observedId;
                NewObservedId = newObservedId;
                Request = request;
                Type = type;
            }

            public string AffectedId
            {
                get;
                private set;
            }
            public string ObservedId
            {
                get;
                private set;
            }
            public string NewObservedId
            {
                get;
                private set;
            }
            public AccessRequest Request
            {
                get;
                private set;
            }
            public AccessType Type
            {
                get;
                private set;
            }

            public override string ToString()
            {
                StringBuilder result = new StringBuilder();

                result.Append(ObservedId);
                result.Append(": ");
                result.Append(Request);
                result.Append(" ");
                result.Append(Type);

                return result.ToString();
            }
        }

        public void Register(string id, EventHandler<AccessObserverEventArgs> handler)
        {
            lock (observedIds)
            {
                observedIds[id] = handler;
            }
        }
        public void Unregister(string id)
        {
            lock (observedIds)
            {
                if (!observedIds.ContainsKey(id))
                {
                    throw new Exception(
                        "VirtualDrive.UnregisterAccessNotifier not registered: \""
                        + id + "\"");
                }

                observedIds.Remove(id);
            }
        }

        public void CallAction(string id, AccessRequest request, AccessType type)
        {
            CallAction(id, id, request, type);
        }
        public void CallAction(string id, string newId, AccessRequest request, AccessType type)
        {
            string[] touchedIds = null;

            lock (observedIds)
            {
                touchedIds = (from item in observedIds.Keys where item.StartsWith(id) select item).ToArray();
            }

            if (touchedIds.Length > 0)
            {
                foreach (var item in touchedIds)
                {
                    if (type == AccessObserver.AccessType.Move && request == AccessObserver.AccessRequest.FreeShared)
                    {
                        string newItem = newId;
                        if (id != item)
                        {
                            if (id.EndsWith(@"\"))
                            {
                                newItem = Path.Combine(newId, item.Substring(id.Length));
                            }
                            else
                            {
                                newItem = Path.Combine(newId, item.Substring(id.Length + 1));
                            }
                        }

                        observedIds[item](id, new AccessObserver.AccessObserverEventArgs(
                            id, item, newItem, request, type));
                    }
                    else
                    {
                        observedIds[item](id, new AccessObserver.AccessObserverEventArgs(id, item, request, type));
                    }
                }
            }
        }

        public int CountObservedIds
        {
            get
            {
                lock (observedIds)
                {
                    return observedIds.Keys.Count();
                }
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append(GetType().Name);
            result.Append('\n');

            foreach (var key in observedIds.Keys)
            {
                result.Append("  ");
                result.Append(key);
                result.Append('\n');
            }

            return result.ToString();
        }

        private Dictionary<string, EventHandler<AccessObserver.AccessObserverEventArgs>> observedIds
            = new Dictionary<string, EventHandler<AccessObserver.AccessObserverEventArgs>>();
    }
    class IdLockCounter
    {
        public bool Lock(string id)
        {
            bool fire = false;

            lock (lockCounterById)
            {
                if (lockCounterById.ContainsKey(id))
                {
                    lockCounterById[id]++;
                }
                else
                {
                    lockCounterById[id] = 1;
                    fire = true;
                }
            }

            return fire;
        }
        public bool Free(string id)
        {
            bool fire = false;

            lock (lockCounterById)
            {
                lockCounterById[id]--;

                if (lockCounterById[id] == 0)
                {
                    lockCounterById.Remove(id);
                    fire = true;
                }
            }

            return fire;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append(GetType().Name);
            result.Append('\n');

            foreach (var key in lockCounterById.Keys)
            {
                result.Append("  ");
                result.Append(key);
                result.Append('\n');
            }

            return result.ToString();
        }

        private Dictionary<string, int> lockCounterById = new Dictionary<string, int>();
    }
    class OutStreamStore
    {
        public string IdByStream(Stream stream)
        {
            string result;
            lock (idByStream)
            {
                result = idByStream[stream];
            }
            return result;
        }
        public void Open(string id, Stream stream)
        {
            lock (idByStream)
            {
                if (idByStream.ContainsKey(stream))
                {
                    throw new Exception("VirtualDrive can not have two streams with same hash!");
                }

                idByStream[stream] = id;
            }
        }
        public void Close(string id, Stream stream)
        {
            lock (idByStream)
            {
                if (!idByStream.ContainsKey(stream) || idByStream[stream] != id)
                {
                    throw new Exception("VirtualDrive trying to close unknown stream/id!");
                }

                idByStream.Remove(stream);
            }
        }

        private Dictionary<Stream, string> idByStream = new Dictionary<Stream, string>();
    }

    public class TestID3VirtualDrive
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestID3VirtualDrive));
        }

        private static void TestVirtualDrivePaths()
        {
            string folder0 = VirtualDrive.VirtualFileName("folder");
            string parent0 = VirtualDrive.Parent(folder0);
            UnitTest.Test(parent0 == VirtualDriveImpl.virtualDrive);
        }

        private static void TestVirtualDriveWriteRead()
        {
            string fileName = VirtualDrive.VirtualFileName(
                @"TestID3VirtualDrive\file0");

            UnitTest.Test(!VirtualDrive.ExistsFile(fileName));

            Stream outStream = VirtualDrive.OpenOutStream(fileName);
            outStream.WriteByte(0);
            outStream.WriteByte(1);
            outStream.WriteByte(2);
            outStream.Close();

            UnitTest.Test(VirtualDrive.ExistsFile(fileName));
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 3);

            Stream inStream = VirtualDrive.OpenInStream(fileName);
            UnitTest.Test(inStream.ReadByte() == 0);
            UnitTest.Test(inStream.ReadByte() == 1);
            UnitTest.Test(inStream.ReadByte() == 2);
            inStream.Close();
        }
        private static void TestVirtualDriveReWrite()
        {
            string fileName = VirtualDrive.VirtualFileName(
                @"TestID3VirtualDrive\file1");

            UnitTest.Test(!VirtualDrive.ExistsFile(fileName));

            Stream outStream0 = VirtualDrive.OpenOutStream(fileName);
            outStream0.WriteByte(0);
            outStream0.WriteByte(1);
            outStream0.WriteByte(2);
            outStream0.Close();

            UnitTest.Test(VirtualDrive.ExistsFile(fileName));
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 3);

            Stream outStream1 = VirtualDrive.OpenOutStream(fileName);
            outStream1.WriteByte(5);
            outStream1.WriteByte(6);
            outStream1.Close();

            UnitTest.Test(VirtualDrive.ExistsFile(fileName));
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 3);

            Stream inStream = VirtualDrive.OpenInStream(fileName);
            UnitTest.Test(inStream.ReadByte() == 5);
            UnitTest.Test(inStream.ReadByte() == 6);
            UnitTest.Test(inStream.ReadByte() == 2);
            inStream.Close();
        }
        private static void TestVirtualDriveReWriteAppend()
        {
            string fileName = VirtualDrive.VirtualFileName(
                @"TestID3VirtualDrive\file2");

            UnitTest.Test(!VirtualDrive.ExistsFile(fileName));

            Stream outStream0 = VirtualDrive.OpenOutStream(fileName);
            outStream0.WriteByte(0);
            outStream0.WriteByte(1);
            outStream0.WriteByte(2);
            outStream0.Close();

            UnitTest.Test(VirtualDrive.ExistsFile(fileName));
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 3);

            Stream outStream1 = VirtualDrive.OpenOutStream(fileName);
            outStream1.Seek(3, SeekOrigin.Begin);
            outStream1.WriteByte(5);
            outStream1.WriteByte(6);
            outStream1.Close();

            UnitTest.Test(VirtualDrive.ExistsFile(fileName));
            UnitTest.Test(VirtualDrive.FileLength(fileName) == 5);

            Stream inStream = VirtualDrive.OpenInStream(fileName);
            UnitTest.Test(inStream.ReadByte() == 0);
            UnitTest.Test(inStream.ReadByte() == 1);
            UnitTest.Test(inStream.ReadByte() == 2);
            UnitTest.Test(inStream.ReadByte() == 5);
            UnitTest.Test(inStream.ReadByte() == 6);
            inStream.Close();
        }

        private static void TestVirtualDriveExistsDirectory()
        {
            UnitTest.Test(VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("TestID3VirtualDrive")));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("ARandomPath")));

            string folder = VirtualDrive.VirtualFileName("folder");
            string fileName = Path.Combine(folder, "t00.bin");
            VirtualDrive.Store(fileName, null);
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(fileName));
            VirtualDrive.DeleteFile(fileName);
        }
        private static void TestVirtualDriveDeleteDirectory()
        {
            VirtualDrive.Store(
                VirtualDrive.VirtualFileName(@"Testdir0\testfile0.bin"),
                null);
            VirtualDrive.Store(
                VirtualDrive.VirtualFileName(@"Testdir0\testfile1.bin"),
                null);

            UnitTest.Test(VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir0")));

            VirtualDrive.DeleteDirectory(
                VirtualDrive.VirtualFileName("Testdir0"), true);

            UnitTest.Test(!VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir0")));
        }
        private static void TestVirtualDriveMoveDirectory()
        {
            string filename00 = VirtualDrive.VirtualFileName(@"Testdir0\testfile0.bin");
            string filename01 = VirtualDrive.VirtualFileName(@"Testdir0\testfile1.bin");

            string filename10 = VirtualDrive.VirtualFileName(@"Testdir1\testfile0.bin");
            string filename11 = VirtualDrive.VirtualFileName(@"Testdir1\testfile1.bin");

            VirtualDrive.Store(filename00, null);
            VirtualDrive.Store(filename01, null);

            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir0")));

            VirtualDrive.MoveDirectory(
                VirtualDrive.VirtualFileName("Testdir0"),
                VirtualDrive.VirtualFileName("Testdir1"));

            UnitTest.Test(!VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir0")));
            UnitTest.Test(VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir1")));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(VirtualDrive.ExistsFile(filename11));

            VirtualDrive.DeleteDirectory(
                VirtualDrive.VirtualFileName("Testdir1"), true);
        }
        private static void TestVirtualDriveCopyDirectory()
        {
            string filename00 = VirtualDrive.VirtualFileName(@"Testdir0\testfile0.bin");
            string filename01 = VirtualDrive.VirtualFileName(@"Testdir0\testfile1.bin");

            string filename10 = VirtualDrive.VirtualFileName(@"Testdir1\testfile0.bin");
            string filename11 = VirtualDrive.VirtualFileName(@"Testdir1\testfile1.bin");

            VirtualDrive.Store(filename00, null);
            VirtualDrive.Store(filename01, null);

            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir0")));

            VirtualDrive.CopyDirectory(
                VirtualDrive.VirtualFileName("Testdir0"),
                VirtualDrive.VirtualFileName("Testdir1"));

            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir0")));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(VirtualDrive.ExistsFile(filename11));
            UnitTest.Test(VirtualDrive.ExistsDirectory(
                VirtualDrive.VirtualFileName("Testdir1")));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName("Testdir0"), true);
            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName("Testdir1"), true);
        }

        private static void TestVirtualDriveWildcardToRegex()
        {
            string[] texts = new string[]
            {
                @"\\VirtualDrive\Test1.mp3",
                @"\\VirtualDrive\Test2.mp3",
                @"\\VirtualDrive\Test3.bin"
            };

            string wildCards = @"\\VirtualDrive\*.mp3";
            string pattern = VirtualDriveImpl.WildcardToRegex(wildCards);

            string[] result =
                (from t
                in texts
                where System.Text.RegularExpressions.Regex.IsMatch(
                        t, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                select t).ToArray();

            UnitTest.Test(result.Length == 2);
            UnitTest.Test(result[0] == texts[0]);
            UnitTest.Test(result[1] == texts[1]);
        }

        private static void TestAccessObserverOpenWriteStream()
        {
            string fileName = VirtualDrive.VirtualFileName("myFile0.bin");

            bool didOpenCalledBack = false;
            bool didCloseCalledBack = false;

            EventHandler<AccessObserver.AccessObserverEventArgs> freeHandler = 
                delegate(object sender, AccessObserver.AccessObserverEventArgs args)
                {
                    UnitTest.Test(args.AffectedId == fileName);
                    UnitTest.Test(args.ObservedId == fileName);
                    UnitTest.Test(args.Type == AccessObserver.AccessType.Write);
                    UnitTest.Test(args.Request == AccessObserver.AccessRequest.FreeShared);

                    didCloseCalledBack = true;

                    VirtualDrive.ObserverFreeShared.Unregister(fileName);
                    UnitTest.Test(VirtualDrive.ObserverFreeShared.CountObservedIds == 0);
                };

            EventHandler<AccessObserver.AccessObserverEventArgs> lockHandler =
                delegate(object sender, AccessObserver.AccessObserverEventArgs args)
                {
                    UnitTest.Test(args.AffectedId == fileName);
                    UnitTest.Test(args.ObservedId == fileName);
                    UnitTest.Test(args.Type == AccessObserver.AccessType.Write);
                    UnitTest.Test(args.Request == AccessObserver.AccessRequest.LockExclusive);

                    didOpenCalledBack = true;

                    VirtualDrive.ObserverLockExclusive.Unregister(fileName);
                    UnitTest.Test(VirtualDrive.ObserverLockExclusive.CountObservedIds == 0);

                    VirtualDrive.ObserverFreeShared.Register(fileName, freeHandler);
                };

            VirtualDrive.ObserverLockExclusive.Register(fileName, lockHandler);

            UnitTest.Test(!didOpenCalledBack);
            UnitTest.Test(!didCloseCalledBack);

            using (Stream stream = VirtualDrive.OpenOutStream(fileName))
            {
                UnitTest.Test(didOpenCalledBack);
                UnitTest.Test(!didCloseCalledBack);
            }

            UnitTest.Test(didOpenCalledBack);
            UnitTest.Test(didCloseCalledBack);
        }
        private static void TestAccessObserverMoveFile()
        {
            bool didOpenCalledBack = false;
            bool didCloseCalledBack = false;

            string fileName0 = VirtualDrive.VirtualFileName(@"myFile0.bin");
            string fileName1 = VirtualDrive.VirtualFileName(@"myFile1.bin");

            VirtualDrive.Store(fileName0, new byte[] { });

            EventHandler<AccessObserver.AccessObserverEventArgs> freeHandler =
                delegate(object sender, AccessObserver.AccessObserverEventArgs args)
                {
                    UnitTest.Test(args.AffectedId == fileName0);
                    UnitTest.Test(args.ObservedId == fileName0);
                    UnitTest.Test(args.NewObservedId == fileName1);
                    UnitTest.Test(args.Type == AccessObserver.AccessType.Move);
                    UnitTest.Test(args.Request == AccessObserver.AccessRequest.FreeShared);

                    didCloseCalledBack = true;

                    VirtualDrive.ObserverFreeShared.Unregister(fileName0);
                    UnitTest.Test(VirtualDrive.ObserverFreeShared.CountObservedIds == 0);
                };

            EventHandler<AccessObserver.AccessObserverEventArgs> lockHandler =
                delegate(object sender, AccessObserver.AccessObserverEventArgs args)
                {
                    UnitTest.Test(args.AffectedId == fileName0);
                    UnitTest.Test(args.ObservedId == fileName0);
                    UnitTest.Test(args.NewObservedId == fileName0);
                    UnitTest.Test(args.Type == AccessObserver.AccessType.Move);
                    UnitTest.Test(args.Request == AccessObserver.AccessRequest.LockExclusive);

                    didOpenCalledBack = true;

                    VirtualDrive.ObserverLockExclusive.Unregister(fileName0);
                    UnitTest.Test(VirtualDrive.ObserverLockExclusive.CountObservedIds == 0);

                    VirtualDrive.ObserverFreeShared.Register(fileName0, freeHandler);
                };

            VirtualDrive.ObserverLockExclusive.Register(fileName0, lockHandler);

            UnitTest.Test(!didOpenCalledBack);
            UnitTest.Test(!didCloseCalledBack);
            VirtualDrive.MoveFile(fileName0, fileName1);
            UnitTest.Test(didOpenCalledBack);
            UnitTest.Test(didCloseCalledBack);

            VirtualDrive.DeleteFile(fileName1);
        }
        private static void TestAccessObserverMoveDir()
        {
            bool didOpenCalledBack = false;
            bool didCloseCalledBack = false;

            string folder0 = VirtualDrive.VirtualFileName("myFolder0");
            string folder1 = VirtualDrive.VirtualFileName("myFolder1");
            string fileName0 = VirtualDrive.VirtualFileName(@"myFolder0\myFile0.bin");
            string fileName1 = VirtualDrive.VirtualFileName(@"myFolder0\myFile1.bin");

            VirtualDrive.Store(fileName0, new byte[] { });
            VirtualDrive.Store(fileName1, new byte[] { });

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));

            EventHandler<AccessObserver.AccessObserverEventArgs> freeHandler =
                delegate(object sender, AccessObserver.AccessObserverEventArgs args)
                {
                    UnitTest.Test(args.AffectedId == folder0);
                    UnitTest.Test(args.ObservedId == fileName0);
                    UnitTest.Test(args.NewObservedId == VirtualDrive.VirtualFileName(@"myFolder1\myFile0.bin"));
                    UnitTest.Test(args.Type == AccessObserver.AccessType.Move);
                    UnitTest.Test(args.Request == AccessObserver.AccessRequest.FreeShared);

                    didCloseCalledBack = true;

                    VirtualDrive.ObserverFreeShared.Unregister(fileName0);
                    UnitTest.Test(VirtualDrive.ObserverFreeShared.CountObservedIds == 0);
                };

            EventHandler<AccessObserver.AccessObserverEventArgs> lockHandler =
                delegate(object sender, AccessObserver.AccessObserverEventArgs args)
                {
                    UnitTest.Test(args.AffectedId == folder0);
                    UnitTest.Test(args.ObservedId == fileName0);
                    UnitTest.Test(args.NewObservedId == fileName0);
                    UnitTest.Test(args.Type == AccessObserver.AccessType.Move);
                    UnitTest.Test(args.Request == AccessObserver.AccessRequest.LockExclusive);

                    didOpenCalledBack = true;

                    VirtualDrive.ObserverLockExclusive.Unregister(fileName0);
                    UnitTest.Test(VirtualDrive.ObserverLockExclusive.CountObservedIds == 0);

                    VirtualDrive.ObserverFreeShared.Register(fileName0, freeHandler);
                };

            VirtualDrive.ObserverLockExclusive.Register(fileName0, lockHandler);


            UnitTest.Test(!didOpenCalledBack);
            UnitTest.Test(!didCloseCalledBack);
            VirtualDrive.MoveDirectory(folder0, folder1);
            UnitTest.Test(didOpenCalledBack);
            UnitTest.Test(didCloseCalledBack);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));

            VirtualDrive.DeleteDirectory(VirtualDrive.VirtualFileName(folder1), true);

            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder1));
        }

        private static void TestGetDirectories()
        {
            string[] fileNames = new string[]
            {
                VirtualDrive.VirtualFileName(@"TestGetDirectories\TestDir0\Data0.bin"),
                VirtualDrive.VirtualFileName(@"TestGetDirectories\TestDir0\Data1.bin"),
                VirtualDrive.VirtualFileName(@"TestGetDirectories\TestDir1\Data0.bin"),
                VirtualDrive.VirtualFileName(@"TestGetDirectories\TestDir1\Data1.bin")
            };
            foreach (var file in fileNames)
            {
                VirtualDrive.Store(file, new byte[] {});
            }

            string[] dirs = VirtualDrive.GetDirectories(VirtualDrive.VirtualFileName(@"TestGetDirectories"));
            UnitTest.Test(dirs.Length == 2);
            UnitTest.Test(dirs[0] == VirtualDrive.VirtualFileName(@"TestGetDirectories\TestDir0"));
            UnitTest.Test(dirs[1] == VirtualDrive.VirtualFileName(@"TestGetDirectories\TestDir1"));

            foreach (var file in fileNames)
            {
                VirtualDrive.DeleteFile(file);
            }
        }

        private static void TestGetFiles()
        {
            string[] fileNames = new string[]
            {
                VirtualDrive.VirtualFileName(@"TestGetFiles\TestDir0\Data0.bin"),
                VirtualDrive.VirtualFileName(@"TestGetFiles\TestDir0\Data1.bin"),
                VirtualDrive.VirtualFileName(@"TestGetFiles\TestDir1\Data2.bin"),
                VirtualDrive.VirtualFileName(@"TestGetFiles\TestDir1\Data3.bin")
            };
            foreach (var file in fileNames)
            {
                VirtualDrive.Store(file, new byte[] { });
            }

            string[] files0 = VirtualDrive.GetFiles(
                VirtualDrive.VirtualFileName(@"TestGetFiles"), "*.bin");

            UnitTest.Test(files0.Length == 0);


            string[] files1 = VirtualDrive.GetFiles(
                VirtualDrive.VirtualFileName(@"TestGetFiles\TestDir0"), "*.bin");
            UnitTest.Test(files1.Length == 2);
            UnitTest.Test(files1[0] == fileNames[0]);
            UnitTest.Test(files1[1] == fileNames[1]);

            string[] files2 = VirtualDrive.GetFiles(
                VirtualDrive.VirtualFileName(@"TestGetFiles\TestDir1"), "*.bin");
            UnitTest.Test(files2.Length == 2);
            UnitTest.Test(files2[0] == fileNames[2]);
            UnitTest.Test(files2[1] == fileNames[3]);


            foreach (var file in fileNames)
            {
                VirtualDrive.DeleteFile(file);
            }
        }
    }
}
