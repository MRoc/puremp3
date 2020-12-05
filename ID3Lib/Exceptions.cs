using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ID3.IO;

namespace ID3
{
    public class TagException : Exception
    {
        public TagException(string filename)
            : base(filename)
        { }
    }

    public class NoTagException : TagException
    {
        public NoTagException(string filename)
            : base(filename)
        { }
    }

    public class InvalidFrameException : TagException
    {
        public InvalidFrameException(string frameId)
            : base("Exception in unknown file.\n"
            + "   Invalid frame found \"" + frameId + "\".")
        {
            Filename = "Unknown";
            FrameID = frameId;
        }

        public InvalidFrameException(Reader reader, string frameId)
            : base("Exception in file \"" + reader.Filename + "\".\n"
            + "   Invalid frame found \"" + frameId + "\".\n"
            + "   Desynchronization was set to \"" + reader.Unsynchronization + "\".")
        {
            Filename = reader.Filename;
            FrameID = frameId;
        }

        public string Filename { get; private set; }
        public string FrameID { get; private set; }
    }

    public class InvalidHeaderFlagsException : TagException
    {
        public InvalidHeaderFlagsException(string message)
            : base(message)
        {
        }
    }

    public class InvalidVersionException : TagException
    {
        public InvalidVersionException(string filename, int versionMajor, int versionMinor)
            : base(filename)
        {
            VersionMajor = versionMajor;
            VersionMinor = versionMinor;
        }

        public int VersionMajor { get; private set; }
        public int VersionMinor { get; private set; }
    }

    public class TextCodecException : Exception
    {
        public TextCodecException(string errorMessage)
            : base(errorMessage)
        { }
    }

    public class CorruptFrameContentException : Exception
    {
        public enum Handling
        {
            // Do not handle
            Strict,

            // Handle, drop frame
            Drop,

            // Handle, Silently add the corrupt data
            Ignore,
        };

        public static Handling _handling = Handling.Drop;

        public CorruptFrameContentException(string errorMessage)
            : base(errorMessage)
        {}

        public void Handle()
        {
            if (_handling == Handling.Strict)
            {
                throw this;
            }
        }
    }

    // Thrown if a version error was found in existing objects
    public class VersionInvariant : Exception
    {
        public VersionInvariant(string errorMessage)
            : base(errorMessage)
        { }
    }
}
