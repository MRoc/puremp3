using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CoreDocument;
using CoreThreading;
using ID3;
using ID3.Utils;
using CoreVirtualDrive;
using CoreUtils;
using CoreDocument.Text;

namespace ID3TagModel
{
    public class TagModelList : DocNode, IDropTargetProvider, IHelpTextProvider
    {
        public TagModelList()
        {
            Help = new LocalizedText("TagModelListHelp");

            Items = new DocList<TagModel>();
            FileName = new DocObj<string>();
            Version = new TagVersionEnum();
            Version.Help = new LocalizedText("TagModelListVersionHelp");
            Version.IsEnabled = false;
            HasSelection = new DocObj<bool>();

            multiVersionAdapter.LinkedToHook = false;
            multiVersionAdapter.RegardListTransaction = true;
            multiVersionAdapter.PropertyProvider = TagModel.VersionItemProvider;
            multiVersionAdapter.PropertyProviderSelected = TagModel.SelectedVersionItemProvider;
            multiVersionAdapter.Items = Items;
            multiVersionAdapter.DocEnum = Version;

            Version.Hook = VersionEnumHook;

            selectionAdapterHook.LinkedToHook = true;
            selectionAdapterHook.RegardListTransaction = true;
            selectionAdapterHook.PropertyChangedEvent += OnSelectionChangedHook;
            selectionAdapterHook.PropertyProvider = TagModel.SelectionItemProvider;
            selectionAdapterHook.Items = Items;

            selectionAdapter.LinkedToHook = false;
            selectionAdapter.RegardListTransaction = true;
            selectionAdapter.PropertyChangedEvent += OnSelectionChanged;
            selectionAdapter.PropertyProvider = TagModel.SelectionItemProvider;
            selectionAdapter.Items = Items;
        }

        public DocList<TagModel> Items
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<string> FileName
        {
            get;
            private set;
        }
        [DocObjRef]
        public TagVersionEnum Version
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<bool> HasSelection
        {
            get;
            set;
        }

        public DocListObjListener<TagModel, bool> SelectionAdapterHook
        {
            get
            {
                return selectionAdapterHook;
            }
        }
        public DocListObjListener<TagModel, bool> SelectionAdapter
        {
            get
            {
                return selectionAdapter;
            }
        }

        public bool HasModels
        {
            get
            {
                return Items.Count > 0;
            }
        }

        public void SetFiles(IEnumerable<string> fileNames)
        {
            TagListLoader loader = new TagListLoader("", fileNames.ToArray());
            loader.Run();

            SetFiles(loader);
        }
        public void SetFiles(TagListLoader loader)
        {
            Items.Transaction.Value += 1;

            Items.Clear();

            if (!Object.ReferenceEquals(loader, null))
            {
                foreach (string fileName in  loader.FileNames)
                {
                    Tag tag = loader.TagByFilename(fileName);
                    int bitRate = loader.BitrateByTag(tag);

                    Add(fileName, tag, bitRate);
                }
            }

            Items.Transaction.Value -= 1;
        }
        public void Add(string fileName)
        {
            Tag tag = TagUtils.ReadTag(new FileInfo(fileName));

            if (tag == null)
                tag = new Tag(ID3.Preferences.PreferredVersion);

            Add(fileName, tag, -1);
        }
        public void Add(string fileName, Tag tag, int bitrate)
        {
            if (Contains(fileName))
            {
                throw new Exception("Can't add twice!");
            }

            TagModel model = DocNode.Create<TagModel>();

            model.Init(fileName, tag, bitrate);

            model.IsSelected.TransactionIdModeToUse = DocObj<bool>.TransactionIdMode.UseFixed;
            model.IsSelected.FixedTransactionId = GetHashCode();
            Items.Add(model);
        }
        public void Remove(string fileName)
        {
            if (!Contains(fileName))
            {
                throw new Exception("Not added before!");
            }

            Items.Remove(this[fileName]);
        }
        public bool Contains(string fileName)
        {
            return !Object.ReferenceEquals(this[fileName], null);
        }

        [DocObjRef]
        public TagModel this[int index]
        {
            get
            {
                return Items[index];
            }
        }
        [DocObjRef]
        public TagModel this[string fileName]
        {
            get
            {
                return
                    (from model
                    in Items
                     where model.FileNameFull == fileName
                     select model).FirstOrDefault();
            }
        }

        public IEnumerable<TagModel> AllModels
        {
            get
            {
                return Items;
            }
        }
        public IEnumerable<TagModel> SelectedModels
        {
            get
            {
                return from model in Items where model.IsSelected.Value select model;
            }
        }

        private void VersionEnumHook(object sender, EventArgs e)
        {
            if (conversionReEntryBlock)
            {
                return;
            }

            conversionReEntryBlock = true;

            int newValue = (e as DocEnum.DocObjCommand).NewValue;

            History.Instance.ExecuteInTransaction(
                () => SelectedModels.ConvertToVersion(ID3.Version.Versions[newValue]),
                History.Instance.NextFreeTransactionId(), "Version conversion");

            conversionReEntryBlock = false;
        }
        private void OnSelectionChangedHook(object sender, EventArgs e)
        {
            multiVersionAdapter.ForceUpdate();
        }
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            int numSelectedModels = SelectedModels.Count();

            if (numSelectedModels == 0)
            {
                FileName.Value = "";
            }
            else if (numSelectedModels == 1)
            {
                FileName.Value = SelectedModels.First().FileName;
            }
            else
            {
                FileName.Value = "*";
            }

            HasSelection.Value = numSelectedModels > 0;

            Version.IsEnabled = SelectedModels.Count() > 0;
        }

        public class TagListSaveAction : ActionList, IWork
        {
            private bool undo = false;

            public TagListSaveAction(int id)
                : base(id)
            {
            }

            public override void Do()
            {
                WorkerThreadPool.Instance.StartWork(this);
            }
            public override void Undo()
            {
                WorkerThreadPool.Instance.StartWork(this);
            }

            public void Before()
            {
            }
            public void Run()
            {
                if (undo)
                {
                    base.Undo();
                }
                else
                {
                    base.Do();
                }
            }
            public void After()
            {
                undo = !undo;
            }
            public IWorkType Type
            {
                get
                {
                    return IWorkType.Lock;
                }
            }
            public bool Abort
            {
                get
                {
                    return false;
                }
                set
                {
                }
            }
        }
        public class TagListLoader : IWork
        {
            public TagListLoader(string path, string[] fileNames)
            {
                Path = path;
                FileNames = fileNames;
                FileNames.ForEach(n => fileToTagMap[n] = null);
            }

            public string[] FileNames { get; set; }
            public Tag TagByFilename(string fileName)
            {
                Tag result = null;

                fileToTagMap.TryGetValue(fileName, out result);

                if (Object.ReferenceEquals(result, null))
                {
                    result = new Tag(ID3.Preferences.PreferredVersion);
                }

                return result;
            }
            public int BitrateByTag(Tag tag)
            {
                if (fileToBitrate.ContainsKey(tag))
                {
                    return fileToBitrate[tag];
                }
                else
                {
                    return -1;
                }
            }

            public void Before()
            {
            }
            public void Run()
            {
                foreach (string fileName in FileNames)
                {
                    if (Abort)
                        return;

                    Tag tag = TagUtils.ReadTag(new FileInfo(fileName));

                    fileToTagMap[fileName] = tag;

                    if (!Object.ReferenceEquals(tag, null))
                    {
                        try
                        {
                            int tagSize = TagUtils.TagSizeV2(new FileInfo(fileName));
                            using (Stream stream = VirtualDrive.OpenInStream(fileName))
                            {
                                stream.Seek(tagSize, SeekOrigin.Begin);
                                fileToBitrate[tag] = ID3MediaFileHeader.MP3Header.ReadBitrate(
                                    stream, VirtualDrive.FileLength(fileName));
                            }
                        }
                        catch (Exception)
                        {
                            fileToBitrate[tag] = -1;
                        }
                    }
                }
            }
            public void After()
            {
            }
            public IWorkType Type
            {
                get
                {
                    return IWorkType.Background;
                }
            }
            public bool Abort { get; set; }

            public string Path
            {
                get;
                private set;
            }

            private Dictionary<string, Tag> fileToTagMap = new Dictionary<string, Tag>();
            private Dictionary<Tag, int> fileToBitrate = new Dictionary<Tag, int>();
        }

        class TagModeListDropTarget : IDropTarget
        {
            public TagModeListDropTarget(TagModelList tml)
            {
                Model = tml;
            }
            public DropTypes[] SupportedTypes
            {
                get
                {
                    return new DropTypes[] { DropTypes.Picture };
                }
            }
            public bool AllowDrop(object obj)
            {
                return Model.Items.Count() > 0;
            }
            public void Drop(object obj)
            {
                Model.Items.DropPicture(obj.ToString());
            }

            private TagModelList Model
            {
                get;
                set;
            }
        }
        public IDropTarget DropTarget
        {
            get
            {
                return new TagModeListDropTarget(this);
            }
        }

        private bool conversionReEntryBlock = false;

        private DocListObjListener<TagModel, bool> selectionAdapterHook
            = new DocListObjListener<TagModel, bool>();
        private DocListObjListener<TagModel, bool> selectionAdapter
            = new DocListObjListener<TagModel, bool>();

        private DocEnumMultiAdapter<TagModel> multiVersionAdapter
            = new DocEnumMultiAdapter<TagModel>();
    }

    public static class TagModelListOperations
    {
        public static void CreateItemByFrameId(this IEnumerable<TagModel> seq, string frameId)
        {
            if (!seq.IsVersionUnique())
            {
                throw new Exception("Adding frame to list of tags failed due to different versions");
            }

            foreach (var model in seq)
            {
                model.Create(frameId);
            }
        }
        public static void CreateItemByMeaning(this IEnumerable<TagModel> seq, FrameMeaning meaning)
        {
            foreach (var model in seq)
            {
                if (model.ToTag().DescriptionMap[meaning] == null)
                {
                    model.ConvertVersion(ID3.Version.Max(
                        ID3.Version.v2_0, ID3.Preferences.PreferredVersion));
                }
                model.Create(meaning);
            }
        }

        public static bool IsVersionUnique(this IEnumerable<TagModel> seq)
        {
            ID3.Version v = null;

            foreach (var item in seq)
            {
                if (v == null)
                {
                    v = item.Version.ValueVersion;
                }
                else if (v != item.Version.ValueVersion)
                {
                    return false;
                }
            }

            return true;
        }
        public static void ConvertToMaxVersion(this IEnumerable<TagModel> seq)
        {
            if (!seq.IsVersionUnique())
            {
                seq.ConvertToVersion(ID3.Version.Max(seq.MaxVersion(), ID3.Preferences.PreferredVersion));
            }
        }
        public static ID3.Version MaxVersion(this IEnumerable<TagModel> seq)
        {
            return (from model in seq select model.Version.ValueVersion).Max();
        }
        public static void ConvertToFirstVersion(this IEnumerable<TagModel> seq, ID3.Version v)
        {
            seq.ConvertToVersion(v);
        }
        public static void ConvertToVersion(this IEnumerable<TagModel> seq, ID3.Version version)
        {
            foreach (var item in seq)
            {
                item.ConvertVersion(version);
            }
        }

        public static TagModel Previous(this IEnumerable<TagModel> seq, Predicate<TagModel> predicate)
        {
            for (int i = 0; i < seq.Count(); i++)
            {
                if (predicate(seq.ElementAt(i)))
                {
                    int index = ((i + seq.Count()) - 1) % seq.Count();
                    return seq.ElementAt(index);
                }
            }

            return seq.LastOrDefault();
        }
        public static TagModel Next(this IEnumerable<TagModel> seq, Predicate<TagModel> predicate)
        {
            for (int i = 0; i < seq.Count(); i++)
            {
                if (predicate(seq.ElementAt(i)))
                {
                    int index = (i + 1) % seq.Count();
                    return seq.ElementAt(index);
                }
            }

            return seq.FirstOrDefault();
        }

        public static void DropPicture(this IEnumerable<TagModel> seq, string fileName)
        {
            foreach (TagModel tagModel in seq)
            {
                FrameDescription frameDesc = ID3.TagDescriptionMap.Instance
                    [tagModel.Version.ValueVersion]
                    [ID3.FrameMeaning.Picture];

                if (!Object.ReferenceEquals(frameDesc, null))
                {
                    tagModel.Remove(
                        (from item in tagModel.Items
                         where item.FrameId.Value == frameDesc.FrameId
                         select item as TagModelItemPicture).ToArray());
                }
            }

            foreach (TagModel tagModel in seq)
            {
                FrameDescription frameDesc = ID3.TagDescriptionMap.Instance
                    [tagModel.Version.ValueVersion]
                    [ID3.FrameMeaning.Picture];

                if (Object.ReferenceEquals(frameDesc, null))
                {
                    tagModel.ConvertVersion(ID3.Version.Max(
                        ID3.Version.v2_0,
                        ID3.Preferences.PreferredVersion));

                    frameDesc = ID3.TagDescriptionMap.Instance
                        [tagModel.Version.ValueVersion]
                        [ID3.FrameMeaning.Picture];
                }

                tagModel.Create(frameDesc.FrameId);

                (from item in tagModel.Items
                 where item.FrameId.Value == frameDesc.FrameId
                 select item as TagModelItemPicture)
                 .ForEach(n => n.LoadPicture(fileName));
            }
        }

        public static void Save(this IEnumerable<TagModel> seq, int transactionId)
        {
            TagModelList.TagListSaveAction action =
                new TagModelList.TagListSaveAction(transactionId);

            foreach (var model in seq)
            {
                action.Add(model.SaveAction());
            }

            History.Instance.Execute(action);
        }

        public static int IndexOfTagModelItemByFrameId(
            this IEnumerable<TagModel> seq,
            TagModelItem toSearchFor)
        {
            int i = 0;

            foreach (var tagModel in seq)
            {
                foreach (var tagModelItem in tagModel.Items)
                {
                    if (Object.ReferenceEquals(tagModelItem, toSearchFor))
                    {
                        return i;
                    }

                    if (tagModelItem.FrameId.Value == toSearchFor.FrameId.Value)
                    {
                        i++;
                    }
                }
            }

            return i;
        }
        public static int IndexOfTagModelItemByMeaning(
            this IEnumerable<TagModel> seq,
            TagModelItem toSearchFor)
        {
            int i = 0;

            foreach (var tagModel in seq)
            {
                foreach (var tagModelItem in tagModel.Items)
                {
                    if (Object.ReferenceEquals(tagModelItem, toSearchFor))
                    {
                        return i;
                    }

                    if (tagModelItem.Meaning == toSearchFor.Meaning)
                    {
                        i++;
                    }
                }
            }

            return i;
        }
        public static int IndexOf(
            this IEnumerable<TagModel> seq,
            TagModel item)
        {
            int i = 0;

            foreach (var tagModel in seq)
            {
                if (Object.ReferenceEquals(tagModel, item))
                {
                    return i;
                }

                i++;
            }

            return i;
        }

        public static IList<FrameDescription> CreatableFramesIds(this IEnumerable<TagModel> seq)
        {
            return seq.First().CreatableFramesIds;
        }

        private static IList<Tag> Tags(this IEnumerable<TagModel> seq)
        {
            List<Tag> tags = new List<Tag>();
            foreach (var model in seq)
            {
                tags.Add(model.ToTag());
            }
            return tags;
        }

        public static void CheckInvariant(this IEnumerable<TagModel> seq)
        {
            foreach (var model in seq)
            {
                model.CheckInvariant();
            }
        }
        public static void CheckVersion(this IEnumerable<TagModel> seq)
        {
            foreach (var model in seq)
            {
                model.ToTag().CheckVersion();
            }
        }
    }
}
