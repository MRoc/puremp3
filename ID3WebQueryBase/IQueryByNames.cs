using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ID3WebQueryBase
{
    public interface IQueryByNames
    {
        Release QueryRelease(string artist, string album, int numTracks);
    }
}
