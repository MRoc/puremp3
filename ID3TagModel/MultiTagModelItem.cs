using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreDocument;
using System.Collections.ObjectModel;
using CoreDocument.Text;
using System.Windows.Input;
using CoreControls.Commands;
using CoreUtils;
using ID3;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace ID3TagModel
{
    public class MultiTagModelItem : DocNode
    {
        [DocObjRef]
        private TagModelItem item;
        [DocObjRef]
        private DocList<TagModelItem> items = new DocList<TagModelItem>(true);

        ObservableCollection<ICommand> commands = new ObservableCollection<ICommand>();

        private DocListObjListener<TagModelItem, string> textAdapter
            = new DocListObjListener<TagModelItem, string>();

        private DocListObjListener<TagModelItem, bool> templateItemAdapter
            = new DocListObjListener<TagModelItem, bool>();

        public MultiTagModelItem()
        {
            IsTemplateItem = new DocObj<bool>();
            Help = new LocalizedText("MultiTagModelItemHelp");

            textAdapter.LinkedToHook = false;
            textAdapter.RegardListTransaction = true;
            textAdapter.PropertyProvider = TagModelItem.TextItemProvider;
            textAdapter.ItemsChangedEvent += (object sender, EventArgs e) => UpdateFromItems();
            textAdapter.PropertyChangedEvent += (object sender, EventArgs e) => UpdateTextFromItems();
            textAdapter.Items = items;

            templateItemAdapter.LinkedToHook = false;
            templateItemAdapter.RegardListTransaction = true;
            templateItemAdapter.PropertyProvider = TagModelItem.IsTemplateItemProvider;
            templateItemAdapter.ItemsChangedEvent += (object sender, EventArgs e) => UpdateIsTemplateItem();
            templateItemAdapter.PropertyChangedEvent += (object sender, EventArgs e) => UpdateIsTemplateItem();
            templateItemAdapter.Items = items;

            IsTemplateItem.PropertyChanged += (sender, e) => UpdateTextFromItems();
        }
        public void InitFixed(TagModelItem protoItem)
        {
            IsFixed = true;
            Item = protoItem;
        }

        [DocObjRef]
        public TagModelItem Item
        {
            get
            {
                return item;
            }
            set
            {
                if (item != null)
                {
                    item.Text.Hook -= TextHook;
                }

                item = value;

                if (item != null)
                {
                    item.Text.Hook = TextHook;
                }
            }
        }
        [DocObjRef]
        public DocList<TagModelItem> Items
        {
            get
            {
                return items;
            }
        }
        [DocObjRef]
        public DocObj<string> Text
        {
            get
            {
                return item.Text;
            }
        }
        [DocObjRef]
        public TagModelItem FirstItem
        {
            get
            {
                return items.FirstOrDefault();
            }
        }
        [DocObjRef]
        public MultiTagModel ParentModel
        {
            get
            {
                if (((IDocLeaf)this).Parent != null)
                {
                    return (MultiTagModel)(((IDocLeaf)this).Parent.Parent);
                }
                else
                {
                    return null;
                }
            }
        }

        public int CountItems
        {
            get
            {
                return Items.Count();
            }
        }
        public int CountTemplateItems
        {
            get
            {
                return (
                    from tagModelItem
                    in Items
                    where tagModelItem.IsTemplateItem.Value
                    select tagModelItem
                    ).Count();
            }
        }
        public int CountNonTemplateItems
        {
            get
            {
                return CountItems - CountTemplateItems;
            }
        }
        [DocObjRef]
        public DocObj<bool> IsTemplateItem
        {
            get;
            set;
        }
        private bool CalculateIsTemplateItem
        {
            get
            {
                return CountTemplateItems == CountItems;
            }
        }
        private void UpdateIsTemplateItem()
        {
            IsTemplateItem.Value = CalculateIsTemplateItem;
            UpdateTextFromItems();
        }

        public string FrameId
        {
            get
            {
                if (IsFixed)
                {
                    return Item.FrameId.Value;
                }
                else
                {
                    if (items.Count == 0)
                    {
                        return "FRAMEID";
                    }
                    return FirstItem.FrameId.Value;
                }
            }
        }
        public FrameMeaning Meaning
        {
            get
            {
                if (IsFixed)
                {
                    return Item.Meaning;
                }
                else
                {
                    return FirstItem.Meaning;
                }
            }
        }
        public string FrameDescription
        {
            get
            {
                if (IsFixed)
                {
                    return Item.FrameDescription.Value;
                }
                else
                {
                    if (items.Count == 0)
                    {
                        return "DESCRIPTION";
                    }
                    return FirstItem.FrameDescription.Value;
                }
            }
        }

        public bool IsClassIdUnique
        {
            get
            {
                TagModelItem firstItem = null;

                foreach (TagModelItem item in items)
                {
                    if (firstItem == null)
                    {
                        firstItem = item;
                    }
                    else if (firstItem.GetType() != item.GetType())
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        public bool IsFrameUnique
        {
            get
            {
                if (BlockUpdates)
                    return true;

                TagModelItem firstItem = null;

                foreach (TagModelItem item in items)
                {
                    if (firstItem == null)
                    {
                        firstItem = item;
                    }
                    else if (!firstItem.IsEqual(item))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        public bool IsTextUnique
        {
            get
            {
                if (BlockUpdates)
                    return true;

                TagModelItem firstItem = null;

                foreach (TagModelItem item in items)
                {
                    if (!item.IsTemplateItemValue)
                    {
                        if (firstItem == null)
                        {
                            firstItem = item;
                        }
                        else if (firstItem.Text.Value != item.Text.Value)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }
        public bool IsEnabled
        {
            set
            {
                NotifyPropertyChanged(this, m => m.IsEnabled);
            }
            get
            {
                return IsFixed
                    ? Object.ReferenceEquals(ParentModel, null) || ParentModel.TagModels.Count() > 0
                    : Items.Count > 0;
            }
        }

        public string FirstText
        {
            get
            {
                TagModelItem item =
                    (from n
                     in Items
                     where !n.IsTemplateItemValue
                     select n).FirstOrDefault();

                if (Object.ReferenceEquals(item, null))
                {
                    return "";
                }
                else
                {
                    return FirstItem.Text.Value;
                }
            }
        }

        private CallbackCommand deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                if (deleteCommand == null)
                {
                    deleteCommand = new CallbackCommand(
                        delegate()
                        {
                            History.Instance.ExecuteInTransaction(
                                delegate()
                                {
                                    foreach (var i in Items.ToArray())
                                    {
                                        i.TagModel.Remove(i);
                                    }
                                },
                                GetHashCode(),
                                "MultiTagModelItemDelete");
                        },
                        delegate(object obj)
                        {
                            return !Object.ReferenceEquals(FirstItem, null)
                                && !IsTemplateItem.Value;
                        },
                        new LocalizedText("MultiTagModelItemDelete"),
                        new LocalizedText("MultiTagModelItemDeleteHelp"));

                    Items.Transaction.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                    {
                        if (Items.Transaction.Value == 0)
                        {
                            deleteCommand.TriggerCanExecute(null, null);
                        }
                    };

                    IsTemplateItem.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                    {
                        deleteCommand.TriggerCanExecute(null, null);
                    };
                }

                return deleteCommand;
            }
        }

        private CallbackCommand duplicateCommand;
        public ICommand DuplicateCommand
        {
            get
            {
                if (duplicateCommand == null)
                {
                    duplicateCommand = new CallbackCommand(
                        delegate()
                        {
                            History.Instance.ExecuteInTransaction(
                                delegate()
                                {
                                    if (IsFixed && ParentModel.TagModels.MaxVersion() == ID3.Version.v1_0)
                                    {
                                        ParentModel.TagModels.ConvertToVersion(
                                            ID3.Version.Max(ID3.Preferences.PreferredVersion, ID3.Version.v2_0));
                                    }

                                    // Duplicate 

                                    FirstItem.Commit();

                                    bool textIsUnique = IsTextUnique;
                                    Frame firstFrame = FirstItem.Frame;

                                    ID3.Version v = FirstItem.TagModel.Version.ValueVersion;

                                    IEnumerable<TagModel> tagModels =
                                        from tagModel in ParentModel.TagModels
                                        where tagModel.Version.ValueVersion == v && !tagModel.Contains(firstFrame.FrameId)
                                        select tagModel;

                                    foreach (var tagModel in tagModels)
                                    {
                                        Frame newFrame = firstFrame.Clone();

                                        if (!textIsUnique && firstFrame.Description.Type == ID3.FrameDescription.FrameType.Text)
                                        {
                                            (newFrame.Content as FrameContentText).Text = tagModel.NameWithoutExtension;
                                        }

                                        tagModel.Add(tagModel.CreateItemByFrame(newFrame));
                                    }
                                },
                                GetHashCode(),
                                "MultiTagModelItemDuplicate");
                        },
                        delegate(object obj)
                        {
                            return (!IsFixed && !Object.ReferenceEquals(FirstItem, null) && (Object.ReferenceEquals(ParentModel, null) || ParentModel.TagModelList.SelectedModels.Count() > this.Items.Count))
                                || (IsFixed && CountNonTemplateItems > 0 && CountTemplateItems > 0);
                        },
                        new LocalizedText("MultiTagModelItemDuplicate"),
                        new LocalizedText("MultiTagModelItemDuplicateHelp"));

                    Items.Transaction.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                    {
                        if (Items.Transaction.Value == 0)
                        {
                            duplicateCommand.TriggerCanExecute(null, null);
                        }
                    };
                    IsTemplateItem.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                    {
                        duplicateCommand.TriggerCanExecute(null, null);
                    };
                }

                return duplicateCommand;
            }
        }
        
        public ObservableCollection<ICommand> ConvertToCommands
        {
            get
            {
                if (commands.Count == 0)
                {
                    UpdateCommandsFromItems();
                }

                return commands;
            }
        }

        private void TextHook(object sender, EventArgs e)
        {
            bool oldBlockUpdate = BlockUpdates;

            try
            {
                BlockUpdates = true;

                History.Instance.ExecuteInTransaction(
                    delegate()
                    {
                        if (IsFixed && Items.Count == 0)
                        {
                            ParentModel.TagModels.CreateItemByMeaning(Item.Meaning);
                        }

                        Items.ForEach(n => n.Text.Value = (e as DocObj<string>.DocObjCommand).NewValue);
                    },
                    this.GetHashCode(),
                    "MultiTagModelItem.TextHook");
            }
            finally
            {
                BlockUpdates = oldBlockUpdate;
            }
        }

        protected virtual void UpdateFromItems()
        {
            if (BlockUpdates)
                return;

            UpdateItemFromItems();
            UpdateTextFromItems();
            UpdateCommandsFromItems();
        }
        private void UpdateItemFromItems()
        {
            if (BlockUpdates || IsFixed)
                return;

            if (item == null && items.Count > 0)
            {
                if (IsClassIdUnique)
                {
                    Item = (TagModelItem)Activator.CreateInstance(FirstItem.GetType(), new Object[] { });
                }
                else
                {
                    Item = new TagModelItem();
                }
            }
            else if (item != null && items.Count == 0)
            {
                Item = null;
            }
        }
        private void UpdateTextFromItems()
        {
            if (BlockUpdates)
                return;

            if (Item != null)
            {
                if (IsTextUnique)
                {
                    Text.ForceValue = FirstText;
                }
                else
                {
                    Text.ForceValue = "*";
                }
            }
        }
        private void UpdateCommandsFromItems()
        {
            if (BlockUpdates)
                return;

            commands.Clear();

            if (ConvertMultiTagModelItemCommand.CanConvertFrame(this))
            {
                var frameDescs = ConvertMultiTagModelItemCommand.PossibleFrameConversions(this);

                foreach (var frameDesc in frameDescs)
                {
                    commands.Add(new ConvertMultiTagModelItemCommand(this, frameDesc));
                }
            }
        }

        public bool BlockUpdates
        {
            get
            {
                return Items.Transaction.Value == 1;
            }
            set
            {
                if (value)
                {
                    Items.Transaction.Value = 1;
                }
                else
                {
                    Items.Transaction.Value = 0;
                }
            }
        }

        public bool IsFixed
        {
            get;
            private set;
        }
    }
}
