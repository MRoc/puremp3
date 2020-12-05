using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using CoreVirtualDrive;
using CoreUtils;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;

namespace CoreControls.Preferences
{
    public class AppPreferences : IXml
    {
        private static AppPreferences preferences;

        public static void Load(string appName)
        {
            preferences = XmlUtils.SafeLoad<AppPreferences>(PreferencesFileFullName(appName));
        }
        public static void Save(string appName)
        {
            if (File.Exists(PreferencesFileFullName(appName)))
            {
                File.Delete(PreferencesFileFullName(appName));
            }

            XmlUtils.Save(preferences, PreferencesFileFullName(appName));
        }
        public static AppPreferences Instance
        {
            get
            {
                return preferences;
            }
        }

        public void Set<T>(string key, T value)
        {
            dictionary[key] = value;
        }
        public bool HasKey(string key)
        {
            return dictionary.ContainsKey(key);
        }
        public T Get<T>(string key, T defaultValue)
        {
            if (HasKey(key))
            {
                return (T)dictionary[key];
            }
            else
            {
                return defaultValue;
            }
        }

        private Dictionary<string, object> dictionary = new Dictionary<string, object>();
        #region IXml Members
        public void FromXml(XmlElement element)
        {
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement))
                {
                    continue;
                }
                XmlElement item = node as XmlElement;

                XmlElement keyElement = item["key"];
                XmlElement valueElement = item["value"];

                string key = keyElement.GetAttribute("value");

                object value = Create(
                    valueElement.GetAttribute("class"),
                    valueElement.GetAttribute("value"));

                dictionary.Add(key, value);
            }
        }
        public XmlElement ToXml(XmlDocument document)
        {
            XmlElement root = document.CreateElement("root");
            root.SetAttribute("class", GetType().AssemblyQualifiedName);

            foreach (var key in dictionary.Keys)
            {
                XmlElement itemElement = document.CreateElement("item");

                XmlElement keyElement = document.CreateElement("key");
                keyElement.SetAttribute("class", key.GetType().Name);
                keyElement.SetAttribute("value", key.ToString());

                object value = dictionary[key];
                XmlElement valueElement = document.CreateElement("value");
                valueElement.SetAttribute("class", value.GetType().Name);
                valueElement.SetAttribute("value", value.ToString());

                itemElement.AppendChild(keyElement);
                itemElement.AppendChild(valueElement);

                root.AppendChild(itemElement);
            }

            return root;
        }
        private static object Create(string className, string value)
        {
            Type t = Type.GetType("System." + className);
            MethodInfo m = t.GetMethod("Parse", new Type[] { typeof(string) });

            if (m != null)
            {
                return m.Invoke(null, new object[] { value });
            }
            else
            {
                return (object)value;
            }
        }
        #endregion

        public AppPreferences()
        {
        }
        private static string PreferencesDirectoryName(string appName)
        {
            string appDataFolder = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.ApplicationData);

            if (!VirtualDrive.ExistsDirectory(appDataFolder))
            {
                throw new Exception(appDataFolder + @" not found!");
            }

            string preferencesFolder = Path.Combine(appDataFolder, appName);
            if (!VirtualDrive.ExistsDirectory(preferencesFolder))
            {
                VirtualDrive.CreateDirectory(preferencesFolder);
            }

            return preferencesFolder;
        }
        private static string PreferencesFileName()
        {
            return typeof(AppPreferences).Name + ".xml";
        }
        private static string PreferencesFileFullName(string appName)
        {
            return Path.Combine(PreferencesDirectoryName(appName), PreferencesFileName());
        }
    }
}
