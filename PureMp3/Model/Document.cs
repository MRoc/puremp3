using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CoreControls.Threading;
using CoreDocument;
using CoreDocument.Text;
using ID3.Utils;
using ID3TagModel;
using System.Collections.ObjectModel;
using CoreControls.Commands;
using CoreThreading;
using CoreVirtualDrive;
using CoreFileTree;
using System.Windows.Input;
using System.Web;
using ID3Player;
using PureMp3.Model.Batch;
using ID3.Processor;
using ID3;
using ID3Library;
using CoreUtils;

namespace PureMp3.Model
{
    public class PlaylistRouter : ITrackAvailability, ITrackNavigation
    {
        public PlaylistRouter()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(null, null);
            }
        }

        public DocList<TagModel> EditorList
        {
            get
            {
                return editorList;
            }
            set
            {
                if (value != editorList)
                {
                    if (!Object.ReferenceEquals(editorList, null))
                    {
                        editorList.Transaction.PropertyChanged -= OnPlaylistTransactionChanged;
                    }

                    editorList = value;

                    if (!Object.ReferenceEquals(editorList, null))
                    {
                        editorList.Transaction.PropertyChanged += OnPlaylistTransactionChanged;
                    }
                }
            }
        }
        private DocList<TagModel> editorList;
        private void OnPlaylistTransactionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, e);
            }
        }

        public DocList<TagModel> Playlist
        {
            get
            {
                return playlist;
            }
            set
            {
                if (value != playlist)
                {
                    if (!Object.ReferenceEquals(playlist, null))
                    {
                        playlist.Transaction.PropertyChanged -= OnPlaylistTransactionChanged;
                    }

                    playlist = value;

                    if (!Object.ReferenceEquals(playlist, null))
                    {
                        playlist.Transaction.PropertyChanged += OnPlaylistTransactionChanged;
                    }
                }
            }
        }
        private DocList<TagModel> playlist;

        [DocObjRef]
        public DocObj<int> VisibleTab
        {
            get
            {
                return visibleTab;
            }
            set
            {
                visibleTab = value;
            }
        }
        private DocObj<int> visibleTab;

        public PlayerModel PlayerModel
        {
            get
            {
                return playerModel;
            }
            set
            {
                playerModel = value;
            }
        }
        private PlayerModel playerModel;

        public bool HasItems()
        {
            return Items.Count() > 0;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public DocList<TagModel> Items
        {
            get
            {
                return VisibleTab.Value == 0 ? EditorList : Playlist;
            }
        }
        public void JumpFirst()
        {
            playerModel.CurrentModel.Value = Items.FirstOrDefault();
        }
        public void JumpNext()
        {
            PlayerModel.CurrentModel.Value =
                Items.Next(n => n.FileNameFull == PlayerModel.CurrentFileNameFull);
        }
        public void JumpPrevious()
        {
            PlayerModel.CurrentModel.Value =
                Items.Previous(n => n.FileNameFull == PlayerModel.CurrentFileNameFull);
        }
    }

    public class Document : DocNode
    {
        public Document()
        {
            Preferences = Preferences.LoadPreferences();

            FileTreeModel = new FileTreeModel();
            FileTreeModel.PropertyChanged += OnTreeNodeSelectionChanged;
            FileTreeModel.Help = new LocalizedText("FileTreeModelHelp");

            Editor = DocNode.Create<TagModelEditor>();
            Editor.Path.PropertyChanged += OnPathChanged;

            IsBatchActive = new DocObj<bool>();
            VisibleTab = new DocObj<int>();

            PlayerModel = DocNode.Create<PlayerModel>();
            Playlist = DocNode.Create<Playlist>();
            Playlist.Player = PlayerModel;

            PlaylistRouter = new PlaylistRouter();
            PlaylistRouter.Playlist = Playlist.Items;
            PlaylistRouter.EditorList = Editor.TagModelList.Items;
            PlaylistRouter.PlayerModel = PlayerModel;
            PlaylistRouter.VisibleTab = VisibleTab;

            PlayerController = DocNode.Create<PlayerController>();
            PlayerCommands = new PlayerCommands(PlayerController, PlaylistRouter, PlaylistRouter);

            IsPlayingUpdater = new PlayerModelIsPlayingUpdater();
            IsPlayingUpdater.Model = PlayerModel;
            IsPlayingUpdater.Items = Editor.TagModelList.Items;

            FileTreeModel.CommandsProvider += CommandsForNode;

            try
            {
                InitLibrary();
            }
            catch (Exception ex)
            {
                CoreUtils.CrashDumpWriter.DumpException(ex, "PureMp3", "mail@mroc.de");
            }

            IsWorkerThreadActive = new DocObj<bool>();
            IsWorkerThreadActive.Help = new LocalizedText("IsWorkerThreadActive");

            //XmlUtils.DumpXml(this.ToXmlDump());
        }

        private void InitLibrary()
        {
            Library = new ID3Library.Library();
            Library.Init(Preferences.PrefsCommon.LibraryPath.Value<string>());

            Preferences.PrefsCommon.LibraryPath.ItemT<DocObj<string>>().PropertyChanged +=
                delegate(object sender, PropertyChangedEventArgs e)
                {
                    Library.Refresh(Preferences.PrefsCommon.LibraryPath.Value<string>());
                };
        }

        public TagModelEditor Editor
        {
            get;
            private set;
        }
        [DocObjRef]
        public PlayerModelIsPlayingUpdater IsPlayingUpdater
        {
            get;
            private set;
        }
        [DocObjRef]
        public Preferences Preferences
        {
            get;
            private set;
        }
        [DocObjRef]
        public FileTreeModel FileTreeModel
        {
            get;
            private set;
        }
        [DocObjRef]
        public Library Library
        {
            get;
            set;
        }
        [DocObjRef]
        public Playlist Playlist
        {
            get;
            private set;
        }
        [DocObjRef]
        public PlaylistRouter PlaylistRouter
        {
            get;
            private set;
        }
        [DocObjRef]
        public PlayerModel PlayerModel
        {
            get;
            private set;
        }
        [DocObjRef]
        public PlayerController PlayerController
        {
            get;
            private set;
        }
        [DocObjRef]
        public PlayerCommands PlayerCommands
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<bool> IsWorkerThreadActive
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<bool> IsBatchActive
        {
            get;
            set;
        }
        [DocObjRef]
        public DocObj<int> VisibleTab
        {
            get;
            set;
        }

        public ICommand UndoCommand
        {
            get
            {
                CallbackCommand cmd = new CallbackCommand(
                    () => History.Instance.Undo(),
                    (n) => History.Instance.HasUndo,
                    new LocalizedText("Undo"),
                    new LocalizedText("UndoHelp"));

                History.Instance.PropertyChanged += cmd.TriggerCanExecute;

                return cmd;
            }
        }
        public ICommand RedoCommand
        {
            get
            {
                CallbackCommand cmd = new CallbackCommand(
                    () => History.Instance.Redo(),
                    (n) => History.Instance.HasRedo,
                    new LocalizedText("Redo"),
                    new LocalizedText("RedoHelp"));

                History.Instance.PropertyChanged += cmd.TriggerCanExecute;

                return cmd;
            }
        }
        public ICommand BatchCancelCommand
        {
            get
            {
                CallbackCommand cmd = new CallbackCommand(
                    () => WorkerThreadPool.Instance.Abort = true,
                    (n) => IsBatchActive.Value,
                    new LocalizedText("Cancel"),
                    new LocalizedText("CancelHelp"));

                IsBatchActive.PropertyChanged += cmd.TriggerCanExecute;

                return cmd;
            }
        }
        public ObservableCollection<ICommand> CommandsForNode(TreeNode node)
        {
            ObservableCollection<ICommand> result
                = new ObservableCollection<ICommand>();

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorSearchAlbums,
                node.ItemPath,
                new LocalizedText("DocumentCommandsAlbumExplorerProcessor"),
                new LocalizedText("DocumentCommandsAlbumExplorerProcessorHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorPreparse,
                node.ItemPath,
                new LocalizedText("DocumentCommandsPreparse"),
                new LocalizedText("DocumentCommandsPreparseHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorCheckForFreedb,
                node.ItemPath,
                new LocalizedText("DocumentCommandsCheckForFreedb"),
                new LocalizedText("DocumentCommandsCheckForFreedbHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorCheckForDiscogs,
                node.ItemPath,
                new LocalizedText("DocumentCommandsCheckForDiscogs"),
                new LocalizedText("DocumentCommandsCheckForDiscogsHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorCheckForMusicBrainz,
                node.ItemPath,
                new LocalizedText("DocumentCommandsCheckForMusicBrainz"),
                new LocalizedText("DocumentCommandsCheckForMusicBrainzHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorCoverSearch,
                node.ItemPath,
                new LocalizedText("DocumentCommandsCoverSearch"),
                new LocalizedText("DocumentCommandsCoverSearchHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorFilenameToTag,
                node.ItemPath,
                new LocalizedText("DocumentCommandsFilenameToTag"),
                new LocalizedText("DocumentCommandsFilenameToTagHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorTagToFilename,
                node.ItemPath,
                new LocalizedText("DocumentCommandsTagToFilename"),
                new LocalizedText("DocumentCommandsTagToFilenameHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorAlbumToDirectory,
                node.ItemPath,
                new LocalizedText("DocumentCommandsAlbumToDirectory"),
                new LocalizedText("DocumentCommandsAlbumToDirectoryHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorAlbumToLibrary,
                node.ItemPath,
                new LocalizedText("DocumentCommandsAlbumToLibrary"),
                new LocalizedText("DocumentCommandsAlbumToLibraryHelp")));

            result.Add(new CallbackCommand(
                () => Process.Start("explorer.exe", node.ItemPath),
                new LocalizedText("DocumentCommandsOpenExplorer"),
                new LocalizedText("DocumentCommandsOpenExplorerHelp")));

            result.Add(new BatchCommand(
                this,
                Preferences.CreateProcessorRecycleBin,
                node.ItemPath,
                new LocalizedText("DocumentCommandsDeleteFolder"),
                new LocalizedText("DocumentCommandsDeleteFolderHelp")));

            //result.Add(new CallbackCommand(
            //    delegate()
            //    {
            //        string queryString = HttpUtility.UrlEncode(node.ItemName);
            //            //.Replace(" ", "+")
            //            //.Replace("-", "");

            //        Process.Start(@"http://www.wikipedia.org/wiki/Special:Search?search=" + queryString + "&go=Go");
            //    },
            //    new Text("Wikipedia \"" + node.ItemName + "\""),
            //    new LocalizedText("DocumentCommandsOpenWikipedia")));

            return result;
        }

        public event EventHandler RequestPlayingChanged;
        public void RequestPlaying(TagModel model)
        {
            if (RequestPlayingChanged != null)
            {
                RequestPlayingChanged(model, null);
            }
        }
        public void RequestPlaying(string filename)
        {
            TagModel model = DocNode.Create<TagModel>();
            model.FileNameFull = filename;

            try
            {
                int tagSize = TagUtils.TagSizeV2(new FileInfo(filename));
                using (Stream stream = VirtualDrive.OpenInStream(filename))
                {
                    stream.Seek(tagSize, SeekOrigin.Begin);
                    model.Bitrate.Value = ID3MediaFileHeader.MP3Header.ReadBitrate(
                        stream, VirtualDrive.FileLength(filename));
                }
            }
            catch (Exception)
            {
            }

            FileInfo fileInfo = new FileInfo(filename);
            FileTreeModel.ExpandAndSelect(fileInfo.DirectoryName, true);

            RequestPlaying(model);
        }

        private void OnPathChanged(Object sender, PropertyChangedEventArgs e)
        {
            string selectedPath = FileTreeModel.SelectedPathString();

            if (!FileUtils.ArePathsEqual(selectedPath, Editor.Path.Value))
            {
                FileTreeModel.ExpandAndSelect(Editor.Path.Value, false);
            }
        }
        private void OnTreeNodeSelectionChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == DocBase.PropertyName(FileTreeModel, m => m.SelectedTreeNode)
                && !Object.ReferenceEquals(FileTreeModel.SelectedTreeNode, null))
            {
                string newPath = FileTreeModel.SelectedTreeNode.ItemPath;
                string oldPath = Editor.Path.Value;
                
                if (!FileUtils.ArePathsEqual(newPath, oldPath))
                {
                    History.Instance.ExecuteInTransaction(
                        delegate()
                        {
                            Editor.Path.Value = newPath;
                        },
                        Editor.Transaction.LazyId,
                        new Text("Change Folder to ") + newPath);
                }
            }
        }

        public void StartBatch(BatchCommand command)
        {
            Debug.Assert(!Editor.Transaction.HasId);
            Editor.Transaction.Start();

            OnBeforeBatch();
            OnBatch(command);
        }
        private void OnBeforeBatch()
        {
            IsBatchActive.Value = true;

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    Editor.RefreshFlank.Value = true;
                    Editor.Path.CreateAction(Editor.Path.Value);
                },
                Editor.Transaction.CurrentId,
                "Document.StartBatch");
        }
        private void OnBatch(BatchCommand command)
        {
            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    BatchAction action = new BatchAction(command);

                    action.Id = Editor.Transaction.CurrentId;
                    action.OnFinished += OnAfterBatch;

                    History.Instance.Execute(action);
                },
                Editor.Transaction.CurrentId,
                "Document.OnBatch");
        }
        private void OnAfterBatch(object sender, EventArgs e)
        {
            if (!Editor.Transaction.HasId)
            {
                Debug.Assert(!Editor.RefreshFlank.Value);
                FileTreeModel.Refresh();
            }
            else
            {
                try
                {
                    History.Instance.ExecuteInTransaction(
                        delegate()
                        {
                            FileTreeModel.Refresh();
                            Editor.RepairPath();
                            Editor.RefreshFlank.Value = false;
                        },
                        Editor.Transaction.CurrentId,
                        "Document.OnBatchFinished");
                }
                finally
                {
                    Editor.Transaction.End();
                }
            }

            IsBatchActive.Value = false;
        }
    }
}
