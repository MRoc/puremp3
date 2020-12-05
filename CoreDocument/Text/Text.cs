using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreDocument.Text
{
    public class Text
    {
        public Text(object text)
        {
            this.text = text.ToString();
        }

        public override string ToString()
        {
            return this.text;
        }

        protected string text;
    }
}
