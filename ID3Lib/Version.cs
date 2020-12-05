using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ID3.Utils;
using CoreTest;

namespace ID3
{
    public class Version : System.IEquatable<Version>, System.IComparable<Version>
    {
        public static readonly Version v1_0 = new Version(1, 0, "1.0"); // 1.0
        public static readonly Version v2_0 = new Version(2, 0, "2.0"); // 2.0
        public static readonly Version v2_3 = new Version(3, 0, "2.3"); // 2.3
        public static readonly Version v2_4 = new Version(4, 0, "2.4"); // 2.4

        public static readonly Version[] vs1_0 = new Version[] { v1_0 };
        public static readonly Version[] vs2_0 = new Version[] { v2_0 };
        public static readonly Version[] vs2_3 = new Version[] { v2_3 };
        public static readonly Version[] vs2_0And2_3 = new Version[] { v2_0, v2_3 };
        public static readonly Version[] vs2_4 = new Version[] { v2_4 };
        public static readonly Version[] vs2_0And2_3And2_4 = new Version[] { v2_0, v2_3, v2_4 };
        public static readonly Version[] versions = new Version[] { v1_0, v2_0, v2_3, v2_4 };

        private Version(int major, int minor, string text)
        {
            Major = major;
            Minor = minor;
            Text = text;
        }

        public int Major { get; private set; }
        public int Minor { get; private set; }
        public string Text { get; private set; }

        public bool Equals(Version other)
        {
            return Major == other.Major && Minor == other.Minor;
        }
        public int CompareTo(Version other)
        {
            return Compare(this, other);
        }
        public override string ToString()
        {
            return Text;
        }

        public static Version[] Versions
        {
            get
            {
                return versions;
            }
        }
        public static Version PreviousVersion(Version v)
        {
            int indexOfVersion = IndexOfVersion(v);

            if (indexOfVersion > 0)
            {
                return Versions[indexOfVersion - 1];
            }
            else
            {
                return null;
            }
        }
        public static int IndexOfVersion(Version version)
        {
            return Array.FindIndex(Versions, delegate(Version v)
            {
                return version.Equals(v);
            });
        }
        public static Version VersionByMajorMinor(int major, int minor)
        {
            foreach (Version version in Versions)
            {
                if (version.Major == major && version.Minor == minor)
                {
                    return version;
                }
            }

            throw new Exception("Version not supported");
        }

        // a == b -> 0. a <  b -> negative. a >  b -> positive
        public static int Compare(Version a, Version b)
        {
            if (a.Minor >= 256 || b.Minor >= 256 || a.Major >= 256 || b.Major >= 256)
            {
                throw new Exception("Can't compare Version: major/minor exceeds maximum");
            }

            int vA = (a.Major << 8) + (a.Minor);
            int vB = (b.Major << 8) + (b.Minor);

            return vA - vB;
        }
        public static Version Max(Version a, Version b)
        {
            int c = Compare(a, b);

            if (c > 0)
            {
                return a;
            }
            else
            {
                return b;
            }
        }
        // builds a list in of versions from src to dst
        public static IEnumerable<Version> BuildPath(Version src, Version dst)
        {
            int indexSrc = IndexOfVersion(src);
            int indexDst = IndexOfVersion(dst);

            for (int i = indexSrc; i != indexDst; i += (indexSrc <= indexDst ? 1 : -1))
            {
                yield return Versions[i];
            }
            yield return dst;
        }
    }
    public interface IVersionable
    {
        Version[] SupportedVersions { get; }
    }
    public static class Extensions
    {
        public static bool IsSupported(this IVersionable v0, Version v)
        {
            return v0.SupportedVersions.Contains(v);
        }
    }

    public class TestVersion
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestVersion));
        }

        private static void TestVersionEqual()
        {
            UnitTest.Test(ID3.Version.v2_0.Equals(ID3.Version.v2_0));

            UnitTest.Test(!ID3.Version.v1_0.Equals(ID3.Version.v2_0));
            UnitTest.Test(!ID3.Version.v2_0.Equals(ID3.Version.v2_3));
            UnitTest.Test(!ID3.Version.v2_3.Equals(ID3.Version.v2_4));
        }
        private static void TestVersions()
        {
            UnitTest.Test(ID3.Version.Versions.Length == 4);
            UnitTest.Test(ID3.Version.Versions[0].Equals(ID3.Version.v1_0));
            UnitTest.Test(ID3.Version.Versions[1].Equals(ID3.Version.v2_0));
            UnitTest.Test(ID3.Version.Versions[2].Equals(ID3.Version.v2_3));
            UnitTest.Test(ID3.Version.Versions[3].Equals(ID3.Version.v2_4));

            UnitTest.Test(!ID3.Version.Versions[1].Equals(ID3.Version.v1_0));
            UnitTest.Test(!ID3.Version.Versions[2].Equals(ID3.Version.v2_0));
            UnitTest.Test(!ID3.Version.Versions[3].Equals(ID3.Version.v2_3));
        }
        private static void TestVersionFindIndex()
        {
            UnitTest.Test(ID3.Version.IndexOfVersion(ID3.Version.v1_0) == 0);
            UnitTest.Test(ID3.Version.IndexOfVersion(ID3.Version.v2_0) == 1);
            UnitTest.Test(ID3.Version.IndexOfVersion(ID3.Version.v2_3) == 2);
            UnitTest.Test(ID3.Version.IndexOfVersion(ID3.Version.v2_4) == 3);
        }
        private static void TestVersionPreviousVersion()
        {
            UnitTest.Test(ID3.Version.PreviousVersion(ID3.Version.v1_0) == null);
            UnitTest.Test(ID3.Version.PreviousVersion(ID3.Version.v2_0).Equals(ID3.Version.v1_0));
            UnitTest.Test(ID3.Version.PreviousVersion(ID3.Version.v2_3).Equals(ID3.Version.v2_0));
            UnitTest.Test(ID3.Version.PreviousVersion(ID3.Version.v2_4).Equals(ID3.Version.v2_3));
        }
        private static void TestVersionComparison()
        {
            UnitTest.Test(ID3.Version.Compare(ID3.Version.v2_0, ID3.Version.v2_0) == 0);
            UnitTest.Test(ID3.Version.Compare(ID3.Version.v2_0, ID3.Version.v2_4) < 0);
            UnitTest.Test(ID3.Version.Compare(ID3.Version.v2_3, ID3.Version.v1_0) > 0);
        }
        private static void TestVersionMax()
        {
            UnitTest.Test(ID3.Version.Max(ID3.Version.v2_0, ID3.Version.v2_0) == ID3.Version.v2_0);
            UnitTest.Test(ID3.Version.Max(ID3.Version.v2_3, ID3.Version.v2_0) == ID3.Version.v2_3);
            UnitTest.Test(ID3.Version.Max(ID3.Version.v2_0, ID3.Version.v2_3) == ID3.Version.v2_3);
        }
        private static void TestVersionConversionPath()
        {
            ID3.Version[] conversionPath = ID3.Version.BuildPath(
                ID3.Version.v2_0, ID3.Version.v2_0).ToArray();
            UnitTest.Test(conversionPath.Length == 1);
            UnitTest.Test(conversionPath[0] == ID3.Version.v2_0);

            conversionPath = ID3.Version.BuildPath(
                ID3.Version.v1_0, ID3.Version.v2_4).ToArray();
            UnitTest.Test(conversionPath.Length == 4);
            UnitTest.Test(conversionPath[0] == ID3.Version.v1_0);
            UnitTest.Test(conversionPath[1] == ID3.Version.v2_0);
            UnitTest.Test(conversionPath[2] == ID3.Version.v2_3);
            UnitTest.Test(conversionPath[3] == ID3.Version.v2_4);

            conversionPath = ID3.Version.BuildPath(
                ID3.Version.v2_4, ID3.Version.v1_0).ToArray();
            UnitTest.Test(conversionPath.Length == 4);
            UnitTest.Test(conversionPath[0] == ID3.Version.v2_4);
            UnitTest.Test(conversionPath[1] == ID3.Version.v2_3);
            UnitTest.Test(conversionPath[2] == ID3.Version.v2_0);
            UnitTest.Test(conversionPath[3] == ID3.Version.v1_0);

            conversionPath = ID3.Version.BuildPath(
                ID3.Version.v2_0, ID3.Version.v2_3).ToArray();
            UnitTest.Test(conversionPath.Length == 2);
            UnitTest.Test(conversionPath[0] == ID3.Version.v2_0);
            UnitTest.Test(conversionPath[1] == ID3.Version.v2_3);

            conversionPath = ID3.Version.BuildPath(
                ID3.Version.v2_3, ID3.Version.v2_0).ToArray();
            UnitTest.Test(conversionPath.Length == 2);
            UnitTest.Test(conversionPath[0] == ID3.Version.v2_3);
            UnitTest.Test(conversionPath[1] == ID3.Version.v2_0);
        }
    }
}
