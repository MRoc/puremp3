using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using ID3.IO;
using ID3.Utils;
using CoreUtils;
using CoreTest;

namespace ID3.Codec
{
    public abstract class FrameContentCodecBase : IVersionable
    {
        public abstract FrameDescription.FrameType Type { get; }
        public abstract void Read(Stream stream, int count, FrameContent frameContent);
        public abstract void Write(Stream stream, FrameContent frameContent);
        public virtual int RequiredBytes(
            FrameContent frameContent,
            bool desynchronization,
            Reader.UnsyncMode unsyncMode)
        {
            if (unsyncMode == Reader.UnsyncMode.CountIncludesUnsyncBytes)
            {
                using (Writer writer = new Writer())
                {
                    writer.Unsynchronization = desynchronization;

                    using (WriterStream stream = new WriterStream(writer))
                    {
                        frameContent.Codec.Write(stream, frameContent);
                    }

                    writer.Flush();

                    return (int) writer.Length;
                }
            }
            else if (unsyncMode == Reader.UnsyncMode.CountExcludesUnsyncBytes)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    frameContent.Codec.Write(stream, frameContent);

                    return (int)stream.Length;
                }
            }
            else
            {
                throw new NotSupportedException("Unknown unsync mode");
            }
        }
        public void CheckEnoughDataForRead(Stream stream, int numBytes)
        {
            if (stream.Length - stream.Position < numBytes)
            {
                throw new CorruptFrameContentException("Non standard frame content!");
            }
        }

        public void CheckVersion(Version version)
        {
            if (!this.IsSupported(version))
            {
                throw new VersionInvariant("Check failed: frame content codec version");
            }
        }
        public abstract Version[] SupportedVersions { get; }

        public abstract FrameContentCodecBase Clone();
    }
    public class FrameContentCodecGeneric : FrameContentCodecBase
    {
        private IList<CodecItem> codecItems;
        private CodecState state = new CodecState();
        private Version[] supportedVersions;
        private FrameDescription.FrameType type;

        public FrameContentCodecGeneric(
            IList<CodecItem> codecItems,
            TextCodecSet textCodecSet,
            Version[] supportedVersions,
            FrameDescription.FrameType type)
        {
            this.codecItems = codecItems;
            this.state.TextCodecSet = textCodecSet;
            this.supportedVersions = supportedVersions;
            this.type = type;
        }
        public FrameContentCodecGeneric(CodecDescription codec)
        {
            this.codecItems = codec.Entries;
            this.state.TextCodecSet = codec.TextCodecSet;
            this.supportedVersions = codec.SupportedVersions;
            this.type = codec.Type;
        }
        public FrameContentCodecGeneric(FrameContentCodecGeneric other)
        {
            this.codecItems = other.codecItems;
            this.state.TextCodecSet = other.state.TextCodecSet;
            this.supportedVersions = other.supportedVersions;
            this.type = other.type;
        }

        public override void Read(Stream stream, int count, FrameContent content)
        {
            state.ItemCount = codecItems.Count;

            for (state.ItemIndex = 0; state.ItemIndex < state.ItemCount; state.ItemIndex++)
            {
                CodecItem entry = codecItems[state.ItemIndex];

                object value = entry.Read(state, stream);
                object target = ObjectByTarget(entry.DestinationObject, content);
                SetProperty(PropInfo(target, entry.PropertyName), target, value);
            }
        }
        public override void Write(Stream stream, FrameContent content)
        {
            state.ItemCount = codecItems.Count;

            for (state.ItemIndex = 0; state.ItemIndex < state.ItemCount; state.ItemIndex++)
            {
                CodecItem entry = codecItems[state.ItemIndex];

                object target = ObjectByTarget(entry.DestinationObject, content);
                object value = GetProperty(PropInfo(target, entry.PropertyName), target);

                entry.Write(state, stream, value);
            }
        }

        public int CurrentTextCodec
        {
            get
            {
                return state.CurrentTextCodecIndex;
            }
            set
            {
                state.CurrentTextCodec = state.TextCodecSet.CodecByCodecType(value);
            }
        }

        private object ObjectByTarget(CodecItem.Dsts target, object content)
        {
            if (target == CodecItem.Dsts.Codec)
            {
                return state;
            }
            else if (target == CodecItem.Dsts.Content)
            {
                return content;
            }

            throw new Exception("ObjectByTarget failed");
        }
        private PropertyInfo PropInfo(object obj, string name)
        {
            return obj.GetType().GetProperty(name);
        }
        private void SetProperty(PropertyInfo p, object target, object value)
        {
            p.SetValue(target, value, null);
        }
        private object GetProperty(PropertyInfo p, object target)
        {
            return p.GetValue(target, null);
        }

        public override Version[] SupportedVersions
        {
            get
            {
                return supportedVersions;
            }
        }
        public override FrameDescription.FrameType Type
        {
            get
            {
                return type;
            }
        }

        public override FrameContentCodecBase Clone()
        {
            return new FrameContentCodecGeneric(this);
        }

        public override string ToString()
        {
            return "TextCodec=" + state.CurrentTextCodec.GetType().Name;
        }
    }

    class FrameContentCodec1_0 : FrameContentCodecBase
    {
        public string FrameId
        {
            get;
            set;
        }

        public override FrameDescription.FrameType Type
        {
            get { return FrameDescription.FrameType.Text; }
        }
        public override Version[] SupportedVersions
        {
            get
            {
                return Version.vs1_0;
            }
        }
        public override void Read(Stream stream, int count, FrameContent content)
        {
            CodecState state = new CodecState();
            CodecItem codecItem = CodecItemByFrameId(FrameId);

            (content as FrameContentText).Text = codecItem.Read(state, stream) as string;
        }
        public override void Write(Stream stream, FrameContent content)
        {
            CodecState state = new CodecState();
            CodecItem codecItem = CodecItemByFrameId(FrameId);

            codecItem.Write(state, stream, (content as FrameContentText).Text);
        }

        public override FrameContentCodecBase Clone()
        {
            return instance;
        }

        public static FrameContentCodec1_0 instance = new FrameContentCodec1_0();

        private static CodecItem CodecItemByFrameId(string frameId)
        {
            if (!codecItemsByFrameId.ContainsKey(frameId))
            {
                codecItemsByFrameId[frameId] = new CodecItem(
                    CreateSerializerByFrameId(frameId),
                    CreateConverterByFrameId(frameId),
                    CodecItem.Dsts.Content,
                    ContentPropertyNameByFrameId(frameId));
            }

            return codecItemsByFrameId[frameId];
        }
        private static ICodecItemSerializer CreateSerializerByFrameId(string frameId)
        {
            TagDescription tg = TagDescriptionMap.Instance[Version.v1_0];

            if (frameId == tg[FrameMeaning.Title].FrameId
                || frameId == tg[FrameMeaning.Artist].FrameId
                || frameId == tg[FrameMeaning.Album].FrameId)
            {
                return new CodecItemSerializerStringFixLen_ISO_8859_1(30);
            }
            else if (frameId == tg[FrameMeaning.Comment].FrameId)
            {
                return new CodecItemSerializerStringFixLen_ISO_8859_1(29);
            }
            else if (frameId == tg[FrameMeaning.ReleaseYear].FrameId)
            {
                return new CodecItemSerializerStringFixLen_ISO_8859_1(4);
            }
            else if (frameId == tg[FrameMeaning.TrackNumber].FrameId
                || frameId == tg[FrameMeaning.ContentType].FrameId)
            {
                return new CodecItemSerializerByte();
            }

            return null;
        }
        private static ICodecItemConverter CreateConverterByFrameId(string frameId)
        {
            TagDescription tg = TagDescriptionMap.Instance[Version.v1_0];

            if (frameId == tg[FrameMeaning.TrackNumber].FrameId
                || frameId == tg[FrameMeaning.ContentType].FrameId)
            {
                return new CodecItemConverterByteToString();
            }

            return CodecItemConverterDefault.Instance;
        }
        private static string ContentPropertyNameByFrameId(string frameId)
        {
            return "Text";
        }

        private static Dictionary<string, CodecItem> codecItemsByFrameId = new Dictionary<string, CodecItem>();
    }

    public class TextCodecSet
    {
        public TextCodecSet(Version[] v)
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[v[0]];

            CodecClasses = tagDescription.TextCodecClasses;
            PreferredCodecClass = tagDescription.TextCodecClassPreferred;
        }

        private Type[] CodecClasses { get; set; }
        private Type PreferredCodecClass { get; set; }

        public TextCodec CodecByCodecType(int encoding)
        {
            return Activator.CreateInstance(CodecClasses[encoding]) as TextCodec;
        }
        public int CodecTypeByCodec(TextCodec encoder)
        {
            int index = 0;

            foreach (Type t in CodecClasses)
            {
                if (!Object.ReferenceEquals(t, null) && t.Equals(encoder.GetType()))
                {
                    return index;
                }

                index++;
            }

            throw new Exception("CodecTypeByCodec failed: " + encoder.GetType().ToString());
        }
        public TextCodec PreferredCodec()
        {
            return Activator.CreateInstance(PreferredCodecClass) as TextCodec;
        }
    }

    public class CodecItem
    {
        public enum Dsts
        {
            Codec,
            Content
        };

        public CodecItem(
            ICodecItemSerializer serializer,
            ICodecItemConverter converter,
            Dsts dst,
            string propertyName)
        {
            Serializer = serializer;
            Converter = converter;
            PropertyName = propertyName;
            DestinationObject = dst;
        }

        public ICodecItemSerializer Serializer { get; private set; }
        public ICodecItemConverter Converter { get; private set; }

        public Dsts DestinationObject { get; private set; }
        public string PropertyName { get; private set; }

        public object Read(CodecState state, Stream stream)
        {
            return Converter.ConvertAfterRead(Serializer.Read(state, stream));
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            Serializer.Write(state, stream, Converter.ConvertBeforeWrite(obj));
        }
    }
    public class CodecDescription : IVersionable
    {
        private List<CodecItem> entries = new List<CodecItem>();
        private TextCodecSet textCodecSet;
        private Version[] supportedVersions;
        private FrameDescription.FrameType type;

        public CodecDescription(Version[] versions, FrameDescription.FrameType type)
        {
            this.supportedVersions = versions;
            this.type = type;
            this.textCodecSet = new TextCodecSet(versions);
        }

        public CodecDescription Add(
            ICodecItemSerializer serializer,
            string dstProperty)
        {
            entries.Add(new CodecItem(
                serializer,
                new CodecItemConverterDefault(),
                CodecItem.Dsts.Content,
                dstProperty));

            return this;
        }
        public CodecDescription Add(
            ICodecItemSerializer serializer,
            ICodecItemConverter converter,
            string dstProperty)
        {
            entries.Add(new CodecItem(
                serializer,
                converter,
                CodecItem.Dsts.Content,
                dstProperty));

            return this;
        }
        public CodecDescription AddTextCodec()
        {
            entries.Add(new CodecItem(
                new CodecItemSerializerByte(),
                new CodecItemConverterDefault(),
                CodecItem.Dsts.Codec,
                "CurrentTextCodecIndex"));
            return this;
        }

        public IList<CodecItem> Entries
        {
            get
            {
                return entries;
            }
        }
        public TextCodecSet TextCodecSet
        {
            get
            {
                return textCodecSet;
            }
        }

        public virtual Version[] SupportedVersions
        {
            get
            {
                return supportedVersions;
            }
        }
        public FrameDescription.FrameType Type
        {
            get
            {
                return type;
            }
        }
    }
    public class CodecState
    {
        public int ItemCount { get; set; }
        public int ItemIndex { get; set; }

        public TextCodecSet TextCodecSet
        {
            get
            {
                return textCodecSet;
            }
            set
            {
                textCodecSet = value;

                if (!Object.ReferenceEquals(textCodecSet, null))
                {
                    CurrentTextCodec = textCodecSet.PreferredCodec();
                }
            }
        }
        public int CurrentTextCodecIndex
        {
            get
            {
                return TextCodecSet.CodecTypeByCodec(currentTextCodec);
            }
            set
            {
                currentTextCodec = TextCodecSet.CodecByCodecType(value);
            }
        }
        public TextCodec CurrentTextCodec
        {
            get
            {
                return currentTextCodec;
            }
            set
            {
                currentTextCodec = value;
            }
        }

        public void CheckEnoughDataForRead(Stream stream, int numBytes)
        {
            if (stream.Length - stream.Position < numBytes)
            {
                throw new CorruptFrameContentException("Non standard frame content!");
            }
        }

        private TextCodecSet textCodecSet;
        private TextCodec currentTextCodec;
    }

    public interface ICodecItemSerializer
    {
        object Read(CodecState state, Stream stream);
        void Write(CodecState state, Stream stream, object obj);
    }
    class CodecItemSerializerByte : ICodecItemSerializer
    {
        public object Read(CodecState state, Stream stream)
        {
            state.CheckEnoughDataForRead(stream, 1);
            return (int)stream.ReadByte();
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            stream.WriteByte((byte)(int)obj);
        }
    }
    class CodecItemSerializerBinary : ICodecItemSerializer
    {
        public object Read(CodecState state, Stream stream)
        {
            int length = (int)(stream.Length - stream.Position);
            byte[] data = new byte[length];
            int bytesRead = stream.Read(data, 0, length);

            if (bytesRead < length)
            {
                byte[] copy = new byte[bytesRead];
                Array.Copy(data, copy, bytesRead);
                return copy;
            }
            else
            {
                return data;
            }
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            byte[] data = obj as byte[];
            stream.Write(data, 0, data.Length);
        }
    }
    class CodecItemSerializerStringFixLen_ISO_8859_1 : ICodecItemSerializer
    {
        public CodecItemSerializerStringFixLen_ISO_8859_1(int length)
        {
            Length = length;
        }
        public object Read(CodecState state, Stream stream)
        {
            state.CheckEnoughDataForRead(stream, Length);
            return ID3.Codec.TextEncoderISO_8859_1.Instance.ReadStringFixedLength(stream, Length);
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            ID3.Codec.TextEncoderISO_8859_1.Instance.WriteStringFixedLength(
                stream, (string)obj, Length);
        }
        public int Length { get; set; }
    }
    class CodecItemSerializerString_ISO_8859_1 : ICodecItemSerializer
    {
        public object Read(CodecState state, Stream stream)
        {
            return ID3.Codec.TextEncoderISO_8859_1.Instance.ReadString(stream, false);
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            ID3.Codec.TextEncoderISO_8859_1.Instance.WriteString(stream, (string)obj);
            if (state.ItemIndex < state.ItemCount - 1)
            {
                ID3.Codec.TextEncoderISO_8859_1.Instance.WriteDelimiter(stream);
            }
        }
    }
    class CodecItemSerializerString : ICodecItemSerializer
    {
        public object Read(CodecState state, Stream stream)
        {
            return state.CurrentTextCodec.ReadString(stream, false);
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            state.CurrentTextCodec.WriteString(stream, (string)obj);
            if (state.ItemIndex < state.ItemCount - 1)
            {
                state.CurrentTextCodec.WriteDelimiter(stream);
            }
        }
    }
    class CodecItemSerializerStringTerminating : ICodecItemSerializer
    {
        public object Read(CodecState state, Stream stream)
        {
            return state.CurrentTextCodec.ReadString(stream, true);
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            state.CurrentTextCodec.WriteString(stream, (string)obj);
            if (state.ItemIndex < state.ItemCount - 1)
            {
                state.CurrentTextCodec.WriteDelimiter(stream);
            }
        }
    }
    class CodecItemSerializerStringList : ICodecItemSerializer
    {
        public object Read(CodecState state, Stream stream)
        {
            return state.CurrentTextCodec.ReadStrings(stream);
        }
        public void Write(CodecState state, Stream stream, object obj)
        {
            state.CurrentTextCodec.WriteStrings(stream, (List<string>)obj);
        }
    }

    public interface ICodecItemConverter
    {
        object ConvertAfterRead(object src);
        object ConvertBeforeWrite(object src);
    }
    class CodecItemConverterDefault : ICodecItemConverter
    {
        public object ConvertAfterRead(object src)
        {
            return src;
        }
        public object ConvertBeforeWrite(object src)
        {
            return src;
        }

        public static CodecItemConverterDefault Instance = new CodecItemConverterDefault();
    }
    class CodecItemConverterStringMap : ICodecItemConverter
    {
        public CodecItemConverterStringMap(string[] fileText, string[] documentText)
        {
            Debug.Assert(fileText.Length == documentText.Length);

            for (int i = 0; i < fileText.Length; i++)
            {
                fileToDocument.Add(fileText[i].ToLower(), documentText[i]);
                documentToFile.Add(documentText[i].ToLower(), fileText[i]);
            }
        }
        public object ConvertAfterRead(object src)
        {
            return fileToDocument[(src as string).ToLower()];
        }
        public object ConvertBeforeWrite(object src)
        {
            return documentToFile[(src as string).ToLower()];
        }

        private Dictionary<string, string> fileToDocument = new Dictionary<string, string>();
        private Dictionary<string, string> documentToFile = new Dictionary<string, string>();
    }
    class CodecItemConverterIntClip : ICodecItemConverter
    {
        public CodecItemConverterIntClip(int min, int max)
        {
            Min = min;
            Max = max;
        }
        public object ConvertAfterRead(object src)
        {
            int v = (int)src;

            v = Math.Max(v, Min);
            v = Math.Min(v, Max);

            return v;
        }
        public object ConvertBeforeWrite(object src)
        {
            return src;
        }

        public int Min { get; set; }
        public int Max { get; set; }
    }
    class CodecItemConverterBinaryShrinkZeroArray : ICodecItemConverter
    {
        public object ConvertAfterRead(object src)
        {
            byte[] arr = src as byte[];

            if (!Object.ReferenceEquals(arr, null) && ArrayUtils.IsZero(arr))
            {
                return emptyArray;
            }

            return src;
        }
        public object ConvertBeforeWrite(object src)
        {
            return src;
        }

        private static readonly byte[] emptyArray = new byte[] { };
    }
    class CodecItemConverterByteToString : ICodecItemConverter
    {
        public object ConvertAfterRead(object src)
        {
            return src.ToString();
        }
        public object ConvertBeforeWrite(object src)
        {
            int num = 0;

            string text = src as string;
            if (!String.IsNullOrEmpty(text))
            {
                try
                {
                    num = Int32.Parse(text);
                }
                catch (Exception)
                {
                    try
                    {
                        num = Int32.Parse(text.Split('/')[0]);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return num;
        }
    }

    public class CodecRepository
    {
        private static CodecRepository instance = new CodecRepository();

        private Dictionary<Version, Dictionary<FrameDescription.FrameType, CodecDescription>> codecRepository =
            new Dictionary<Version, Dictionary<FrameDescription.FrameType, CodecDescription>>();

        private CodecRepository()
        {
            CodecRepositoryBuilder.Register(this);
        }

        public void Add(CodecDescription c)
        {
            foreach (Version v in c.SupportedVersions)
            {
                if (!codecRepository.ContainsKey(v))
                {
                    codecRepository.Add(v, new Dictionary<FrameDescription.FrameType, CodecDescription>());
                }

                if (!codecRepository[v].ContainsKey(c.Type))
                {
                    codecRepository[v].Add(c.Type, c);
                }
                else
                {
                    throw new Exception("Codec already added");
                }
            }
        }
        public CodecDescription Codec(Version v, FrameDescription.FrameType t)
        {
            return codecRepository[v][t];
        }
        public FrameContentCodecBase Parser(Version v, FrameDescription.FrameType t)
        {
            if (v == Version.v1_0 && t == FrameDescription.FrameType.Text)
            {
                return FrameContentCodec1_0.instance;
            }
            else
            {
                return new FrameContentCodecGeneric(Codec(v, t));
            }
        }

        public bool HasCodec(Version v, FrameDescription.FrameType t)
        {
            return (v == Version.v1_0 && t == FrameDescription.FrameType.Text)
                || (codecRepository.ContainsKey(v) && codecRepository[v].ContainsKey(t));
        }

        public static CodecRepository Instance
        {
            get
            {
                return instance;
            }
        }
    }

    class CodecRepositoryBuilder
    {
        public static void Register(CodecRepository repository)
        {
            repository.Add(new CodecDescription(Version.versions, FrameDescription.FrameType.Binary)
                .Add(new CodecItemSerializerBinary(), "Content"));

            repository.Add(new CodecDescription(Version.vs2_0And2_3And2_4, FrameDescription.FrameType.URL)
                .Add(new CodecItemSerializerString_ISO_8859_1(), "Url"));

            repository.Add(new CodecDescription(Version.vs2_0And2_3, FrameDescription.FrameType.Text)
                .AddTextCodec()
                .Add(new CodecItemSerializerStringTerminating(), "Text"));

            repository.Add(new CodecDescription(Version.vs2_0And2_3And2_4, FrameDescription.FrameType.StringList)
                .AddTextCodec()
                .Add(new CodecItemSerializerStringList(), "Texts"));

            repository.Add(new CodecDescription(Version.vs2_0And2_3, FrameDescription.FrameType.Comment)
                .AddTextCodec()
                .Add(new CodecItemSerializerStringFixLen_ISO_8859_1(3), "Language")
                .Add(new CodecItemSerializerString(), "Description")
                .Add(new CodecItemSerializerStringList(), "Texts"));

            repository.Add(new CodecDescription(Version.vs2_0And2_3, FrameDescription.FrameType.UserText)
                .AddTextCodec()
                .Add(new CodecItemSerializerString(), "Description")
                .Add(new CodecItemSerializerString(), "Text"));

            repository.Add(new CodecDescription(Version.vs2_0And2_3, FrameDescription.FrameType.UserURL)
                .AddTextCodec()
                .Add(new CodecItemSerializerString(), "Description")
                .Add(new CodecItemSerializerString_ISO_8859_1(), "Url"));


            repository.Add(new CodecDescription(Version.vs2_4, FrameDescription.FrameType.Text)
                .AddTextCodec()
                .Add(new CodecItemSerializerStringTerminating(), "Text"));

            repository.Add(new CodecDescription(Version.vs2_4, FrameDescription.FrameType.Comment)
                .AddTextCodec()
                .Add(new CodecItemSerializerStringFixLen_ISO_8859_1(3), "Language")
                .Add(new CodecItemSerializerString(), "Description")
                .Add(new CodecItemSerializerStringList(), "Texts"));

            repository.Add(new CodecDescription(Version.vs2_4, FrameDescription.FrameType.UserText)
                .AddTextCodec()
                .Add(new CodecItemSerializerString(), "Description")
                .Add(new CodecItemSerializerStringList(), "Texts"));

            repository.Add(new CodecDescription(Version.vs2_4, FrameDescription.FrameType.UserURL)
                .AddTextCodec()
                .Add(new CodecItemSerializerString(), "Description")
                .Add(new CodecItemSerializerString_ISO_8859_1(), "Url"));

            repository.Add(new CodecDescription(Version.vs2_0, FrameDescription.FrameType.Picture)
                .AddTextCodec()
                .Add(
                    new CodecItemSerializerStringFixLen_ISO_8859_1(3),
                    new CodecItemConverterStringMap(
                        new string[]
                        {
                            "PNG",
                            "JPG",
                            "BMP",
                        },
                        new string[]
                        {
                            Images.MimeTypeToMimeTypeText(Images.MimeType.Png),
                            Images.MimeTypeToMimeTypeText(Images.MimeType.Jpg),
                            Images.MimeTypeToMimeTypeText(Images.MimeType.Bmp),
                        }),
                    "MimeTypeText")
                .Add(
                    new CodecItemSerializerByte(),
                    new CodecItemConverterIntClip(0, FrameContentPicture.PictureTypes.Count - 1),
                    "PictureType")
                .Add(new CodecItemSerializerString(), "Description")
                .Add(
                    new CodecItemSerializerBinary(),
                    new CodecItemConverterBinaryShrinkZeroArray(),
                    "Content"));

            repository.Add(new CodecDescription(Version.vs2_3, FrameDescription.FrameType.Picture)
                .AddTextCodec()
                .Add(new CodecItemSerializerString_ISO_8859_1(), "MimeTypeText")
                .Add(
                    new CodecItemSerializerByte(),
                    new CodecItemConverterIntClip(0, FrameContentPicture.PictureTypes.Count - 1),
                    "PictureType")
                .Add(new CodecItemSerializerString(), "Description")
                .Add(
                    new CodecItemSerializerBinary(),
                    new CodecItemConverterBinaryShrinkZeroArray(),
                    "Content"));

            repository.Add(new CodecDescription(Version.vs2_4, FrameDescription.FrameType.Picture)
                .AddTextCodec()
                .Add(new CodecItemSerializerString_ISO_8859_1(), "MimeTypeText")
                .Add(
                    new CodecItemSerializerByte(),
                    new CodecItemConverterIntClip(0, FrameContentPicture.PictureTypes.Count - 1),
                    "PictureType")
                .Add(new CodecItemSerializerString(), "Description")
                .Add(
                    new CodecItemSerializerBinary(),
                    new CodecItemConverterBinaryShrinkZeroArray(),
                    "Content"));
        }
    }

    public class TestFrameContentCodecs
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestFrameContentCodecs));      
        }
        
        private static void TestGenericCodecBinary()
        {
            byte[] data = { 0, 1, 2, 3, 4 };

            foreach (Version v in Version.vs2_0And2_3And2_4)
            {
                var fc = new FrameContentBinary(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc);

                UnitTest.Test(ArrayUtils.IsEqual(data, fc.Content));
            }
        }
        private static void TestGenericCodecText()
        {
            byte[] data = { (byte) 0, (byte) 'T', (byte) 'e', (byte) 'x', (byte) 't', (byte) '0' };

            foreach (Version v in Version.vs2_0And2_3And2_4)
            {
                var fc = new FrameContentText(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc);

                UnitTest.Test((fc.Codec as FrameContentCodecGeneric).CurrentTextCodec == 0);
                UnitTest.Test(fc.Text == "Text0");
            }
        }
        private static void TestGenericCodecUserText2_4()
        {
            byte[] data = 
            {
                (byte) 0,
                (byte) 'D', (byte) 'e', (byte) 's', (byte) 'c', (byte) 0, 
                (byte) 'T', (byte) 'e', (byte) 'x', (byte) 't', (byte) '0', (byte) 0,
                (byte) 'T', (byte) 'e', (byte) 'x', (byte) 't', (byte) '1'
            };

            foreach (Version v in Version.vs2_4)
            {
                var fc = new FrameContentUserText(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc);

                UnitTest.Test((fc.Codec as FrameContentCodecGeneric).CurrentTextCodec == 0);
                UnitTest.Test(fc.Description == "Desc");
                UnitTest.Test(fc.Texts.Count() == 2);
                UnitTest.Test(fc.Texts.ElementAt(0) == "Text0");
                UnitTest.Test(fc.Texts.ElementAt(1) == "Text1");
            }
        }
        private static void TestGenericCodecUserText2_0And2_3BinaryAttached()
        {
            byte[] data = 
            {
                (byte) 1,
                0xFF, 0xFE, (byte) 'T', 0, (byte) '0', 0, 0, 0,
                0xFF, 0xFE, (byte) 'T', 0, (byte) '1', 0, 0, 0, 0x1, 0x2, 0x3
            };

            foreach (Version v in Version.vs2_0And2_3)
            {
                var fc = new FrameContentUserText(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc, false);

                UnitTest.Test((fc.Codec as FrameContentCodecGeneric).CurrentTextCodec == 1);
                UnitTest.Test(fc.Description == "T0");
                UnitTest.Test(fc.Texts.Count() == 1);
                UnitTest.Test(fc.Texts.ElementAt(0) == "T1");
            }
        }
        private static void TestGenericCodecComment()
        {
            byte[] data = 
            {
                (byte) 0,
                (byte) 'e', (byte) 'n', (byte) 'g',
                (byte) 'D', (byte) 'e', (byte) 's', (byte) 'c', (byte) 'r', (byte) 'i',
                (byte) 'p', (byte) 't', (byte) 'i', (byte) 'o', (byte) 'n',
                (byte) 0, 
                (byte) 'T', (byte) 'e', (byte) 'x', (byte) 't', (byte) '0',
                (byte) 0,
                (byte) 'T', (byte) 'e', (byte) 'x', (byte) 't', (byte) '1'
            };

            foreach (Version v in Version.vs2_0And2_3And2_4)
            {
                var fc = new FrameContentComment(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc);

                UnitTest.Test((fc.Codec as FrameContentCodecGeneric).CurrentTextCodec == 0);
                UnitTest.Test(fc.Language == "eng");
                UnitTest.Test(fc.Description == "Description");
                UnitTest.Test(fc.Texts.Count() == 2);
                UnitTest.Test(fc.Texts.ElementAt(0) == "Text0");
                UnitTest.Test(fc.Texts.ElementAt(1) == "Text1");
            }
        }
        private static void TestGenericCodecUrl()
        {
            byte[] data = { (byte) 'U', (byte) 'r', (byte) 'l' };

            foreach (Version v in Version.vs2_0And2_3And2_4)
            {
                var fc = new FrameContentUrlLink(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc);

                UnitTest.Test(fc.Url == "Url");
            }
        }
        private static void TestGenericCodecUserUrl()
        {
            byte[] data = 
            {
                (byte) 0,
                (byte) 'D', (byte) 'e', (byte) 's', (byte) 'c', (byte) 'r', (byte) 'i',
                (byte) 'p', (byte) 't', (byte) 'i', (byte) 'o', (byte) 'n',
                (byte) 0, 
                (byte) 'U', (byte) 'r', (byte) 'l'
            };

            foreach (Version v in Version.vs2_0And2_3And2_4)
            {
                var fc = new FrameContentUserUrlLink(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc);

                UnitTest.Test((fc.Codec as FrameContentCodecGeneric).CurrentTextCodec == 0);
                UnitTest.Test(fc.Description == "Description");
                UnitTest.Test(fc.Url == "Url");
            }
        }
        private static void TestGenericCodecPicture()
        {
            byte[] data20 = 
            {
                (byte) 0,
                (byte) 'P', (byte) 'N', (byte) 'G',
                (byte) 3,
                (byte) 'T', (byte) 'e', (byte) 'x', (byte) 't', (byte) '0',
                (byte) 0,
                (byte) 1, (byte) 2
            };
            byte[] dataAbove20 = 
            {
                (byte) 0,
                (byte) 'i', (byte) 'm', (byte) 'a', (byte) 'g', (byte) 'e', (byte) '/',
                (byte) 'p', (byte) 'n', (byte) 'g',
                (byte) 0, 
                (byte) 3,
                (byte) 'T', (byte) 'e', (byte) 'x', (byte) 't', (byte) '0',
                (byte) 0,
                (byte) 1, (byte) 2
            };
            var dataByVersion = new Dictionary<Version, byte[]>()
            {
                { Version.v2_0, data20 },
                { Version.v2_3, dataAbove20 },
                { Version.v2_4, dataAbove20 },
            };

            foreach (var item in dataByVersion)
            {
                Version v = item.Key;
                byte[] data = item.Value;

                var fc = new FrameContentPicture(TagDescriptionMap.Instance[v]);

                TestCodec(data, fc);

                UnitTest.Test((fc.Codec as FrameContentCodecGeneric).CurrentTextCodec == 0);
                UnitTest.Test(fc.MimeTypeText.ToLower().Contains("png"));
                UnitTest.Test(fc.PictureType == 3);
                UnitTest.Test(fc.Description == "Text0");
                UnitTest.Test(fc.Content.Length == 2);
                UnitTest.Test(fc.Content[0] == 1);
                UnitTest.Test(fc.Content[1] == 2);
            }
        }
        private static void TestGenericCodecPicture_2_0()
        {
            byte[] head = 
            {
                (byte) 0,
                (byte) 'P', (byte) 'N', (byte) 'G',
                (byte) 0,
                (byte) 0,
            };
            byte[] payload = TestTags.demoPicturePng;

            byte[] data = head.Concat(payload).ToArray();

            Version v = Version.v2_0;
            var fc = new FrameContentPicture(TagDescriptionMap.Instance[v]);

            TestCodec(data, fc);
            UnitTest.Test((fc.Codec as FrameContentCodecGeneric).CurrentTextCodec == 0);
            UnitTest.Test(fc.MimeTypeText.ToLower().Contains("png"));
            UnitTest.Test(fc.PictureType == 0);
            UnitTest.Test(fc.Description == "");
            UnitTest.Test(fc.Content.Length == TestTags.demoPicturePng.Length);
        }

        private static void TestCodec(byte[] data, FrameContent content, bool checkBinaryEqual = true)
        {
            using (MemoryStream memStr = new MemoryStream(data, 0, data.Length))
            {
                content.Codec.Read(memStr, data.Length, content);

                using (Writer writer = new Writer())
                {
                    using (WriterStream oStream = new WriterStream(writer))
                    {
                        content.Codec.Write(oStream, content);
                    }

                    if (checkBinaryEqual)
                    {
                        UnitTest.Test(ArrayUtils.IsEqual(data, writer.OutData));
                    }
                }
            }
        }
    }
}
