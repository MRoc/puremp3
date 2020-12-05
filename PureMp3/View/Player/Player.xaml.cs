using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CoreControls.Commands;
using CoreDocument;
using CoreVirtualDrive;
using ID3TagModel;
using System.ComponentModel;
using CoreLogging;
using CoreDocument.Text;
using ID3Player;
using PureMp3.Model;

namespace PureMp3
{
    public partial class Player : UserControl
    {
        public Player()
        {
            InitializeComponent();

            player.MediaFailed += new EventHandler<ExceptionEventArgs>(OnPlayerMediaFailed);
            player.MediaEnded += new EventHandler(OnPlayerMediaEnded);

            DataContextChanged += new DependencyPropertyChangedEventHandler(OnDataContextChanged);

            sliderPosition.IsEnabled = false;
            sliderVolume.Maximum = 1.0;
        }

        public void Init()
        {
            PlayerModel model = (DataContext as Document).PlayerModel;

            Position.Player = player;
            Position.Controller = Controller;
            Position.Model = model;

            mediaLength.Controller = Controller;
            mediaLength.Player = player;
            mediaLength.Model = model;

            interuptionHandler.Player = player;
            interuptionHandler.Controller = Controller;
            interuptionHandler.Model = model;

            Controller.PlayerStop += delegate()
            {
                player.Stop();
                player.Close();
            };
            Controller.PlayerPlay += delegate()
            {
                player.Play();
            };
            Controller.PlayerPause += delegate()
            {
                player.Pause();
            };
            Controller.PlayerLoad += delegate()
            {
                if (!Object.ReferenceEquals(Model.CurrentModel.Value, null))
                {
                    player.Open(new Uri(Model.CurrentModel.Value.FileNameFull));
                }
                else
                {
                    player.Close();
                }
            };
            Controller.StateChanged += delegate(object obj, EventArgs args)
            {
                sliderPosition.IsEnabled = Controller.CurrentState != PlayerState.Stopped;
            };

            model.Volume.PropertyChanged += delegate(object obj, PropertyChangedEventArgs e)
            {
                UpdateVolumeFromModel();
            };
            player.MediaOpened += delegate(object obj, EventArgs e)
            {
                UpdateVolumeFromModel();
            };
        }

        public PlayPositionTrigger Position
        {
            get
            {
                return playposition;
            }
        }
        public ITrackNavigation TrackNavigation
        {
            get
            {
                return (DataContext as Document).PlaylistRouter;
            }
        }
        public PlayerController Controller
        {
            get
            {
                return (DataContext as Document).PlayerController;
            }
        }
        public PlayerModel Model
        {
            get
            {
                if (Object.ReferenceEquals(DataContext, null))
                {
                    return null;
                }
                else
                {
                    return (DataContext as Document).PlayerModel;
                }
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as Document).RequestPlayingChanged
                    -= OnPlayRequest;

                (e.OldValue as Document).Editor.TagModelList.Items.Transaction.Hook
                    -= OnHasFileChanged;
            }
            if (e.NewValue != null)
            {
                (e.NewValue as Document).Editor.TagModelList.Items.Transaction.Hook
                    += OnHasFileChanged;

                (e.NewValue as Document).RequestPlayingChanged
                    += OnPlayRequest;
            }

            Init();
        }

        private void OnPlayRequest(object sender, EventArgs e)
        {
            PlayerController controller = (DataContext as Document).PlayerController;

            controller.Stop();
            Model.CurrentModel.Value = sender as TagModel;
            controller.Play();
        }
        private void OnHasFileChanged(object sender, EventArgs args)
        {
            if (Controller.CurrentState == PlayerState.Stopped)
            {
                TrackNavigation.JumpFirst();
            }
        }

        private void OnPlayerMediaFailed(object sender, ExceptionEventArgs e)
        {
            Controller.Stop();
        }
        private void OnPlayerMediaEnded(object sender, EventArgs e)
        {
            Controller.Stop();
            TrackNavigation.JumpNext();
            Controller.Play();
        }

        void UpdateVolumeFromModel()
        {
            if (Object.ReferenceEquals(Model, null))
                return;

            player.Volume = Model.Volume.Value;
        }

        private MediaPlayer player = new MediaPlayer();
        private PlayPositionTrigger playposition = new PlayPositionTrigger();
        private MediaLengthTrigger mediaLength = new MediaLengthTrigger();
        private InteruptionHandler interuptionHandler = new InteruptionHandler();
    }

    public class InteruptionHandler
    {
        public InteruptionHandler()
        {
        }

        public MediaPlayer Player
        {
            get;
            set;
        }
        public PlayerController Controller
        {
            get
            {
                return controller;
            }
            set
            {
                if (!Object.ReferenceEquals(controller, null))
                {
                    controller.PlayerStop -= OnControllerStop;
                    controller.PlayerLoad -= OnControllerLoad;
                }

                controller = value;

                if (!Object.ReferenceEquals(controller, null))
                {
                    controller.PlayerStop += OnControllerStop;
                    controller.PlayerLoad += OnControllerLoad;
                }
            }
        }
        public PlayerModel Model
        {
            get;
            set;
        }

        private void OnControllerStop()
        {
            if (!Object.ReferenceEquals(Model.CurrentModel.Value, null))
            {
                VirtualDrive.ObserverLockExclusive.Unregister(Model.CurrentModel.Value.FileNameFull);
            }
        }
        private void OnControllerLoad()
        {
            if (!Object.ReferenceEquals(Model.CurrentModel.Value, null))
            {
                VirtualDrive.ObserverLockExclusive.Register(Model.CurrentModel.Value.FileNameFull, OnFileRequest);
            }
        }

        private void OnFileRequest(object sender, AccessObserver.AccessObserverEventArgs args)
        {
            Action action = null;

            switch (args.Request)
            {
                case AccessObserver.AccessRequest.LockExclusive:
                    action = delegate
                    {
                        if (Model.CurrentModel.Value.FileNameFull == args.ObservedId)
                        {
                            PausePosition = (long)Player.Position.TotalMilliseconds;

                            Controller.Stop();

                            System.Threading.Thread.Sleep(100);

                            VirtualDrive.ObserverFreeShared.Register(Model.CurrentModel.Value.FileNameFull, OnFileRequest);
                        }
                    };
                    break;

                case AccessObserver.AccessRequest.FreeShared:
                    action = delegate
                    {
                        VirtualDrive.ObserverFreeShared.Unregister(args.ObservedId);

                        if (Model.CurrentModel.Value.FileNameFull == args.ObservedId)
                        {
                            if (String.IsNullOrEmpty(args.NewObservedId))
                            {
                                Model.CurrentModel.Value = null;
                            }
                            else
                            {
                                try
                                {
                                    TagModel model = DocNode.Create<TagModel>();
                                    model.Load(args.NewObservedId);
                                    Model.CurrentModel.Value = model;

                                    Controller.Play();
                                    Player.Position = new TimeSpan(0, 0, 0, 0, (int)PausePosition);
                                }
                                catch (Exception e)
                                {
                                    Logger.WriteLine(Tokens.Exception, e);
                                }
                            }
                        }
                    };
                    break;
            }

            if (!Object.ReferenceEquals(action, null))
            {
                Player.Dispatcher.Invoke(action);
            }
        }
        private long PausePosition
        {
            get;
            set;
        }

        private PlayerController controller;
    }

    public class MediaLengthTrigger
    {
        public MediaLengthTrigger()
        {
        }

        public MediaPlayer Player
        {
            get
            {
                return player;
            }
            set
            {
                if (!Object.ReferenceEquals(player, null))
                {
                    Player.MediaOpened -= OnMediaOpened;
                }

                player = value;

                if (!Object.ReferenceEquals(player, null))
                {
                    Player.MediaOpened += OnMediaOpened;
                }
            }
        }
        public PlayerController Controller
        {
            get;
            set;
        }
        public PlayerModel Model
        {
            get;
            set;
        }

        public TimeSpan Length
        {
            get
            {
                if (Controller.CurrentState == PlayerState.Stopped)
                {
                    return new TimeSpan(0, 0, 0, 0, 0);
                }
                else
                {
                    return Player.NaturalDuration.TimeSpan;
                }
            }
        }

        private void OnMediaOpened(object obj, EventArgs args)
        {
            Model.Position.MediaLength.Value = Length.TotalMilliseconds;
        }

        private MediaPlayer player;
    }

    public class PlayPositionTrigger
    {
        public PlayPositionTrigger()
        {
            Timer = new DispatcherTimer();
            Timer.Tick += delegate(object obj, EventArgs args)
            {
                UpdatePositionFromPlayer();
            };
        }

        public PlayerModel Model
        {
            get
            {
                return model;
            }
            set
            {
                if (!Object.ReferenceEquals(model, null))
                {
                    model.Position.Position.PropertyChanged -= OnModelPositionChanged;
                }

                model = value;

                if (!Object.ReferenceEquals(model, null))
                {
                    model.Position.Position.PropertyChanged += OnModelPositionChanged;
                }
            }
        }
        public MediaPlayer Player
        {
            get;
            set;
        }
        public PlayerController Controller
        {
            get
            {
                return controller;
            }
            set
            {
                if (!Object.ReferenceEquals(controller, null))
                {
                    controller.StateChanged -= OnControllerStateChanged;
                }

                controller = value;

                if (!Object.ReferenceEquals(controller, null))
                {
                    controller.StateChanged += OnControllerStateChanged;
                }
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (Controller.CurrentState == PlayerState.Stopped)
                {
                    return new TimeSpan(0, 0, 0, 0, 0);
                }
                else
                {
                    return Player.Position;
                }
            }
            set
            {
                if (!BlockUpdates)
                {
                    BlockUpdates = true;
                    Model.Position.Position.Value = value.TotalMilliseconds;
                    BlockUpdates = false;
                }
            }
        }

        private void OnControllerStateChanged(object obj, EventArgs args)
        {
            if (Controller.CurrentState != PlayerState.Stopped)
            {
                Timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                Timer.Start();
            }
            else
            {
                Timer.Stop();
                UpdatePositionFromPlayer();
            }
        }
        private void OnModelPositionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!BlockUpdates)
            {
                BlockUpdates = true;
                Player.Position = new TimeSpan(0, 0, 0, 0, (int)Model.Position.Position.Value);
                Model.CurrentPosition.Value = Position.ToString();
                BlockUpdates = false;
            }
        }

        private void UpdatePositionFromPlayer()
        {
            if (!BlockUpdates)
            {
                BlockUpdates = true;
                Model.Position.Position.Value = Player.Position.TotalMilliseconds;
                Model.CurrentPosition.Value = Position.ToString();
                BlockUpdates = false;
            }
        }
        private bool BlockUpdates
        {
            get;
            set;
        }

        private DispatcherTimer Timer
        {
            get;
            set;
        }

        private PlayerController controller;
        private PlayerModel model;
    }
}
