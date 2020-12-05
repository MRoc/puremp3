using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using ID3MediaFileHeader;

namespace ID3Freedb
{
    public class TestID3Freedb
    {
        public static UnitTest Tests()
        {
            return new UnitTest(new Type[]
            {
                typeof(TestMultipleItemChooser),
                typeof(TestMP3Header),
                typeof(TestFactory_CreateReleasePreviewFromResponse),
                typeof(TestSite)
            });
        }
    }
}
