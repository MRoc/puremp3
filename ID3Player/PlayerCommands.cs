using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CoreControls.Commands;
using CoreDocument.Text;
using System.ComponentModel;
using CoreTest;
using FakeItEasy;

namespace ID3Player
{
    public class PlayerCommands
    {
        public PlayerCommands(IPlayerController controller, ITrackAvailability trackAvailability, ITrackNavigation trackNavigation)
        {
            Controller = controller;

            TrackAvailability = trackAvailability;
            TrackNavigation = trackNavigation;
        }

        public ICommand PlayCommand
        {
            get
            {
                if (playCommand == null)
                {
                    CallbackCommand cmd = new CallbackCommand(
                        () => Controller.Play(),
                        (n) => TrackAvailability.HasItems(),
                        new LocalizedText("PlayerCommandsPlay"),
                        new LocalizedText("PlayerCommandsPlayHelp"));

                    TrackAvailability.PropertyChanged += cmd.TriggerCanExecute;

                    playCommand = cmd;
                }
                return playCommand;
            }
        }
        public ICommand StopCommand
        {
            get
            {
                if (stopCommand == null)
                {
                    CallbackCommand cmd = new CallbackCommand(
                        () => Controller.Stop(),
                        (n) => Controller.CurrentState != PlayerState.Stopped,
                        new LocalizedText("PlayerCommandsStop"),
                        new LocalizedText("PlayerCommandsStopHelp"));

                    Controller.StateChanged += cmd.TriggerCanExecute;

                    stopCommand = cmd;
                }
                return stopCommand;
            }
        }
        public ICommand PauseCommand
        {
            get
            {
                if (pauseCommand == null)
                {
                    CallbackCommand cmd = new CallbackCommand(
                        () => Controller.Pause(),
                        (n) => Controller.CurrentState != PlayerState.Stopped,
                        new LocalizedText("PlayerCommandsPause"),
                        new LocalizedText("PlayerCommandsPauseHelp"));

                    Controller.StateChanged += cmd.TriggerCanExecute;

                    pauseCommand = cmd;
                }
                return pauseCommand;
            }
        }
        public ICommand PreviousCommand
        {
            get
            {
                if (previousCommand == null)
                {
                    CallbackCommand cmd = new CallbackCommand(
                        delegate()
                        {
                            Controller.Stop();
                            TrackNavigation.JumpPrevious();
                            Controller.Play();
                        },
                        (n) => TrackAvailability.HasItems(),
                        new LocalizedText("PlayerCommandsPrev"),
                        new LocalizedText("PlayerCommandsPrevHelp"));

                    TrackAvailability.PropertyChanged += cmd.TriggerCanExecute;

                    previousCommand = cmd;
                }
                return previousCommand;
            }
        }
        public ICommand NextCommand
        {
            get
            {
                if (nextCommand == null)
                {
                    CallbackCommand cmd = new CallbackCommand(
                        delegate()
                        {
                            Controller.Stop();
                            TrackNavigation.JumpNext();
                            Controller.Play();
                        },
                        (n) => TrackAvailability.HasItems(),
                        new LocalizedText("PlayerCommandsNext"),
                        new LocalizedText("PlayerCommandsNextHelp"));

                    TrackAvailability.PropertyChanged += cmd.TriggerCanExecute;

                    nextCommand = cmd;
                }
                return nextCommand;
            }
        }

        private ICommand playCommand;
        private ICommand stopCommand;
        private ICommand pauseCommand;
        private ICommand previousCommand;
        private ICommand nextCommand;

        private IPlayerController Controller
        {
            get;
            set;
        }
        private ITrackAvailability TrackAvailability
        {
            get;
            set;
        }
        private ITrackNavigation TrackNavigation
        {
            get;
            set;
        }
    }

    public class TestPlayerCommands
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestPlayerCommands));
        }

        public static void Test_Command_Play_CanExecute_False()
        {
            ITrackAvailability ta = A.Fake<ITrackAvailability>();
            A.CallTo(() => ta.HasItems()).Returns(false);

            PlayerCommands commands = new PlayerCommands(A.Fake<IPlayerController>(), ta, A.Fake<ITrackNavigation>());

            UnitTest.Test(!commands.PlayCommand.CanExecute(null));
        }
        public static void Test_Command_Play_CanExecute_True()
        {
            ITrackAvailability ta = A.Fake<ITrackAvailability>();
            A.CallTo(() => ta.HasItems()).Returns(true);

            PlayerCommands commands = new PlayerCommands(A.Fake<IPlayerController>(), ta, A.Fake<ITrackNavigation>());

            UnitTest.Test(commands.PlayCommand.CanExecute(null));
        }
        public static void Test_Command_Play_Execute()
        {
            ITrackAvailability ta = A.Fake<ITrackAvailability>();
            ITrackNavigation tn = A.Fake<ITrackNavigation>();
            IPlayerController pc = A.Fake<IPlayerController>();

            A.CallTo(() => ta.HasItems()).Returns(true);

            PlayerCommands commands = new PlayerCommands(pc, ta, tn);
            commands.PlayCommand.Execute(null);

            A.CallTo(() => pc.Play()).MustHaveHappened();
        }

        public static void Test_Command_Stop_CanExecute_False()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Stopped);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), A.Fake<ITrackNavigation>());

            UnitTest.Test(!commands.StopCommand.CanExecute(null));
        }
        public static void Test_Command_Stop_CanExecute_True()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Playing);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), A.Fake<ITrackNavigation>());

            UnitTest.Test(commands.StopCommand.CanExecute(null));
        }
        public static void Test_Command_Stop_Execute()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Playing);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), A.Fake<ITrackNavigation>());
            commands.StopCommand.Execute(null);

            A.CallTo(() => pc.Stop()).MustHaveHappened();
        }

        public static void Test_Command_Pause_CanExecute_False()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Stopped);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), A.Fake<ITrackNavigation>());

            UnitTest.Test(!commands.PauseCommand.CanExecute(null));
        }
        public static void Test_Command_Pause_CanExecute_True()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Playing);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), A.Fake<ITrackNavigation>());

            UnitTest.Test(commands.PauseCommand.CanExecute(null));
        }
        public static void Test_Command_Pause_Execute()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Playing);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), A.Fake<ITrackNavigation>());
            commands.PauseCommand.Execute(null);

            A.CallTo(() => pc.Pause()).MustHaveHappened();
        }

        public static void Test_Command_Previous_CanExecute_False()
        {
            ITrackAvailability ta = A.Fake<ITrackAvailability>();
            A.CallTo(() => ta.HasItems()).Returns(false);

            PlayerCommands commands = new PlayerCommands(A.Fake<IPlayerController>(), ta, A.Fake<ITrackNavigation>());

            UnitTest.Test(!commands.PreviousCommand.CanExecute(null));
        }
        public static void Test_Command_Previous_CanExecute_True()
        {
            ITrackAvailability ta = A.Fake<ITrackAvailability>();
            A.CallTo(() => ta.HasItems()).Returns(true);

            PlayerCommands commands = new PlayerCommands(A.Fake<IPlayerController>(), ta, A.Fake<ITrackNavigation>());

            UnitTest.Test(commands.PreviousCommand.CanExecute(null));
        }
        public static void Test_Command_Previous_Execute()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            ITrackNavigation tn = A.Fake<ITrackNavigation>();

            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Playing);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), tn);
            commands.PreviousCommand.Execute(null);

            A.CallTo(() => tn.JumpPrevious()).MustHaveHappened();
        }

        public static void Test_Command_Next_CanExecute_False()
        {
            ITrackAvailability ta = A.Fake<ITrackAvailability>();
            A.CallTo(() => ta.HasItems()).Returns(false);

            PlayerCommands commands = new PlayerCommands(A.Fake<IPlayerController>(), ta, A.Fake<ITrackNavigation>());

            UnitTest.Test(!commands.NextCommand.CanExecute(null));
        }
        public static void Test_Command_Next_CanExecute_True()
        {
            ITrackAvailability ta = A.Fake<ITrackAvailability>();
            A.CallTo(() => ta.HasItems()).Returns(true);

            PlayerCommands commands = new PlayerCommands(A.Fake<IPlayerController>(), ta, A.Fake<ITrackNavigation>());

            UnitTest.Test(commands.NextCommand.CanExecute(null));
        }
        public static void Test_Command_Next_Execute()
        {
            IPlayerController pc = A.Fake<IPlayerController>();
            ITrackNavigation tn = A.Fake<ITrackNavigation>();

            A.CallTo(() => pc.CurrentState).Returns(PlayerState.Playing);

            PlayerCommands commands = new PlayerCommands(pc, A.Fake<ITrackAvailability>(), tn);
            commands.NextCommand.Execute(null);

            A.CallTo(() => tn.JumpNext()).MustHaveHappened();
        }
    }
}
