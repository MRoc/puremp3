using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using CoreDocument;
using System.ComponentModel;
using ID3TagModel;
using CoreVirtualDrive;
using CoreTest;

namespace ID3Player
{
    public interface ITrackAvailability
    {
        bool HasItems();
        event PropertyChangedEventHandler PropertyChanged;
    }
    public interface ITrackNavigation
    {
        void JumpFirst();
        void JumpNext();
        void JumpPrevious();
    }
    
    public class Playlist : DocNode
    {
        public Playlist()
        {
            IsPlayingUpdater = new PlayerModelIsPlayingUpdater();
            Items = new DocList<TagModel>();
        }

        public DocList<TagModel> Items
        {
            get
            {
                return items;
            }
            set
            {
                if (!Object.ReferenceEquals(value, items))
                {
                    items = value;
                    IsPlayingUpdater.Items = value;
                    NotifyPropertyChanged(this, m => m.Items);
                }
            }
        }
        public bool HasItems()
        {
            return !Object.ReferenceEquals(Items, null) && Items.Count() > 0;
        }

        [DocObjRef]
        public PlayerModel Player
        {
            get
            {
                return player;
            }
            set
            {
                if (!Object.ReferenceEquals(value, player))
                {
                    if (!Object.ReferenceEquals(player, null))
                    {
                        player.CurrentModel.PropertyChanged -= new PropertyChangedEventHandler(OnPlayerCurrentChanged);
                    }

                    player = value;
                    IsPlayingUpdater.Model = value;

                    if (!Object.ReferenceEquals(player, null))
                    {
                        player.CurrentModel.PropertyChanged += new PropertyChangedEventHandler(OnPlayerCurrentChanged);
                    }
                }
            }
        }
        [DocObjRef]
        public PlayerModelIsPlayingUpdater IsPlayingUpdater
        {
            get;
            private set;
        }

        void OnPlayerCurrentChanged(object sender, PropertyChangedEventArgs e)
        {
            DocObj<TagModel> node = sender as DocObj<TagModel>;

            if (node.Value != null)
            {
                bool isContained =
                    (from n
                    in Items
                     where n.FileNameFull == node.Value.FileNameFull
                     select n).Count() > 0;

                if (!isContained)
                {
                    Items.Add(TagModel.CreateClone(node.Value));
                }

                IsPlayingUpdater.UpdateIsPlaying();
            }
        }

        private PlayerModel player;
        private DocList<TagModel> items;
    }

    public class TestPlaylist
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestPlaylist));
        }

        public static void TestBasic()
        {
            Playlist playlist = DocNode.Create<Playlist>();
            playlist.Player = new PlayerModel();

            UnitTest.Test(Object.ReferenceEquals(playlist.IsPlayingUpdater.Model, playlist.Player));
            UnitTest.Test(Object.ReferenceEquals(playlist.IsPlayingUpdater.Items, playlist.Items));
        }
     }
}
