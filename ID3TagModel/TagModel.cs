using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CoreDocument;
using CoreUtils;
using ID3;
using ID3.Processor;
using CoreVirtualDrive;
using System.Collections.Specialized;
using ID3MediaFileHeader;

namespace ID3TagModel
{
    public class TagModel : DocNode
    {
        public static TagModel CreateClone(TagModel other)
        {
            TagModel tagModel = DocNode.Create<TagModel>();
            tagModel.Init(other.FileNameFull, other.ToTag(), other.Bitrate.Value);
            return tagModel;
        }

        public TagModel()
        {
            File = new DocObj<string>();
            IsSelected = new DocObj<bool>(true);
            IsPlaying = new DocObj<bool>(false);
            Version = new TagVersionEnum();
            Bitrate = new DocObj<int>(-1);
            Items = new DocList<TagModelItem>();

            createableFrames.Model = this;

            Items.CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemsChanged);
        }

        public void Init(string fileName, Tag tag, int bitrate)
        {
            File.Value = fileName;
            Bitrate.Value = bitrate;
            FromTag(tag);
        }
        public void Load(string fileName)
        {
            Tag tag = TagUtils.ReadTag(new FileInfo(fileName));

            int bitrate = -1;

            if (!Object.ReferenceEquals(tag, null))
            {
                try
                {
                    int tagSize = TagUtils.TagSizeV2(new FileInfo(fileName));
                    using (Stream stream = VirtualDrive.OpenInStream(fileName))
                    {
                        stream.Seek(tagSize, SeekOrigin.Begin);
                        bitrate = ID3MediaFileHeader.MP3Header.ReadBitrate(
                            stream, VirtualDrive.FileLength(fileName));
                    }
                }
                catch (Exception)
                {
                    bitrate = -1;
                }
            }

            Init(fileName, tag, bitrate);
        }

        public DocObj<string> File
        {
            get;
            private set;
        }
        public string JustFilename
        {
            get
            {
                return VirtualDrive.FileName(File.Value);
            }
        }
        public DocObj<bool> IsSelected
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<bool> IsPlaying
        {
            get;
            private set;
        }
        public TagVersionEnum Version
        {
            get;
            private set;
        }
        public DocObj<int> Bitrate
        {
            get;
            private set;
        }
        public DocList<TagModelItem> Items
        {
            get;
            private set;
        }

        public string FileNameFull
        {
            get
            {
                return File.Value;
            }
            set
            {
                File.Value = value;

                try
                {
                    Tag tag = TagUtils.ReadTag(new FileInfo(value));

                    if (tag == null)
                    {
                        tag = new Tag(ID3.Preferences.PreferredVersion);
                    }

                    FromTag(tag);
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
        public string FileName
        {
            get
            {
                return VirtualDrive.FileName(FileNameFull);
            }
        }
        public string NameWithoutExtension
        {
            get
            {
                return FileUtils.NameWithoutExtension(FileNameFull);
            }
        }

        public TagModelItem CreateItemByMeaning(ID3.FrameMeaning meaning)
        {
            if (Supports(meaning))
            {
                return CreateItemByFrameId(tag.DescriptionMap[meaning].FrameId);
            }
            else
            {
                return null;
            }
        }
        public TagModelItem CreateItemByFrameId(string frameId)
        {
            return CreateItemByFrame(new Frame(tag.DescriptionMap, frameId));
        }
        public TagModelItem CreateItemByFrame(Frame f)
        {
            TagModelItem item = TagModelItemFactory.Create(f.Description.Type);
            item.Frame = f;
            return item;
        }

        [DocObjRef]
        public TagModelItem this[int index]
        {
            get
            {
                return Items[index];
            }
        }
        [DocObjRef]
        public TagModelItem this[string frameId]
        {
            get
            {
                return Items.Where(n => n.FrameId.Value == frameId).FirstOrDefault();
            }
        }
        [DocObjRef]
        public TagModelItem this[FrameMeaning meaning]
        {
            get
            {
                return Items.Where(n => n.Meaning == meaning).FirstOrDefault();
            }
        }

        public IList<FrameDescription> CreatableFramesIds
        {
            get
            {
                return createableFrames.CreateableFrames;
            }
        }

        public bool Supports(FrameMeaning meaning)
        {
            return !Object.ReferenceEquals(tag.DescriptionMap[meaning], null);
        }

        public bool Contains(string frameId)
        {
            return Items.Where(n => n.FrameId.Value == frameId).FirstOrDefault() != null;
        }
        public bool Contains(FrameMeaning meaning)
        {
            return Items.Where(n => n.Meaning == meaning).FirstOrDefault() != null;
        }
        public bool Contains(TagModelItem item)
        {
            return Items.Contains(item);
        }
        public void Create(string frameId)
        {
            Add(CreateItemByFrameId(frameId));
        }
        public void Create(FrameMeaning meaning)
        {
            Create(tag.DescriptionMap[meaning].FrameId);
        }

        public void Add(TagModelItem item)
        {
            Items.Add(item);
        }
        public void Remove(TagModelItem item)
        {
            Items.Remove(item);
        }
        public void Remove(IEnumerable<TagModelItem> items)
        {
            items.ForEach(n => Remove(n));
        }

        public void Clear()
        {
            Items.Clear();
        }

        public void Save()
        {
            try
            {
                History.Instance.Execute(SaveAction());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
        public void ConvertVersion(ID3.Version version)
        {
            Debug.Assert(CheckInvariant());

            Tag oldTag = ToTag();

            Clear();

            Tag newTag = oldTag.Clone();
            new TagVersionProcessor(version).Process(newTag);

            DocUtils.PerformAction(this, new TagConversionAction(this, oldTag, newTag));

            FromTag(tag);

            Debug.Assert(CheckInvariant());
        }
        public bool CheckInvariant()
        {
            foreach (TagModelItem item in Items)
            {
                if (item.Frame == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void FromTag(Tag tag)
        {
            this.tag = tag;

            Clear();

            Version.ValueVersion = tag.DescriptionMap.Version;

            foreach (Frame frame in tag.Frames)
            {
                Add(CreateItemByFrame(frame.Clone()));
            }
        }
        public Tag ToTag()
        {
            Tag tag = this.tag.Clone();
            tag.Clear();

            foreach (TagModelItem item in Items)
            {
                item.Commit();
                tag.Add(item.Frame.Clone());
            }

            return tag;
        }
        public IAtomicOperation SaveAction()
        {
            return new TagSaveAction(
                File.Value,
                TagUtils.TagToRaw(ToTag()));
        }

        public static DocObj<bool> SelectionItemProvider(object item)
        {
            return (item as TagModel).IsSelected;
        }
        public static TagVersionEnum VersionItemProvider(object item)
        {
            return (item as TagModel).Version;
        }
        public static TagVersionEnum SelectedVersionItemProvider(object item)
        {
            TagModel model = item as TagModel;

            if (model.IsSelected.Value)
            {
                return (item as TagModel).Version;
            }
            else
            {
                return null;
            }
        }

        public class CreatableFrames
        {
            public IList<FrameDescription> CreateableFrames
            {
                get
                {
                    List<ID3.FrameDescription> createableFrames = new List<ID3.FrameDescription>();

                    Tag tag = Model.tag;

                    if (tag != null)
                    {
                        TagDescription description = TagDescriptionMap.Instance[Model.Version.ValueVersion];

                        createableFrames.Add(description[FrameMeaning.Artist]);
                        createableFrames.Add(description[FrameMeaning.Album]);
                        createableFrames.Add(description[FrameMeaning.Title]);
                        createableFrames.Add(description[FrameMeaning.Comment]);

                        if (!Object.ReferenceEquals(
                            description[FrameMeaning.Picture], null))
                        {
                            createableFrames.Add(description[FrameMeaning.Picture]);
                        }

                        foreach (string frameId in description.FrameIds)
                        {
                            if (description[frameId].Type == FrameDescription.FrameType.Text
                                && !tag.Contains(frameId))
                            {
                                createableFrames.Add(description[frameId]);
                            }
                        }
                    }

                    return createableFrames;
                }
            }
            public TagModel Model { get; set; }
        }
        private class TagConversionAction : DocAtomicOperation
        {
            public TagConversionAction(TagModel model, Tag oldTag, Tag newTag)
                : base(model)
            {
                OldTag = oldTag;
                NewTag = newTag;
            }

            private Tag OldTag { get; set; }
            private Tag NewTag { get; set; }

            public override void Do()
            {
                Debug.Assert(Document<TagModel>().CheckInvariant());

                Document<TagModel>().tag = NewTag;

                Debug.Assert(Document<TagModel>().CheckInvariant());
            }
            public override void Undo()
            {
                Debug.Assert(Document<TagModel>().CheckInvariant());

                Document<TagModel>().tag = OldTag;

                Debug.Assert(Document<TagModel>().CheckInvariant());
            }
            public override string ToString()
            {
                StringBuilder str = new StringBuilder();

                str.Append(GetType().Name);
                str.Append("(\"");
                str.Append(OldTag.DescriptionMap.Version);
                str.Append("\", \"");
                str.Append(NewTag.DescriptionMap.Version);
                str.Append("\")");

                return str.ToString();
            }
        }
        private class TagSaveAction : CoreDocument.AtomicOperation
        {
            public TagSaveAction(string fileName, byte[] newTag)
                : base(-1)
            {
                FileName = fileName;
                NewTag = newTag;
            }

            private string FileName { get; set; }
            private byte[] OldTag0 { get;  set; }
            private byte[] OldTag1 { get; set; }
            private byte[] NewTag { get; set; }

            public override void Do()
            {
                FileInfo f = new FileInfo(FileName);

                bool hasV1InFile = TagUtils.HasTagV1(f);
                bool hasV2InFile = TagUtils.HasTagV2(f);
                bool hasV1ToWrite = TagUtils.HasTagV1(NewTag);
                bool hasV2ToWrite = TagUtils.HasTagV2(NewTag);

                if (hasV1InFile)
                {
                    OldTag0 = TagUtils.ReadTagV1Raw(f);
                }
                if (hasV2InFile)
                {
                    OldTag1 = TagUtils.ReadTagV2Raw(f);
                }

                if (hasV1InFile && hasV2ToWrite || hasV2InFile && hasV1ToWrite)
                {
                    TagUtils.StripTags(f, 0, 0);
                }

                TagUtils.WriteTag(NewTag, f);
            }
            public override void Undo()
            {
                FileInfo f = new FileInfo(FileName);

                bool hasV1InFile = TagUtils.HasTagV1(f);
                bool hasV2InFile = TagUtils.HasTagV2(f);
                bool hasV1ToWrite = TagUtils.HasTagV1(OldTag0);
                bool hasV2ToWrite = TagUtils.HasTagV2(OldTag1);

                if (hasV1InFile && hasV2ToWrite
                    || hasV2InFile && hasV1ToWrite
                    || Object.ReferenceEquals(OldTag0, null) && Object.ReferenceEquals(OldTag1, null))
                {
                    TagUtils.StripTags(f, 0, 0);
                }

                if (!Object.ReferenceEquals(OldTag0, null))
                {
                    TagUtils.WriteTag(OldTag0, f);
                }
                if (!Object.ReferenceEquals(OldTag1, null))
                {
                    TagUtils.WriteTag(OldTag1, f);
                }
            }
            public override string ToString()
            {
                StringBuilder str = new StringBuilder();

                str.Append(GetType().Name);
                str.Append("(\"");
                str.Append(FileName);
                str.Append("\")");

                return str.ToString();
            }
        }

        private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    (item as TagModelItem).TagModel = null;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    (item as TagModelItem).TagModel = this;
                }
            }
        }

        public override string ToString()
        {
            return FileNameFull;
        }

        private Tag tag;
        private CreatableFrames createableFrames = new CreatableFrames();
    }
}
