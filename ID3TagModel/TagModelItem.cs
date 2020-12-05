using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CoreControls.Commands;
using CoreDocument;
using ID3;
using CoreDocument.Text;
using System.ComponentModel;
using CoreLogging;

namespace ID3TagModel
{
    public class TagModelItem : DocNode
    {
        public TagModelItem()
        {
            Help = new LocalizedText("TagModelItemHelp");
            FrameId = new DocObj<string>();
            FrameDescription = new DocObj<string>();
            Text = new DocObj<string>();
            IsTemplateItem = new DocObj<bool>();
        }

        public Frame Frame
        {
            get { return frame; }
            set
            {
                frame = value;

                if (frame != null)
                {
                    Load();
                }
            }
        }

        public DocObj<string> FrameId
        {
            get;
            private set;
        }
        public DocObj<string> FrameDescription
        {
            get;
            private set;
        }
        public DocObj<string> Text
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<bool> IsTemplateItem
        {
            get;
            set;
        }
        public bool IsTemplateItemValue
        {
            get
            {
                return CalculateIsTemplateItem;
            }
        }
        public FrameMeaning Meaning
        {
            get
            {
                FrameDescription desc = frame.DescriptionMap[frame.FrameId];

                if (Object.ReferenceEquals(desc, null))
                {
                    return FrameMeaning.Unknown;
                }
                else
                {
                    return desc.Meaning;
                }
            }
        }

        private TagModel model;
        [DocObjRef]
        public TagModel TagModel
        {
            get
            {
                return model;
            }
            set
            {
                if (model != value)
                {
                    model = value;
                    IsTemplateItem.Value = CalculateIsTemplateItem;
                }
            }
        }

        public virtual void Load()
        {
            FrameId.Value = Frame.FrameId;
            FrameDescription.Value = Frame.Description.ShortDescription;
            Text.Value = Frame.Content.Text;
        }
        public virtual void Commit()
        {
            Debug.Assert(FrameId.Value == Frame.FrameId);
            Debug.Assert(FrameDescription.Value == Frame.Description.ShortDescription);

            frame.Content.Text = Text.Value;
        }

        public override void ResolveParentLink(IDocNode parent, string name)
        {
            base.ResolveParentLink(parent, name);
            IsTemplateItem.Value = CalculateIsTemplateItem;
        }
        private bool CalculateIsTemplateItem
        {
            get
            {
                return !Object.ReferenceEquals(TagModel, null)
                    && !TagModel.Items.Contains(this);
            }
        }

        public virtual bool IsEqual(TagModelItem other)
        {
            if (Frame != null && other.Frame != null)
            {
                return Frame.Content.IsEqual(other.Frame.Content);
            }
            else
            {
                return false;
            }
        }
        public override string ToString()
        {
            return base.ToString() + " [" + Meaning + "]";
        }

        public static DocObj<string> TextItemProvider(object obj)
        {
            return (obj as TagModelItem).Text;
        }
        public static DocObj<bool> IsTemplateItemProvider(object obj)
        {
            return (obj as TagModelItem).IsTemplateItem;
        }

        protected Frame frame;

        CallbackCommand deleteCommand;
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
                                    TagModel.Remove(this);
                                },
                                GetHashCode(),
                                "TagModelItemDeleteTagModelItem");
                        },
                        delegate(object obj)
                        {
                            return !IsTemplateItem.Value;
                        },
                        new LocalizedText("TagModelItemDeleteTagModelItem"),
                        new LocalizedText("TagModelItemDeleteTagModelItemHelp"));

                    IsTemplateItem.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                    {
                        deleteCommand.TriggerCanExecute(sender, e);
                    };
                }

                return deleteCommand;
            }
        }
    }
    public class TagModelItemText : TagModelItem
    {
        public override bool IsEqual(TagModelItem other)
        {
            return Text.Value == other.Text.Value;
        }
    }
    public class TagModelItemComment : TagModelItem
    {
        public TagModelItemComment()
        {
            Language = new DocEnum(Languages);
            Description = new DocObj<string>();
        }

        public DocEnum Language
        {
            get;
            private set;
        }
        public DocObj<string> Description
        {
            get;
            private set;
        }

        public override bool IsEqual(TagModelItem other)
        {
            return Text.Value == other.Text.Value
                && Description.Value == ((TagModelItemComment)other).Description.Value
                && Language.Value == ((TagModelItemComment)other).Language.Value;
        }

        public override void Load()
        {
            base.Load();

            Language.TryValueStr = ((ID3.FrameContentComment)Frame.Content).Language;
            Description.Value = ((ID3.FrameContentComment)Frame.Content).Description;
        }
        public override void Commit()
        {
            ((FrameContentComment)frame.Content).Description = Description.Value;
            ((FrameContentComment)frame.Content).Language = Language.ValueStr;

            base.Commit();
        }

        private static ReadOnlyCollection<string> Languages
        {
            get
            {
                if (languages == null)
                {
                    languages = new ReadOnlyCollection<string>(
                        ID3.Utils.LanguageTable.LanguagesCodes);
                }

                return languages;
            }
        }
        private static ReadOnlyCollection<string> languages = null;
    }
    public class TagModelItemPicture : TagModelItem
    {
        public TagModelItemPicture()
        {
            PictureType = new DocEnum(ID3.FrameContentPicture.PictureTypes);
            Content = new DocObj<byte[]>();

            Content.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == DocBase.PropertyName(Content, m => m.Value))
                {
                    bitmapFrameCache = null;
                    NotifyPropertyChanged(this, m => m.Image);
                }
            };
        }

        public static BitmapDecoder DecoderByMimeType(
            ID3.Images.MimeType mimeType, Stream inStream)
        {
            if (mimeType == ID3.Images.MimeType.Png)
            {
                return new PngBitmapDecoder(
                    inStream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.Default);
            }
            else if (mimeType == ID3.Images.MimeType.Jpg)
            {
                return new JpegBitmapDecoder(
                    inStream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.Default);
            }
            else if (mimeType == ID3.Images.MimeType.Bmp)
            {
                return new BmpBitmapDecoder(
                    inStream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.Default);
            }
            else
            {
                return null;
            }
        }
        public static string BrowseForSaveByMimeType(
            ID3.Images.MimeType mimeType)
        {
            switch (mimeType)
            {
                case ID3.Images.MimeType.Jpg:
                    return CoreControls.FileBrowserUtils.BrowseSaveJpg();
                case ID3.Images.MimeType.Png:
                    return CoreControls.FileBrowserUtils.BrowseSavePng();
            }

            throw new Exception("Unknown mime type");
        }

        public DocEnum PictureType
        {
            get;
            private set;
        }
        public DocObj<byte[]> Content
        {
            get;
            private set;
        }

        public BitmapFrame Image
        {
            get
            {
                if (Object.ReferenceEquals(bitmapFrameCache, null)
                    && Content.Value != null && Content.Value.Length > 0)
                {
                    try
                    {
                        BitmapDecoder decoder = DecoderByMimeType(
                            Images.ArrayToMimeType(Content.Value),
                            new MemoryStream(Content.Value));

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

        public Images.MimeType CurrentMimeType
        {
            get
            {
                return Images.ArrayToMimeType(Content.Value);
            }
        }
        public string MimeText
        {
            get
            {
                return Images.MimeTypeToMimeTypeText(CurrentMimeType);
            }
        }
        public string InfoText
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(MimeText);

                if (Image != null)
                {
                    sb.Append(" ");
                    sb.Append(Math.Round(Image.Width));
                    sb.Append("x");
                    sb.Append(Math.Round(Image.Height));
                }

                return sb.ToString();
            }
        }

        public override void Load()
        {
            base.Load();

            ID3.FrameContentPicture fcp = (ID3.FrameContentPicture)Frame.Content;

            PictureType.Value = fcp.PictureType;
            Content.Value = fcp.Content;

            NotifyPropertyChanged(this, m => m.Image);
            NotifyPropertyChanged(this, m => m.InfoText);
        }
        public override void Commit()
        {
            ID3.FrameContentPicture fcp = (ID3.FrameContentPicture)Frame.Content;

            fcp.PictureType = PictureType.Value;
            fcp.Description = Text.Value;
            fcp.Content = Content.Value;
            fcp.MimeType = CurrentMimeType;
        }

        public static DocObj<byte[]> ContentItemProvider(object obj)
        {
            if (obj is TagModelItemPicture)
            {
                return (obj as TagModelItemPicture).Content;
            }
            else
            {
                return null;
            }
        }

        public void LoadPicture(string filename)
        {
            Content.Value = ID3.Utils.Id3FileUtils.LoadFileBinary(filename);
        }
        public void SavePicture(string filename)
        {
            ID3.Utils.Id3FileUtils.SaveFileBinary(
                Images.CorrectFilenameByMimeType(CurrentMimeType, filename),
                Content.Value);
        }

        public ICommand LoadPictureCommand
        {
            get
            {
                return new CallbackCommand(
                    delegate()
                    {
                        string filename = CoreControls.FileBrowserUtils.BrowseForImage();

                        if (System.IO.File.Exists(filename))
                        {
                            LoadPicture(filename);
                        }
                    },
                    new LocalizedText("Load"),
                    new LocalizedText("LoadPicture"));
            }
        }
        public ICommand SavePictureCommand
        {
            get
            {
                return new CallbackCommand(
                    delegate()
                    {
                        string filename = BrowseForSaveByMimeType(CurrentMimeType);

                        if (!String.IsNullOrEmpty(filename))
                        {
                            SavePicture(filename);
                        }
                    },
                    new LocalizedText("Save"),
                    new LocalizedText("SavePicture"));
            }
        }
    }
}
