using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreDocument;
using CoreUtils;
using ID3TagModel;
using CoreTest;

namespace ID3Player
{
    public class PlayerModelIsPlayingUpdater
    {
        public PlayerModel Model
        {
            get
            {
                return model;
            }
            set
            {
                if (value != model)
                {
                    if (!Object.ReferenceEquals(model, null))
                    {
                        model.CurrentModel.PropertyChanged -= OnPlayerCurrentModelChanged;
                    }

                    model = value;

                    if (!Object.ReferenceEquals(model, null))
                    {
                        model.CurrentModel.PropertyChanged += OnPlayerCurrentModelChanged;
                    }
                }
            }
        }
        private PlayerModel model;

        public DocList<TagModel> Items
        {
            get
            {
                return items;
            }
            set
            {
                if (items != value)
                {
                    if (!Object.ReferenceEquals(items, null))
                    {
                        Items.ForEach(n => n.IsPlaying.Value = false);
                    }

                    items = value;

                    UpdateIsPlaying();
                }
            }
        }
        private DocList<TagModel> items;

        private void OnPlayerCurrentModelChanged(object sender, EventArgs e)
        {
            UpdateIsPlaying();
        }
        public void UpdateIsPlaying()
        {
            if (!Object.ReferenceEquals(Items, null)
                && !Object.ReferenceEquals(Model, null))
            {
                if (Object.ReferenceEquals(model.CurrentModel.Value, null))
                {
                    Items.ForEach(n => n.IsPlaying.Value = false);
                }
                else
                {
                    Items.ForEach(n => n.IsPlaying.Value = n.FileNameFull == model.CurrentModel.Value.FileNameFull);
                }
            }
        }
    }

    public class TestTagModelIsPlayingUpdater
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagModelIsPlayingUpdater));
        }

        static void Init()
        {
            TestTagModel.CreateTestFiles();
        }

        public static void TestTagModelIsPlayingUpdater_IsPlaying()
        {
            PlayerModel model = new PlayerModel();

            TagModelList tml = DocNode.Create<TagModelList>();
            tml.SetFiles(TestTagModel.testFileNames);

            PlayerModelIsPlayingUpdater updater = new PlayerModelIsPlayingUpdater();
            updater.Model = model;
            updater.Items = tml.Items;

            for (int i = 0; i < tml.Items.Count; ++i)
            {
                model.CurrentModel.Value = tml[i];

                for (int j = 0; j < tml.Items.Count; ++j)
                {
                    UnitTest.Test(tml[j].IsPlaying.Value == (i == j));
                }
            }

            model.CurrentModel.Value = null;
        }
    }
}
