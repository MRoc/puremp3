using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace CoreDocument
{
    public class DocEnumMultiAdapter<T> : DocListObjListener<T, int> where T : IDocLeaf
    {
        public DocEnumMultiAdapter()
        {
            LinkedToHook = true;

            PropertyChangedEvent += delegate(object obj, EventArgs e)
            {
                ForceUpdate();
            };
        }

        public PropertyProviderDelegate PropertyProviderSelected
        {
            get;
            set;
        }
        public void ForceUpdate()
        {
            docEnum.ForceValue = Value;
        }

        private DocEnum docEnum;
        public DocEnum DocEnum
        {
            get
            {
                return docEnum;
            }
            set
            {
                if (docEnum != null)
                {
                }

                docEnum = value;

                if (docEnum != null)
                {
                    docEnum.Hook = EnumHook;
                    docEnum.ForceValue = Value;
                }
            }
        }

        public int Value
        {
            get
            {
                int value = DocEnum.undefined;

                foreach (T obj in Items)
                {
                    DocObj<int> curEnum = PropertyProviderSelected(obj);

                    if (!Object.ReferenceEquals(curEnum, null))
                    {
                        if (value == DocEnum.undefined)
                        {
                            value = curEnum.Value;
                        }
                        else if (value != curEnum.Value)
                        {
                            value = DocEnum.multiple;
                        }
                    }
                }

                return value;
            }
        }

        private void EnumHook(object sender, EventArgs e)
        {
            int newValue = (e as DocEnum.DocObjCommand).NewValue;

            try
            {
                foreach (object obj in Items)
                {
                    PropertyProvider(obj).Value = newValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
