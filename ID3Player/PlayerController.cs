using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CoreTest;
using CoreDocument;

namespace ID3Player
{
    public enum PlayerState
    {
        Stopped,
        Playing,
        Paused
    }

    public interface IPlayerController
    {
        void Pause();
        void Play();
        void Stop();

        event EventHandler StateChanged;

        PlayerState CurrentState
        {
            get;
        }
    }

    public class PlayerController : DocNode, IPlayerController
    {
        public PlayerController()
        {
        }
        public PlayerController(PlayerState state)
        {
            CurrentState = state;
        }

        public Action PlayerPlay;
        public Action PlayerStop;
        public Action PlayerPause;
        public Action PlayerLoad;

        public void Pause()
        {
            if (CurrentState == PlayerState.Playing)
            {
                Halt();
            }
            else if (CurrentState == PlayerState.Paused)
            {
                Continue();
            }
        }
        public void Play()
        {
            if (CurrentState != PlayerState.Playing)
            {
                if (CurrentState != PlayerState.Paused)
                {
                    Stop();
                    Load();
                }

                Continue();
            }
            else
            {
                Pause();
            }
        }
        public void Stop()
        {
            if (CurrentState != PlayerState.Stopped)
            {
                CurrentState = PlayerState.Stopped;

                if (PlayerStop != null)
                {
                    PlayerStop();
                }
            }
        }

        private void Continue()
        {
            CurrentState = PlayerState.Playing;

            if (PlayerPlay != null)
            {
                PlayerPlay();
            }
        }
        private void Halt()
        {
            if (CurrentState == PlayerState.Playing)
            {
                CurrentState = PlayerState.Paused;

                if (PlayerPause != null)
                {
                    PlayerPause();
                }
            }
        }
        private void Load()
        {
            if (PlayerLoad != null)
            {
                PlayerLoad();
            }
        }

        public event EventHandler StateChanged;
        public PlayerState CurrentState
        {
            get
            {
                return currentState;
            }
            private set
            {
                currentState = value;

                if (StateChanged != null)
                {
                    StateChanged(this, new PropertyChangedEventArgs("CurrentState"));
                }
            }
        }
        private PlayerState currentState;
    }

    public class TestPlayerController
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestPlayerController));
        }

        public enum Input
        {
            Stop,
            Play,
            Pause
        }
        public enum Output
        {
            Load,
            Play,
            Stop,
            Pause
        }
        class State
        {
            public State(
                PlayerState state0,
                Input input,
                PlayerState state1,
                IEnumerable<Output> outputs)
            {
                State0 = state0;
                Input = input;
                State1 = state1;
                Outputs = outputs;
            }

            public PlayerState State0
            {
                get;
                private set;
            }
            public Input Input
            {
                get;
                private set;
            }
            public PlayerState State1
            {
                get;
                private set;
            }
            public IEnumerable<Output> Outputs
            {
                get;
                private set;
            }
        }
        public static void TestPlayerController_States()
        {
            State[] transitions =
            {
                new State(PlayerState.Stopped, Input.Stop,  PlayerState.Stopped, new Output[] {}),
                new State(PlayerState.Stopped, Input.Pause, PlayerState.Stopped, new Output[] {}),
                new State(PlayerState.Stopped, Input.Play,  PlayerState.Playing, new Output[] { Output.Load, Output.Play }),

                new State(PlayerState.Playing, Input.Stop,  PlayerState.Stopped, new Output[] { Output.Stop }),
                new State(PlayerState.Playing, Input.Pause, PlayerState.Paused,  new Output[] { Output.Pause }),
                new State(PlayerState.Playing, Input.Play,  PlayerState.Paused,  new Output[] { Output.Pause }),

                new State(PlayerState.Paused,  Input.Stop,  PlayerState.Stopped, new Output[] { Output.Stop }),
                new State(PlayerState.Paused,  Input.Pause, PlayerState.Playing, new Output[] { Output.Play }),
                new State(PlayerState.Paused,  Input.Play,  PlayerState.Playing, new Output[] { Output.Play }),
            };

            foreach (var item in transitions)
            {
                PlayerController playerController = new PlayerController(item.State0);

                PropertyChangedTest stateChangedTest = new PropertyChangedTest();
                playerController.StateChanged += stateChangedTest.PropertyChanged;

                List<Output> outputs = new List<Output>();
                playerController.PlayerLoad += () => outputs.Add(Output.Load);
                playerController.PlayerStop += () => outputs.Add(Output.Stop);
                playerController.PlayerPlay += () => outputs.Add(Output.Play);
                playerController.PlayerPause += () => outputs.Add(Output.Pause);

                playerController.GetType().GetMethod(item.Input.ToString()
                    ).Invoke(playerController, new object[] { });

                UnitTest.Test(playerController.CurrentState == item.State1);

                if (item.State0 != item.State1)
                {
                    stateChangedTest.TestWasCalledOnce();
                }
                else
                {
                    stateChangedTest.TestWasCalled(0);
                }

                UnitTest.Test(outputs.SequenceEqual(item.Outputs));
            }
        }
    }
}
