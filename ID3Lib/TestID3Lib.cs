using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreUtils;
using CoreTest;
using CoreVirtualDrive;

namespace ID3
{
    public class TestID3Lib
    {
        public static void Run()
        {
            ID3.IO.TestID3IO.Tests();
            ID3.Codec.TestTextEncoders.Tests();
            ID3.TestVersion.Tests();
            ID3.TestTagDescription.Tests();
            ID3.TestTagUtils.Tests();
            ID3.Utils.TestID3FileNameUtils.Tests();
            ID3.Codec.TestFrameContentCodecs.Tests();
            ID3.Codec.TestTagCodecs.Tests();
            ID3.Codec.TestFrameCodecs.Tests();
            ID3.Processor.TestDirectoryProcessor.Tests();
            ID3.Processor.TestAlbumExplorerProcessor.Tests();
            ID3.Processor.TestTextProcessor.Tests();
            ID3.Processor.TestTagProcessorDropFrames.Tests();
            ID3.Processor.TestTagVersionProcessor.Tests();
            ID3.Processor.TestTagProcessor.Tests();
            ID3.Processor.TestTagProcessorTrackNumber.Tests();
            ID3.Processor.TestUndoFile.Tests();
            ID3.Processor.TestFileProcessor.Tests();
            ID3.Processor.TestFileCopyProcessor.Tests();
            ID3.Processor.TestFilenameToTagProcessor.Tests();
            ID3.Processor.TestAlbumTagToFilenameProcessor.Tests();
            ID3.Processor.TestAlbumTagToDirectoryProcessor.Tests();
            ID3.Processor.TestAlbumToLibraryProcessor.Tests();
        }
    }
}
