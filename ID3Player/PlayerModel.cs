using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreDocument;
using CoreDocument.Text;
using System.IO;
using System.ComponentModel;
using ID3TagModel;
using CoreTest;

namespace ID3Player
{
    public class PlayerPosition : DocBase
    {
        public PlayerPosition()
        {
            Help = new LocalizedText("PlayerPositionHelp");

            Position = new DocObj<double>(0.0);
            MediaLength = new DocObj<double>(1.0);
        }

        [DocObjRef]
        public DocObj<double> MediaLength
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<double> Position
        {
            get;
            private set;
        }
    }

    public class PlayerModel : DocNode
    {
        public PlayerModel()
        {
            CurrentModel = new DocObj<TagModel>();
            CurrentTrack = new DocObj<string>();
            CurrentTrack.Help = new LocalizedText("PlayerCurrentTrackHelp");
            CurrentBitrate = new DocObj<string>();
            CurrentBitrate.Help = new LocalizedText("PlayerCurrentBitrateHelp");
            CurrentPosition = new DocObj<string>("00:00:00");
            CurrentPosition.Help = new LocalizedText("PlayerCurrentPositionHelp");
            Volume = new DocObj<double>(0.5);
            Volume.Help = new LocalizedText("PlayerVolumeHelp");
            Position = new PlayerPosition();

            CurrentModel.PropertyChanged += new PropertyChangedEventHandler(OnCurrentModelChanged);
        }

        [DocObjRef]
        public DocObj<TagModel> CurrentModel
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<string> CurrentTrack
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<string> CurrentBitrate
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<string> CurrentPosition
        {
            get;
            private set;
        }
        [DocObjRef]
        public DocObj<double> Volume
        {
            get;
            private set;
        }
        [DocObjRef]
        public PlayerPosition Position
        {
            get;
            set;
        }

        public string CurrentFileNameFull
        {
            get
            {
                if (Object.ReferenceEquals(CurrentModel.Value, null))
                {
                    return null;
                }
                else
                {
                    return CurrentModel.Value.FileNameFull;
                }
            }
        }

        private void OnCurrentModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Object.ReferenceEquals(CurrentModel.Value, null))
            {
                if (CurrentModel.Value.Contains(ID3.FrameMeaning.Artist)
                    && CurrentModel.Value.Contains(ID3.FrameMeaning.Title))
                {
                    CurrentTrack.Value = CurrentModel.Value[ID3.FrameMeaning.Artist].Text.Value
                        + " - " + CurrentModel.Value[ID3.FrameMeaning.Title].Text.Value;
                }
                else
                {
                    CurrentTrack.Value = CurrentModel.Value.FileName;
                }

                if (CurrentModel.Value.Bitrate.Value != -1)
                {
                    CurrentBitrate.Value = CurrentModel.Value.Bitrate.Value.ToString() + " kBit";
                }
                else
                {
                    CurrentBitrate.Value = "Unknown";
                }
            }
            else
            {
                CurrentTrack.Value = String.Empty;
                CurrentBitrate.Value = String.Empty;
            }
        }

    }

    public class TestPlayerModel
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestPlayerModel));
        }

        static void Init()
        {
            TestTagModel.CreateTestFiles();
        }
        public static void Test_CurrentModel()
        {
            PlayerModel model = new PlayerModel();

            TagModel tag = new TagModel();
            tag.Load(TestTagModel.testFileName0_2_3);
            UnitTest.Test(!tag.IsPlaying.Value);

            model.CurrentModel.Value = tag;

            UnitTest.Test(model.CurrentTrack.Value == "Artist - Title 1");
            UnitTest.Test(model.CurrentBitrate.Value == "Unknown");
            UnitTest.Test(model.CurrentPosition.Value == "00:00:00");
            UnitTest.Test(model.Volume.Value == 0.5);

            model.CurrentModel.Value = null;

            UnitTest.Test(model.CurrentTrack.Value == String.Empty);
            UnitTest.Test(model.CurrentBitrate.Value == String.Empty);
            UnitTest.Test(model.CurrentPosition.Value == "00:00:00");
            UnitTest.Test(model.Volume.Value == 0.5);
        }
    }
}
