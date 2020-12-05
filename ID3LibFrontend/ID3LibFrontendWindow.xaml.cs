using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CoreControls;
using CoreControls.Threading;
using CoreLogging;
using CoreThreading;
using CoreVirtualDrive;
using ID3.Processor;

namespace ID3LibFrontend
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ID3LibFrontendWindow : Window
    {
        private LoggerTextBox logger;
        private BatchWork batchWork;

        public ID3LibFrontendWindow()
        {
            InitializeComponent();

            logger = new LoggerTextBox(textboxOutput, labelStatus);

            List<ID3Operations.Operation> operations =
                ID3Operations.Instance.Operations;

            foreach (ID3Operations.Operation op in operations)
            {
                comboboxOperations.Items.Add(op);
            }

            comboboxOperations.SelectedIndex = 0;

            WorkerThreadPool.Instance.InvokingThread = new WpfDispatcher(Dispatcher);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            CurrentPath = ((App)Application.Current).startupPath;
            if (String.IsNullOrEmpty(CurrentPath))
            {
                CurrentPath = CoreControls.Preferences.AppPreferences.Instance.Get<string>("path", "");
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            WorkerThreadPool.Instance.Exit();

            CoreControls.Preferences.AppPreferences.Instance.Set<string>("path", CurrentPath);

            base.OnClosing(e);
        }

        private String CurrentPath
        {
            get { return textBoxDirectory.Directory; }
            set { textBoxDirectory.Directory = value; }
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            if (VirtualDrive.ExistsDirectory(CurrentPath))
            {
                if (batchWork != null)
                {
                    StopWorkerThread();
                }
                else
                {
                    StartWorkerThread();
                }
            }
        }

        private void StartWorkerThread()
        {
            EnableWindow(false);
            logger.Verbose = IsVeboseChecked();

            batchWork = new BatchWork(
                CurrentPath,
                null,
                OnWorkerThreadFinished,
                Operation());

            WorkerThreadPool.Instance.StartWork(batchWork);
        }
        private void StopWorkerThread()
        {
            WorkerThreadPool.Instance.Abort = true;
        }

        private void WaitForWorkerThread()
        {
            WorkerThreadPool.Instance.Exit();
        }
        private void OnWorkerThreadFinished(IWork work)
        {
            batchWork = null;
            EnableWindow(true);
        }

        private void EnableWindow(bool enable)
        {
            textboxOutput.IsEnabled = enable;
            textBoxDirectory.IsEnabled = enable;
            comboboxOperations.IsEnabled = enable;
            checkboxVerbose.IsEnabled = enable;

            if (enable)
            {
                buttonStart.Content = "Start";
            }
            else
            {
                buttonStart.Content = "Stop";
            }
        }

        private bool IsVeboseChecked()
        {
            bool? test = checkboxVerbose.IsChecked;

            if (test.HasValue) //check for a value
            {
                return (bool)test;
            }
            else
            {
                return false;
            }
        }

        private ID3.Processor.IProcessorMutable Operation()
        {
            return ID3Operations.Instance.Instantiate(comboboxOperations.SelectedItem);
        }
    }

    class BatchWork : IWork
    {
        private ID3.Processor.IProcessorMutable Operation { get; set; }
        private Action<IWork> BeforeCallback { get; set; }
        private Action<IWork> AfterCallback { get; set; }
        private string RootDirectory { get; set; }

        public BatchWork(
            string root,
            Action<IWork> beforeCallback,
            Action<IWork> afterCallback,
            ID3.Processor.IProcessorMutable operation)
        {
            BeforeCallback = beforeCallback;
            AfterCallback = afterCallback;
            RootDirectory = root;

            if (operation.SupportedClasses().Contains(typeof(FileInfo))
                || operation.SupportedClasses().Contains(typeof(DirectoryInfo)))
            {
                ID3.Processor.DirectoryProcessor processor = new ID3.Processor.DirectoryProcessor(operation);
                processor.ForceRecurse = true;
                Operation = processor;
            }
            else
            {
                throw new Exception("Operation not supported: " + operation.GetType().Name);
            }
        }

        public IWorkType Type
        {
            get
            {
                return IWorkType.AbortableLock;
            }
        }
        public bool Abort
        {
            get
            {
                ProcessorMessageQueryAbort msg = new ProcessorMessageQueryAbort();
                Operation.ProcessMessage(msg);
                return msg.Abort;
            }
            set
            {
                Operation.ProcessMessage(new ProcessorMessageAbort(value));
            }
        }

        public void Before()
        {
            if (!Object.ReferenceEquals(BeforeCallback, null))
                BeforeCallback(this);

            Operation.ProcessMessage(new ProcessorMessageInit());
        }
        public void Run()
        {
            Operation.Process(new DirectoryInfo(RootDirectory));
        }
        public void After()
        {
            Operation.ProcessMessage(new ProcessorMessageExit());

            if (!Object.ReferenceEquals(AfterCallback, null))
                AfterCallback(this);
        }
    }

    class LoggerTextBox : TextBoxLogger, ILogger
    {
        public LoggerTextBox(TextBox textbox, Label status)
            : base(textbox, status)
        {
            Logger.Instance = this;
        }

        public bool Verbose { get; set; }
        public bool Warnings { get; set; }

        public void Write(object text)
        {
            AppendText(text);
        }
        public void WriteLine(object text)
        {
            AppendText(text + "\n");
        }
        public void WriteLineIfVerbose(object text)
        {
            if (Verbose)
            {
                AppendText(text + "\n");
            }
        }
        public void WriteStatus(object text)
        {
            SetStatus(text);
        }
        public void WriteWarning(object warning)
        {
            if (Verbose || Warnings)
            {
                AppendText(warning + "\n");
            }
        }
    }
}
