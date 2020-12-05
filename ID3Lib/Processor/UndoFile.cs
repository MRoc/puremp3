using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ID3.IO;
using ID3.Utils;
using CoreUtils;
using CoreTest;
using CoreVirtualDrive;
using CoreLogging;

namespace ID3.Processor
{
    public class UndoFileMessage : IProcessorMessage
    {
        public UndoFileMessage()
        {
        }
        public UndoFileMessage(UndoFileWriter writer)
        {
            UndoFile = writer;
        }

        public bool DryRun { get; set; }
        public UndoFileWriter UndoFile { get; set; }
    }
     
    public class UndoFilePlayer : IDisposable
    {
        public enum Direction
        {
            Do,
            Undo
        };

        public static void Undo(string fileName)
        {
            UndoFilePlayer r = new UndoFilePlayer(fileName);
            r.Process(Direction.Undo);
            r.Close();
        }
        public static void Redo(string fileName)
        {
            UndoFilePlayer r = new UndoFilePlayer(fileName);
            r.Process(Direction.Do);
            r.Close();
        }

        public UndoFilePlayer(string fileName)
        {
            Reader = new UndoFileReader(fileName);
        }
        public void Dispose()
        {
            Close();
        }

        public void Process(Direction direction)
        {
            switch (direction)
            {
                case Direction.Undo: Undo(); break;
                case Direction.Do: Redo(); break;
                default: throw new Exception("Unknown direction");
            }
        }

        private volatile bool abort;
        public virtual bool Abort
        {
            get
            {
                return abort;
            }
            set
            {
                abort = value;
            }
        }

        private void Undo()
        {
            Logger.WriteLine(Tokens.InfoVerbose, "UNDO: " + Reader.Filename);

            int numCommands = Reader.NumCommands();

            if ((numCommands % 2) != 0)
            {
                throw new Exception("Invalid undo file (#Commands=" + numCommands + ")");
            }

            for (int i = 0; i < numCommands / 2; i++)
            {
                int commandIndex = (numCommands - 2) - (2 * i);
                Debug.Assert((commandIndex % 2) == 0);

                ProcessCommand(Reader.CommandByIndex(commandIndex));
            }
        }
        private void Redo()
        {
            Logger.WriteLine(Tokens.InfoVerbose, "REDO: " + Reader.Filename);

            int numCommands = Reader.NumCommands();

            if ((numCommands % 2) != 0)
            {
                throw new Exception("Invalid undo file (#Commands=" + numCommands + ")");
            }

            for (int i = 0; i < numCommands / 2; i++)
            {
                int commandIndex = 2 * i + 1;
                Debug.Assert((commandIndex % 2) == 1);

                ProcessCommand(Reader.CommandByIndex(commandIndex));
            }
        }
        private void ProcessCommand(SerializedCommand command)
        {
            Logger.WriteLine(Tokens.Info, command.Direction + " " + command.Data[0]);

            Type type = Type.GetType(command.Target);
            MethodInfo method = type.GetMethod("ProcessCommand");

            method.Invoke(null, new object[] { command });
        }
        private UndoFileReader Reader { get; set; }
        public void Close()
        {
            if (Reader != null)
            {
                Reader.Close();
                Reader = null;
            }
        }
    }

    public class SerializedCommand
    {
        public static readonly char Marker = '>';
        public static readonly char Separator = '/';

        public enum UndoRedo
        {
            Do,
            Undo
        }
        public SerializedCommand(StreamReader reader)
        {
            Read(reader);
        }
        public SerializedCommand(Type target, UndoRedo direction, string op, string[] data)
        {
            Target = target.FullName;
            Direction = direction;
            Operation = op;
            Data = data;
        }

        public void Write(StreamWriter writer)
        {
            writer.Write(Marker);
            writer.Write(Separator);
            writer.Write(Target);
            writer.Write(Separator);
            writer.Write(Direction.ToString());
            writer.Write(Separator);
            writer.Write(Operation);
            writer.Write("\n");

            foreach (var line in data)
            {
                writer.WriteLine(line);
            }
        }
        public void Read(StreamReader reader)
        {
            string[] cmdLine = reader.ReadLine().Split(Separator);

            if (cmdLine.Length < 4)
            {
                throw new Exception("Invalid command found!");
            }
            if (cmdLine[0][0] != Marker)
            {
                throw new Exception("Command does not start with marker");
            }

            Target = cmdLine[1];
            Direction = cmdLine[2].ToEnum<UndoRedo>();
            Operation = cmdLine[3];

            List<string> lines = new List<string>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length > 0 && line[0] == Marker)
                {
                    break;
                }

                lines.Add(line);
            }

            Data = lines.ToArray();
        }

        public string Target
        {
            get
            {
                return target;
            }
            private set
            {
                if (value.Contains(Marker) || value.Contains(Separator))
                {
                    throw new Exception("Target contains invalid characters");
                }

                target = value;
            }
        }
        public UndoRedo Direction
        {
            get
            {
                return direction;
            }
            private set
            {
                direction = value;
            }
        }
        public string Operation
        {
            get
            {
                return operation;
            }
            private set
            {
                if (value.Contains(Marker) || value.Contains(Separator))
                {
                    throw new Exception("Operation contains invalid characters");
                }

                operation = value;
            }
        }
        public T OperationEnum<T>()
        {
            return Operation.ToEnum<T>();
        }
        public string[] Data
        {
            get
            {
                return data;
            }
            private set
            {
                if (!Object.ReferenceEquals(value, null))
                {
                    foreach (var line in value)
                    {
                        if (line.Contains(Marker))
                        {
                            throw new Exception("Data can not contain '" + Marker + "'");
                        }
                        if (line.Contains("\n"))
                        {
                            throw new Exception("Data can not contain newline");
                        }
                    }
                }

                data = value;
            }
        }
        
        private string target;
        private UndoRedo direction;
        private string operation;
        private string[] data;

        private SerializedCommand()
        {
        }
    }
    public class UndoFileWriter : IDisposable
    {
        public UndoFileWriter()
        {
        }
        public UndoFileWriter(string fileName)
        {
            Open(fileName);
        }

        public void Open(string fileName)
        {
            Close();

            FileName = fileName;
            Writer = new StreamWriter(VirtualDrive.OpenOutStream(FileName));
        }
        public void Close()
        {
            if (Writer != null)
            {
                Writer.Close();
                Writer = null;
                FileName = null;
            }
        }

        public void Write(SerializedCommand command)
        {
            command.Write(Writer);
            Writer.Flush();
        }

        private StreamWriter Writer { get; set; }
        private string FileName { get; set; }

        public void Dispose()
        {
            Close();
        }
    }
    public class UndoFileReader
    {
        public UndoFileReader()
        {
        }
        public UndoFileReader(string fileName)
        {
            Open(fileName);
        }

        public void Open(string fileName)
        {
            Close();

            Reader = new StreamReader(VirtualDrive.OpenInStream(fileName));
            Filename = fileName;
        }
        public void Close()
        {
            commandByteOffset = null;

            if (Reader != null)
            {
                Reader.Close();
                Reader = null;
            }
        }

        public string Filename
        {
            get;
            private set;
        }

        public int NumCommands()
        {
            return CommandByteOffset.Length;
        }
        public SerializedCommand CommandByIndex(int index)
        {
            Seek(CommandByteOffset[index], SeekOrigin.Begin);

            return ReadCommand();
        }

        private StreamReader Reader { get; set; }
        private SerializedCommand ReadCommand()
        {
            return new SerializedCommand(Reader);
        }
        private void Seek(long position, SeekOrigin origin)
        {
            Reader.BaseStream.Seek(position, origin);
            Reader.DiscardBufferedData();
        }
        private int ReadByte()
        {
            return Reader.BaseStream.ReadByte();
        }

        private int[] commandByteOffset;
        private int[] CommandByteOffset
        {
            get
            {
                if (commandByteOffset == null)
                    commandByteOffset = CreateCommandByteOffsets();

                return commandByteOffset;
            }
        }
        private int[] CreateCommandByteOffsets()
        {
            List<int> result = new List<int>();

            Seek(0, SeekOrigin.Begin);

            int counter = 0;
            int b = -1;
            while ((b = ReadByte()) != -1)
            {
                if (b == '>')
                {
                    result.Add(counter);
                }

                counter++;
            }

            return result.ToArray();
        }
    }

    public static class UndoFile
    {
        public static readonly string undoFileSuffix = "udf";
        public static readonly string defaultUndoFileName = "d:\\undofile.udf";

        public static void DeleteAllUndoFiles(string folder)
        {
            foreach (var file in VirtualDrive.GetFiles(folder, "*." + undoFileSuffix))
            {
                VirtualDrive.DeleteFile(file);
            }
        }
        public static string FindCurrentUndoFileName(string folder)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(folder);
            sb.Append(System.IO.Path.DirectorySeparatorChar);
            sb.Append(FindCurrentUndoFileIndex(folder));
            sb.Append(".");
            sb.Append(undoFileSuffix);

            return sb.ToString();
        }
        public static string FindNextUndoFileName(string folder)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(folder);
            sb.Append(System.IO.Path.DirectorySeparatorChar);
            sb.Append(FindNextUndoFileIndex(folder));
            sb.Append(".");
            sb.Append(undoFileSuffix);

            return sb.ToString();
        }
        private static int FindNextUndoFileIndex(string folder)
        {
            List<int> numbers = new List<int>();

            VirtualDrive.GetFiles(folder, "*." + undoFileSuffix).ForEach(
                (n) => numbers.Add(Int32.Parse(new FileInfo(n).Name.Split('.')[0])));

            if (numbers.Count > 0)
            {
                return numbers.Max() + 1;
            }
            else
            {
                return 0;
            }
        }
        private static int FindCurrentUndoFileIndex(string folder)
        {
            List<int> numbers = new List<int>();

            VirtualDrive.GetFiles(folder, "*." + undoFileSuffix).ForEach(
                (n) => numbers.Add(Int32.Parse(new FileInfo(n).Name.Split('.')[0])));

            if (numbers.Count > 0)
            {
                return numbers.Max();
            }
            else
            {
                return -1;
            }
        }
    }

    public class TestUndoFile
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestUndoFile));
        }

        private static void TestUndoFileWriter()
        {
            UndoFileWriter writer = new UndoFileWriter(
                VirtualDrive.VirtualFileName("TestUndoFileWriter.txt"));

            for (int i = 0; i < 4; i++)
            {
                List<string> parameters = new List<string>();
                for (int j = 0; j < i; j++)
                {
                    parameters.Add("line" + j);
                }

                writer.Write(new SerializedCommand(
                    typeof(TestClass),
                    SerializedCommand.UndoRedo.Do,
                    "TestUndoFileWriter",
                    parameters.ToArray()));
            }

            writer.Close();

            UnitTest.Test(VirtualDrive.FileLength(
                VirtualDrive.VirtualFileName("TestUndoFileWriter.txt")) > 0);
        }
        private static void TestUndoFileReader()
        {
            UndoFileReader reader = new UndoFileReader(
                VirtualDrive.VirtualFileName("TestUndoFileWriter.txt"));

            UnitTest.Test(reader.NumCommands() == 4);

            for (int i = 3; i >= 0; i--)
            {
                SerializedCommand cmd = reader.CommandByIndex(i);
                UnitTest.Test(cmd.Target == typeof(TestClass).FullName);
                UnitTest.Test(cmd.Operation == "TestUndoFileWriter");

                UnitTest.Test(cmd.Data.Length == i);
                for (int j = 0; j < i; j++)
                {
                    UnitTest.Test(cmd.Data[j] == "line" + j);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                SerializedCommand cmd = reader.CommandByIndex(i);
                UnitTest.Test(cmd.Target == typeof(TestClass).FullName);
                UnitTest.Test(cmd.Operation == "TestUndoFileWriter");

                UnitTest.Test(cmd.Data.Length == i);
                for (int j = 0; j < i; j++)
                {
                    UnitTest.Test(cmd.Data[j] == "line" + j);
                }
            }

            reader.Close();
        }

        private class TestClass
        {
        }
    }
}
