#region COPYRIGHT (c) 2004 by Brian Weeres
/* Copyright (c) 2004 by Brian Weeres
 * 
 * Email: bweeres@protegra.com; bweeres@hotmail.com
 * 
 * Permission to use, copy, modify, and distribute this software for any
 * purpose is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * If you modify it then please indicate so. 
 *
 * The software is provided "AS IS" and there are no warranties or implied warranties.
 * In no event shall Brian Weeres and/or Protegra Technology Group be liable for any special, 
 * direct, indirect, or consequential damages or any damages whatsoever resulting for any reason 
 * out of the use or performance of this software
 * 
 * The code was modified by Matthias Mayrock, 2010
 * 
 */
#endregion
using System;
using System.Text;
using System.Net;
using System.IO;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using CoreWeb;
using ID3WebQueryBase;

namespace ID3Freedb
{
	internal class FreedbAPI
	{
		internal enum ResponseCodes
		{
            // our own code
            CODE_INVALID                          = 0,   // Invalid code 

            // return codes
            CODE_200_ExactMatchFound              = 200, // Exact match 
            CODE_202_NoMatch                      = 202, // No match 
            CODE_210_OkOrMultipleExactMatches     = 210, // Okay // or in a query multiple exact matches
            CODE_211_InexactMatchFoundListFollows = 211, // InExact matches found - list follows
            CODE_401_NoSiteInformationAvailable   = 401, // sites: no site information available
			CODE_402_ServerError                  = 402, // Server Error
            CODE_403_DatabaseEntryIsCorrupt       = 403, // Database entry is corrupt
            CODE_409_NoHandshake                  = 409, // No Handshake
            CODE_500_InvalidCommandOrParameters   = 500, // Invalid command, invalid parameters, etc.
		}

        internal class Result<T>
        {
            public Result(ResponseCodes code)
            {
                Code = code;
            }
            public Result(string firstLine)
                : this(FreedbAPI.ParseResponseCode(firstLine))
            {
            }
            public Result(string firstLine, T value)
                : this(firstLine)
            {
                Value = value;
            }

            public ResponseCodes Code
            {
                get;
                set;
            }
            public T Value
            {
                get;
                set;
            }
        }

		public FreedbAPI()
		{
			MainSite = new Site(DEFAULT_FREEDB_ADDRESS, Site.Protocols.http, DEFAULT_ADDITIONAL_URL_INFO);
			ProtocolLevel = 6;
		}

		public int ProtocolLevel
		{
			get;
			private set;
		}
        public Site MainSite
        {
            get;
            private set;
        }
		public Site CurrentSite
		{
			get;
			set;
		}

        public void SetDefaultSiteAddress(string siteAddress)
        {
            if (!siteAddress.Contains("http") || !siteAddress.Contains("cgi"))
            {
                throw new Exception("Invalid Site Address specified");
            }

            MainSite.SiteAddress = siteAddress;
        }
        public Result<IEnumerable<Site>> GetSites()
		{
			IList<string> lines = Call(Commands.CMD_SITES, MainSite.GetUrl());

            Result<IEnumerable<Site>> result = new Result<IEnumerable<Site>>(lines[0], new List<Site>());
            
            if (result.Code == ResponseCodes.CODE_210_OkOrMultipleExactMatches)
            {
                lines.RemoveAt(0);
                foreach (String line in lines)
                {
                    (result.Value as List<Site>).Add(new Site(line));
                }
            }

            return result;
		}

        public Result<IEnumerable<Release>> Query(string query)
		{
			StringBuilder builder = new StringBuilder(FreedbAPI.Commands.CMD_QUERY);
			builder.Append("+");
			builder.Append(query);
			
			IList<string> lines = Call(builder.ToString());

            Result<IEnumerable<Release>> result =
                new Result<IEnumerable<Release>>(lines[0], new List<Release>());

            switch (result.Code)
			{
				case ResponseCodes.CODE_211_InexactMatchFoundListFollows:
				case ResponseCodes.CODE_210_OkOrMultipleExactMatches:
					lines.RemoveAt(0);
					foreach (string line in lines)
					{
                        (result.Value as IList<Release>).Add(Factory.CreateReleasePreviewFromResponse(line, true));
					}
                    break;

				case ResponseCodes.CODE_200_ExactMatchFound:
                    (result.Value as IList<Release>).Add(Factory.CreateReleasePreviewFromResponse(lines[0]));
                    break;
			}

            return result;
		}
        public Result<Release> Read(Release queryResult)
        {
            StringBuilder builder = new StringBuilder(FreedbAPI.Commands.CMD_READ);
            builder.Append("+");
            builder.Append(queryResult.Genre);
            builder.Append("+");
            builder.Append(String.Format("{0:x8}", UInt32.Parse(queryResult.Id)));

            IList<string> lines = Call(builder.ToString());

            Result<Release> result = new Result<Release>(lines[0]);
            if (result.Code == ResponseCodes.CODE_210_OkOrMultipleExactMatches)
            {
                lines.RemoveAt(0); // remove the 210
                result.Value = Factory.CreateReleaseFromResponse(lines);
            }

            return result;
        }

        public Result<IEnumerable<string>> GetCategories()
		{
			IList<string> lines = Call(FreedbAPI.Commands.CMD_CATEGORIES);

            Result<IEnumerable<string>> result =
                new Result<IEnumerable<string>>(lines[0], new List<string>());

            if (result.Code == ResponseCodes.CODE_210_OkOrMultipleExactMatches)
			{
                lines.RemoveAt(0);
                result.Value = lines;
			}

            return result;
		}

		private IList<string> Call(string command)
		{
			if (CurrentSite != null)
				return Call(command, CurrentSite.GetUrl());
			else
				return Call(command, MainSite.GetUrl());
		}
		private IList<string> Call(string commandIn, string url)
		{
            string response = WebUtils.DownloadText(url, BuildCommand(Commands.CMD + commandIn));

            if (String.IsNullOrEmpty(response))
            {
                throw new Exception("No results returned from request.");
            }

            List<string> lines = response.Split(
                new string[]{"\r\n"},
                StringSplitOptions.RemoveEmptyEntries).ToList();

            if (lines[lines.Count - 1] == Commands.CMD_TERMINATOR)
            {
                return lines.GetRange(0, lines.Count - 1);
            }
            else
            {
                return lines;
            }
		}

		private string BuildCommand(string command)
		{
			StringBuilder builder = new StringBuilder(command);
			builder.Append("&");
			BuildCommandHello(builder);
			builder.Append("&");
			BuildCommandProtocol(builder);
			return builder.ToString();
		}
        private void BuildCommandHello(StringBuilder builder)
		{
            string clientName = "ID3Freedb";
            string hostname = "http://www.mroc.de/puremp3";
            string userEmail = "abc@def.com";
            string version = "1.5";

			builder.Append(Commands.CMD_HELLO);
			builder.Append("=");
			builder.Append(userEmail);
			builder.Append("+");
            builder.Append(hostname);
			builder.Append("+");
            builder.Append(clientName);
			builder.Append("+");
            builder.Append(version);
		}
        private void BuildCommandProtocol(StringBuilder builder)
		{
            builder.Append(Commands.CMD_PROTO);
			builder.Append("=");
			builder.Append(ProtocolLevel);
		}

        private static FreedbAPI.ResponseCodes ParseResponseCode(string firstLine)
		{
			int index = firstLine.Trim().IndexOf(' ');

            if (index != -1)
            {
                try
                {
                    return (FreedbAPI.ResponseCodes)Int32.Parse(firstLine.Substring(0, index));
                }
                catch (Exception)
                {
                    throw new Exception("Unable to parse response code. Returned Data: " + firstLine);
                }
            }
            else
            {
                throw new Exception("Unable to parse response code. Returned Data: " + firstLine);
            }
		}

        private static readonly string DEFAULT_FREEDB_ADDRESS = "freedb.freedb.org";
        private static readonly string DEFAULT_ADDITIONAL_URL_INFO = "/~cddb/cddb.cgi";

        private class Commands
        {
            public static readonly string CMD_HELLO = "hello";
            public static readonly string CMD_READ = "cddb+read";
            public static readonly string CMD_QUERY = "cddb+query";
            public static readonly string CMD_SITES = "sites";
            public static readonly string CMD_PROTO = "proto";
            public static readonly string CMD_CATEGORIES = "cddb+lscat";
            public static readonly string CMD = "cmd="; // will never use without the equals so put it here
            public static readonly string CMD_TERMINATOR = ".";
        }
	}
}
