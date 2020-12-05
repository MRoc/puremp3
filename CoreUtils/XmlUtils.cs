using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace CoreUtils
{
    public interface IXml
    {
        XmlElement ToXml(XmlDocument document);
        void FromXml(XmlElement element);
    }

    public static class XmlUtils
    {
        public static void DumpXml(XmlDocument document)
        {
            using (StringWriter sw = new StringWriter())
            {
                document.Save(sw);
                Console.WriteLine(sw.ToString());
            }
        }
        public static void DumpXml(XmlDocument document, string fileName)
        {
            using (Stream s = File.Open(fileName, FileMode.Create))
            {
                document.Save(s);
            }
        }
        public static XmlDocument StringToXml(string text)
        {
            if (Object.ReferenceEquals(text, null))
            {
                return null;
            }

            XmlDocument document = new XmlDocument();
            using (TextReader reader = new StringReader(text))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                return doc;
            }
        }

        public static void Load(IXml node, string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);
            node.FromXml(doc.DocumentElement);
        }
        public static void Load(IXml node, Stream stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            node.FromXml(doc.DocumentElement);
        }

        public static void Save(IXml node, string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (Stream stream = File.Create(fileName))
            {
                Save(node, stream);
            }
        }
        public static void Save(IXml node, Stream stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(node.ToXml(doc));
            doc.Save(stream);
        }

        public static T SafeLoad<T>(string fileName) where T : IXml, new()
        {
            T result = new T();

            if (File.Exists(fileName))
            {
                try
                {
                    XmlUtils.Load(result as IXml, fileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine(ex.StackTrace);

                    result = new T();
                }
            }
            return result;
        }
        public static T SafeLoad<T>(Stream stream) where T : IXml, new()
        {
            T result = new T();

            try
            {
                XmlUtils.Load(result as IXml, stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);

                result = new T();
            }

            return result;
        }

        public static string SafeElementValue(this XmlElement element, string name)
        {
            if (element[name] != null && element[name].FirstChild != null)
            {
                return element[name].FirstChild.Value;
            }

            return null;
        }

        public static XmlElement AddElementText(XmlDocument doc, XmlElement parent, string elementName, string text)
        {
            XmlElement element = doc.CreateElement(elementName);
            parent.AppendChild(element);

            if (text != null)
            {
                element.AppendChild(doc.CreateTextNode(text));
            }

            return element;
        }
    }

}
