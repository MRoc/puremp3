using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using CoreControls;
using CoreControls.Threading;
using CoreDocument;
using ID3.Processor;
using ID3.Utils;
using CoreThreading;
using CoreVirtualDrive;
using CoreLogging;

namespace PureMp3.Model.Batch
{
    class BatchAction : IAtomicOperation, IWork
    {
        public BatchAction(BatchCommand command)
        {
            Title = command.DisplayName.ToString();
            RootDirectory = command.RootDir;
            Processor = command.Factory();
        }

        public string Title { get; set; }
        public int Id { get; set; }
        public string RootDirectory { get; set; }
        public ID3.Processor.IProcessorMutable Processor { get; set; }
        public EventHandler OnFinished { get; set; }

        public void Do()
        {
            Debug.Assert(Object.ReferenceEquals(Writer, null));
            Debug.Assert(Object.ReferenceEquals(Player, null));

            Writer = new UndoFileWriter(FindNextUndoFileName);

            Processor.ProcessMessage(new UndoFileMessage(Writer));

            WorkerThreadPool.Instance.StartWork(this);
        }
        public void Undo()
        {
            Debug.Assert(Object.ReferenceEquals(Writer, null));
            Debug.Assert(Object.ReferenceEquals(Player, null));

            Player = new UndoFilePlayer(UndoFileName);

            WorkerThreadPool.Instance.StartWork(this);
        }
        public bool IsValidForHistory
        {
            get
            {
                return true;
            }
        }

        public void Before()
        {
            TimeOfStart = DateTime.Now;

            if (!Object.ReferenceEquals(Writer, null))
            {
                Processor.ProcessMessage(new ProcessorMessageInit());
            }
        }
        public void Run()
        {
            if (!Processor.SupportedClasses().Contains(typeof(DirectoryInfo)))
            {
                throw new Exception(GetType().Name
                    + " can only be used with DirectoryInfo processors and with "
                    + Processor.GetType().Name);
            }

            Logger.WriteLine(Tokens.Info, Title + " --------------------------------------------------------------------------------------------------------------------------");

            Abort = false;

            if (!Object.ReferenceEquals(Writer, null))
            {
                try
                {
                    Processor.Process(new DirectoryInfo(RootDirectory));
                }
                catch (Exception e)
                {
                    Logger.WriteLine(Tokens.Exception, e);
                }
            }
            if (!Object.ReferenceEquals(Player, null))
            {
                try
                {
                    Player.Process(UndoFilePlayer.Direction.Undo);
                }
                catch (Exception e)
                {
                    Logger.WriteLine(Tokens.Exception, e);
                }
            }
        }
        public void After()
        {
            if (!Object.ReferenceEquals(Writer, null))
            {
                Processor.ProcessMessage(new ProcessorMessageExit());
                Writer.Close();
            }

            if (!Object.ReferenceEquals(Player, null))
            {
                Player.Close();
            }

            Writer = null;
            Player = null;

            Logger.WriteLine(Tokens.Info, "Time required total: " + (DateTime.Now - TimeOfStart).ToString());
            Logger.WriteLine(Tokens.Status, "Ready");

            if (OnFinished != null)
            {
                OnFinished(this, null);
            }
        }
        public bool Abort
        {
            get
            {
                if (!Object.ReferenceEquals(Writer, null))
                {
                    ProcessorMessageQueryAbort msg = new ProcessorMessageQueryAbort();
                    Processor.ProcessMessage(msg);
                    return msg.Abort;
                }
                if (!Object.ReferenceEquals(Player, null))
                {
                    return Player.Abort;
                }

                return false;
            }
            set
            {
                if (!Object.ReferenceEquals(Writer, null))
                {
                    Processor.ProcessMessage(new ProcessorMessageAbort(value));
                }
                if (!Object.ReferenceEquals(Player, null))
                {
                    Player.Abort = value;
                }
            }
        }
        public IWorkType Type
        {
            get
            {
                return IWorkType.AbortableLock;
            }
        }

        private string FindNextUndoFileName
        {
            get
            {
                UndoFileName = UndoFile.FindNextUndoFileName(App.AppUndoFolder);
                return UndoFileName;
            }
        }
        public static void DeleteUndoFiles()
        {
            UndoFile.DeleteAllUndoFiles(App.AppUndoFolder);
        }

        private UndoFileWriter Writer { get; set; }
        private UndoFilePlayer Player { get; set; }
        private string UndoFileName { get; set; }

        private DateTime TimeOfStart
        {
            get;
            set;
        }
    }
}
