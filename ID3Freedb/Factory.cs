using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3WebQueryBase;
using CoreUtils;
using System.Globalization;
using System.Diagnostics;

namespace ID3Freedb
{
    internal class Factory
    {
        public static Release CreateReleaseFromResponse(IEnumerable<string> data)
        {
            Release release = new Release();
            List<Track> tracks = new List<Track>();

            release.Tracks = tracks;

			foreach (string line in data)
			{
				// check for comment
				if (line.Length == 0 || line[0] == '#')
					continue;

				int index = line.IndexOf('=');

                // couldn't find equal sign have no clue what the data is
				if (index == -1)
					continue;

				string field = line.Substring(0, index);

                // move it past the equal sign
				index++;

				switch (field)
				{
					case "DISCID":
                        release.Id = line.Substring(index);
						continue;

					case "DTITLE":
                        release.Artist += line.Substring(index);
						continue;

					case "DYEAR":
                        release.Year = line.Substring(index);
						continue;

					case "DGENRE":
                        release.Genre += line.Substring(index);
						continue;

					case "EXTD":
                        release.AdditionalData += line.Substring(index);
						continue;

					case "PLAYORDER":
                        //release.PlayOrder += line.Substring(index);
						continue;
					
					default:
						if (field.StartsWith("TTITLE"))
						{
                            int trackNumber = int.Parse(field.Substring("TTITLE".Length));
                            string title = line.Substring(index);

                            if (trackNumber < tracks.Count)
                            {
                                tracks[trackNumber].Title += title;
                            }
                            else
                            {
                                tracks.Add(new Track(title));
                            }
						}
						else if (field.StartsWith("EXTT"))
						{
							int trackNumber = int.Parse(field.Substring("EXTT".Length));
                            string extendedData = line.Substring(index);

                            if (trackNumber < tracks.Count)
                            {
                                tracks[trackNumber].AdditionalData += extendedData;
                            }
                            else
                            {
                                tracks.Add(new Track("", extendedData));
                            }
						}
						continue;
				}
			}

			// split the title and artist from DTITLE; see if we have a slash
            if (release.Artist.IndexOf(" / ") != -1)
			{
                string titleArtist = release.Artist;
                release.Artist = StringSplitOff.SplitOffByDivide(ref titleArtist);
                release.Title = titleArtist.Trim();
			}

            return release;
		}

        public static Release CreateReleasePreviewFromResponse(string queryResult)
        {
            return ParseRelease(queryResult, true);
        }
        public static Release CreateReleasePreviewFromResponse(string queryResult, bool multiMatchInput)
        {
            return ParseRelease(queryResult, !multiMatchInput);
        }

		private static Release ParseRelease(string text, bool exactMatch)
		{
            if (exactMatch)
            {
                string responseCode = StringSplitOff.SplitOffBySpace(ref text);
            }

            Release release = new Release();

            release.Genre = StringSplitOff.SplitOffBySpace(ref text);
            release.Id = UInt32.Parse(StringSplitOff.SplitOffBySpace(ref text), NumberStyles.HexNumber).ToString();
            release.Artist = StringSplitOff.SplitOffByDivide(ref text);
            release.Title = text;

            return release;
		}
    }

    public class TestFactory_CreateReleasePreviewFromResponse
    {
        public static void TestQueryResult_ExactMatch()
        {
            string resultExactMatch = "code categ 00123abc artist bla / title bla";
            Release resultExact = Factory.CreateReleasePreviewFromResponse(resultExactMatch, false);
            Debug.Assert(resultExact.Artist == "artist bla");
            Debug.Assert(resultExact.Genre == "categ");
            Debug.Assert(resultExact.Id == "1194684");
            Debug.Assert(resultExact.Title == "title bla");
        }
        public static void TestQueryResult_InexactMatch()
        {
            string resultInexactMatch = "categ 00123abc artist bla / title bla";
            Release resultInexact = Factory.CreateReleasePreviewFromResponse(resultInexactMatch, true);
            Debug.Assert(resultInexact.Artist == "artist bla");
            Debug.Assert(resultInexact.Genre == "categ");
            Debug.Assert(resultInexact.Id == "1194684");
            Debug.Assert(resultInexact.Title == "title bla");
        }
    }
}
