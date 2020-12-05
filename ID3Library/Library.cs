using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreThreading;
using System.Threading;
using System.IO;
using CoreLogging;
using System.Data.EntityClient;
using CoreDocument;
using System.Diagnostics;
using System.Windows.Threading;
using CoreDocument.Text;
using System.ComponentModel;

namespace ID3Library
{
    public class Library : DocNode
    {
        public Library()
        {
            Help = new LocalizedText("Library");

            SearchString = new DocObj<string>("");
            SearchString.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnSearchStringChanged);
            SearchString.Help = new LocalizedText("LibrarySearchStringHelp");
            
            IsRefreshing = new DocObj<bool>();
            IsRefreshing.Help = new LocalizedText("LibraryIsRefreshing");
        }

        public void Init(string root)
        {
            string connectionString = @"Data Source=|DataDirectory|\LibraryDatabase.sdf";

            refreshThread = new RefreshThread();
            refreshThread.FinishCallback = OnRefreshFinished;
            refreshThread.DatabaseName = connectionString;
            WorkerThreadPool.Instance.StartWork(refreshThread);

            searchThread = new SearchThread();
            searchThread.FinishCallback = OnSearchFinished;
            searchThread.DatabaseName = connectionString;
            WorkerThreadPool.Instance.StartWork(searchThread);

            threadStateTrigger = new DispatcherTimer();
            threadStateTrigger.Tick += OnThreadStateTrigger;
            threadStateTrigger.Interval = new TimeSpan(0, 0, 0, 0, 50);
            threadStateTrigger.Start();

            refreshThread.InputQueue.Enqueue(root);
        }

        public void Refresh(string root)
        {
            IsRefreshing.Value = true;
            refreshThread.InputQueue.Enqueue(root);
        }

        public Tracks[] Tracks
        {
            get
            {
                return searchThread.Tracks;
            }
        }
        public DocObj<string> SearchString
        {
            get;
            set;
        }
        public DocObj<bool> IsRefreshing
        {
            get;
            set;
        }

        private void OnSearchStringChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            searchThread.InputQueue.Enqueue(SearchString.Value);
        }
        private void OnSearchFinished()
        {
            DateTime start = DateTime.Now;
            NotifyPropertyChanged(this, m => m.Tracks);
            Console.WriteLine("{0}: NotifyPropertyChanged took {1} ms", GetType().Name, (DateTime.Now - start).TotalMilliseconds);
        }
        private void OnRefreshFinished()
        {
            searchThread.InputQueue.Enqueue(SearchString.Value);
        }

        private void OnThreadStateTrigger(object sender, EventArgs e)
        {
            IsRefreshing.Value = refreshThread.IsActive || searchThread.IsActive;
        }

        private RefreshThread refreshThread
        {
            get;
            set;
        }
        private SearchThread searchThread;

        private DispatcherTimer threadStateTrigger;
    }
}
