using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using System.Net;
using System.IO;
using System.Xml;
using CoreUtils;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace CoreWeb
{
    public class UrlHelper
    {
        public static string Encode(string str)
        {
            var charClass = String.Format("0-9a-zA-Z{0}", Regex.Escape("-_.!~*'()"));
            return Regex.Replace(str,
                String.Format("[^{0}]", charClass),
                new MatchEvaluator(EncodeEvaluator));
        }

        public static string EncodeEvaluator(Match match)
        {
            return (match.Value == " ") ? "+" : String.Format("%{0:X2}", Convert.ToInt32(match.Value[0]));
        }

        public static string DecodeEvaluator(Match match)
        {
            return Convert.ToChar(int.Parse(match.Value.Substring(1), System.Globalization.NumberStyles.HexNumber)).ToString();
        }

        public static string Decode(string str)
        {
            return Regex.Replace(str.Replace('+', ' '), "%[0-9a-zA-Z][0-9a-zA-Z]", new MatchEvaluator(DecodeEvaluator));
        }
    }

    public class WebUtils
    {
        public static readonly int TimeoutInMillis = 40000;

        public static string EncodeUrl(string text)
        {
            return UrlHelper.Encode(text);
        }

        public static string DownloadText(string url, bool appendUserAgent)
        {
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.ContentType = "text/plain";

            if (appendUserAgent)
            {
                request.UserAgent = "PureMp3";
            }

            request.Method = "GET";
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = TimeoutInMillis;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                return ReadToEnd(response);
            }
        }
        public static string DownloadText(string url, string content)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "text/plain";
            request.Method = "POST";
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = TimeoutInMillis;

            using (Stream newStream = request.GetRequestStream())
            {
                byte[] data = Encoding.UTF8.GetBytes(content);
                newStream.Write(data, 0, data.Length);
            }

            using (WebResponse response = request.GetResponse())
            {
                return ReadToEnd(response);
            }
        }
        public static XmlDocument DownloadXml(string url)
        {
            string response = null;

            try
            {
                response = WebUtils.DownloadText(url, true);
            }
            catch (WebException ex)
            {
                if (Object.ReferenceEquals(ex.Response, null)
                    || (ex.Response as HttpWebResponse).StatusCode != HttpStatusCode.NotFound)
                {
                    throw ex;
                }
            }
            
            return XmlUtils.StringToXml(response);
        }
        public static byte[] DownloadBinary(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "text/plain";
            request.Method = "GET";
            request.Timeout = TimeoutInMillis;

            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (MemoryStream mem = new MemoryStream())
                    {
                        int value = -1;
                        while ((value = stream.ReadByte()) != -1)
                        {
                            mem.WriteByte((byte)value);
                        }

                        byte[] result = new byte[mem.Length];
                        Array.Copy(mem.GetBuffer(), result, result.Length);

                        return result;
                    }
                }
            }
        }

        private static string ReadToEnd(WebResponse response)
        {
            StringBuilder result = new StringBuilder();

            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    string line = null;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (result.Length > 0)
                        {
                            result.Append("\r\n");
                        }

                        result.Append(line);
                    }
                }
            }

            return result.ToString();
        }
    }

    public class TestWebUtils
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestWebUtils));
        }

        public static void TestEncodeUrl()
        {
            string expected = "h%23llo+w%23rld";
            string actual = WebUtils.EncodeUrl("h#llo w#rld");
            UnitTest.Test(expected == actual);
        }
    }
}
