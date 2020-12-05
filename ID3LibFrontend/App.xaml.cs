using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Data;
using System.Linq;
using System.Windows;
using CoreControls;
using CoreVirtualDrive;

namespace ID3LibFrontend
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string startupPath;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length == 1 &&
              (VirtualDrive.ExistsDirectory(e.Args[0]) || VirtualDrive.ExistsFile(e.Args[0])))
            {
                startupPath = e.Args[0];
            }

            CoreControls.Preferences.AppPreferences.Load(AppName);

            ApplySkin(new Uri("/CoreControls;component/resources/BaseSkin.xaml", UriKind.Relative));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CoreControls.Preferences.AppPreferences.Save("ID3LibFrontend");
            base.OnExit(e);
        }

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

        public static string AppName
        {
            get
            {
                return "ID3LibFrontend";
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
    }
}
