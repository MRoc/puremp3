using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace CoreDocument.Text
{
    public class TextBindingProvider : DynamicObject
    {
        public static TextBindingProvider Instance
        {
            get
            {
                return instance;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder,
                                          out object result)
        {
            result = LocalizationDatabase.Instance.GetText(binder.Name);
            return true;
        }

        private static TextBindingProvider instance = new TextBindingProvider();
    }
}
