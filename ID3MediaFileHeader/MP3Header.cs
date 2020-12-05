// This code is taken originally from http://www.devhood.com/tutorials/tutorial_details.aspx?tutorial_id=79
// and was converted by Robert Wlodarczyk from the original c++ sources writte by Gustav Munkby.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CoreVirtualDrive;

namespace ID3MediaFileHeader
{
    public class MP3Header
    {
        public static int ReadBitrate(Stream stream, long dataLength)
        {
            MP3Header h = new MP3Header();
            h.Read(stream, dataLength);

            if (h.IsValidHeader())
            {
                return h.BitRate;
            }
            else
            {
                return -1;
            }
        }

        public MP3Header()
        {
            Header = new RawHeader();
        }

        public long FileSize
        {
            get;
            private set;
        }

        public int BitRate
        {
            get
            {
                return getBitrate();
            }
        }
        public int Frequency
        {
            get
            {
                return getFrequency();
            }
        }
        public enum Modes
        {
            Stereo,
            JointStereo,
            DualChannel,
            SingleChannel
        }
        public Modes Mode
        {
            get
            {
                return (Modes)Header.getModeIndex();
            }
        }
        public double LengthInSeconds
        {
            get
            {
                double fileSizeInKiloBits = FileSize * 8.0 / 1000.0;
                return fileSizeInKiloBits / (double)getBitrate();
            }
        }
        public bool HasVBR
        {
            get;
            private set;
        }

        public bool Read(Stream stream, long dataLength)
        {
            FileSize = dataLength;

            byte[] bytHeader = new byte[4];

            // Keep reading 4 bytes from the header until we know for sure that in 
            // fact it's an MP3
            int value = 0;
            do
            {
                value = stream.ReadByte();
                bytHeader[0] = bytHeader[1];
                bytHeader[1] = bytHeader[2];
                bytHeader[2] = bytHeader[3];
                bytHeader[3] = (byte)value;

                LoadMP3Header(bytHeader);
            }
            while (!Header.IsValidHeader() && value != -1);

            // If we did not reach end of stream, check for more
            if (value != -1)
            {
                if (Header.getVersionIndex() == 3)     // MPEG Version 1
                {
                    if (Mode == Modes.SingleChannel)
                    {
                        stream.Seek(17, SeekOrigin.Current);
                    }
                    else
                    {
                        stream.Seek(32, SeekOrigin.Current);
                    }
                }
                else                            // MPEG Version 2.0 or 2.5
                {
                    if (Mode == Modes.SingleChannel)
                    {
                        stream.Seek(9, SeekOrigin.Current);
                    }
                    else
                    {
                        stream.Seek(17, SeekOrigin.Current);
                    }
                }

                // Check to see if the MP3 has a variable bitrate
                byte[] bytVBitRate = new byte[12];
                stream.Read(bytVBitRate, 0, 12);
                HasVBR = LoadVBRHeader(bytVBitRate);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsValidHeader()
        {
            return Header.IsValidHeader();
        }

        private void LoadMP3Header(byte[] c)
        {
            Header.Data = (uint)
                ( ((c[0] & 255) << 24)
                | ((c[1] & 255) << 16)
                | ((c[2] & 255) << 8)
                | ((c[3] & 255) << 0)
                );
        }
        private bool LoadVBRHeader(byte[] inputheader)
        {
            // If it's a variable bitrate MP3, the first 4 bytes will read 'Xing'
            // since they're the ones who added variable bitrate-edness to MP3s
            if (inputheader[0] == 88  && inputheader[1] == 105 &&
                inputheader[2] == 110 && inputheader[3] == 103)
            {
                int flags = (int)
                    ( ((inputheader[4] & 255) << 24)
                    | ((inputheader[5] & 255) << 16)
                    | ((inputheader[6] & 255) << 8 )
                    | ((inputheader[7] & 255  << 0 )));

                if ((flags & 0x0001) == 1)
                {
                    NumFramesVBR = (int)
                        ( ((inputheader[8]  & 255) << 24)
                        | ((inputheader[9]  & 255) << 16)
                        | ((inputheader[10] & 255) << 8 )
                        | ((inputheader[11] & 255) << 0 ));

                    return true;
                }
                else
                {
                    NumFramesVBR = -1;

                    return true;
                }
            }
            return false;
        }

        private int getBitrate()
        {
            // If the file has a variable bitrate, then we return an integer average bitrate,
            // otherwise, we use a lookup table to return the bitrate
            if (HasVBR)
            {
                double medFrameSize = (double)FileSize / (double)NumFrames();
                return (int)((medFrameSize * (double)getFrequency()) / (1000.0 * (Header.getLayerIndex() == 3 ? 12.0 : 144.0)));
            }
            else
            {
                int[, ,] table =
                {
                    { // MPEG 2 & 2.5
                        {0,  8, 16, 24, 32, 40, 48, 56, 64, 80, 96,112,128,144,160,0}, // Layer III
                        {0,  8, 16, 24, 32, 40, 48, 56, 64, 80, 96,112,128,144,160,0}, // Layer II
                        {0, 32, 48, 56, 64, 80, 96,112,128,144,160,176,192,224,256,0}  // Layer I
                    },
                    { // MPEG 1
                        {0, 32, 40, 48, 56, 64, 80, 96,112,128,160,192,224,256,320,0}, // Layer III
                        {0, 32, 48, 56, 64, 80, 96,112,128,160,192,224,256,320,384,0}, // Layer II
                        {0, 32, 64, 96,128,160,192,224,256,288,320,352,384,416,448,0}  // Layer I
                    }
                };

                return table[Header.getVersionIndex() & 1, Header.getLayerIndex() - 1, Header.getBitrateIndex()];
            }
        }
        private int getFrequency()
        {
            int[,] table =
            {    
                {32000, 16000,  8000}, // MPEG 2.5
                {    0,     0,     0}, // reserved
                {22050, 24000, 16000}, // MPEG 2
                {44100, 48000, 32000}  // MPEG 1
            };

            return table[Header.getVersionIndex(), Header.getFrequencyIndex()];
        }
        private string getMode()
        {
            switch (Header.getModeIndex())
            {
                default:
                    return "Stereo";
                case 1:
                    return "Joint Stereo";
                case 2:
                    return "Dual Channel";
                case 3:
                    return "Single Channel";
            }
        }

        private int NumFrames()
        {
            if (HasVBR)
            {
                return NumFramesVBR;
            }
            else
            {
                double medFrameSize = (Header.getLayerIndex() == 3 ? 12.0 : 144.0) * 1000.0 * (double)getBitrate() / (double)getFrequency();
                return (int)(FileSize / medFrameSize);
            }
        }
        private int NumFramesVBR
        {
            get;
            set;
        }

        private RawHeader Header
        {
            get;
            set;
        }

        class RawHeader
        {
            public uint Data
            {
                get;
                set;
            }
            public bool IsValidHeader()
            {
                return ((getFrameSync() & 2047) == 2047) &&
                        ((getVersionIndex() & 3) != 1) &&
                        ((getLayerIndex() & 3) != 0) &&
                        ((getBitrateIndex() & 15) != 0) &&
                        ((getBitrateIndex() & 15) != 15) &&
                        ((getFrequencyIndex() & 3) != 3) &&
                        ((getEmphasisIndex() & 3) != 2);
            }

            public uint getFrameSync()
            {
                return (Data >> 21) & 0x7FF;
            }
            public uint getVersionIndex()
            {
                return (Data >> 19) & 0x3;
            }
            public uint getLayerIndex()
            {
                return (Data >> 17) & 0x3;
            }
            public uint getProtectionBit()
            {
                return (Data >> 16) & 0x1;
            }
            public uint getBitrateIndex()
            {
                return (Data >> 12) & 0xF;
            }
            public uint getFrequencyIndex()
            {
                return (Data >> 10) & 0x3;
            }
            public uint getPaddingBit()
            {
                return (Data >> 9) & 0x1;
            }
            public uint getpublicBit()
            {
                return (Data >> 8) & 0x1;
            }
            public uint getModeIndex()
            {
                return (Data >> 6) & 0x3;
            }
            public uint getModeExtIndex()
            {
                return (Data >> 4) & 0x3;
            }
            public uint getCoprightBit()
            {
                return (Data >> 3) & 0x1;
            }
            public uint getOrginalBit()
            {
                return (Data >> 2) & 0x1;
            }
            public uint getEmphasisIndex()
            {
                return (Data >> 0) & 0x3;
            }
            public double getVersion()
            {
                double[] table = { 2.5, 0.0, 2.0, 1.0 };
                return table[getVersionIndex()];
            }
            public int getLayer()
            {
                return (int)(4 - getLayerIndex());
            }
        }
    }

    public class TestMP3Header
    {
        static byte[] mp3FixedBitrate = 
        {
            0xFF, 0xFB, 0x90, 0x4C,
            0, 0, 0, 0, 0, 0x4B, 0x5, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        };

        static byte[] mp3VBR = 
        {
            0xFF, 0xFB, 0x90, 0x64,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x58, 0x69, 0x6e, 0x67, 0x0, 0x0, 0x0, 0xF, 0x0, 0x0, 0x7, 0x8d
        };

        public static void TestRead_FixedBitrate()
        {
            using (MemoryStream stream = new MemoryStream(mp3FixedBitrate))
            {
                MP3Header header = new MP3Header();
                header.Read(stream, 4341760);

                Debug.Assert(header.BitRate == 128);
                Debug.Assert(header.FileSize == 4341760);
                Debug.Assert(header.HasVBR == false);
                Debug.Assert(Math.Abs(header.LengthInSeconds - 271.36) < 0.01);
                Debug.Assert(header.Mode == MP3Header.Modes.JointStereo);
            }
        }
        public static void TestRead_VariableBitrate()
        {
            using (MemoryStream stream = new MemoryStream(mp3VBR))
            {
                MP3Header header = new MP3Header();
                header.Read(stream, 1171900);

                Debug.Assert(header.BitRate == 185);
                Debug.Assert(header.FileSize == 1171900);
                Debug.Assert(header.HasVBR == true);
                Debug.Assert(header.LengthInSeconds == 50.67675675675676);
                Debug.Assert(header.Mode == MP3Header.Modes.JointStereo);
            }
        }
    }

    public static class MP3Tools
    {
        public delegate long MpegDataSize(FileInfo file);
        public delegate int MpegSkipBytes(FileInfo file);
        public static int LoadFileLengthFromMp3(string file, MpegDataSize sizeProvider, MpegSkipBytes skipBytes)
        {
            using (Stream stream = VirtualDrive.OpenInStream(file))
            {
                MP3Header mp3hdr = new MP3Header();
                FileInfo fileInfo = new FileInfo(file);

                stream.Seek(skipBytes(fileInfo), SeekOrigin.Current);

                bool boolIsMP3 = mp3hdr.Read(stream, sizeProvider(fileInfo));
                if (boolIsMP3)
                {
                    return (int)Math.Round(mp3hdr.LengthInSeconds);
                }
                else
                {
                    return 0;
                }
            }
        }
        public static IEnumerable<int> LoadFileLenthFromMp3s(string dir, MpegDataSize sizeProvider, MpegSkipBytes skipBytes)
        {
            List<int> result = new List<int>();

            foreach (var file in VirtualDrive.GetFiles(dir, "*.mp3"))
            {
                int fileLengthInSecs = LoadFileLengthFromMp3(file, sizeProvider, skipBytes);
                if (fileLengthInSecs != -1)
                {
                    result.Add(fileLengthInSecs);
                }
            }

            return result;
        }
    }
}
