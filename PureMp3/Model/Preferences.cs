using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CoreControls.Controls;
using CoreDocument;
using CoreLogging;
using CoreVirtualDrive;
using ID3;
using ID3.Processor;
using ID3Lib.Processor;
using ID3TagModel;
using ID3CoverSearch;
using CoreDocument.Text;
using ID3Lib;
using PureMp3.Model.Batch;
using ID3Freedb;
using CoreUtils;
using System.Xml.Linq;
using System.Xml;
using ID3.Utils;

namespace PureMp3.Model
{
    public class PreferencesCommon : PreferencesCategory
    {
        public PreferencesCommon()
            : base(
            new LocalizedText("PreferencesCommonCommon"),
            new LocalizedText(typeof(PreferencesCommon).Name))
        {
            LibraryPath = new PreferencesItem(
                new LocalizedText("PreferencesCommonLibraryPath"),
                new LocalizedText("PreferencesCommonLibraryPathHelp"),
                new DocObj<string>(""), typeof(DirectoryTextBox));

            Version = new PreferencesItem(
                new LocalizedText("PreferencesCommonVersion"),
                new LocalizedText("PreferencesCommonVersionHelp"),
                new TagVersionEnum(ID3.Preferences.PreferredVersion));

            FileNamePattern = new PreferencesItem(
                new LocalizedText("PreferencesCommonFilenamePattern"),
                new LocalizedText("PreferencesCommonFilenamePatternHelp"),
                new DocObj<string>("Artist - Album - TrackNumber - Title"));

            DirectoryPattern = (new PreferencesItem(
                new LocalizedText("PreferencesCommonDirectoryPattern"),
                new LocalizedText("PreferencesCommonDirectoryPatternHelp"),
                new DocObj<string>("Artist - Album{ (ReleaseYear)}")));

            TrackNumberPattern = new PreferencesItem(
                new LocalizedText("PreferencesCommonTrackNumberPattern"),
                new LocalizedText("PreferencesCommonTrackNumberPatternHelp"),
                new DocObj<string>("00/0"));

            ShowHelpView = new PreferencesItem(
                new LocalizedText("PreferencesCommonShowHelpView"),
                new LocalizedText("PreferencesCommonShowHelpViewHelp"),
                new DocObj<bool>(true));

            Verbose = new PreferencesItem(
                new LocalizedText("PreferencesCommonVerbose"),
                new LocalizedText("PreferencesCommonVerboseHelp"),
                new DocObj<bool>());

            Version.ItemT<TagVersionEnum>().PropertyChanged += new PropertyChangedEventHandler(OnVersionChanged);
            Verbose.ItemT<DocObj<bool>>().PropertyChanged += new PropertyChangedEventHandler(OnVerboseChanged);
        }

        public PreferencesItem LibraryPath
        {
            get;
            set;
        }
        public PreferencesItem Version
        {
            get;
            private set;
        }
        public PreferencesItem FileNamePattern
        {
            get;
            set;
        }
        public PreferencesItem DirectoryPattern
        {
            get;
            set;
        }
        public PreferencesItem TrackNumberPattern
        {
            get;
            set;
        }

        public PreferencesItem ShowHelpView
        {
            get;
            private set;
        }
        public PreferencesItem Verbose
        {
            get;
            private set;
        }

        void OnVersionChanged(object sender, PropertyChangedEventArgs e)
        {
            ID3.Preferences.PreferredVersion = Version.ItemT<TagVersionEnum>().ValueVersion;
        }
        void OnVerboseChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.EnableToken(Tokens.InfoVerbose, Verbose.Value<bool>());
        }

        public TrackNumberGenerator CreateTrackNumberGenerator()
        {
            return new TrackNumberGenerator(TrackNumberPattern.Value<string>());
        }
    }
    public class PreferencesAlbumRecognition : PreferencesCategory
    {
        public PreferencesAlbumRecognition()
            : base(
            new LocalizedText("PreferencesAlbumRecognition"),
            new LocalizedText("PreferencesAlbumRecognitionHelp"))
        {
            ArtistRequired = new PreferencesItem(
                new LocalizedText("PreferencesAlbumRecognitionArtistRequired"),
                new LocalizedText("PreferencesAlbumRecognitionArtistRequiredHelp"),
                new DocObj<bool>(true));

            AlbumRequired = new PreferencesItem(
                new LocalizedText("PreferencesAlbumRecognitionAlbumRequired"),
                new LocalizedText("PreferencesAlbumRecognitionAlbumRequiredHelp"),
                new DocObj<bool>(true));

            TitleRequired = new PreferencesItem(
                new LocalizedText("PreferencesTitleRecognitionTitleRequired"),
                new LocalizedText("PreferencesTitleRecognitionTitleRequiredHelp"),
                new DocObj<bool>(true));

            TrackNumberRequired = new PreferencesItem(
                new LocalizedText("PreferencesTrackNumberRecognitionTrackNumberRequired"),
                new LocalizedText("PreferencesTrackNumberRecognitionTrackNumberRequiredHelp"),
                new DocObj<bool>(true));

            ReleaseYearRequired = new PreferencesItem(
                new LocalizedText("PreferencesYearRecognitionYearRequired"),
                new LocalizedText("PreferencesYearRecognitionYearRequiredHelp"),
                new DocObj<bool>(false));

            MinimumTracksRequired = new PreferencesItem(
                new LocalizedText("PreferencesAlbumRecognitionMinimumTracksRequired"),
                new LocalizedText("PreferencesAlbumRecognitionMinimumTracksRequiredHelp"),
                new DocEnum(new string[] {"1", "2", "3", "4", "5", "6"}, 2)); 
        }

        public PreferencesItem ArtistRequired
        {
            get;
            private set;
        }
        public PreferencesItem AlbumRequired
        {
            get;
            private set;
        }
        public PreferencesItem TitleRequired
        {
            get;
            private set;
        }
        public PreferencesItem TrackNumberRequired
        {
            get;
            private set;
        }
        public PreferencesItem ReleaseYearRequired
        {
            get;
            private set;
        }
        public PreferencesItem MinimumTracksRequired
        {
            get;
            private set;
        }

        public IProcessorMutable CreateProcessor()
        {
            return new ID3.Processor.DirectoryProcessor(
                CreateProcessorExplorer(true), true);
        }

        public AlbumExplorerProcessor CreateProcessorExplorer(bool verbose)
        {
            AlbumExplorerProcessor explorer
                = new AlbumExplorerProcessor(verbose);

            explorer.Explorer.ArtistRequired = ArtistRequired.Value<bool>();
            explorer.Explorer.AlbumRequired = AlbumRequired.Value<bool>();
            explorer.Explorer.TitleRequired = TitleRequired.Value<bool>();
            explorer.Explorer.TrackNumberRequired = TrackNumberRequired.Value<bool>();
            explorer.Explorer.ReleaseYearRequired = ReleaseYearRequired.Value<bool>();
            explorer.Explorer.MinimumTracksRequired = Int32.Parse(MinimumTracksRequired.ItemT<DocEnum>().ValueStr);

            return explorer;
        }
    }
    public class PreferencesFilenameToTag : PreferencesCategory
    {
        public PreferencesFilenameToTag()
            : base(
            new LocalizedText("PreferencesFilenameToTag"),
            new LocalizedText("PreferencesFilenameToTagHelp"))
        {
            Pattern = new PreferencesItem(
                new LocalizedText("PreferencesFilenameToTagPattern"),
                new LocalizedText("PreferencesFilenameToTagPatternHelp"),
                new DocObj<string>("Artist - Album - TrackNumber - Title"));
        }

        public PreferencesItem Pattern
        {
            get;
            private set;
        }

        public IProcessorMutable CreateProcessor()
        {
            return new DirectoryProcessor(
                new FilenameToTagProcessor(Pattern.Value<string>()), true);
        }
    }

    public class PreferencesAlbumToToLibrary : PreferencesCategory
    {
        public PreferencesAlbumToToLibrary()
            : base(
            new LocalizedText("PreferencesAlbumToToLibrary"),
            new LocalizedText("PreferencesAlbumToToLibraryHelp"))
        {
            MoveOrCopy = new PreferencesItem(
                new LocalizedText("PreferencesAlbumToToLibraryMoveOrCopy"),
                new LocalizedText("PreferencesAlbumToToLibraryMoveOrCopyHelp"),
                new DocEnum(new string[] { "Copy", "Move" } ));

            Conflicts = new PreferencesItem(
                new LocalizedText("PreferencesAlbumToToLibraryConflicts"),
                new LocalizedText("PreferencesAlbumToToLibraryConflictsHelp"),
                new DocEnum(typeof(FileOperationProcessor.ConflictSolving)));
        }

        private PreferencesItem MoveOrCopy
        {
            get;
            set;
        }
        private PreferencesItem Conflicts
        {
            get;
            set;
        }

        public IProcessorMutable CreateProcessor(string libraryPath, AlbumExplorerProcessor explorer)
        {
            explorer.OnFineProcessor = new AlbumToLibraryProcessor(
                libraryPath,
                (FileOperationProcessor.FileOperation)MoveOrCopy.Value<int>(),
                (FileOperationProcessor.ConflictSolving)Conflicts.Value<int>());

            return new DirectoryProcessor(explorer, true);
        }
    }
    
    public class PreferencesPreparse : PreferencesCategory
    {
        public PreferencesPreparse()
            : base(
            new LocalizedText("PreferencesPreparse"),
            new LocalizedText("PreferencesPreparseHelp"))
        {
            TextTrim = new PreferencesItem(
                new LocalizedText("PreferencesPreparseTextTrim"),
                new LocalizedText("PreferencesPreparseTextTrimHelp"),
                new DocObj<bool>(true));
            TextBreakCamelCase = new PreferencesItem(
                new LocalizedText("PreferencesPreparseTextBreakCamelCase"),
                new LocalizedText("PreferencesPreparseTextBreakCamelCaseHelp"),
                new DocObj<bool>(true));
            TextBreakUnderscores = new PreferencesItem(
                new LocalizedText("PreferencesPreparseTextBreakUnderscores"),
                new LocalizedText("PreferencesPreparseTextBreakUnderscoresHelp"),
                new DocObj<bool>(true));
            TextFirstCharUpper = new PreferencesItem(
                new LocalizedText("PreferencesPreparseTextFirstCharUpper"),
                new LocalizedText("PreferencesPreparseTextFirstCharUpperHelp"),
                new DocObj<bool>(true));
            WordList = new PreferencesItem(
                new LocalizedText("PreferencesPreparseWordList"),
                new LocalizedText("PreferencesPreparseWordListHelp"),
                new DocObj<string>(ID3.Preferences.WordsReadonly));

            CreateTrackNumbers = new PreferencesItem(
                new LocalizedText("PreferencesPreparseCreateTrackNumbers"),
                new LocalizedText("PreferencesPreparseCreateTrackNumbersHelp"),
                new DocObj<bool>(false));

            DropUnwantedFrames = new PreferencesItem(
               new LocalizedText("PreferencesPreparseDropUnwantedFrames"),
                new LocalizedText("PreferencesPreparseDropUnwantedFramesHelp"),
               new DocObj<bool>(false));

            DropCodecs = new PreferencesItem(
                new LocalizedText("PreferencesPreparseDropCodecs"),
                new LocalizedText("PreferencesPreparseDropCodecsHelp"),
                new DocObj<bool>(true));

            FrameIds = new DocList<DocList<PreferencesItem>>();
            CurrentFrameIds = new PreferencesItem(
                new LocalizedText("PreferencesPreparseFramesToKeep"),
                new LocalizedText("PreferencesPreparseFramesToKeepHelp"),
                new DocList<PreferencesItem>(true));

            AlbumToFilename = new PreferencesItem(
                new LocalizedText("PreferencesPreparseAlbumToFilename"),
                new LocalizedText("PreferencesPreparseAlbumToFilenameHelp"),
                new DocObj<bool>(false));
            AlbumToDirectory = new PreferencesItem(
                new LocalizedText("PreferencesPreparseAlbumToDirectory"),
                new LocalizedText("PreferencesPreparseAlbumToDirectoryHelp"),
                new DocObj<bool>(false));

            FrameIds.ReadOnly = true;

            foreach (ID3.Version v in ID3.Version.Versions)
            {
                DocList<PreferencesItem> list = new DocList<PreferencesItem>();
                list.ReadOnly = true;

                TagDescription td = ID3.TagDescriptionMap.Instance[v];

                IEnumerable<string> def = TagProcessorDropFrames.DefaultFrameIds(v);

                foreach (string frameId in td.FrameIds)
                {
                    list.Add(new PreferencesItem(
                        frameId,
                        new Text(frameId + " " + td.DescriptionTextByID(frameId)),
                        new LocalizedText("PreferencesPreparseFramesToKeepHelp"),
                        new DocObj<bool>(def.Contains(frameId))));
                }

                FrameIds.Add(list);
            }

            WordList.ItemT<DocObj<string>>().PropertyChanged += new PropertyChangedEventHandler(OnWordListChanged);
        }

        public override void FromXml(XmlElement e)
        {
            base.FromXml(e);
        }

        public void SetVersion(TagVersionEnum v)
        {
            DocList<PreferencesItem> list = CurrentFrameIds.ItemT<DocList<PreferencesItem>>();

            list.Clear();
            foreach (var item in FrameIds[v.Value])
            {
                list.Add(item);
            }
        }

        public IProcessorMutable CreateProcessor(
            TrackNumberGenerator trackNumberGenerator,
            PreferencesCommon prefsCommon,
            PreferencesAlbumRecognition prefsAlbumRecognition)
        {
            ProcessorListMutable toplevel = new ProcessorListMutable();

            {
                ProcessorListMutable plm = new ProcessorListMutable();

                if (DropCodecs.Value<bool>()) plm.ProcessorList.Add(new DropCodecsProcessor());

                plm.ProcessorList.Add(new TagVersionProcessor(ID3.Preferences.PreferredVersion));

                if (CreateTrackNumbers.Value<bool>()) plm.ProcessorList.Add(new TagProcessorTrackNumber(trackNumberGenerator));
                if (DropUnwantedFrames.Value<bool>()) plm.ProcessorList.Add(CreateDropFramesProcessor());

                TextProcessorList textProcessorList = new TextProcessorList();
                if (TextTrim.Value<bool>()) textProcessorList.ProcessorList.Add(new TextTrim());
                if (TextBreakCamelCase.Value<bool>()) textProcessorList.ProcessorList.Add(new TextBreakCamelCase());
                if (TextBreakUnderscores.Value<bool>()) textProcessorList.ProcessorList.Add(new TextBreakUnderscores());
                if (TextFirstCharUpper.Value<bool>()) textProcessorList.ProcessorList.Add(new TextFirstCharUpper());
                if (textProcessorList.ProcessorList.Count > 0)
                {
                    plm.ProcessorList.Add(new TagProcessor(new FrameProcessorText(textProcessorList)));
                }
                //return new DirectoryProcessor(new FileProcessor(plm), true);

                toplevel.ProcessorList.Add(new DirectoryProcessor(new FileProcessor(plm), true));
            }

            {
                AlbumExplorerProcessor explorer = prefsAlbumRecognition.CreateProcessorExplorer(false);
                explorer.OnFineProcessor = new AlbumTagToFilenameProcessor(prefsCommon.FileNamePattern.Value<string>());

                toplevel.ProcessorList.Add(new ID3.Processor.DirectoryProcessor(explorer, true));
            }

            {
                AlbumExplorerProcessor explorer = prefsAlbumRecognition.CreateProcessorExplorer(false);
                explorer.OnFineProcessor = new AlbumTagToDirectoryProcessor(prefsCommon.DirectoryPattern.Value<string>());

                toplevel.ProcessorList.Add(new ID3.Processor.DirectoryProcessor(explorer, true));
            }

            return toplevel;
        }
        private IProcessorMutable CreateDropFramesProcessor()
        {
            return new TagProcessorDropFrames(
                ID3.Preferences.PreferredVersion,
                from n
                in FrameIds[ID3.Version.IndexOfVersion(ID3.Preferences.PreferredVersion)]
                where n.Value<bool>()
                select n.Id);
        }
        
        private PreferencesItem TextTrim
        {
            get;
            set;
        }
        private PreferencesItem TextBreakCamelCase
        {
            get;
            set;
        }
        private PreferencesItem TextBreakUnderscores
        {
            get;
            set;
        }
        private PreferencesItem TextFirstCharUpper
        {
            get;
            set;
        }
        private PreferencesItem WordList
        {
            get;
            set;
        }
        private PreferencesItem CreateTrackNumbers
        {
            get;
            set;
        }
        private PreferencesItem DropCodecs
        {
            get;
            set;
        }
        private PreferencesItem DropUnwantedFrames
        {
            get;
            set;
        }

        private void OnWordListChanged(object sender, PropertyChangedEventArgs e)
        {
            ID3.Preferences.WordsReadonly = WordList.Value<string>();
        }

        public DocList<DocList<PreferencesItem>> FrameIds
        {
            get;
            set;
        }
        public PreferencesItem CurrentFrameIds
        {
            get;
            set;
        }

        private PreferencesItem AlbumToFilename
        {
            get;
            set;
        }
        private PreferencesItem AlbumToDirectory
        {
            get;
            set;
        }
    }

    public class PreferencesWebToTag : PreferencesCategory
    {
        public PreferencesWebToTag()
            : base(
            new LocalizedText("PreferencesWebToTag"),
            new LocalizedText("PreferencesWebToTagHelp"))
        {
            MultipleChoiseHeuristic = new PreferencesItem(
                new LocalizedText("PreferencesWebToTagMultipleChoiseHeuristic"),
                new LocalizedText("PreferencesWebToTagMultipleChoiseHeuristicHelp"),
                new DocEnum(new string[]  { "Strict", "Fuzzy" }));
        }

        public IProcessorMutable CreateProcessor(TrackNumberGenerator trackNumberGenerator)
        {
            return new DirectoryProcessor(new DirectoryFreedbToTags(
                (MultipleItemChooser.MultipleChoiseHeuristic)MultipleChoiseHeuristic.ItemT<DocEnum>().Value,
                trackNumberGenerator),
                true);
        }

        private PreferencesItem MultipleChoiseHeuristic
        {
            get;
            set;
        }
    }

    public class Preferences : DocNode
    {
        public Preferences()
        {
            PrefsCommon = DocNode.Create<PreferencesCommon>();
            PrefsAlbumRecognition = DocNode.Create<PreferencesAlbumRecognition>();
            PrefsFilenameToTag = DocNode.Create<PreferencesFilenameToTag>();
            PrefsAlbumToLibrary = DocNode.Create<PreferencesAlbumToToLibrary>();
            PrefsPreparse = DocNode.Create<PreferencesPreparse>();
            PrefsFreedb = DocNode.Create<PreferencesWebToTag>();

            (PrefsCommon.Version.Item as TagVersionEnum).PropertyChanged
                += new PropertyChangedEventHandler(OnVersionChanged);

            ResolveChildrenLinks();

            PrefsPreparse.SetVersion(PrefsCommon.Version.Item as TagVersionEnum);
        }

        public PreferencesCommon PrefsCommon
        {
            get;
            set;
        }
        public PreferencesAlbumRecognition PrefsAlbumRecognition
        {
            get;
            set;
        }
        public PreferencesFilenameToTag PrefsFilenameToTag
        {
            get;
            set;
        }
        public PreferencesAlbumToToLibrary PrefsAlbumToLibrary
        {
            get;
            set;
        }
        public PreferencesPreparse PrefsPreparse
        {
            get;
            set;
        }
        public PreferencesWebToTag PrefsFreedb
        {
            get;
            set;
        }

        public IEnumerable<PreferencesCategory> Categories
        {
            get
            {
                yield return PrefsCommon;
                yield return PrefsAlbumRecognition;
                yield return PrefsPreparse;
                yield return PrefsFreedb;
                yield return PrefsFilenameToTag;
                //yield return PrefsTagToFilename;
                //yield return PrefsAlbumToDirectory;
                yield return PrefsAlbumToLibrary;
            }
        }

        public IProcessorMutable CreateProcessorSearchAlbums()
        {
            return PrefsAlbumRecognition.CreateProcessor();
        }
        public IProcessorMutable CreateProcessorPreparse()
        {
            return PrefsPreparse.CreateProcessor(
                PrefsCommon.CreateTrackNumberGenerator(),
                PrefsCommon,
                PrefsAlbumRecognition);
        }
        public IProcessorMutable CreateProcessorTagToFilename()
        {
            AlbumExplorerProcessor explorer = PrefsAlbumRecognition.CreateProcessorExplorer(false);
            explorer.OnFineProcessor = new AlbumTagToFilenameProcessor(PrefsCommon.FileNamePattern.Value<string>());

            return new ID3.Processor.DirectoryProcessor(explorer, true);
        }
        public IProcessorMutable CreateProcessorFilenameToTag()
        {
            return PrefsFilenameToTag.CreateProcessor();
        }
        public IProcessorMutable CreateProcessorAlbumToDirectory()
        {
            AlbumExplorerProcessor explorer = PrefsAlbumRecognition.CreateProcessorExplorer(false);
            explorer.OnFineProcessor = new AlbumTagToDirectoryProcessor(PrefsCommon.DirectoryPattern.Value<string>());

            return new ID3.Processor.DirectoryProcessor(explorer, true);
        }
        public IProcessorMutable CreateProcessorAlbumToLibrary()
        {
            return PrefsAlbumToLibrary.CreateProcessor(
                PrefsCommon.LibraryPath.Value<string>(),
                PrefsAlbumRecognition.CreateProcessorExplorer(false));
        }
        public IProcessorMutable CreateProcessorCheckForFreedb()
        {
            return PrefsFreedb.CreateProcessor(PrefsCommon.CreateTrackNumberGenerator());
        }
        public IProcessorMutable CreateProcessorCheckForDiscogs()
        {
            return new DirectoryProcessor(new DirectoryWebQueryToTags(
                new ID3Discogs.DiscogsAccess(),
                PrefsCommon.CreateTrackNumberGenerator()), true);
        }
        public IProcessorMutable CreateProcessorCheckForMusicBrainz()
        {
            return new DirectoryProcessor(new DirectoryWebQueryToTags(
                new ID3MusicBrainz.MusicbrainzAccess(),
                PrefsCommon.CreateTrackNumberGenerator()), true);
        }
        public IProcessorMutable CreateProcessorCoverSearch()
        {
            return new DirectoryProcessor
            (
                new FrameByMeaningMissingProcessor
                (
                    FrameMeaning.Picture,
                    new FileProcessor
                    (
                        new CoverSearchProcessor()
                    )
                ),
                true
            );
        }
        public IProcessorMutable CreateProcessorRecycleBin()
        {
            return new FileOperationProcessor(FileOperationProcessor.FileOperation.Recycle);
        }

        public static string PreferencesFileName()
        {
            return Path.Combine(App.AppDataFolder, typeof(Preferences).Name + ".xml");
        }
        public static Preferences LoadPreferences()
        {
            return XmlUtils.SafeLoad<Preferences>(PreferencesFileName());
        }
        public static void SavePreferences(Preferences preferences)
        {
            XmlUtils.Save(preferences, PreferencesFileName());
        }

        private void OnVersionChanged(object sender, PropertyChangedEventArgs e)
        {
            PrefsPreparse.SetVersion(PrefsCommon.Version.Item as TagVersionEnum);
        }
    }
}
