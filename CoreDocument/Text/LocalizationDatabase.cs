using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace CoreDocument.Text
{
    public class LocalizationDatabase
    {
        public void Load(Stream stream)
        {
            texts.Clear();

            XmlDocument document = new XmlDocument();
            document.Load(stream);

            foreach (XmlNode node in document["Items"])
            {
                XmlElement element = node as XmlElement;

                if (element == null)
                    continue;

                string id = element.GetAttribute("Id");
                string text = (element.FirstChild as XmlText).Value.Trim().Replace("\r\n", "");

                if (texts.ContainsKey(id))
                {
                    Console.WriteLine("Warning: entry \"" + id + "\" duplicated");
                }

                texts[id] = text;
            }
        }

        public string GetText(string id)
        {
            if (texts.ContainsKey(id))
            {
                return texts[id];
            }
            else
            {
                if (!warned.Contains(id))
                {
                    Console.WriteLine(GenerateXmlEntry(id));
                    warned.Add(id);
                }
                return id;
            }
        }

        public string GenerateXmlEntry(string id)
        {
            XmlDocument doc = new XmlDocument();

            XmlElement element = doc.CreateElement("Item");
            element.SetAttribute("Id", id);
            doc.AppendChild(element);

            element.AppendChild(doc.CreateTextNode("DUMMY TEXT"));

            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            return sw.ToString();
        }

        public static LocalizationDatabase Instance
        {
            get
            {
                return instance;
            }
        }
        private static LocalizationDatabase instance = new LocalizationDatabase();

        private Dictionary<string, string> texts = new Dictionary<string, string>();

        private HashSet<string> warned = new HashSet<string>();
    }
}
