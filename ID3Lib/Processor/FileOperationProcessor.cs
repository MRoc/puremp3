using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.IO;
using ID3.Utils;
using CoreTest;
using CoreVirtualDrive;
using CoreLogging;

namespace ID3.Processor
{
    public class FileOperationProcessor : IProcessorMutable
    {
        public enum FileOperation
        {
            Copy,
            Move,
            Recycle,
        }
        public enum ConflictSolving
        {
            Skip,
            Overwrite
        }

        public class Message : IProcessorMessage
        {
            public Message(string newName, FileOperation operation)
            {
                NewName = newName;
                Operation = operation;
            }
            public Message(string newName, FileOperation operation, ConflictSolving confilctSolving)
                : this(newName, operation)
            {
                Conflicts = confilctSolving;
            }
            public string NewName { get; set; }
            public FileOperation Operation { get; set; }
            public ConflictSolving Conflicts { get; set; }
        }

        public FileOperationProcessor()
        {
        }
        public FileOperationProcessor(FileOperation operation)
        {
            Operation = operation;
        }

        public UndoFileWriter UndoFile { get; set; }
        public string NewName { get; set; }
        public FileOperation Operation { get; set; }
        public ConflictSolving Conflicts { get; set; }

        public Type[] SupportedClasses()
        {
            return new Type[] { typeof(FileInfo), typeof(DirectoryInfo) };
        }
        public virtual void Process(object obj)
        {
            SerializedCommand cmd = CreateDoCommand(obj);

            if (Operation == FileOperation.Copy || Operation == FileOperation.Move)
            {
                if (String.IsNullOrEmpty(NewName))
                {
                    throw new Exception(Operation.ToString() + " invalid filename: \"" + NewName + "\"");
                }

                string src = cmd.Data[0];
                string dst = cmd.Data[1];

                if (src.Equals(dst))
                {
                    return;
                }

                if (AlreadyExists(src, dst))
                {
                    if (Conflicts == ConflictSolving.Skip)
                    {
                        Logger.WriteLine(Tokens.Warning, Operation.ToString() + " already exists: \"" + dst + "\"");
                        return;
                    }
                    else if (Conflicts == ConflictSolving.Overwrite)
                    {
                        MoveToRecycleBin(dst);
                    }
                }
            }

            ProcessCommand(cmd);

            if (!Object.ReferenceEquals(UndoFile, null))
            {
                UndoFile.Write(CreateUndoCommand(cmd));
                UndoFile.Write(cmd);
            }
        }
        public virtual void ProcessMessage(IProcessorMessage message)
        {
            if (message is UndoFileMessage)
            {
                UndoFile = (message as UndoFileMessage).UndoFile;
            }
            if (message is Message)
            {
                NewName = (message as Message).NewName;
                Operation = (message as Message).Operation;
                Conflicts = (message as Message).Conflicts;
            }
        }
        public IEnumerable<IProcessor> Processors
        {
            get
            {
                return ProcessorUtils.Empty;
            }
        }

        private void MoveToRecycleBin(string id)
        {
            SerializedCommand cmd;

            if (VirtualDrive.ExistsDirectory(id))
            {
                cmd = CreateDeleteCommand(new DirectoryInfo(id));
            }
            else if (VirtualDrive.ExistsFile(id))
            {
                cmd = CreateDeleteCommand(new FileInfo(id));
            }
            else
            {
                throw new Exception("\"" + id + "\" does not exist!");
            }

            ProcessCommand(cmd);

            if (!Object.ReferenceEquals(UndoFile, null))
            {
                UndoFile.Write(CreateUndoCommand(cmd));
                UndoFile.Write(cmd);
            }
        }

        public static void ProcessCommand(SerializedCommand command)
        {
            if (command.OperationEnum<FileOperation>() == FileOperation.Recycle)
            {
                Logger.WriteLine(
                    Tokens.Info,
                    command.OperationEnum<FileOperation>().ToString()
                    + " \"" + command.Data[0]
                    + "\"");
            }
            else
            {
                Logger.WriteLine(
                    Tokens.Info,
                    command.OperationEnum<FileOperation>().ToString()
                    + " \"" + command.Data[0]
                    + "\" to \"" + command.Data[1]
                    + "\"");
            }

            switch (command.Direction)
            {
                case SerializedCommand.UndoRedo.Do:

                    switch (command.OperationEnum<FileOperation>())
                    {
                        case FileOperation.Copy: ProcessCopy(command); break;
                        case FileOperation.Move: ProcessMove(command); break;
                        case FileOperation.Recycle: ProcessRecycle(command); break;
                        default: throw new NotSupportedException("Unknown operation");
                    }
                    break;

                case SerializedCommand.UndoRedo.Undo:

                    switch (command.OperationEnum<FileOperation>())
                    {
                        case FileOperation.Copy: ProcessDelete(command); break;
                        case FileOperation.Move: ProcessMove(command); break;
                        case FileOperation.Recycle: ProcessDerecycle(command); break;
                        default: throw new NotSupportedException("Unknown operation");
                    }
                    break;

                default:
                    throw new NotSupportedException("Unknown direction");
            }
        }
        private static void ProcessMove(SerializedCommand command)
        {
            if (VirtualDrive.ExistsDirectory(command.Data[0]))
            {  
                //CoreVirtualDrive.FileSystemOperations.SafeOperations.MoveDirectory(command.Data[0], command.Data[1]);
                VirtualDrive.MoveDirectory(command.Data[0], command.Data[1]);
            }
            else if (VirtualDrive.ExistsFile(command.Data[0]))
            {
                //CoreVirtualDrive.FileSystemOperations.SafeOperations.MoveFile(command.Data[0], command.Data[1]);
                VirtualDrive.MoveFile(command.Data[0], command.Data[1]);
            }
            else
            {
                throw new Exception("\"" + command.Data[0] + "\" does not exist!");
            }
        }
        private static void ProcessCopy(SerializedCommand command)
        {
            if (VirtualDrive.ExistsDirectory(command.Data[0]))
            {
                //CoreVirtualDrive.FileSystemOperations.SafeOperations.CopyDirectory(command.Data[0], command.Data[1]);
                VirtualDrive.CopyDirectory(command.Data[0], command.Data[1]);
            }
            else if (VirtualDrive.ExistsFile(command.Data[0]))
            {
                //CoreVirtualDrive.FileSystemOperations.SafeOperations.CopyFile(command.Data[0], command.Data[1]);
                VirtualDrive.CopyFile(command.Data[0], command.Data[1]);
            }
            else
            {
                throw new Exception("\"" + command.Data[0] + "\" does not exist!");
            }
        }
        private static void ProcessDelete(SerializedCommand command)
        {
            if (VirtualDrive.ExistsDirectory(command.Data[0]))
            {
                VirtualDrive.DeleteDirectory(command.Data[0], true);
            }
            else if (VirtualDrive.ExistsFile(command.Data[0]))
            {
                VirtualDrive.DeleteFile(command.Data[0]);
            }
            else
            {
                throw new Exception("\"" + command.Data[0] + "\" does not exist!");
            }
        }
        private static void ProcessRecycle(SerializedCommand command)
        {
            CoreVirtualDrive.RecycleBin.Instance.MoveToRecycleBin(command.Data[0]);
        }
        private static void ProcessDerecycle(SerializedCommand command)
        {
            CoreVirtualDrive.RecycleBin.Instance.Restore(command.Data[0]);
        }
        private static bool AlreadyExists(string src, string dst)
        {
            if (VirtualDrive.ExistsDirectory(src))
            {
                if (VirtualDrive.ExistsDirectory(dst)
                    && !src.Equals(dst, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            else
            {
                if (VirtualDrive.ExistsFile(dst)
                    && !src.Equals(dst, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private SerializedCommand CreateDoCommand(object obj)
        {
            SerializedCommand cmd = null;

            if (Operation == FileOperation.Copy || Operation == FileOperation.Move)
            {
                if (obj is FileInfo)
                {
                    var fileInfo = obj as FileInfo;

                    cmd = CreateCommand(
                        SerializedCommand.UndoRedo.Do,
                        Operation.ToString(),
                        fileInfo.FullName,
                        Path.Combine(fileInfo.DirectoryName, NewName));
                }
                else if (obj is DirectoryInfo)
                {
                    var directoryInfo = obj as DirectoryInfo;

                    cmd = CreateCommand(
                        SerializedCommand.UndoRedo.Do,
                        Operation.ToString(),
                        directoryInfo.FullName,
                        NewName);
                }
                else
                {
                    throw new NotSupportedException("Unknown type");
                }
            }
            else if (Operation == FileOperation.Recycle)
            {
                cmd = CreateDeleteCommand(obj);
            }

            return cmd;
        }
        private SerializedCommand CreateDeleteCommand(object obj)
        {
            SerializedCommand cmd = null;

            if (obj is FileInfo)
            {
                var fileInfo = obj as FileInfo;

                cmd = CreateCommand(
                    SerializedCommand.UndoRedo.Do,
                    FileOperation.Recycle.ToString(),
                    fileInfo.FullName);
            }
            else if (obj is DirectoryInfo)
            {
                var directoryInfo = obj as DirectoryInfo;

                cmd = CreateCommand(
                    SerializedCommand.UndoRedo.Do,
                    FileOperation.Recycle.ToString(),
                    directoryInfo.FullName);
            }
            else
            {
                throw new NotSupportedException("\"" + obj.GetType().Name + "\" is not supported");
            }

            return cmd;
        }
        private SerializedCommand CreateCommand(
            SerializedCommand.UndoRedo undoRedo,
            string cmd,
            string from,
            string to)
        {
            return new SerializedCommand(
                GetType(),
                undoRedo,
                cmd,
                new string[] { from, to });
        }
        private SerializedCommand CreateCommand(
            SerializedCommand.UndoRedo undoRedo,
            string cmd,
            string file)
        {
            return new SerializedCommand(
                GetType(),
                undoRedo,
                cmd,
                new string[] { file });
        }
        private SerializedCommand CreateUndoCommand(SerializedCommand doCommand)
        {
            return new SerializedCommand(
                GetType(),
                SerializedCommand.UndoRedo.Undo,
                doCommand.Operation.ToString(),
                doCommand.Data.Reverse().ToArray());
        }
    }

    public class TestFileCopyProcessor
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestFileCopyProcessor));
        }
        private static void TestMoveFile()
        {
            string undoFileName = VirtualDrive.VirtualFileName(
                "TestFileCopyProcessorUndoFile.txt");

            string folder0 = VirtualDrive.VirtualFileName("folder0");
            string filename0 = Path.Combine(folder0, "t0.bin");
            string filename1 = Path.Combine(folder0, "t1.bin");

            byte[] data = new byte[] { 0 };
            VirtualDrive.Store(filename0, data);

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename1));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Move;
                p.UndoFile = undoFileWriter;
                p.NewName = "t1.bin";

                p.Process(new FileInfo(filename0));
            }

            UnitTest.Test(!VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));

            UndoFilePlayer.Undo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename1));

            UndoFilePlayer.Redo(undoFileName);

            UnitTest.Test(!VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));

            VirtualDrive.DeleteDirectory(folder0, true);
        }
        private static void TestMoveDir()
        {
            string undoFileName = VirtualDrive.VirtualFileName(
                "TestFileCopyProcessorUndoFile.txt");

            string srcDir = VirtualDrive.VirtualFileName(@"srcFolder\folder0");
            string srcFile0 = Path.Combine(srcDir, "t0.bin");
            string srcFile1 = Path.Combine(srcDir, "t1.bin");

            string dstDir = VirtualDrive.VirtualFileName(@"dstFolder\folder1");
            string dstFile0 = Path.Combine(dstDir, "t0.bin");
            string dstFile1 = Path.Combine(dstDir, "t1.bin");

            VirtualDrive.Store(VirtualDrive.VirtualFileName(srcFile0), null);
            VirtualDrive.Store(VirtualDrive.VirtualFileName(srcFile1), null);

            UnitTest.Test(VirtualDrive.ExistsDirectory(srcDir));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(dstDir));
            UnitTest.Test(VirtualDrive.ExistsFile(srcFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(srcFile1));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Move;
                p.UndoFile = undoFileWriter;
                p.NewName = dstDir;

                p.Process(new DirectoryInfo(srcDir));
            }

            UnitTest.Test(!VirtualDrive.ExistsDirectory(srcDir));
            UnitTest.Test(VirtualDrive.ExistsDirectory(dstDir));
            UnitTest.Test(VirtualDrive.ExistsFile(dstFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(dstFile1));

            UndoFilePlayer.Undo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsDirectory(srcDir));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(dstDir));
            UnitTest.Test(VirtualDrive.ExistsFile(srcFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(srcFile1));
            UnitTest.Test(!VirtualDrive.ExistsFile(dstFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(dstFile1));

            UndoFilePlayer.Redo(undoFileName);

            UnitTest.Test(!VirtualDrive.ExistsDirectory(srcDir));
            UnitTest.Test(VirtualDrive.ExistsDirectory(dstDir));
            UnitTest.Test(!VirtualDrive.ExistsFile(srcFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(srcFile1));
            UnitTest.Test(VirtualDrive.ExistsFile(dstFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(dstFile1));

            VirtualDrive.DeleteDirectory(dstDir, true);
        }
        private static void TestCopyFile()
        {
            string undoFileName = VirtualDrive.VirtualFileName(
                "TestFileCopyProcessorUndoFile.txt");

            string folder0 = VirtualDrive.VirtualFileName("folder0");
            string filename0 = Path.Combine(folder0, "t0.bin");
            string filename1 = Path.Combine(folder0, "t1.bin");

            byte[] data = new byte[] { 0 };
            VirtualDrive.Store(filename0, data);

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename1));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Copy;
                p.UndoFile = undoFileWriter;
                p.NewName = "t1.bin";

                p.Process(new FileInfo(filename0));
            }

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));

            UndoFilePlayer.Undo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename1));

            UndoFilePlayer.Redo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));

            VirtualDrive.DeleteDirectory(folder0, true);
        }
        private static void TestCopyDir()
        {
            string undoFileName = VirtualDrive.VirtualFileName(
                "TestFileCopyProcessorUndoFile.txt");

            string folder0 = VirtualDrive.VirtualFileName("folder0");
            string filename00 = Path.Combine(folder0, "t0.bin");
            string filename01 = Path.Combine(folder0, "t1.bin");

            string folder1 = VirtualDrive.VirtualFileName("folder1");
            string filename10 = Path.Combine(folder1, "t0.bin");
            string filename11 = Path.Combine(folder1, "t1.bin");

            VirtualDrive.Store(VirtualDrive.VirtualFileName(filename00), null);
            VirtualDrive.Store(VirtualDrive.VirtualFileName(filename01), null);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Copy;
                p.UndoFile = undoFileWriter;
                p.NewName = folder1;

                p.Process(new DirectoryInfo(folder0));
            }

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(VirtualDrive.ExistsFile(filename11));

            UndoFilePlayer.Undo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename11));

            UndoFilePlayer.Redo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(VirtualDrive.ExistsFile(filename11));

            VirtualDrive.DeleteDirectory(folder0, true);
            VirtualDrive.DeleteDirectory(folder1, true);
        }

        private static void TestMoveDir_DifferentParent()
        {
            string undoFileName = VirtualDrive.VirtualFileName(
                "TestFileCopyProcessorUndoFile.txt");

            string folder0 = VirtualDrive.VirtualFileName(@"fold00\folder0");
            string filename00 = Path.Combine(folder0, "t0.bin");
            string filename01 = Path.Combine(folder0, "t1.bin");

            string folder1 = VirtualDrive.VirtualFileName(@"fold01\folder1");
            string filename10 = Path.Combine(folder1, "t0.bin");
            string filename11 = Path.Combine(folder1, "t1.bin");

            VirtualDrive.Store(VirtualDrive.VirtualFileName(filename00), null);
            VirtualDrive.Store(VirtualDrive.VirtualFileName(filename01), null);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Move;
                p.UndoFile = undoFileWriter;
                p.NewName = folder1;

                p.Process(new DirectoryInfo(folder0));
            }

            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(VirtualDrive.ExistsFile(filename11));

            VirtualDrive.DeleteDirectory(folder1, true);
        }

        private static void TestMoveDir_AlreadyExists_Skip()
        {
            string undoFileName = VirtualDrive.VirtualFileName(
                "TestMoveDir_AlreadyExists_Skip.txt");

            string folder0 = VirtualDrive.VirtualFileName(@"fold00\folder0");
            string filename00 = Path.Combine(folder0, "t0.bin");
            string filename01 = Path.Combine(folder0, "t1.bin");

            string folder1 = VirtualDrive.VirtualFileName(@"fold01\folder1");
            string filename10 = Path.Combine(folder1, "t0.bin");
            string filename11 = Path.Combine(folder1, "t1.bin");

            VirtualDrive.Store(VirtualDrive.VirtualFileName(filename00), null);
            VirtualDrive.Store(VirtualDrive.VirtualFileName(filename01), null);
            VirtualDrive.Store(VirtualDrive.VirtualFileName(filename10), null);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename11));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Move;
                p.UndoFile = undoFileWriter;
                p.NewName = folder1;
                p.Conflicts = FileOperationProcessor.ConflictSolving.Skip;

                p.Process(new DirectoryInfo(folder0));
            }

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename11));

            UndoFilePlayer.Undo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename11));

            UndoFilePlayer.Redo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(filename00));
            UnitTest.Test(VirtualDrive.ExistsFile(filename01));
            UnitTest.Test(VirtualDrive.ExistsFile(filename10));
            UnitTest.Test(!VirtualDrive.ExistsFile(filename11));

            VirtualDrive.DeleteDirectory(folder0, true);
            VirtualDrive.DeleteDirectory(folder1, true);
        }
        private static void TestMoveDir_AlreadyExists_Overwrite()
        {
            SetupRecycleBin();

            string undoFileName = VirtualDrive.VirtualFileName(
                "TestFileCopyProcessorUndoFile.txt");

            string folder0 = VirtualDrive.VirtualFileName(@"fold00\folder0");
            string folder1 = VirtualDrive.VirtualFileName(@"fold01\folder1");

            string srcFile0 = Path.Combine(folder0, "t0.bin");
            string replaceFile0 = Path.Combine(folder1, "t1.bin");
            string dstFile0 = Path.Combine(folder1, "t0.bin");

            string recycleBin = VirtualDrive.VirtualFileName(@"recycle");
            string recycleFile0 = Path.Combine(Path.Combine(recycleBin, "0"), "t1.bin");
            
            VirtualDrive.Store(VirtualDrive.VirtualFileName(srcFile0), null);
            VirtualDrive.Store(VirtualDrive.VirtualFileName(replaceFile0), null);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(srcFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(dstFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(replaceFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(recycleFile0));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Move;
                p.UndoFile = undoFileWriter;
                p.NewName = folder1;
                p.Conflicts = FileOperationProcessor.ConflictSolving.Overwrite;

                p.Process(new DirectoryInfo(folder0));
            }

            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(!VirtualDrive.ExistsFile(srcFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(dstFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(replaceFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(recycleFile0));

            UndoFilePlayer.Undo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(VirtualDrive.ExistsFile(srcFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(dstFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(replaceFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(recycleFile0));

            UndoFilePlayer.Redo(undoFileName);

            UnitTest.Test(!VirtualDrive.ExistsDirectory(folder0));
            UnitTest.Test(VirtualDrive.ExistsDirectory(folder1));
            UnitTest.Test(!VirtualDrive.ExistsFile(srcFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(dstFile0));
            UnitTest.Test(!VirtualDrive.ExistsFile(replaceFile0));
            UnitTest.Test(VirtualDrive.ExistsFile(recycleFile0));

            VirtualDrive.DeleteDirectory(folder1, true);
            VirtualDrive.DeleteDirectory(recycleBin, true);
        }

        private static void TestMoveFile_AlreadyExists_Overwrite()
        {
            SetupRecycleBin();

            string undoFileName = VirtualDrive.VirtualFileName(
                "TestMoveFile_AlreadyExists_Overwrite.txt");

            string folder0 = VirtualDrive.VirtualFileName("folder0");
            string filename0 = Path.Combine(folder0, "t0.bin");
            string filename1 = Path.Combine(folder0, "t1.bin");

            string recycleBin = VirtualDrive.VirtualFileName(@"recycle");
            string recycleFile0 = Path.Combine(recycleBin, "1.trash");

            byte[] data0 = new byte[] { 0 };
            VirtualDrive.Store(filename0, data0);

            byte[] data1 = new byte[] { 1 };
            VirtualDrive.Store(filename1, data1);

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));
            UnitTest.Test(!VirtualDrive.ExistsFile(recycleFile0));

            using (UndoFileWriter undoFileWriter = new UndoFileWriter(undoFileName))
            {
                FileOperationProcessor p = new FileOperationProcessor();
                p.Operation = FileOperationProcessor.FileOperation.Move;
                p.UndoFile = undoFileWriter;
                p.NewName = "t1.bin";
                p.Conflicts = FileOperationProcessor.ConflictSolving.Overwrite;

                p.Process(new FileInfo(filename0));
            }

            UnitTest.Test(!VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));
            UnitTest.Test(VirtualDrive.ExistsFile(recycleFile0));
            UnitTest.Test(VirtualDrive.Load(filename1).SequenceEqual(data0));

            UndoFilePlayer.Undo(undoFileName);

            UnitTest.Test(VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));
            UnitTest.Test(!VirtualDrive.ExistsFile(recycleFile0));

            UndoFilePlayer.Redo(undoFileName);

            UnitTest.Test(!VirtualDrive.ExistsFile(filename0));
            UnitTest.Test(VirtualDrive.ExistsFile(filename1));
            UnitTest.Test(VirtualDrive.ExistsFile(recycleFile0));
        }

        private static void SetupRecycleBin()
        {
            string recycleBin = VirtualDrive.VirtualFileName(@"recycle");
            RecycleBin.Instance.RootDir = recycleBin;
            VirtualDrive.Store(Path.Combine(recycleBin, "dummy.bin"), null);
        }
    }
}
