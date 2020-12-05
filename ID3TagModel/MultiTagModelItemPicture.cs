using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Windows.Media.Imaging;
using System.IO;
using CoreLogging;
using ID3;
using CoreDocument;
using CoreUtils;

namespace ID3TagModel
{
    public class MultiTagModelItemPicture
        : MultiTagModelItem
        , IDropTargetProvider
    {
        private DocListObjListener<TagModelItem, byte[]> contentAdapter
            = new DocListObjListener<TagModelItem, byte[]>();

        public MultiTagModelItemPicture()
        {
            EventHandler imageChangedTrigger = delegate(object sender, EventArgs e)
            {
                if (BlockUpdates)
                    return;

                UpdateImageFromItems();
            };

            contentAdapter.LinkedToHook = false;
            contentAdapter.RegardListTransaction = true;
            contentAdapter.PropertyProvider = TagModelItemPicture.ContentItemProvider;
            contentAdapter.PropertyChangedEvent += imageChangedTrigger;
            contentAdapter.Items = Items;
        }

        protected override void UpdateFromItems()
        {
            base.UpdateFromItems();

            if (BlockUpdates)
                return;

            UpdateImageFromItems();
        }
        private void UpdateImageFromItems()
        {
            bitmapFrameCache = null;
            NotifyPropertyChanged(this, m => m.Image);
        }

        public IDropTarget DropTarget
        {
            get
            {
                return new DropTargetProvider(this);
            }
        }

        class DropTargetProvider : IDropTarget
        {
            public DropTargetProvider(MultiTagModelItem item)
            {
                Item = item;
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
                return Item.FirstItem is TagModelItemPicture || Item.IsFixed;
            }
            public void Drop(object obj)
            {
                if (Item.IsFixed)
                {
                    Item.ParentModel.TagModels.DropPicture(obj.ToString());
                }
                else
                {
                    IEnumerable<TagModelItem> itemsToRemove =
                        (from item in Item.Items
                         select item as TagModelItemPicture).ToArray();

                    HashSet<TagModel> modelMap = new HashSet<TagModel>();
                    itemsToRemove.ForEach(n => modelMap.Add(n.TagModel));
                    itemsToRemove.ForEach(n => n.TagModel.Remove(n));

                    foreach (TagModel model in modelMap)
                    {
                        model.Create(ID3.FrameMeaning.Picture);

                        (from i in model.Items
                         where i.Meaning == ID3.FrameMeaning.Picture
                         select i as TagModelItemPicture)
                         .ForEach(n => n.LoadPicture(obj.ToString()));
                    }
                }
            }

            private MultiTagModelItem Item
            {
                get;
                set;
            }
        }

        public BitmapFrame Image
        {
            get
            {
                TagModelItemPicture item = Items.FirstOrDefault() as TagModelItemPicture;

                if (!Object.ReferenceEquals(item, null)
                    && Object.ReferenceEquals(bitmapFrameCache, null)
                    && item.Content.Value != null && item.Content.Value.Length > 0)
                {
                    try
                    {
                        BitmapDecoder decoder = TagModelItemPicture.DecoderByMimeType(
                            Images.ArrayToMimeType(item.Content.Value),
                            new MemoryStream(item.Content.Value));

                        if (!Object.ReferenceEquals(decoder, null))
                        {
                            bitmapFrameCache = decoder.Frames[0];
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine(Tokens.Exception, ex);
                    }
                }

                return bitmapFrameCache;
            }
        }
        private BitmapFrame bitmapFrameCache;
    }
}
