using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;

namespace ID3CoverSearch
{
    enum Keywords
    {
        Eof,
        StringLiteral,
        Number,
        OpenBrackets,
        ClosedBrackets,
        Comma
    }
    class Scanner
    {
        private string input;
        private int c0;
        private int position;
        private StringBuilder stringBuilder = new StringBuilder();

        public Scanner(string text)
        {
            this.input = text;
            Next();
        }

        public class ScanResult
        {
            public ScanResult(Keywords keyword, string text)
            {
                Keyword = keyword;
                Text = text;
            }
            public Keywords Keyword
            {
                get;
                set;
            }
            public string Text
            {
                get;
                set;
            }
            public override string ToString()
            {
                return Text;
            }
        }
        public IEnumerable<ScanResult> Scan()
        {
            Keywords curKeyWord;

            while ((curKeyWord = Yylex()) != Keywords.Eof)
            {
                yield return new ScanResult(curKeyWord, CurrentText);
            }
        }

        private Keywords Yylex()
        {
            stringBuilder.Clear();
            SkipWhiteSpace();

            switch (c0)
            {
                case '[':
                case ']':
                case ',':
                    return ScanToken();

                case '"':
                    return ScanString();

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return ScanNumber(1);
            }

            return Keywords.Eof;
        }
        private void Next()
        {
            if (position == input.Length)
            {
                c0 = -1;
            }
            else
            {
                c0 = input[position++];
            }
        }
        private void SkipWhiteSpace()
        {
            while (c0 != -1)
            {
                switch (c0)
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        Next();
                        break;
                    default:
                        return;
                }
            }
        }
        private Keywords ScanToken()
        {
            stringBuilder.Clear();
            stringBuilder.Append((char)c0);

            Next();

            switch (stringBuilder.ToString())
            {
                case "[": return Keywords.OpenBrackets;
                case "]": return Keywords.ClosedBrackets;
                case ",": return Keywords.Comma;
            }

            throw new Exception("Scanner failed");
        }
        private Keywords ScanString()
        {
            stringBuilder.Clear();

            Next();
            while ((c0 != -1) && (c0 != '"'))
            {
                if (c0 == '\\')
                {
                    Next();
                }
                stringBuilder.Append((char)c0);

                Next();
            }

            if (c0 != -1)
            {
                Next();
            }

            return Keywords.StringLiteral;
        }
        private Keywords ScanNumber(int _state)
        {
            int state = _state;
            bool run = true;
            do
            {
                stringBuilder.Append((char)c0);
                Next();

                switch (state)
                {
                    case 0:
                        if (Char.IsDigit((char)c0))
                            state = 1;
                        else if (c0 == '.')
                            state = 2;
                        else if (c0 == '-' || c0 == '+')
                            state = 3;
                        else
                            throw new Exception("syntax error");
                        break;
                    case 1:
                        if (Char.IsDigit((char)c0))
                            state = 1;
                        else if (c0 == 'l' || c0 == 'L')
                            state = 9;
                        else if (c0 == 'f' || c0 == 'F' || c0 == 'd' || c0 == 'D')
                            state = 8;
                        else if (c0 == 'e' || c0 == 'E')
                            state = 5;
                        else
                        {
                            // Integer
                            run = false;
                        }

                        break;
                    case 2:
                        if (Char.IsDigit((char)c0))
                            state = 4;
                        else
                            throw new Exception("syntax error");
                        break;
                    case 3:
                        if (c0 == '.')
                            state = 2;
                        else if (Char.IsDigit((char)c0))
                            state = 1;
                        else
                            throw new Exception("syntax error");
                        break;
                    case 4:
                        // FLOAT
                        if (Char.IsDigit((char)c0))
                            state = 4;
                        else if (c0 == 'e' || c0 == 'E')
                            state = 5;
                        else
                        {
                            // FLOAT
                            run = false;
                        }
                        break;
                    case 5:
                        if (c0 == '+' || c0 == '-')
                            state = 6;
                        else if (Char.IsDigit((char)c0))
                            state = 7;
                        else
                            throw new Exception("syntax error");
                        break;
                    case 6:
                        if (Char.IsDigit((char)c0))
                            state = 7;
                        else
                            throw new Exception("syntax error");
                        break;
                    case 7:
                        if (Char.IsDigit((char)c0))
                            state = 7;
                        else if (c0 == 'f' || c0 == 'F' || c0 == 'd' || c0 == 'D')
                            state = 8;
                        else
                        {
                            // FLOAT
                            run = false;
                        }
                        break;
                    case 8:
                        run = false;
                        // FLOAT
                        break;
                    case 9:
                        run = false;
                        // INTEGER
                        break;
                }
            } while (run);

            return Keywords.Number;
        }

        private string CurrentText
        {
            get
            {
                return stringBuilder.ToString();
            }
        }
    }
}
