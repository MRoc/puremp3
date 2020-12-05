using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ID3.Utils;
using ID3.Codec;
using CoreUtils;

namespace ID3
{
    public abstract class FrameContent
    {
        private FrameContentCodecBase codec;

        public FrameContent(TagDescription description)
        {
            DescriptionMap = description;
        }
        public FrameContent(FrameContent other)
            : this(other.DescriptionMap)
        {
            Codec = other.Codec.Clone();
        }

        public TagDescription DescriptionMap
        {
            get;
            set;
        }
        public abstract FrameDescription.FrameType Type
        {
            get;
        }
        public FrameContentCodecBase Codec
        {
            get
            {
                if (Object.ReferenceEquals(codec, null))
                {
                    codec = DescriptionMap.CreateContentCodec(Type);
                }

                return codec;
            }
            set
            {
                if (!Object.ReferenceEquals(value, null)
                    && !Object.ReferenceEquals(DescriptionMap, null)
                    && !value.IsSupported(DescriptionMap.Version))
                {
                    throw new Exception("Version invariant: codec not supported");
                }

                codec = value;
            }
        }

        public abstract string Text
        {
            get;
            set;
        }
        public abstract bool IsEqual(Object other);

        public void CheckVersion(Version version)
        {
            if (!DescriptionMap.Version.Equals(version))
            {
                throw new VersionInvariant("Check failed: content description map version");
            }

            Codec.CheckVersion(version);
        }

        public override string ToString()
        {
            return Text;
        }

        public abstract FrameContent Clone();
    }
    public class FrameContentBinary : FrameContent
    {
        protected byte[] content;

        public FrameContentBinary(TagDescription description)
            : base(description)
        {
        }
        public FrameContentBinary(FrameContentBinary other)
            : base(other)
        {
            Content = other.Content.Clone() as byte[];
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.Binary;
            }
        }

        public byte[] Content
        {
            get
            {
                if (Object.ReferenceEquals(content, null))
                {
                    content = empty;
                }
                return content;
            }
            set
            {
                content = value;
            }
        }
        public int Length
        {
            get
            {
                return content.Length;
            }
        }

        public override string Text
        {
            get
            {
                return "BINARY";
            }
            set
            {
                //throw new Exception("FAILED");
            }
        }
        public override bool IsEqual(Object other)
        {
            return ArrayUtils.CountEquals(
                Content,
                (other as FrameContentBinary).Content) == Content.Length;
        }

        public override FrameContent Clone()
        {
            return new FrameContentBinary(this);
        }

        public static readonly byte[] empty = new byte[0];
    }
    public class FrameContentText : FrameContent
    {
        private List<string> texts = new List<string>();

        public FrameContentText(TagDescription description)
            : base(description)
        {
        }
        public FrameContentText(FrameContentText other)
            : base(other)
        {
            Texts = other.Texts.ToList();
        }
        public FrameContentText(string text)
            : base((TagDescription)null)
        {
            Text = text;
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.Text;
            }
        }

        public virtual IEnumerable<string> Texts
        {
            get
            {
                return texts;
            }
            set
            {
                texts = value.ToList();
            }
        }
        public string TextsAsString
        {
            get
            {
                return texts.Concatenate(", ");
            }
        }

        public override string Text
        {
            get
            {
                return TextsAsString;
            }
            set
            {
                texts = new List<string>() { value };
            }
        }
        public override bool IsEqual(Object other)
        {
            return this.Text == (other as FrameContent).Text;
        }

        public override FrameContent Clone()
        {
            return new FrameContentText(this);
        }
    }
    public class FrameContentUserText : FrameContentText
    {
        public FrameContentUserText(TagDescription tagDescription)
            : base(tagDescription)
        {
        }
        public FrameContentUserText(FrameContentUserText other)
            : base(other)
        {
            Description = other.Description;
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.UserText;
            }
        }

        public string Description
        {
            get;
            set;
        }

        public override FrameContent Clone()
        {
            return new FrameContentUserText(this);
        }
    }
    public class FrameContentComment : FrameContentText
    {
        private byte[] language = new byte[3];
        private string description;

        public FrameContentComment(TagDescription tagDescription)
            : base(tagDescription)
        {
        }
        public FrameContentComment(FrameContentComment other)
            : base(other)
        {
            Language = other.Language;
            Description = other.Description;
        }
        public FrameContentComment(string text)
            : base((TagDescription)null)
        {
            Text = text;
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.Comment;
            }
        }

        public string Description
        {
            get
            {
                if (description != null)
                {
                    return description;
                }
                else
                {
                    return "";
                }
            }
            set
            {
                description = value;
            }
        }
        public byte[] LanguageBytes
        {
            get { return language; }
            set { language = value; }
        }
        public string Language
        {
            get
            {
                return new string(new char[]
                {
                    (char)language[0],
                    (char)language[1],
                    (char)language[2]
                });
            }
            set
            {
                if (value.Length != 3 || value == "")
                {
                    language[0] = 0;
                    language[1] = 0;
                    language[2] = 0;
                }
                else
                {
                    language[0] = (byte)value[0];
                    language[1] = (byte)value[1];
                    language[2] = (byte)value[2];
                }
            }
        }

        public override FrameContent Clone()
        {
            return new FrameContentComment(this);
        }
    }
    public class FrameContentUrlLink : FrameContent
    {
        private string url;

        public FrameContentUrlLink(TagDescription description)
            : base(description)
        {
        }
        public FrameContentUrlLink(FrameContentUrlLink other)
            : base(other)
        {
            Url = other.Url;
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.URL;
            }
        }

        public string Url
        {
            get
            {
                if (Object.ReferenceEquals(url, null))
                {
                    url = "";
                }
                return url;
            }
            set
            {
                url = value;
            }
        }

        public override string Text
        {
            get
            {
                return Url;
            }
            set
            {
                Url = value;
            }
        }
        
        public override bool IsEqual(Object other)
        {
            return this.Text == (other as FrameContent).Text;
        }

        public override FrameContent Clone()
        {
            return new FrameContentUrlLink(this);
        }
    }
    public class FrameContentUserUrlLink : FrameContentUrlLink
    {
        private string description;

        public FrameContentUserUrlLink(TagDescription tagDescription)
            : base(tagDescription)
        {
        }
        public FrameContentUserUrlLink(FrameContentUserUrlLink other)
            : base(other)
        {
            Description = other.Description;
        }

        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.UserURL;
            }
        }

        public virtual string Description
        {
            get
            {
                if (Object.ReferenceEquals(description, null))
                {
                    description = "";
                }
                return description;
            }
            set
            {
                description = value;
            }
        }

        public override bool IsEqual(Object other)
        {
            return Text == (other as FrameContentUserUrlLink).Text
                && Description == (other as FrameContentUserUrlLink).Description;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Description);
            sb.Append(" ");
            sb.Append(Text);

            return sb.ToString();
        }

        public override FrameContent Clone()
        {
            return new FrameContentUserUrlLink(this);
        }
    }
    public class Images
    {
        public enum MimeType
        {
            Invalid,
            Jpg,
            Png,
            Bmp
        }

        public static MimeType TextToMimeType(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                return
                    (from imageType in descriptions.Values where imageType.IsValid
                    from imageText in imageType.Texts where text.ToLower().EndsWith(imageText)
                    select imageType.MimeType).FirstOrDefault();
            }
            else
            {
                return MimeType.Invalid;
            }
        }
        public static MimeType ArrayToMimeType(byte[] arr)
        {
            if (!Object.ReferenceEquals(arr, null) && arr.Length > 0)
            {
                return
                    (from imageType in descriptions.Values
                     where arr.StartsWith(imageType.MagicNumbers)
                     select imageType.MimeType).FirstOrDefault();
            }
            else
            {
                return MimeType.Invalid;
            }
        }

        public static string MimeTypeToFileSuffix(MimeType mimeType)
        {
            return descriptions[mimeType].Texts.First();
        }
        public static string MimeTypeToMimeTypeText(MimeType mimeType)
        {
            return descriptions[mimeType].MimeTypeText;
        }
        public static string CorrectFilenameByMimeType(MimeType mimeType, string filename)
        {
            return descriptions[mimeType].CorrectFilename(filename);
        }

        public class ImageTypeDescription
        {
            public ImageTypeDescription(MimeType mimeType, byte[] magicNumbers, string[] texts)
            {
                MimeType = mimeType;
                MagicNumbers = magicNumbers;
                Texts = texts;
            }

            public bool IsValid
            {
                get
                {
                    return MimeType != MimeType.Invalid;
                }
            }

            public MimeType MimeType
            {
                get;
                set;
            }
            public byte[] MagicNumbers
            {
                get;
                set;
            }
            public string[] Texts
            {
                get;
                set;
            }

            public string MimeTypeText
            {
                get
                {
                    if (Texts.Length > 0)
                    {
                        return "image/" + Texts[0];
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            public string CorrectFilename(string fileName)
            {
                bool found =
                    (from text
                    in Texts
                    where fileName.ToLower().EndsWith(text)
                    select text).Count() == 1;

                if (found)
                {
                    return fileName;
                }
                else
                {
                    return fileName + "." + Texts[0];
                }
            }
        }

        static Images()
        {
            descriptions[MimeType.Jpg] = new ImageTypeDescription(
                MimeType.Jpg,
                new byte[] { 0xFF, 0xD8 },
                new string[] { "jpg", "jpeg" });

            descriptions[MimeType.Png] = new ImageTypeDescription(
                MimeType.Png,
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
                new string[] { "png" });

            descriptions[MimeType.Bmp] = new ImageTypeDescription(
                MimeType.Bmp,
                new byte[] { 0x42, 0x4D },
                new string[] { "bmp" });

            descriptions[MimeType.Invalid] = new ImageTypeDescription(
                MimeType.Invalid,
                new byte[] {},
                new string[] {});
        }

        public static Dictionary<MimeType, ImageTypeDescription> descriptions
            = new Dictionary<MimeType, ImageTypeDescription>();
    }
    public class FrameContentPicture : FrameContent
    {
        private string mimeTypeText;
        private int pictureType = 3;
        private string description;
        private byte[] content;

        public FrameContentPicture(TagDescription description)
            : base(description)
        {
        }
        public FrameContentPicture(FrameContentPicture other)
            : base(other)
        {
            MimeTypeText = other.MimeTypeText;
            PictureType = other.PictureType;
            Description = other.Description;
            Content = other.Content.Clone() as byte[];
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.Picture;
            }
        }

        public string MimeTypeText
        {
            get
            {
                if (Object.ReferenceEquals(mimeTypeText, null))
                {
                    mimeTypeText = "";
                }
                return mimeTypeText;
            }
            set
            {
                mimeTypeText = value;
            }
        }
        public Images.MimeType MimeType
        {
            get
            {
                return Images.TextToMimeType(MimeTypeText);
            }
            set
            {
                MimeTypeText = Images.MimeTypeToMimeTypeText(value);
            }
         }
        public string Description
        {
            get
            {
                if (Object.ReferenceEquals(description, null))
                {
                    description = "";
                }
                return description;
            }
            set
            {
                description = value;
            }
        }
        public int PictureType
        {
            get
            {
                return pictureType;
            }
            set
            {
                pictureType = value;
            }
        }
        public byte[] Content
        {
            get
            {
                if (Object.ReferenceEquals(content, null))
                {
                    content = FrameContentBinary.empty;
                }
                return content;
            }
            set
            {
                content = value;
            }
        }
        public int Length
        {
            get
            {
                return Content.Length;
            }
        }

        public override string Text
        {
            get
            {
                return Description;
            }
            set
            {
                Description = value;
            }
        }
        public override bool IsEqual(Object other)
        {
            return MimeTypeText == (other as FrameContentPicture).MimeTypeText
                && Description == (other as FrameContentPicture).Description
                && PictureType == (other as FrameContentPicture).PictureType
                && ArrayUtils.IsEqual(Content, (other as FrameContentPicture).Content);
        }

        public static List<string> PictureTypes
        {
            get
            {
                return new List<string>
                {
                    "Other",
                    "32x32 pixels 'file icon' (PNG only)",
                    "Other file icon",
                    "Cover (front)",
                    "Cover (back)",
                    "Leaflet page",
                    "Media (e.g. lable side of CD)",
                    "Lead artist/lead performer/soloist",
                    "Artist/performer",
                    "Conductor",
                    "Band/Orchestra",
                    "Composer",
                    "Lyricist/text writer",
                    "Recording Location",
                    "During recording",
                    "During performance",
                    "Movie/video screen capture",
                    "A bright coloured fish",
                    "Illustration",
                    "Band/artist logotype",
                    "Publisher/Studio logotype"
                };
            }
        }

        public override FrameContent Clone()
        {
            return new FrameContentPicture(this);
        }
    }
    public class FrameContentTextList : FrameContent
    {
        private List<string> texts = new List<string>();

        public FrameContentTextList(TagDescription tagDescription)
            : base(tagDescription)
        {
        }
        public FrameContentTextList(FrameContentTextList other)
            : base(other)
        {
            Texts = other.Texts.ToList();
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return FrameDescription.FrameType.StringList;
            }
        }

        public virtual List<string> Texts
        {
            get
            {
                return texts;
            }
            set
            {
                texts = value.ToList();
            }
        }
        public string TextsAsString
        {
            get
            {
                return texts.Concatenate(", ");
            }
        }

        public override string Text
        {
            get
            {
                return TextsAsString;
            }
            set
            {
                texts = new List<string>() { value };
            }
        }
        public override bool IsEqual(Object other)
        {
            return Text == (other as FrameContent).Text;
        }

        public override FrameContent Clone()
        {
            return new FrameContentTextList(this);
        }
    }

    public class FrameContentConverter
    {
        static FrameContentConverter()
        {
            conversions.Add(new FrameConversion(
                typeof(FrameContentComment),
                typeof(FrameContentText),
                n => new FrameContentText(n.Text)));

            conversions.Add(new FrameConversion(
                typeof(FrameContentText),
                typeof(FrameContentComment),
                n => new FrameContentComment(n.Text)));
        }
        private class FrameConversion
        {
            public FrameConversion(Type src, Type dst, Func<FrameContent, FrameContent> converter)
            {
                SrcType = src;
                DstType = dst;
                Converter = converter;
            }
            public Type SrcType
            {
                get;
                set;
            }
            public Type DstType
            {
                get;
                set;
            }
            public Func<FrameContent, FrameContent> Converter
            {
                get;
                set;
            }
        }
        private static List<FrameConversion> conversions = new List<FrameConversion>();

        public static bool CanConvert(Type t0, Type t1)
        {
            return
                (from item
                in conversions
                 where item.SrcType == t0 && item.DstType == t1
                 select item).Count() > 0;
        }
        public static FrameContent Convert(FrameContent fc, Type dst)
        {
            Func<FrameContent, FrameContent> converter =
                (from item
                in conversions
                where item.SrcType == fc.GetType() && item.DstType == dst
                select item.Converter).FirstOrDefault();

            if (Object.ReferenceEquals(converter, null))
            {
                throw new Exception("Conversion failed");
            }
            else
            {
                FrameContent result = converter(fc);

                if (result.GetType() != dst)
                {
                    throw new Exception("Conversion failed. Requested type: " + dst.Name
                        + " Converted type: " + result.GetType().Name);
                }

                return result;
            }
        }
    }
}
