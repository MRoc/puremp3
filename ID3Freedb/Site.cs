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
using System.Diagnostics;
using System.Text;
using CoreUtils;

namespace ID3Freedb
{
    internal class Site
	{
        internal enum Protocols
        {
            http,
            cddbp,
            all
        }

		public Site(string siteFromCDDB)
		{
            Parse(siteFromCDDB);
		}
		public Site(string siteAddress, Protocols protocol, string additionAddressInfo)
		{
			SiteAddress = siteAddress;
			Protocol = protocol;
			AdditionalAddressInfo = additionAddressInfo;
		}

        public string AdditionalAddressInfo
        {
            get;
            set;
        }
        public string SiteAddress
        {
            get;
            set;
        }
        public Protocols Protocol
        {
            get;
            set;
        }
        public string Port
        {
            get;
            set;
        }
        public string Description
        {
            get;
            set;
        }
        public string Latitude
        {
            get;
            set;
        }
        public string Longitude
        {
            get;
            set;
        }

		public void Parse(string siteAsString)
		{
            siteAsString = siteAsString.Trim();

            SiteAddress = StringSplitOff.SplitOffBySpace(ref siteAsString);
            Protocol = (Protocols) Enum.Parse(typeof(Protocols), StringSplitOff.SplitOffBySpace(ref siteAsString));
            Port = StringSplitOff.SplitOffBySpace(ref siteAsString);
            AdditionalAddressInfo = StringSplitOff.SplitOffBySpace(ref siteAsString);
            Latitude = StringSplitOff.SplitOffBySpace(ref siteAsString);
            Longitude = StringSplitOff.SplitOffBySpace(ref siteAsString);
            Description = siteAsString.Trim();
		}
		public string GetUrl()
		{
            if (Protocol == Site.Protocols.http)
            {
                return "http://" + SiteAddress + AdditionalAddressInfo;
            }
            else
            {
                return SiteAddress;
            }
		}

		public override string ToString()
		{
            StringBuilder result = new StringBuilder();

            result.Append(SiteAddress);
            result.Append(' ');
            result.Append(Protocol);
            result.Append(' ');
            result.Append(Port);
            result.Append(' ');
            result.Append(AdditionalAddressInfo);
            result.Append(' ');
            result.Append(Latitude);
            result.Append(' ');
            result.Append(Longitude);
            result.Append(' ');
            result.Append(Description);

            return result.ToString();
		}
	}

    public class TestSite
    {
        public static void TestSite_cddbp()
        {
            string site0Text = "freedb.freedb.org cddbp 8880 - N000.00 W000.00 Random freedb server";
            Site site0 = new Site(site0Text);
            Debug.Assert(site0.SiteAddress == "freedb.freedb.org");
            Debug.Assert(site0.Protocol.ToString() == "cddbp");
            Debug.Assert(site0.Port == "8880");
            Debug.Assert(site0.AdditionalAddressInfo == "-");
            Debug.Assert(site0.Latitude == "N000.00");
            Debug.Assert(site0.Longitude == "W000.00");
            Debug.Assert(site0.Description == "Random freedb server");
            Debug.Assert(site0.ToString() == site0Text);
        }
        public static void TestSite_http()
        {
            string site1Text = "freedb.freedb.org http 80 /~cddb/cddb.cgi N000.00 W000.00 Random freedb server";
            Site site1 = new Site(site1Text);
            Debug.Assert(site1.SiteAddress == "freedb.freedb.org");
            Debug.Assert(site1.Protocol.ToString() == "http");
            Debug.Assert(site1.Port == "80");
            Debug.Assert(site1.AdditionalAddressInfo == "/~cddb/cddb.cgi");
            Debug.Assert(site1.Latitude == "N000.00");
            Debug.Assert(site1.Longitude == "W000.00");
            Debug.Assert(site1.Description == "Random freedb server");
            Debug.Assert(site1.ToString() == site1Text);
        }
    }
}
