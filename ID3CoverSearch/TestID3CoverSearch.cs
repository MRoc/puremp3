using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreTest;
using System.Text.RegularExpressions;
using System.IO;
using CoreUtils;

namespace ID3CoverSearch
{
    public class TestID3CoverSearch
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestID3CoverSearch));
        }

        public static void TestScanner()
        {
            string response =
                "[" +
                    "[\"t00\", \"\", \"t01\"]" +
                    "[\"t10\", [], 1, \"t11\"]" +
                "]";

            Scanner.ScanResult[] expected =
            {
                new Scanner.ScanResult(Keywords.OpenBrackets, "["),
                new Scanner.ScanResult(Keywords.OpenBrackets, "["),
                new Scanner.ScanResult(Keywords.StringLiteral, "t00"),
                new Scanner.ScanResult(Keywords.Comma, ","),
                new Scanner.ScanResult(Keywords.StringLiteral, ""),
                new Scanner.ScanResult(Keywords.Comma, ","),
                new Scanner.ScanResult(Keywords.StringLiteral, "t01"),
                new Scanner.ScanResult(Keywords.ClosedBrackets, "]"),
                new Scanner.ScanResult(Keywords.OpenBrackets, "["),
                new Scanner.ScanResult(Keywords.StringLiteral, "t10"),
                new Scanner.ScanResult(Keywords.Comma, ","),
                new Scanner.ScanResult(Keywords.OpenBrackets, "["),
                new Scanner.ScanResult(Keywords.ClosedBrackets, "]"),
                new Scanner.ScanResult(Keywords.Comma, ","),
                new Scanner.ScanResult(Keywords.Number, "1"),
                new Scanner.ScanResult(Keywords.Comma, ","),
                new Scanner.ScanResult(Keywords.StringLiteral, "t11"),
                new Scanner.ScanResult(Keywords.ClosedBrackets, "]"),
                new Scanner.ScanResult(Keywords.ClosedBrackets, "]"),
            };

            Scanner.ScanResult[] result = new Scanner(response).Scan().ToArray();

            UnitTest.Test(expected.Length == result.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                UnitTest.Test(expected[i].Keyword == result[i].Keyword);
                UnitTest.Test(expected[i].Text == result[i].Text);
            }
        }
        public static void TestParser()
        {
            string response =
                "[" +
                    "[\"t00[]\", \"\", \"t01\"]" +
                    "[\"t10\", 1, \"t11\"]" +
                "]";

            List<object> result = Parser.Parse(response);
            UnitTest.Test(result.Count == 2);
            UnitTest.Test((result[0] as List<object>).Count == 3);
            UnitTest.Test((result[0] as List<object>)[0].ToString() == "t00[]");
            UnitTest.Test((result[0] as List<object>)[1].ToString() == "");
            UnitTest.Test((result[0] as List<object>)[2].ToString() == "t01");

            UnitTest.Test((result[1] as List<object>).Count == 3);
            UnitTest.Test((result[1] as List<object>)[0].ToString() == "t10");
            UnitTest.Test((result[1] as List<object>)[1].ToString() == "1");
            UnitTest.Test((result[1] as List<object>)[2].ToString() == "t11");
        }
        public static void TestExtractUrl()
        {
            string input = @"/imgres?imgurlx3dhttp://appleadayproject.files.wordpress.com/2011/03/apple-full2.jpg\x26imgrefurl\x3dhttp://appleadayproject.wordpress.com/\x26usg\x3d__Ag_Gx3aDDpaGDDtq3Os5XmavEAo\x3d\x26h\x3d348\x26w\x3d345\x26sz\x3d11\x26hl\x3den\x26start\x3d1\x26zoom\x3d1\x26um\x3d1\x26itbs\x3d1";

            string expected = "http://appleadayproject.files.wordpress.com/2011/03/apple-full2.jpg";
            string actual = GoogleImageQuery.ExtractUrl(input);

            UnitTest.Test(expected == actual);
        }
        public static void TestGoogleImageQuerySimple()
        {
            GoogleImageQuery query = new GoogleImageQuery();
            GoogleImageQuery.ImageResult result = query.Query("apple");
            UnitTest.Test(result.Url == "http://appleadayproject.files.wordpress.com/2011/03/apple-full2.jpg");
            UnitTest.Test(result.Image.Length == 10700);
        }
    }
}
