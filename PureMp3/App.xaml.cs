using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using ID3;
using ID3.Processor;
using ID3.Utils;
using CoreVirtualDrive;
using System.Reflection;
using PureMp3.Model.Batch;
using CoreControls.Preferences;
using System.Windows.Navigation;
using CoreDocument.Text;
using CoreThreading;
using CoreControls.Threading;
using CoreDocument;
using System.Threading;
using PureMp3.Model;
using CoreUtils;
using CoreControls;
using System.Windows.Input;

namespace PureMp3
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            WorkerThreadPool.Instance.InvokingThread = new WpfDispatcher(Dispatcher);
            History.Instance.AllowedThreadId = Thread.CurrentThread.ManagedThreadId;

            ApplySkin(new Uri("/CoreControls;component/resources/BaseSkin.xaml", UriKind.Relative));

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "PureMp3.Resources.Texts.xml"))
            {
                LocalizationDatabase.Instance.Load(stream);
            }

            try
            {
                AppPreferences.Load(App.AppName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);
            }

            try
            {
                RecycleBin.Instance.RootDir = App.AppRecycleFolder;
                RecycleBin.Instance.DeleteContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);
            }

            Doc = DocNode.Create<Document>();
            History.Instance.Root = Doc;

            try
            {
                keyboardListener.KeyDown += new RawKeyEventHandler(keyboardListener_KeyDown);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);
            }
        }

        void keyboardListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            ICommand command = null;
            switch (args.Key)
            {
                case System.Windows.Input.Key.MediaPreviousTrack:
                    command = Doc.PlayerCommands.PreviousCommand;
                    break;
                case System.Windows.Input.Key.MediaPlayPause:
                    command = Doc.PlayerCommands.PlayCommand;
                    break;
                case System.Windows.Input.Key.MediaNextTrack:
                    command = Doc.PlayerCommands.NextCommand;
                    break;
            }

            if (command != null)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (command.CanExecute(null))
                    {
                        command.Execute(null);
                    }
                }));
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                AppPreferences.Save(App.AppName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);
            }

            try
            {
                RecycleBin.Instance.DeleteContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);
            }

            try
            {
                BatchAction.DeleteUndoFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);
            }

            base.OnExit(e);
        }

        public Document Doc
        {
            get;
            set;
        }
        private KeyboardListener keyboardListener = new KeyboardListener();

        public void ApplySkin(Uri skinDictionaryUri)
        {
            // Load the ResourceDictionary into memory.
            ResourceDictionary skinDict = Application.LoadComponent(skinDictionaryUri) as ResourceDictionary;

            Collection<ResourceDictionary> mergedDicts = base.Resources.MergedDictionaries;

            // Remove the existing skin dictionary, if one exists.
            // NOTE: In a real application, this logic might need
            // to be more complex, because there might be dictionaries
            // which should not be removed.
            if (mergedDicts.Count > 0)
                mergedDicts.Clear();

            // Apply the selected skin so that all elements in the
            // application will honor the new look and feel.
            mergedDicts.Add(skinDict);
        }

        private void Application_DispatcherUnhandledException(
            object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string title = "A Crash Occurred";
            string message = "Sorry, PureMp3 crashed. A file for diagnostics " +
                "will be written on your Desktop. You can help to improve the " +
                "quality of the program by send the file to mail@mroc.de";

            MessageBox.Show(message, title);

            CrashDumpWriter.DumpException(e.Exception, "PureMp3", "mail@mroc.de");

            e.Handled = true;

            App.Current.Dispatcher.BeginInvokeShutdown(
                System.Windows.Threading.DispatcherPriority.Normal);
        }

        public static string AppName
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Name;
            }
        }
        public static string AppURL
        {
            get
            {
                return "http://www.mroc.de/puremp3";
            }
        }
        public static string AppDataFolder
        {
            get
            {
                string appDataFolder = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.ApplicationData);

                if (!VirtualDrive.ExistsDirectory(appDataFolder))
                {
                    throw new Exception(appDataFolder + @" not found!");
                }

                return Path.Combine(appDataFolder, AppName);
            }
        }
        public static string AppRecycleFolder
        {
            get
            {
                string path = Path.Combine(AppDataFolder, "Recycle");

                if (!VirtualDrive.ExistsDirectory(path))
                {
                    VirtualDrive.CreateDirectory(path);
                }

                return path;
            }
        }
        public static string AppUndoFolder
        {
            get
            {
                string path = Path.Combine(AppDataFolder, "Undo");

                if (!VirtualDrive.ExistsDirectory(path))
                {
                    VirtualDrive.CreateDirectory(path);
                }

                return path;
            }
        }
    }
}
