using System;
using System.Collections.Generic;
using System.Linq;
using CoreUtils;
using System.Text;
using CoreThreading;
using System.Threading;

namespace ID3Library
{
    internal class SearchThread : IWork
    {
        public SearchThread()
        {
            InputQueue = new DelayedStringThreadQueue();
            InputQueue.Enqueue("");
        }

        public string DatabaseName
        {
            get;
            set;
        }
        public DelayedStringThreadQueue InputQueue
        {
            get;
            private set;
        }
        public Action FinishCallback
        {
            get;
            set;
        }
        public bool IsActive
        {
            get;
            private set;
        }
        public Tracks[] Tracks
        {
            get
            {
                lock (this)
                {
                    return tracks;
                }
            }
            private set
            {
                lock (this)
                {
                    tracks = value;
                }
            }
        }
        public string[] Artists
        {
            get
            {
                lock (this)
                {
                    return artists;
                }
            }
            private set
            {
                lock (this)
                {
                    artists = value;
                }
            }
        }

        public void Before()
        {
            Console.WriteLine("{0}: Starting. Opening database connection", GetType().Name);
            context = new LibraryDatabase(DatabaseName);
        }
        public void Run()
        {
            while (!Abort)
            {
                string toSearchFor = InputQueue.DequeueLast();

                if (!Object.ReferenceEquals(toSearchFor, null))
                {
                    IsActive = true;

                    RunQuery(toSearchFor);

                    IsActive = false;

                    if (InputQueue.Count == 0)
                    {
                        WorkerThreadPool.Instance.InvokingThread.BeginInvokeLowPrio(FinishCallback);
                    }
                }
            }
        }
        public void After()
        {
            Console.WriteLine("{0}: Stopped. Closing database connection", GetType().Name);
            context.Dispose();
            context = null;
        }

        public IWorkType Type
        {
            get
            {
                return IWorkType.Invisible;
            }
        }
        public bool Abort
        {
            get
            {
                return abort;
            }
            set
            {
                abort = value;

                if (abort)
                {
                    InputQueue.Abort = value;
                }
            }
        }

        private void RunQuery(string toSearchFor)
        {
            Console.WriteLine("{0}: Starting search", GetType().Name);

            DateTime before = DateTime.Now;
            if (String.IsNullOrEmpty(toSearchFor))
            {
                Tracks = (from item
                          in context.Tracks
                          orderby item.Artist, item.Album
                          select item).ToArray();
            }
            else
            {
                if (toSearchFor.Contains('"'))
                {
                    string cleanedText = toSearchFor.RemoveChar('"');

                    Tracks = (from item
                              in context.Tracks
                              where item.FullText.Contains(cleanedText)
                              orderby item.Artist, item.Album
                              select item).ToArray();
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string word in toSearchFor.SplitByWords())
                    {
                        if (sb.Length != 0)
                        {
                            sb.Append(" && ");
                        }

                        sb.Append("FullText.Contains(\"");
                        sb.Append(word);
                        sb.Append("\")");
                    }

                    Tracks =
                        context.Tracks.
                        Where(sb.ToString()).
                        OrderBy("Artist, Album").ToArray();
                }
            }
            Console.WriteLine("{0}: Search took {1} ms", GetType().Name, (DateTime.Now - before).TotalMilliseconds);
        }

        private bool abort;
        private LibraryDatabase context;
        private Tracks[] tracks;
        private string[] artists;
    }
}
