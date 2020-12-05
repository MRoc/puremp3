using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CoreTest;

namespace ID3CoverSearch
{
    class Parser
    {
        public static List<object> Parse(string text)
        {
            Stack<List<object>> stack = new Stack<List<object>>();

            foreach (var item in new Scanner(text).Scan())
            {
                if (item.Keyword == Keywords.OpenBrackets)
                {
                    stack.Push(new List<object>());
                }
                else if (item.Keyword == Keywords.ClosedBrackets)
                {
                    if (stack.Count == 1)
                    {
                        return stack.Pop();
                    }
                    else
                    {
                        object oldTop = stack.Pop();
                        stack.Peek().Add(oldTop);
                    }
                }
                else if (item.Keyword != Keywords.Comma)
                {
                    if (stack.Count() > 1)
                    {
                        stack.Peek().Add(item.Text);
                    }
                    else
                    {
                        return stack.ElementAt(0);
                    }
                }
            }

            if (stack.Count() == 0)
            {
                return new List<object>();
            }
            else
            {
                return stack.ElementAt(0);
            }
        }
    }
}
