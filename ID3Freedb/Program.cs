using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ID3Freedb;
using ID3WebQueryBase;
using ID3MediaFileHeader;

namespace ID3Freedb
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading offsets from mp3s--------------------------------------------------");
            IEnumerable<int> discOffsets = DiscID.MakeOffsets(MP3Tools.LoadFileLenthFromMp3s(
                args[0],
                delegate(FileInfo f) { return (int)f.Length; },
                delegate(FileInfo f) { return 0; }));

            int counter = 0;
            foreach (int offset in discOffsets)
            {
                Console.WriteLine("Marker {0}: Offset: {1}", counter, offset);
                counter++;
            }

            Console.WriteLine("Build freedb query---------------------------------------------------------");
            string query = DiscID.FreedbQuery(DiscID.DiscId(discOffsets), discOffsets);
            Console.WriteLine(query);

            FreedbAPI freedb = new FreedbAPI();

            Console.WriteLine("Get freedb sites-----------------------------------------------------------");
            FreedbAPI.Result<IEnumerable<Site>> sites = freedb.GetSites();
            foreach (var item in sites.Value)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Get freedb categories------------------------------------------------------");
            FreedbAPI.Result<IEnumerable<string>> categories = freedb.GetCategories();
            foreach (var item in categories.Value)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Query----------------------------------------------------------------------");
            FreedbAPI.Result<IEnumerable<Release>> result = freedb.Query(query);
            if (result.Value.Count() > 0)
            {
                foreach (var item in result.Value)
                {
                    Console.WriteLine(item);
                }
            }
            else
            {
                Console.WriteLine("Query unsuccessful: " + result.Code);
                return;
            }

            Console.WriteLine("Read first entry-----------------------------------------------------------");
            FreedbAPI.Result<Release> disc = freedb.Read(result.Value.First());
            if (disc.Value != null)
            {
                Console.WriteLine(disc.Value);
            }
            else
            {
                Console.WriteLine("Unable to retrieve cd entry. Code: " + disc.Code);
            }
        }
    }
}
