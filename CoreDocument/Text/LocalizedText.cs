using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreDocument.Text
{
    public class LocalizedText : Text
    {
        public LocalizedText(string text)
            : base(text)
        {
        }

        public override string ToString()
        {
            return LocalizationDatabase.Instance.GetText(text);
        }
    }
}
