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
using System.Xml;
using System.Xml.Serialization;

namespace CoreDocument
{
    public class DocEnum : DocObj<int>
    {
        private ReadOnlyCollection<string> items;

        public static readonly int undefined = -1;
        public static readonly int multiple = -2;

        public DocEnum()
        {
        }
        public DocEnum(Type enumType)
        {
            this.items = new ReadOnlyCollection<string>(Enum.GetNames(enumType));
        }
        public DocEnum(IEnumerable<string> items)
        {
            this.items = new ReadOnlyCollection<string>(items.ToList());
        }
        public DocEnum(IEnumerable<string> items, object defaultValue)
        {
            this.items = new ReadOnlyCollection<string>(items.ToList());
            this.ValueStr = defaultValue.ToString();
        }
        public DocEnum(ReadOnlyCollection<string> items)
        {
            this.items = items;
        }

        public ReadOnlyCollection<string> Items
        {
            get
            {
                return items;
            }
        }
        public string ValueStr
        {
            get
            {
                if (IsUndefined)
                {
                    return "";
                }
                else if (IsMultiple)
                {
                    return "*";
                }
                else
                {
                    return items[Value];
                }
            }
            set
            {
                if (!Contains(value))
                {
                    throw new Exception("Invalid enum value");
                }

                Value = items.IndexOf(value);
            }
        }
        public string ForceValueStr
        {
            get
            {
                return ValueStr;
            }
            set
            {
                if (!Contains(value))
                {
                    throw new Exception("Invalid enum value");
                }

                ForceValue = items.IndexOf(value);
            }
        }
        public string TryValueStr
        {
            get
            {
                return ValueStr;
            }
            set
            {
                if (Contains(value))
                {
                    ValueStr = value;
                }
                else
                {
                    Value = undefined;
                }
            }
        }
        public string ForceTryValueStr
        {
            get
            {
                return ValueStr;
            }
            set
            {
                if (Contains(value))
                {
                    ForceValueStr = value;
                }
                else
                {
                    ForceValue = undefined;
                }
            }
        }
        public bool Contains(string value)
        {
            return items.Contains(value);
        }
        public bool IsUndefined
        {
            get
            {
                return Value == undefined;
            }
        }
        public bool IsMultiple
        {
            get
            {
                return Value == multiple;
            }
        }
        public bool IsDefined
        {
            get
            {
                return !IsUndefined && !IsMultiple;
            }
        }

        public override int ForceValue
        {
            get
            {
                return base.ForceValue;
            }
            set
            {
                base.ForceValue = value;
                NotifyPropertyChanged(this, m => m.ValueStr);
            }
        }
    }
}
