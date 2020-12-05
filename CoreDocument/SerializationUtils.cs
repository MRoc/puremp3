using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace CoreDocument
{
    public static class SerializationUtils
    {
        public static void SerializeXml<T>(Stream s, T obj)
        {
            System.Xml.Serialization.XmlSerializer x =
                new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (TextWriter writer = new StreamWriter(s))
            {
                x.Serialize(writer, obj);
                writer.Flush();
            }
        }
        public static T DeserializeXml<T>(Stream s)
        {
            System.Xml.Serialization.XmlSerializer x =
                new System.Xml.Serialization.XmlSerializer(typeof(T));
            
            using (TextReader reader = new StreamReader(s))
            {
                T result = (T)x.Deserialize(reader);

                if (result is IDocNode)
                {
                    (result as IDocNode).ResolveChildrenLinks();
                }

                return result;
            }
        }
    }
}
