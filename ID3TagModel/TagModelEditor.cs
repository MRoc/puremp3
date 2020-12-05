using System;
using System.IO;
using System.Windows.Input;
using CoreControls;
using CoreControls.Commands;
using CoreDocument;
using CoreThreading;
using ID3.IO;
using CoreVirtualDrive;
using CoreLogging;
using CoreDocument.Text;
using ID3;

namespace ID3TagModel
{
    public class TagModelEditor : DocNode
    {
        public class TransactionId
        {
            public TransactionId()
            {
                Id = -1;
            }
            public int CurrentId
            {
                get
                {
                    if (!HasId)
                    {
                        throw new Exception("No ID available");
                    }
                    return Id;
                }
            }

            public bool HasId
            {
                get
                {
                    return Id != -1;
                }
            }

            public void Start()
            {
                if (Counter == 0 && HasId)
                {
                    throw new Exception("TransactionIdWrapper Start failed");
                }

                if (Counter == 0)
                {
                    Id = History.Instance.NextTransactionId();
                }

                Counter++;
            }
            public void End()
            {
                Counter--;

                if (Counter == 0)
                {
                    Id = -1;
                }
            }

            public int LazyId
            {
                get
                {
                    if (HasId)
                    {
                        return CurrentId;
                    }
                    else
                    {
                        return History.Instance.NextTransactionId();
                    }
                }
            }

            private int Id
            {
                get;
                set;
            }
            private int Counter
            {
                get;
                set;
            }
        }
        public class TransactionIdHelper : IDisposable
        {
            public TransactionIdHelper(TransactionId transactionId)
            {
                TransactionId = transactionId;
                TransactionId.Start();
            }
            public void Dispose()
            {
                TransactionId.End();
            }
            private TransactionId TransactionId
            {
                get;
                set;
            }
        }

        private TransactionId transactionId = new TransactionId();
        private TagModelList.TagListLoader loader;

        public TagModelEditor()
        {
            MultiTagEditor = DocNode.Create<MultiTagModel>();
            MultiTagEditor.IsFixed.Value = true;
            
            TagModelList = DocNode.Create<TagModelList>();
            Dirty = new DocObj<bool>();
            RefreshFlank = new DocObj<bool>();
            Path = new DocObj<string>();

            RefreshFlank.Hook += OnFileListRefreshHook;
            Path.Hook += OnPathHook;

            MultiTagEditor.TagModelList = TagModelList;

            History.Instance.MarkDirty = MarkDirty;
        }
        
        public TagModelList TagModelList
        {
            get;
            private set;
        }
        [DocObjRef]
        public MultiTagModel MultiTagEditor
        {
            get;
            private set;
        }
        public DocObj<bool> Dirty
        {
            get;
            private set;
        }
        public DocObj<string> Path
        {
            get;
            private set;
        }
        public DocObj<bool> RefreshFlank
        {
            get;
            private set;
        }

        public void RepairPath()
        {
            if (!VirtualDrive.ExistsDirectory(Path.Value))
            {
                string path = Path.Value;

                while (!String.IsNullOrEmpty(path) && !VirtualDrive.ExistsDirectory(path))
                {
                    DirectoryInfo dir = new DirectoryInfo(path);
                    path = dir.Parent.FullName;
                }

                if (Path.Value != path)
                {
                    Path.Value = path;
                }
            }
        }

        public TransactionId Transaction
        {
            get
            {
                return transactionId;
            }
        }

        private void LoadDirectory()
        {
            if (!Object.ReferenceEquals(loader, null))
                loader.Abort = true;

            WorkerThreadPool.Instance.StartWork(new Work(
                delegate(IWork w)
                {
                    if (!Object.ReferenceEquals(loader, null))
                    {
                        loader.Abort = true;
                        loader = null;
                    }

                    transactionId.Start();
                },
                delegate(IWork w)
                {
                    try
                    {
                        loader = new TagModelList.TagListLoader(
                            this.Path.Value, VirtualDrive.GetFiles(this.Path.Value, "*.mp3"));

                        loader.Run();
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(Tokens.Exception, e);
                    }
                },
                delegate(IWork w)
                {
                    if (!Object.ReferenceEquals(loader, null) && !loader.Abort)
                    {
                        History.Instance.ExecuteInTransaction(
                            delegate()
                            {
                                TagModelList.SetFiles(loader);
                                Dirty.Value = false;
                            },
                            transactionId.CurrentId,
                            "Document.OnLoadingFinished");
                    }

                    transactionId.End();
                }));
        }

        private void OnPathHook( Object sender, EventArgs e)
        {
            var cmd = (e as DocObj<string>.DocObjCommand);

            if (cmd.NewValue != cmd.OldValue)
            {
                LoadDirectory();
            }
        }
        private void OnFileListRefreshHook( Object sender, EventArgs e)
        {
            var args = e as DocObj<bool>.DocObjChangedEventArgs;

            if (args.OldValue && !args.NewValue)
            {
                History.Instance.ExecuteInTransaction(
                    () => LoadDirectory(),
                    transactionId.CurrentId,
                    "Document.OnBatchFinished");
            }
        }

        public void MarkDirty(object sender, EventArgs e)
        {
            if (sender != Dirty
                && (sender is IDocLeaf)
                && (sender as IDocLeaf).IsInHistoryTree()
                && (sender as IDocLeaf).Name != "IsSelected")
            {
                Dirty.Value = true;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new CallbackCommand(delegate()
                    {
                        using (TransactionIdHelper helper = new TransactionIdHelper(transactionId))
                        {
                            History.Instance.ExecuteInTransaction(
                                delegate()
                                {
                                    TagModelList.AllModels.Save(transactionId.CurrentId);
                                    Dirty.Value = false;
                                },
                                transactionId.CurrentId,
                                "DocumentSave");
                        }
                    },
                    new LocalizedText("DocumentSave"),
                    new LocalizedText("DocumentSaveHelp"));
            }
        }
    }
}
