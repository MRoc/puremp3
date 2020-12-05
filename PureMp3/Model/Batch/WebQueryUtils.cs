using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3;
using ID3Lib;
using System.IO;
using ID3WebQueryBase;
using CoreVirtualDrive;
using ID3.Utils;

namespace PureMp3.Model.Batch
{
    public class WebQueryUtils
    {
        public static IEnumerable<KeyValuePair<FileInfo, IDictionary<FrameMeaning, object>>> CreateObjects(
            string dir,
            Release release,
            TrackNumberGenerator trackNumberGenerator)
        {
            string[] files = VirtualDrive.GetFiles(dir, "*.mp3");

            for (int i = 0; i < files.Length; i++)
            {
                yield return new KeyValuePair<FileInfo, IDictionary<FrameMeaning, object>>(
                    new FileInfo(files[i]),
                    CreateObjects(release, i, trackNumberGenerator));
            }
        }

        private static IDictionary<FrameMeaning, object> CreateObjects(
            ID3WebQueryBase.Release release,
            int index,
            TrackNumberGenerator trackNumberGenerator)
        {
            IDictionary<FrameMeaning, object> result = new Dictionary<FrameMeaning, object>();

            result[FrameMeaning.Artist] = release.Artist;
            result[FrameMeaning.Album] = release.Title;
            result[FrameMeaning.TrackNumber] = trackNumberGenerator.ApplyPattern(index + 1, release.Tracks.Count());
            result[FrameMeaning.Title] = release.Tracks.ElementAt(index).Title;
            result[FrameMeaning.ReleaseYear] = release.Year;

            if (release.CoverArt != null)
            {
                result[FrameMeaning.Picture] = release.CoverArt;
            }

            return result;
        }
    }
}
