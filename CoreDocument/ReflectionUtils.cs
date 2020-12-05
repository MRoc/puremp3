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
    static class FieldUtils
    {
        private static readonly BindingFlags bindingFlags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.FlattenHierarchy;

        private static Dictionary<Type, string[]> namesByType =
            new Dictionary<Type, string[]>();

        public static string[] NamesByType(Type type)
        {
            if (!namesByType.ContainsKey(type))
            {
                Dictionary<string, string> fieldNamesCollector =
                    new Dictionary<string, string>();

                Type t = type;
                while (!t.Equals(typeof(object)))
                {
                    CollectNames(t, fieldNamesCollector, IsSupported);
                    t = t.BaseType;
                }

                namesByType[type] = fieldNamesCollector.Values.ToArray();
            }

            return namesByType[type];
        }
        private static void CollectNames(Type type, Dictionary<string, string> names, Predicate<FieldInfo> predicate)
        {
            var infos = type.GetFields(bindingFlags);
            foreach (var info in infos)
            {
                if (predicate(info) && !names.ContainsKey(info.Name))
                {
                    names[info.Name] = info.Name;
                }
            }
        }
        public static System.Attribute[] Attributes(Type type, string name)
        {
            return System.Attribute.GetCustomAttributes(type.GetField(name));
        }
        public static bool IsSupported(FieldInfo info)
        {
            bool isClass = info.FieldType.IsClass;
            bool isInterface = info.FieldType.IsInterface;
            bool isDocLeaf = info.FieldType.Equals(typeof(IDocLeaf));
            bool supportsIDocLeaf = info.FieldType.GetInterface(typeof(IDocLeaf).Name) != null;

            return
                ((isClass && supportsIDocLeaf) || (isInterface && isDocLeaf))
                && !DocObjRef.IsDocObjRef(info);
        }
        public static bool IsSupportedIncludingDocObjRef(FieldInfo info)
        {
            bool isClass = info.FieldType.IsClass;
            bool isInterface = info.FieldType.IsInterface;
            bool isDocLeaf = info.FieldType.Equals(typeof(IDocLeaf));
            bool supportsIDocLeaf = info.FieldType.GetInterface(typeof(IDocLeaf).Name) != null;

            return ((isClass && supportsIDocLeaf) || (isInterface && isDocLeaf));
        }

        public static object ByName(object obj, string name)
        {
            Type t = obj.GetType();
            while (!t.Equals(typeof(object)))
            {
                if (t.GetField(name, bindingFlags) != null)
                {
                    return (IDocLeaf)t.InvokeMember(
                        name,
                        bindingFlags | BindingFlags.GetField,
                        null,
                        obj,
                        new Object[] { });
                }
                t = t.BaseType;
            }

            return null;
        }
        public static void SetByName(object obj, string name, object child)
        {
            Type t = obj.GetType();
            while (!t.Equals(typeof(object)))
            {
                if (t.GetField(name, bindingFlags) != null)
                {
                    t.InvokeMember(
                        name,
                        bindingFlags | BindingFlags.SetField,
                        null,
                        obj,
                        new Object[] { child });
                    return;
                }
                t = t.BaseType;
            }
        }
    }

    static class PropertyUtils
    {
        private static readonly BindingFlags bindingFlags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.FlattenHierarchy;

        private static Dictionary<Type, string[]> namesByType =
            new Dictionary<Type, string[]>();

        public static string[] NamesByType(Type type)
        {
            if (!namesByType.ContainsKey(type))
            {
                namesByType[type] = NamesByType(type, IsSupportedExcludeDocObjRec);
            }

            return namesByType[type];
        }
        public static string[] NamesByType(Type type, Predicate<PropertyInfo> predicate)
        {
            Dictionary<string, string> namesCollector =
                new Dictionary<string, string>();

            Type t = type;
            while (!t.Equals(typeof(object)))
            {
                CollectNames(t, namesCollector, predicate);
                t = t.BaseType;
            }

            return namesCollector.Values.ToArray();
        }
        private static void CollectNames(Type type, Dictionary<string, string> names, Predicate<PropertyInfo> predicate)
        {
            var infos = type.GetProperties(bindingFlags);
            foreach (var info in infos)
            {
                if (predicate(info) && !names.ContainsKey(info.Name))
                {
                    names[info.Name] = info.Name;
                }
            }
        }
        public static System.Attribute[] FieldAttributes(Type type, string fieldName)
        {
            return System.Attribute.GetCustomAttributes(type.GetField(fieldName));
        }

        public static bool IsSupportedExcludeDocObjRec(PropertyInfo info)
        {
            return !DocObjRef.IsDocObjRef(info) && IsSupported(info);
        }
        public static bool IsSupported(PropertyInfo info)
        {
            bool isClass = info.PropertyType.IsClass;
            bool isInterface = info.PropertyType.IsInterface;
            bool isDocLeaf = info.PropertyType.Equals(typeof(IDocLeaf));
            bool supportsIDocLeaf = info.PropertyType.GetInterface(typeof(IDocLeaf).Name) != null;
            bool isIndexParam = info.GetIndexParameters().Length > 0;

            return !isIndexParam &&  ((isClass && supportsIDocLeaf) || (isInterface && isDocLeaf));
        }

        public static object ByName(object obj, string name)
        {
            Type t = obj.GetType();
            while (!t.Equals(typeof(object)))
            {
                if (t.GetProperty(name, bindingFlags) != null)
                {
                    return (IDocLeaf)t.InvokeMember(
                        name,
                        bindingFlags | BindingFlags.GetProperty,
                        null,
                        obj,
                        new Object[] { });
                }
                t = t.BaseType;
            }

            return null;
        }
        public static void SetByName(object obj, string name, object child)
        {
            Type t = obj.GetType();
            while (!t.Equals(typeof(object)))
            {
                if (t.GetProperty(name, bindingFlags) != null)
                {
                    t.InvokeMember(
                        name,
                        bindingFlags | BindingFlags.SetProperty,
                        null,
                        obj,
                        new Object[] { child });
                    return;
                }
                t = t.BaseType;
            }
        }
    }
}
