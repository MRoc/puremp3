using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using ID3.Codec;
using ID3.Utils;
using CoreUtils;
using CoreTest;

namespace ID3
{
    public enum FrameMeaning
    {
        Unknown,
        Artist,
        Title,
        Album,
        TrackNumber,
        PartOfSet,
        Comment,
        Picture,
        ContentType,
        ReleaseYear,
        MusicCdIdentifier,
        LengthInMilliSeconds,
        Composer,
        BandOrchestraAccompaniment,
        ConductorPerformer,
        InterpretedRemixedModified,
        Publisher,
        Encoder,
    }

    public class FrameDescription
    {
        public enum FrameCategory
        {
            Standard,
            NonStandard,
            Invalid
        };

        public enum FrameType
        {
            Binary,
            Text,
            UserText,
            Comment,
            URL,
            UserURL,
            Picture,
            StringList,
        };

        public FrameDescription(
            string frameId,
            string description,
            string shortDescription,
            FrameCategory category,
            FrameType type,
            FrameMeaning meaning)
        {
            FrameId = frameId;
            Description = description;
            ShortDescription = shortDescription;
            Category = category;
            Type = type;
            Meaning = meaning;
        }

        public FrameDescription(XmlNode node)
        {
            XmlElement element = node as XmlElement;

            FrameId = element.GetAttribute("Id");

            Description = element.GetAttribute("Description");

            if (element.HasAttribute("ShortDescription"))
            {
                ShortDescription = element.GetAttribute("ShortDescription");
            }
            else
            {
                ShortDescription = Description;
            }

            Category = element.GetAttribute("Category").ToEnum<FrameCategory>();
            
            Type = element.GetAttribute("Type").ToEnum<FrameType>();

            if (element.HasAttribute("Meaning"))
            {
                Meaning = element.GetAttribute("Meaning").ToEnum<FrameMeaning>();
            }
        }

        public string FrameId { get; private set; }
        public string Description { get; private set; }
        public string ShortDescription { get; private set; }
        public FrameCategory Category { get; private set; }
        public FrameType Type { get; private set; }
        public FrameMeaning Meaning { get; private set; }

        public static bool IsExperimentalFrameId(string frameId)
        {
            return frameId.StartsWith("X")
                || frameId.StartsWith("Y")
                || frameId.StartsWith("Z");
        }
        public static bool MaybeFrameId(string frameId)
        {
            bool isUpperChar = true;

            foreach (var item in frameId)
            {
                if (! (Char.IsUpper(item) || Char.IsDigit(item)))
                {
                    isUpperChar = false;
                }
            }

            return isUpperChar;
        }

        public override string ToString()
        {
            return FrameId + " " + Description;
        }
    }
    public class FrameConversionMap
    {
        private Dictionary<string, string> conversionMap = new Dictionary<string, string>();
        private Dictionary<string, string> conversionMapDown = new Dictionary<string, string>();

        public void Add(string frameIdOld, string frameIdNew)
        {
            conversionMap[frameIdOld] = frameIdNew;
            conversionMapDown[frameIdNew] = frameIdOld;
        }
        public bool HasConversion(string frameIdOld)
        {
            return conversionMap.ContainsKey(frameIdOld);
        }
        public bool HasConversionDown(string frameIdOld)
        {
            return conversionMapDown.ContainsKey(frameIdOld);
        }
        public string Conversion(string frameIdOld)
        {
            return conversionMap[frameIdOld];
        }
        public string ConversionDown(string frameIdOld)
        {
            return conversionMapDown[frameIdOld];
        }

        public IEnumerable<string> OldFrameIds
        {
            get { return conversionMap.Keys; }
        }
        public IEnumerable<string> NewFrameIds
        {
            get { return conversionMapDown.Keys; }
        }
    }
    public class TagDescription : IVersionable
    {
        #region(MEMBERS)
        public Dictionary<FrameDescription.FrameType, Type> contentClasses = new Dictionary<FrameDescription.FrameType, Type>();
        private Dictionary<string, FrameDescription> descriptions = new Dictionary<string, FrameDescription>();
        private Dictionary<FrameMeaning, FrameDescription> descByMeaning = new Dictionary<FrameMeaning, FrameDescription>();
        private FrameConversionMap conversionMap = new FrameConversionMap();
        private HashSet<string> invalidButKnownPadding = new HashSet<string>();
        #endregion

        public TagDescription(Version v)
        {
            Version = v;
        }

        public Version Version { get; private set; }
        public virtual Version[] SupportedVersions
        {
            get
            {
                return new Version[] { Version };
            }
        }

        public Type TagClass { get; set; }
        public Type TagCodecClass { get; set; }
        public Type FrameClass { get; set; }
        public Type FrameCodecClass { get; set; }
        public Type ContentClass(FrameDescription.FrameType _type)
        {
            return contentClasses[_type];
        }
        public Type[] TextCodecClasses { get; set; }
        public Type TextCodecClassPreferred { get; set; }

        public Tag CreateTag()
        {
            return (Tag)Activator.CreateInstance(TagClass, new Object[] { this });
        }
        public TagCodec CreateTagCodec()
        {
            TagCodec tg = (TagCodec)Activator.CreateInstance(TagCodecClass);

            tg.Header.VersionMajor = Version.Major;
            tg.Header.VersionMinor = Version.Minor;

            return tg;
        }
        public Frame CreateFrame()
        {
            return (Frame)Activator.CreateInstance(FrameClass, new Object[] { this });
        }
        public FrameCodec CreateFrameCodec()
        {
            return (FrameCodec)Activator.CreateInstance(FrameCodecClass);
        }
        public FrameContent CreateContent(FrameDescription.FrameType _type)
        {
            FrameContent content = null;

            if (contentClasses.ContainsKey(_type))
            {
                content = (FrameContent)Activator.CreateInstance(contentClasses[_type], new object[] { this });
            }
            else
            {
                content = new FrameContentBinary(this);
            }

            content.DescriptionMap = this;

            return content;
        }
        public FrameContentCodecBase CreateContentCodec(FrameDescription.FrameType _type)
        {
            FrameContentCodecBase result = null;

            if (CodecRepository.Instance.HasCodec(Version, _type))
            {
                result = CodecRepository.Instance.Parser(Version, _type);
            }
            else
            {
                result = CodecRepository.Instance.Parser(Version, FrameDescription.FrameType.Binary);
            }

            return result;
        }

        public FrameConversionMap Conversion
        {
            get { return conversionMap; }
        }

        public void AddContentClass(FrameDescription.FrameType _frameType, Type _contentClass)
        {
            contentClasses.Add(_frameType, _contentClass);
        }
        public void AddDescription(FrameDescription desc)
        {
            descriptions.Add(desc.FrameId, desc);

            if (desc.Meaning != FrameMeaning.Unknown)
            {
                descByMeaning[desc.Meaning] = desc;
            }
        }

        public IEnumerable<string> FrameIds
        {
            get
            {
                return descriptions.Keys;
            }
        }
        public IEnumerable<FrameDescription> FrameDescs
        {
            get
            {
                return descriptions.Values;
            }
        }
        public FrameDescription this[string frameId]
        {
            get
            {
                if (descriptions.ContainsKey(frameId))
                {
                    return descriptions[frameId];
                }
                else
                {
                    return null;
                }
            }
        }
        public FrameDescription this[FrameMeaning meaning]
        {
            get
            {
                if (descByMeaning.ContainsKey(meaning))
                {
                    return descByMeaning[meaning];
                }
                else
                {
                    return null;
                }
            }
        }
        public string DescriptionTextByID(string frameID)
        {
            if (IsValidID(frameID))
            {
                return this[frameID].Description;
            }
            else
            {
                return "UNDEFINED";
            }
        }
        public bool IsValidID(string frameID)
        {
            return descriptions.ContainsKey(frameID);
        }
        public bool IsInvalidButKnownPadding(string frameID)
        {
            return invalidButKnownPadding.Contains(frameID);
        }
        
        public void AddInvalidButKnownPadding(string _frameId)
        {
            invalidButKnownPadding.Add(_frameId);
        }
    }
    public class TagDescriptionMap
    {
        private Dictionary<Version, TagDescription> versionMap
            = new Dictionary<Version, TagDescription>();

        public static TagDescriptionMap Instance
        {
            get
            {
                return instance;
            }
        }

        public TagDescription this[Version version]
        {
            get
            {
                return versionMap[version];
            }
        }

        private TagDescriptionMap()
        {
            TagDescriptionMapLoader.Load(versionMap);
        }
        private void Add(Version version, TagDescription tagDescription)
        {
            versionMap[version] = tagDescription;
        }

        private static TagDescriptionMap instance = new TagDescriptionMap();
    }

    public static class TagDescriptionMapLoader
    {
        public static void Load(Dictionary<Version, TagDescription> versionMap)
        {
            XmlDocument document = new XmlDocument();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "ID3Lib.Resources.TagDescriptions.xml"))
            {
                document.Load(stream);
            }

            foreach (XmlNode node0 in document["TagDescriptions"])
            {
                if (node0 is XmlElement)
                {
                    XmlElement versionElement = node0 as XmlElement;

                    Version v = Version.VersionByMajorMinor(
                        Int32.Parse(versionElement.GetAttribute("Major")),
                        Int32.Parse(versionElement.GetAttribute("Minor")));

                    TagDescription tg = new TagDescription(v);

                    LoadClasses(tg, versionElement["Classes"]);
                    LoadContentClasses(tg, versionElement["ContentClasses"]);
                    LoadFrames(tg, versionElement["Frames"]);
                    LoadInvalidFramePaddings(tg, versionElement["InvalidFrameIds"]);
                    LoadConversions(tg, versionElement["Conversions"]);
                    LoadTextCodecClasses(tg, versionElement["TextCodecs"]);

                    versionMap.Add(v, tg);
                }
            }
        }

        private static void LoadClasses(TagDescription tagDescription, XmlElement classesElement)
        {
            tagDescription.TagClass = Type.GetType(classesElement.GetAttribute("Tag"));
            tagDescription.TagCodecClass = Type.GetType(classesElement.GetAttribute("TagCodec"));
            tagDescription.FrameClass = Type.GetType(classesElement.GetAttribute("Frame"));
            tagDescription.FrameCodecClass = Type.GetType(classesElement.GetAttribute("FrameCodec"));
        }
        private static void LoadContentClasses(TagDescription tagDescription, XmlElement contentClassesElement)
        {
            foreach (XmlNode node in contentClassesElement.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement element = node as XmlElement;

                    tagDescription.AddContentClass(
                        element.GetAttribute("Type").ToEnum<FrameDescription.FrameType>(),
                        Type.GetType(element.GetAttribute("Class")));
                }
            }
        }
        private static void LoadFrames(TagDescription tagDescription, XmlElement framesElement)
        {
            foreach (XmlNode node in framesElement.ChildNodes)
            {
                if (node is XmlElement)
                {
                    tagDescription.AddDescription(new FrameDescription(node));
                }
            }
        }
        private static void LoadInvalidFramePaddings(TagDescription tagDescription, XmlElement invalidFramesElement)
        {
            if (Object.ReferenceEquals(invalidFramesElement, null))
                return;

            foreach (XmlNode node in invalidFramesElement.ChildNodes)
            {
                if (node is XmlElement)
                {
                    tagDescription.AddInvalidButKnownPadding(
                        (node as XmlElement).GetAttribute("Value"));
                }
            }
        }
        private static void LoadConversions(TagDescription tagDescription, XmlElement conversionsElement)
        {
            if (Object.ReferenceEquals(conversionsElement, null))
                return;

            foreach (XmlNode node0 in conversionsElement.ChildNodes)
            {
                if (node0 is XmlElement)
                {
                    XmlElement element0 = node0 as XmlElement;
                    tagDescription.Conversion.Add(
                        element0.GetAttribute("Old"),
                        element0.GetAttribute("New"));
                }
            }
        }
        private static void LoadTextCodecClasses(TagDescription tagDescription, XmlElement textCodecs)
        {
            List<Type> codecClasses = new List<Type>();

            foreach (XmlNode node0 in textCodecs.ChildNodes)
            {
                if (node0 is XmlElement)
                {
                    XmlElement element0 = node0 as XmlElement;

                    string codecClass = element0.GetAttribute("Class");

                    if (String.IsNullOrEmpty(codecClass))
                    {
                        codecClasses.Add(null);
                    }
                    else
                    {
                        Type t = Type.GetType(codecClass);

                        if (Object.ReferenceEquals(t, null))
                            throw new Exception("Type failed");

                        codecClasses.Add(t);
                    }

                    if (element0.HasAttribute("Preferred")
                        && element0.GetAttribute("Preferred") == "True")
                    {
                        tagDescription.TextCodecClassPreferred = Type.GetType(codecClass);
                    }
                }
            }

            tagDescription.TextCodecClasses = codecClasses.ToArray();
        }
    }

    public class TestTagDescription
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagDescription));
        }

        private static void TestTagDescriptionMap()
        {
            UnitTest.Test(TagDescriptionMap.Instance[ID3.Version.v1_0] != null);
            UnitTest.Test(TagDescriptionMap.Instance[ID3.Version.v2_0] != null);
            UnitTest.Test(TagDescriptionMap.Instance[ID3.Version.v2_3] != null);
            UnitTest.Test(TagDescriptionMap.Instance[ID3.Version.v2_4] != null);
        }
        private static void TestTagDescriptionConversionMap()
        {
            ID3.Version[] versions = ID3.Version.Versions;

            for (int i = 0; i < versions.Length; i++)
            {
                TagDescription tgm = TagDescriptionMap.Instance[versions[i]];

                FrameConversionMap fcm = tgm.Conversion;
                foreach (string oldFrameId in fcm.OldFrameIds)
                {
                    UnitTest.Test(tgm.IsValidID(oldFrameId));
                }

                if (i < versions.Length - 1)
                {
                    foreach (string newFrameId in fcm.NewFrameIds)
                    {
                        TagDescription tgmNext = TagDescriptionMap.Instance[versions[i + 1]];

                        UnitTest.Test(tgmNext.IsValidID(newFrameId));
                    }
                }
            }
        }
        private static void TestTagDescriptionFactoryMethods()
        {
            ID3.Version[] versions = ID3.Version.Versions;

            for (int i = 0; i < versions.Length; i++)
            {
                TagDescription tgm = TagDescriptionMap.Instance[versions[i]];

                UnitTest.Test(tgm.CreateTagCodec().IsSupported(versions[i]));
                UnitTest.Test(tgm.CreateFrameCodec().IsSupported(versions[i]));

                foreach (FrameDescription.FrameType frameType
                    in Enum.GetValues(typeof(FrameDescription.FrameType)))
                {
                    ID3.FrameContent content = tgm.CreateContent(frameType);

                    UnitTest.Test(content.Type == frameType || content.Type == FrameDescription.FrameType.Binary);
                }

                foreach (FrameDescription.FrameType frameType
                    in Enum.GetValues(typeof(FrameDescription.FrameType)))
                {
                    ID3.Codec.FrameContentCodecBase codec = tgm.CreateContentCodec(frameType);

                    UnitTest.Test(codec.Type == frameType || codec.Type == FrameDescription.FrameType.Binary);
                    UnitTest.Test(codec.IsSupported(versions[i]));
                }
            }
        }
    }
}
