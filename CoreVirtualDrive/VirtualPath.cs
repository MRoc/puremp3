using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;

namespace CoreVirtualDrive
{
    public class VirtualPath
    {
        public VirtualPath(IEnumerable<string> parts)
        {
            Parts = parts;
        }
        public VirtualPath(IEnumerable<string> root, IEnumerable<string> rest)
        {
            Parts = root.Concat(rest);
        }
        public VirtualPath(string id)
        {
            Parts = VirtualDrive.Split(id);
        }

        public IEnumerable<string> Parts { get; set; }
        public VirtualPath Parent
        {
            get
            {
                return new VirtualPath(Parts.Take(Parts.Count() - 1));
            }
        }
        public VirtualPath PartialPath(int level)
        {
            return new VirtualPath(Parts.Take(level));
        }
        public string Name
        {
            get
            {
                return Parts.Last();
            }
        }
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            int counter = 0;

            foreach (var part in Parts)
            {
                if (counter == 0)
                {
                    if (Parts.First().Length > 2)
                    {
                        result.Append(@"\\");
                    }
                }
                else
                {
                    result.Append(System.IO.Path.DirectorySeparatorChar);
                }
                result.Append(part);

                counter++;
            }

            return result.ToString();
        }
    }

    public class TestVirtualPath
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestVirtualPath));
        }

        public static void Test_Path_Parts()
        {
            string[] pathStrs =
            {
                @"C:\temp\folder0",
                @"\\virtualdrive\folder0",
            };

            string[][] expected =
            {
                new string[]
                {
                    "C:", "temp", "folder0"
                },
                new string[]
                {
                    "virtualdrive", "folder0"
                }
            };

            for (int i = 0; i < pathStrs.Length; ++i)
            {
                VirtualPath path = new VirtualPath(pathStrs[i]);
                UnitTest.Test(path.Parts.SequenceEqual(expected[i]));
            }
        }
        public static void Test_Path_Parent()
        {
            string[] pathStrs =
            {
                @"C:\temp\folder0",
                @"\\virtualdrive\folder0",
            };

            string[][] expected =
            {
                new string[]
                {
                    "C:", "temp"
                },
                new string[]
                {
                    "virtualdrive"
                }
            };

            for (int i = 0; i < pathStrs.Length; ++i)
            {
                VirtualPath path = new VirtualPath(pathStrs[i]).Parent;
                UnitTest.Test(path.Parts.SequenceEqual(expected[i]));
            }
        }
        public static void Test_Path_PartialPath()
        {
            string[] pathStrs =
            {
                @"C:\temp\folder0\folder1",
                @"\\virtualdrive\folder0\folder1",
            };

            string[][] expected =
            {
                new string[]
                {
                    "C:", "temp"
                },
                new string[]
                {
                    "virtualdrive", "folder0"
                }
            };

            for (int i = 0; i < pathStrs.Length; ++i)
            {
                VirtualPath path = new VirtualPath(pathStrs[i]).PartialPath(2);
                UnitTest.Test(path.Parts.SequenceEqual(expected[i]));
            }
        }
        public static void Test_Path_ToString()
        {
            string[] pathStrs =
            {
                @"C:\temp\folder0",
                @"\\virtualdrive\folder0",
            };

            foreach (var pathStr in pathStrs)
            {
                VirtualPath path = new VirtualPath(pathStr);
                UnitTest.Test(path.ToString() == pathStr);
            }
        }
    }
}
