using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using CoreControls.Commands;
using CoreDocument;
using ID3;
using ID3.Utils;
using CoreVirtualDrive;
using CoreUtils;
using CoreDocument.Text;
using System.Diagnostics;

namespace ID3TagModel
{
    public class MultiTagModel
        : DocNode
        , IHelpTextProvider
        , IMultiTagModel
    {
        public MultiTagModel()
        {
            Help = new LocalizedText("MultiTagModelHelp");

            IsFixed = new DocObj<bool>(false);
            IsFixed.Help = new LocalizedText("MultiTagModelIsFixedHelp");

            TagModels = new DocList<TagModel>(true);
            MultiTagModelItems = new DocList<MultiTagModelItem>();
            adapter.MultiTagModel = this;

            IsFixed.PropertyChanged += UpdateIsFixed;
        }

        private FrameMeaning[] FixedMeanings
        {
            get
            {
                return new FrameMeaning[]
                {
                    FrameMeaning.Artist,
                    FrameMeaning.Album,
                    FrameMeaning.ReleaseYear,
                    FrameMeaning.Title,
                    FrameMeaning.TrackNumber,
                    FrameMeaning.Picture,
                    FrameMeaning.PartOfSet,
                    FrameMeaning.Comment,
                    FrameMeaning.ContentType,
                };
            }
        }
        private void UpdateIsFixed(object sender, PropertyChangedEventArgs e)
        {
            Clear();
            MultiTagItems.Clear();

            if (IsFixed.Value)
            {
                foreach (var fm in FixedMeanings)
                {
                    TagDescription desc = TagDescriptionMap.Instance[ID3.Version.v2_3];

                    Frame frame = new Frame(desc, fm);
                    FrameDescription.FrameType type = desc[fm].Type;

                    TagModelItem item = TagModelItemFactory.Create(type);
                    item.Frame = frame;

                    MultiTagModelItem mtmi = MultiTagModelItemFactory.Create(type);
                    mtmi.InitFixed(item);
                    MultiTagModelItems.Add(mtmi);
                }
            }

            adapter.UpdateItems();
        }

        private DocList<MultiTagModelItem> MultiTagModelItems
        {
            get;
            set;
        }
        [DocObjRef]
        public DocList<TagModel> TagModels
        {
            get;
            set;
        }
        [DocObjRef]
        public TagModelList TagModelList
        {
            get
            {
                return adapter.TagModelList;
            }
            set
            {
                adapter.TagModelList = value;
            }
        }
        [DocObjRef]
        public ObservableCollection<MultiTagModelItem> MultiTagItems
        {
            get
            {
                return MultiTagModelItems;
            }
        }

        [DocObjRef]
        public DocObj<bool> IsFixed
        {
            get;
            private set;
        }

        public ObservableCollection<ICommand> CreateFrameCommands
        {
            get
            {
                var result = new ObservableCollection<ICommand>();

                foreach (var frameDesc in TagModelList.SelectedModels.CreatableFramesIds())
                {
                    result.Add(new CreateFrameCommand(this, frameDesc));
                }

                return result;
            }
        }

        [DocObjRef]
        public MultiTagModelItem FindBy(TagModelItem item)
        {
            if (IsFixed.Value)
            {
                return this[item.Meaning];
            }
            else
            {
                return this[item.FrameId.Value];
            }
        }
        [DocObjRef]
        public MultiTagModelItem this[FrameMeaning meaning]
        {
            get
            {
                return (from mtmi in MultiTagModelItems
                        where mtmi.Meaning == meaning
                        select mtmi).FirstOrDefault();
            }
        }
        [DocObjRef]
        public MultiTagModelItem this[string frameId]
        {
            get
            {
                return (from mtmi in MultiTagModelItems
                        where mtmi.FrameId == frameId
                        select mtmi).FirstOrDefault();
            }
        }
        
        private void OnTagModelChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object obj in e.OldItems)
                {
                    TagModelItem item = obj as TagModelItem;

                    if (IsFixed.Value)
                    {
                        TagModel tagModel = tagModelItemToTagModel[item];
                        if (FixedMeanings.Contains(item.Meaning) && !tagModel.Contains(item.Meaning))
                        {
                            AddTagModelItem(CreateTemplateItem(tagModel, item.Meaning));
                        }
                    }

                    RemoveTagModelItem(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (object obj in e.NewItems)
                {
                    TagModelItem item = obj as TagModelItem;

                    AddTagModelItem(item);

                    if (IsFixed.Value)
                    {
                        TagModel tagModel = tagModelItemToTagModel[item];
                        TagModelItem templateItem = FindTemplateItem(tagModel, item.Meaning);

                        if (!Object.ReferenceEquals(templateItem, null))
                        {
                            RemoveTagModelItem(templateItem);
                        }
                    }
                }
            }
        }

        private TagModelItem CreateTemplateItem(TagModel tagModel, FrameMeaning meaning)
        {
            TagModelItem item = tagModel.CreateItemByMeaning(meaning);
            item.TagModel = tagModel;
            item.IsTemplateItem.Value = true;

            if (meaning == FrameMeaning.Title)
            {
                item.Text.Value = tagModel.NameWithoutExtension;
            }

            TagModelItemHook hook = new TagModelItemHook();
            hook.Item = item;

            return item;
        }
        private TagModelItem FindTemplateItem(TagModel tagModel, FrameMeaning meaning)
        {
            if (Object.ReferenceEquals(this[meaning], null))
            {
                return null;
            }
            else
            {
                return
                    (from tagModelItem in this[meaning].Items
                    where tagModelItem.TagModel == tagModel && !tagModel.Contains(tagModelItem)
                    select tagModelItem).FirstOrDefault();
            }
        }

        public void Clear()
        {
            TagModels.ForEach(n => n.Items.CollectionChanged -= OnTagModelChanged);
            TagModels.Clear();

            if (IsFixed.Value)
            {
                MultiTagItems.ForEach(n => n.Items.Clear());
            }
            else
            {
                MultiTagItems.Clear();
            }

            MultiTagModelItems.ForEach(n => n.IsEnabled = false);

            tagModelItemToTagModel.Clear();
        }
        public void AddTagModel(TagModel tagModel)
        {
            int count = TagModels.Count;

            TagModels.Add(tagModel);

            tagModel.Items.ForEach(n => AddTagModelItem(n));
            tagModel.Items.CollectionChanged += OnTagModelChanged;

            if (count == 0)
            {
                MultiTagModelItems.ForEach(n => n.IsEnabled = true);
            }

            if (IsFixed.Value)
            {
                foreach (FrameMeaning meaning in FixedMeanings)
                {
                    if (tagModel.Supports(meaning) && !tagModel.Contains(meaning))
                    {
                        AddTagModelItem(CreateTemplateItem(tagModel, meaning));
                    }
                }
            }
        }
        public void RemoveTagModel(TagModel tagModel)
        {
            tagModel.Items.CollectionChanged -= OnTagModelChanged;
            foreach (TagModelItem tagModelItem in tagModel.Items)
            {
                RemoveTagModelItem(tagModelItem);
            }
            TagModels.Remove(tagModel);

            if (TagModels.Count == 0)
            {
                MultiTagModelItems.ForEach(n => n.IsEnabled = false);
            }

            if (IsFixed.Value)
            {
                foreach (FrameMeaning meaning in FixedMeanings)
                {
                    if (tagModel.Supports(meaning) && !tagModel.Contains(meaning))
                    {
                        RemoveTagModelItem(FindTemplateItem(tagModel, meaning));
                    }
                }
            }
        }
        public bool HasTagModel(TagModel tagModel)
        {
            return TagModels.Contains(tagModel);
        }
        public int NumTagModels()
        {
            return TagModels.Count;
        }
        private void AddTagModelItem(TagModelItem tagModelItem)
        {
            MultiTagModelItem multiTagModelItem = FindBy(tagModelItem);
            
            if (Object.ReferenceEquals(multiTagModelItem, null) && !IsFixed.Value)
            {
                multiTagModelItem = MultiTagModelItemFactory.Create(tagModelItem.Frame.Description.Type);
                multiTagModelItem.BlockUpdates = BlockUpdates;
                MultiTagModelItems.Add(multiTagModelItem);
            }

            if (!Object.ReferenceEquals(multiTagModelItem, null))
            {
                int index = IsFixed.Value
                    ? TagModelList.SelectedModels.IndexOf(tagModelItem.TagModel)
                    : TagModelList.SelectedModels.IndexOfTagModelItemByFrameId(tagModelItem);

                int clippedIndex = Math.Min(index, multiTagModelItem.Items.Count);
                if (!multiTagModelItem.Items.Contains(tagModelItem))
                {
                    multiTagModelItem.Items.Insert(clippedIndex, tagModelItem);
                }
            }

            tagModelItemToTagModel[tagModelItem] = tagModelItem.TagModel;
        }
        private void RemoveTagModelItem(TagModelItem tagModelItem)
        {
            MultiTagModelItem multiItem = FindBy(tagModelItem);

            if (!Object.ReferenceEquals(multiItem, null))
            {
                multiItem.BlockUpdates = BlockUpdates;

                if (multiItem.Items.Contains(tagModelItem))
                {
                    multiItem.Items.Remove(tagModelItem);
                    tagModelItemToTagModel.Remove(tagModelItem);
                }

                if (multiItem.Items.Count == 0 && !IsFixed.Value)
                {
                    MultiTagModelItems.Remove(multiItem);
                }
            }
        }

        private bool blockUpdates;
        public bool BlockUpdates
        {
            get
            {
                return blockUpdates;
            }
            set
            {
                if (blockUpdates != value)
                {
                    blockUpdates = value;

                    MultiTagModelItems.ForEach((n) => n.BlockUpdates = BlockUpdates);
                }                     
            }
        }

        private MultiTagModelAdapter adapter = new MultiTagModelAdapter();

        private Dictionary<TagModelItem, TagModel> tagModelItemToTagModel = new Dictionary<TagModelItem, TagModel>();
    }

    class TagModelItemHook
    {
        public TagModelItem Item
        {
            get
            {
                return item;
            }
            set
            {
                if (!Object.ReferenceEquals(item, null))
                {
                    IDocUtils.RemoveHookRecursive(item, OnHook);
                }

                item = value;

                if (!Object.ReferenceEquals(item, null))
                {
                    IDocUtils.AddHookRecursive(item, OnHook);
                }
            }
        }
        private void OnHook(object sender, EventArgs e)
        {
            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    Item.TagModel.Add(Item);
                    Item = null;
                },
                sender.GetHashCode(),
                "AddTemplateItemOnEdit");
        }

        private TagModelItem item;
    }

    public class CreateFrameCommand : CommandBase
    {
        public static event EventHandler TagCreated;

        public CreateFrameCommand(MultiTagModel document, ID3.FrameDescription desc)
            : base(new Text(desc))
        {
            MultiTagModel = document;
            FrameDesc = desc;
        }

        public override void Execute(object obj)
        {
            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    MultiTagModel.TagModels.ConvertToMaxVersion();
                    MultiTagModel.TagModels.CreateItemByFrameId(FrameDesc.FrameId);
                },
                History.Instance.NextFreeTransactionId(), "MultiTagModel.CreateItemByFrameId");

            if (TagCreated != null)
            {
                TagCreated(FrameDesc, null);
            }
        }

        private MultiTagModel MultiTagModel
        {
            get;
            set;
        }
        public ID3.FrameDescription FrameDesc
        {
            get;
            set;
        }
    }
    class ConvertMultiTagModelItemCommand : CommandBase
    {
        public ConvertMultiTagModelItemCommand(MultiTagModelItem item, ID3.FrameDescription frameDesc)
            : base(new Text(item.FirstItem.FrameId + " -> " + frameDesc.FrameId + ": " + frameDesc.Description))
        {
            Item = item;
            Description = frameDesc;
        }

        public override void Execute(object parameter)
        {
            // 0. Prepare 
            List<TagModelItem> items = Item.Items.ToList();

            Dictionary<TagModelItem, TagModel> models = new Dictionary<TagModelItem, TagModel>();
            items.ForEach(n => models.Add(n, n.TagModel));

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    foreach (var item in items)
                    {
                        // 1. Remove all items affected
                        models[item].Remove(item);

                        // 2. convert frames
                        ID3.Frame newFrame = item.Frame.Clone();
                        newFrame.FrameId = Description.FrameId;

                        // 3. Add new TagModelItems from new frame
                        models[item].Add(models[item].CreateItemByFrame(newFrame));
                    }
                },
                History.Instance.NextFreeTransactionId(),
                "MultiTagView.ConvertMultiTagModelItemCommand");
        }

        private MultiTagModelItem Item { get; set; }
        public ID3.FrameDescription Description { get; set; }

        public static bool CanConvertFrame(MultiTagModelItem item)
        {
            return item.IsClassIdUnique
                && !Object.ReferenceEquals(item.FirstItem, null)
                && item.FirstItem.Frame.Description.Type == ID3.FrameDescription.FrameType.Text;
        }
        public static IEnumerable<ID3.FrameDescription> PossibleFrameConversions(MultiTagModelItem item)
        {
            var frameDescription = item.FirstItem.Frame.Description;
            var version = item.FirstItem.Frame.DescriptionMap.Version;
            var tagDescription = ID3.TagDescriptionMap.Instance[version];

            foreach (var frameId in tagDescription.FrameIds)
            {
                var otherFrameDescription = tagDescription[frameId];

                if (otherFrameDescription.FrameId != frameDescription.FrameId
                    && otherFrameDescription.Type == frameDescription.Type)
                {
                    yield return otherFrameDescription;
                }
            }
        }
    }
}
