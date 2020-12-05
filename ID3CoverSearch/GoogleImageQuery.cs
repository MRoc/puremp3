using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using CoreLogging;
using CoreWeb;
using CoreTest;
using CoreUtils;

namespace ID3CoverSearch
{
    public class GoogleImageQuery
    {
        public static readonly int MaxImageSize = 800 * 1024;
        public static readonly int MinImageSize = 6 * 1024;
        public static readonly int QueryRetries = 2;
        public static readonly int DownloadRetries = 5;

        public class ImageResult
        {
            public ImageResult()
            {
            }
            public ImageResult(byte[] image, string url)
            {
                Image = image;
                Url = url;
                Succeeded = true;
            }
            public byte[] Image
            {
                get;
                private set;
            }
            public string Url
            {
                get;
                private set;
            }
            public bool Succeeded
            {
                get;
                private set;
            }
        }

        public ImageResult Query(string query)
        {
            if (!cache.ContainsKey(query))
            {
                ImageResult result = new ImageResult();

                string googleQuery = BuildGoogleQuery(query);
                LoggerWriter.WriteStep(Tokens.Info, "Google query", googleQuery);

                string[] imageUrls = QueryGoogle(googleQuery).ToArray();

                for (int i = 0; i < Math.Min(imageUrls.Length, DownloadRetries); i++)
                {
                    LoggerWriter.WriteStepIndent(Tokens.Info, "\"" + imageUrls[i] + "\"");
                }

                for (int i = 0
                    ; i < Math.Min(imageUrls.Length, DownloadRetries) && !result.Succeeded
                    ; i++)
                {
                    string url = imageUrls[i];
                    LoggerWriter.WriteStep(Tokens.Info, "Download", url);

                    try
                    {
                        byte[] image = WebUtils.DownloadBinary(url);

                        if (!Object.ReferenceEquals(image, null)
                            && image.Length >= MinImageSize
                            && image.Length <= MaxImageSize)
                        {
                            result = new ImageResult(image, url);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine(Tokens.Exception, ex);
                    }
                }

                cache[query] = result;
            }

            if (cache.ContainsKey(query) && cache[query].Succeeded)
            {
                return cache[query];
            }
            else
            {
                return null;
            }
        }

        private string BuildGoogleQuery(string query)
        {
            StringBuilder result = new StringBuilder();

            result.Append("http://www.google.com/images?q=");

            result.Append(WebUtils.EncodeUrl(
                query
                .Replace(',', ' ')
                .Replace('!', ' ')
                .Replace('_', ' ')
                .Replace('-', ' ')
                ));

            result.Append("&um=1");

            return result.ToString();
        }
        private IEnumerable<string> QueryGoogle(string url)
        {
            string result = null;

            for (int i = 0; i < QueryRetries && String.IsNullOrEmpty(result); i++)
            {
                try
                {
                    result = WebUtils.DownloadText(url, false);
                }
                catch (WebException exception)
                {
                    if (exception.Status != WebExceptionStatus.Timeout)
                    {
                        throw exception;
                    }

                    LoggerWriter.WriteStep(Tokens.Info, "Retry", url);
                }
            }

            if (!String.IsNullOrEmpty(result) && result.Contains("dyn.setResults(["))
            {
                return ParseGoogleResponse(result);
            }
            else
            {
                return new List<string>();
            }
        }
        private IEnumerable<string> ParseGoogleResponse(string line)
        {
            int index = line.IndexOf("dyn.setResults([") + 15;
            string substring = line.Substring(index, line.Length - index);

            List<string> imageUrls = new List<string>();

            foreach (var item in Parser.Parse(substring))
            {
                List<object> curEntry = item as List<object>;

                try
                {
                    string imageUrl = ExtractUrl(curEntry[0].ToString());
                    int width = Int32.Parse(curEntry[4].ToString());
                    int height = Int32.Parse(curEntry[5].ToString());
                    double ratio = Math.Abs((double)width / (double)height);

                    if ((imageUrl.ToLower().EndsWith(".jpg") || imageUrl.ToLower().EndsWith(".png"))
                        && ratio > 0.95 && ratio < 1.05)
                    {
                        imageUrls.Add(imageUrl);
                    }
                }
                catch (Exception)
                {
                }
            }

            return imageUrls;
        }
        public static string ExtractUrl(string text)
        {
            string marker0 = @"/imgres?imgurlx3d";
            string marker1 = @"\x26";
            string marker1b = @"x26";

            if (text.StartsWith(marker0))
            {
                int index0 = marker0.Length;

                int index1 = text.IndexOf(marker1);

                if (index1 == -1)
                {
                    index1 = text.IndexOf(marker1b);

                    if (index1 == -1)
                    {
                        throw new Exception("Seems the format has changed");
                    }
                }

                return text.Substring(index0, index1 - index0);
            }
            else
            {
                return String.Empty;
            }
        }

        private Dictionary<string, ImageResult> cache = new Dictionary<string, ImageResult>();
    }
}
