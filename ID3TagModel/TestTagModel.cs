using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using CoreDocument;
using CoreTest;
using ID3;
using ID3.IO;
using ID3.Utils;
using CoreThreading;
using CoreUtils;
using CoreVirtualDrive;
using System.Reflection;
using System.Windows.Input;
using CoreLogging;

namespace ID3TagModel
{
    public class TestTagModel
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestTagModel));
        }

        static Tag CreateAlbum0_2_3()
        {
            Tag tag = TestTags.CreateDemoTag(ID3.Version.v2_3);

            TagEditor editor = new TagEditor(tag);
            editor.Title = "Title 1";
            editor.TrackNumber = "1";

            return tag;
        }
        static Tag CreateAlbum1_2_3()
        {
            Tag tag = TestTags.CreateDemoTag(ID3.Version.v2_3);

            TagEditor editor = new TagEditor(tag);
            editor.Title = "Title 2";
            editor.TrackNumber = "2";

            return tag;
        }
        static Tag CreateAlbum2_2_0()
        {
            Tag tag = TestTags.CreateDemoTag(ID3.Version.v2_0);

            TagEditor editor = new TagEditor(tag);
            editor.Title = "Title 3";
            editor.TrackNumber = "3";

            return tag;
        }
        static Tag CreateAlbum3_1_0()
        {
            Tag tag = TestTags.CreateDemoTag(ID3.Version.v1_0);

            TagEditor editor = new TagEditor(tag);
            editor.Title = "Title 4";
            editor.TrackNumber = "4";

            return tag;
        }

        public static readonly string testFileName0_2_3 = VirtualDrive.VirtualFileName("test0.mp3");
        public static readonly string testFileName1_2_3 = VirtualDrive.VirtualFileName("test1.mp3");
        public static readonly string testFileName2_2_0 = VirtualDrive.VirtualFileName("test2.mp3");
        public static readonly string testFileName3_1_0 = VirtualDrive.VirtualFileName("test3.mp3");
        public static readonly string[] testFileNames = { testFileName0_2_3, testFileName1_2_3, testFileName2_2_0 };

        public static void CreateTestFiles()
        {
            TagUtils.WriteTag(CreateAlbum0_2_3(), new FileInfo(testFileName0_2_3));
            TagUtils.WriteTag(CreateAlbum1_2_3(), new FileInfo(testFileName1_2_3));
            TagUtils.WriteTag(CreateAlbum2_2_0(), new FileInfo(testFileName2_2_0));
            TagUtils.WriteTag(CreateAlbum3_1_0(), new FileInfo(testFileName3_1_0));
        }

        static void Init()
        {
            WorkerThreadPool.Instance.SingleThreaded = true;
            CreateTestFiles();
        }

        static void Test_TagModelItem()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Artist, "Artist");

            TagModelItem item = DocNode.Create<TagModelItem>();
            item.Frame = frame;

            UnitTest.Test(Object.ReferenceEquals(item.TagModel, null));
            UnitTest.Test(item.Meaning == FrameMeaning.Artist);
            UnitTest.Test(item.FrameId.Value == tagDescription[FrameMeaning.Artist].FrameId);
            UnitTest.Test(item.FrameDescription.Value == tagDescription[FrameMeaning.Artist].ShortDescription);
            UnitTest.Test(item.Text.Value == "Artist");
        }
        static void Test_TagModelItem_Load()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Artist, "Artist");

            TagModelItem item = DocNode.Create<TagModelItem>();
            item.Frame = frame;

            History.Instance.Root = item;

            item.Frame.Content.Text = "Text1";

            History.Instance.ExecuteInTransaction(() => item.Load(), 0, "Dummy");
            UnitTest.Test(item.Text.Value == "Text1");
            UnitTest.Test(item.Frame.Content.Text == "Text1");

            History.Instance.Undo();
            UnitTest.Test(item.Text.Value == "Artist");
            UnitTest.Test(item.Frame.Content.Text == "Text1");
        }
        static void Test_TagModelItem_Commit()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Artist, "Artist");

            TagModelItem item = DocNode.Create<TagModelItem>();
            item.Frame = frame;

            History.Instance.Root = item;

            History.Instance.ExecuteInTransaction(() => item.Text.Value = "Text0", 1, "Dummy");
            UnitTest.Test(item.Text.Value == "Text0");
            UnitTest.Test(item.Frame.Content.Text == "Artist");

            item.Commit();
            UnitTest.Test(item.Text.Value == "Text0");
            UnitTest.Test(item.Frame.Content.Text == "Text0");

            History.Instance.Undo();
            UnitTest.Test(item.Text.Value == "Artist");
            UnitTest.Test(item.Frame.Content.Text == "Text0");
        }
        static void Test_TagModelItem_IsEqual()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Artist, "Artist");

            TagModelItem item0 = DocNode.Create<TagModelItem>();
            item0.Frame = frame;

            TagModelItem item1 = DocNode.Create<TagModelItem>();
            item1.Frame = frame.Clone();

            UnitTest.Test(item0.IsEqual(item1));
        }
        static void Test_TagModelItem_TextItemProvider()
        {
            TagModelItem item = DocNode.Create<TagModelItem>();
            UnitTest.Test(item.Text == TagModelItem.TextItemProvider(item));

        }

        static void Test_TagModelItemComment()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Comment, "Comment");
            (frame.Content as FrameContentComment).Language = "eng";
            (frame.Content as FrameContentComment).Description = "Description";

            TagModelItemComment item = DocNode.Create<TagModelItemComment>();
            item.Frame = frame;

            UnitTest.Test(item.Meaning == FrameMeaning.Comment);
            UnitTest.Test(item.FrameId.Value == tagDescription[FrameMeaning.Comment].FrameId);
            UnitTest.Test(item.FrameDescription.Value == tagDescription[FrameMeaning.Comment].ShortDescription);
            UnitTest.Test(item.Text.Value == "Comment");
            UnitTest.Test(item.Language.ValueStr == "eng");
            UnitTest.Test(item.Description.Value == "Description");
        }
        static void Test_TagModelItemComment_Load()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Comment, "Comment");
            (frame.Content as FrameContentComment).Language = "eng";
            (frame.Content as FrameContentComment).Description = "Description";

            TagModelItemComment item = DocNode.Create<TagModelItemComment>();
            item.Frame = frame;

            History.Instance.Root = item;

            (item.Frame.Content as FrameContentComment).Text = "Comment2";
            (item.Frame.Content as FrameContentComment).Description = "Description2";
            (item.Frame.Content as FrameContentComment).Language = "ger";

            History.Instance.ExecuteInTransaction(() => item.Load(), 0, "Dummy");
            UnitTest.Test(item.Text.Value == "Comment2");
            UnitTest.Test(item.Description.Value == "Description2");
            UnitTest.Test(item.Language.ValueStr == "ger");

            History.Instance.Undo();
            UnitTest.Test(item.Text.Value == "Comment");
            UnitTest.Test(item.Description.Value == "Description");
            UnitTest.Test(item.Language.ValueStr == "eng");
        }
        static void Test_TagModelItemComment_Commit()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Comment, "Comment");
            (frame.Content as FrameContentComment).Language = "eng";
            (frame.Content as FrameContentComment).Description = "Description";

            TagModelItemComment item = DocNode.Create<TagModelItemComment>();
            item.Frame = frame;

            History.Instance.Root = item;

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    item.Text.Value = "Comment2";
                    item.Description.Value = "Description2";
                    item.Language.ValueStr = "ger";
                }, 0, "Dummy");
            UnitTest.Test(item.Frame.Content.Text == "Comment");
            UnitTest.Test((item.Frame.Content as FrameContentComment).Description == "Description");
            UnitTest.Test((item.Frame.Content as FrameContentComment).Language == "eng");

            item.Commit();
            UnitTest.Test(item.Text.Value == "Comment2");
            UnitTest.Test(item.Description.Value == "Description2");
            UnitTest.Test(item.Language.ValueStr == "ger");
            UnitTest.Test(item.Frame.Content.Text == "Comment2");
            UnitTest.Test((item.Frame.Content as FrameContentComment).Description == "Description2");
            UnitTest.Test((item.Frame.Content as FrameContentComment).Language == "ger");

            History.Instance.Undo();
            UnitTest.Test(item.Text.Value == "Comment");
            UnitTest.Test(item.Description.Value == "Description");
            UnitTest.Test(item.Language.ValueStr == "eng");
            UnitTest.Test(item.Frame.Content.Text == "Comment2");
            UnitTest.Test((item.Frame.Content as FrameContentComment).Description == "Description2");
            UnitTest.Test((item.Frame.Content as FrameContentComment).Language == "ger");
        }
        static void Test_TagModelItemComment_IsEqual()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Comment, "Comment");
            (frame.Content as FrameContentComment).Language = "eng";
            (frame.Content as FrameContentComment).Description = "Description";

            TagModelItemComment item0 = DocNode.Create<TagModelItemComment>();
            item0.Frame = frame;

            TagModelItemComment item1 = DocNode.Create<TagModelItemComment>();
            item1.Frame = frame.Clone();

            UnitTest.Test(item0.IsEqual(item1));
        }

        static void Test_TagModelItemPicture()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            FrameContentPicture content = frame.Content as FrameContentPicture;
            content.Content = TestTags.demoPicturePng;
            content.Description = "Description";
            content.MimeType = Images.MimeType.Png;
            content.PictureType = 0;
            UnitTest.Test(content.Text == "Description");

            TagModelItemPicture item = DocNode.Create<TagModelItemPicture>();
            item.Frame = frame;

            UnitTest.Test(item.Meaning == FrameMeaning.Picture);
            UnitTest.Test(item.FrameId.Value == tagDescription[FrameMeaning.Picture].FrameId);
            UnitTest.Test(item.FrameDescription.Value == tagDescription[FrameMeaning.Picture].ShortDescription);
            UnitTest.Test(item.Text.Value == "Description");
            UnitTest.Test(item.CurrentMimeType == Images.MimeType.Png);
            UnitTest.Test(!Object.ReferenceEquals(item.Image, null));
        }
        static void Test_TagModelItemPicture_Load()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            FrameContentPicture content = frame.Content as FrameContentPicture;
            content.Content = TestTags.demoPicturePng;
            content.Description = "Description";
            content.MimeType = Images.MimeType.Png;
            content.PictureType = 0;
            UnitTest.Test(content.Text == "Description");

            TagModelItemPicture item = DocNode.Create<TagModelItemPicture>();
            item.Frame = frame;

            item.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == DocBase.PropertyName(item, m => m.Image) && !Object.ReferenceEquals(item.Image, null))
                {
                    Logger.WriteLine(Tokens.InfoVerbose, item.Image.ToString());
                }
            };

            History.Instance.Root = item;

            content.Content = null;
            content.Description = "Description2";
            content.MimeType = Images.MimeType.Invalid;
            content.PictureType = 1;

            History.Instance.ExecuteInTransaction(() => item.Load(), 0, "Dummy");

            UnitTest.Test(item.Text.Value == "Description2");
            UnitTest.Test(item.PictureType.Value == 1);
            UnitTest.Test(item.CurrentMimeType == Images.MimeType.Invalid);
            UnitTest.Test(Object.ReferenceEquals(item.Image, null));

            History.Instance.Undo();

            UnitTest.Test(item.Meaning == FrameMeaning.Picture);
            UnitTest.Test(item.FrameId.Value == tagDescription[FrameMeaning.Picture].FrameId);
            UnitTest.Test(item.FrameDescription.Value == tagDescription[FrameMeaning.Picture].ShortDescription);
            UnitTest.Test(item.Text.Value == "Description");
            UnitTest.Test(item.CurrentMimeType == Images.MimeType.Png);
            UnitTest.Test(!Object.ReferenceEquals(item.Image, null));
        }
        static void Test_TagModelItemPicture_Commit()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            FrameContentPicture content = frame.Content as FrameContentPicture;
            content.Content = TestTags.demoPicturePng;
            content.Description = "Description";
            content.MimeType = Images.MimeType.Png;
            content.PictureType = 0;
            UnitTest.Test(content.Text == "Description");

            TagModelItemPicture item = DocNode.Create<TagModelItemPicture>();
            item.Frame = frame;

            History.Instance.Root = item;

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    item.Text.Value = "Description2";
                    item.PictureType.Value = 1;
                    item.Content.Value = new byte[] { };
                }, 0, "Dummy");
            UnitTest.Test(item.Content.Value.Length == 0);
            UnitTest.Test(item.Text.Value == "Description2");
            UnitTest.Test(item.PictureType.Value == 1);
            UnitTest.Test(item.CurrentMimeType == Images.MimeType.Invalid);

            item.Commit();
            UnitTest.Test(content.Content.Length == 0);
            UnitTest.Test(content.Description == "Description2");
            UnitTest.Test(content.MimeType == Images.MimeType.Invalid);
            UnitTest.Test(content.PictureType == 1);

            History.Instance.Undo();
            UnitTest.Test(item.Meaning == FrameMeaning.Picture);
            UnitTest.Test(item.FrameId.Value == tagDescription[FrameMeaning.Picture].FrameId);
            UnitTest.Test(item.FrameDescription.Value == tagDescription[FrameMeaning.Picture].ShortDescription);
            UnitTest.Test(item.Text.Value == "Description");
            UnitTest.Test(item.CurrentMimeType == Images.MimeType.Png);
            UnitTest.Test(!Object.ReferenceEquals(item.Image, null));
        }
        static void Test_TagModelItemPicture_IsEqual()
        {
            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            FrameContentPicture content = frame.Content as FrameContentPicture;
            content.Content = TestTags.demoPicturePng;
            content.Description = "Description";
            content.MimeType = Images.MimeType.Png;
            content.PictureType = 0;
            UnitTest.Test(content.Text == "Description");

            TagModelItemPicture item0 = DocNode.Create<TagModelItemPicture>();
            item0.Frame = frame;

            TagModelItemPicture item1 = DocNode.Create<TagModelItemPicture>();
            item1.Frame = frame.Clone();

            UnitTest.Test(item0.IsEqual(item1));
        }
        static void Test_TagModelItemPicture_LoadPicture()
        {
            string pngFileName = VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.png");
            VirtualDrive.Store(pngFileName, TestTags.demoPicturePng);

            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            TagModelItemPicture item = DocNode.Create<TagModelItemPicture>();
            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            item.Frame = frame;

            History.Instance.Root = item;

            item.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == DocBase.PropertyName(item, m => m.Image)
                    && !Object.ReferenceEquals(item.Image, null))
                {
                    Logger.WriteLine(Tokens.InfoVerbose, item.Image.ToString());
                }
            };

            History.Instance.ExecuteInTransaction(() => item.LoadPicture(pngFileName), 0, "Dummy");

            UnitTest.Test(item.Text.Value == "");
            UnitTest.Test(item.CurrentMimeType == Images.MimeType.Png);
            UnitTest.Test(item.PictureType.Value == 3);
            UnitTest.Test(ArrayUtils.IsEqual(item.Content.Value, TestTags.demoPicturePng));
            UnitTest.Test(!Object.ReferenceEquals(item.Image, null));

            History.Instance.Undo();
        }
        static void Test_TagModelItemPicture_SavePicture()
        {
            string pngFileName = VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_SavePicture\test.png");
            VirtualDrive.Store(pngFileName, TestTags.demoPicturePng);

            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            FrameContentPicture content = frame.Content as FrameContentPicture;
            content.Content = TestTags.demoPicturePng;
            content.Description = "Description";
            content.MimeType = Images.MimeType.Png;
            content.PictureType = 0;

            TagModelItemPicture item = DocNode.Create<TagModelItemPicture>();
            item.Frame = frame;

            item.SavePicture(pngFileName);
            UnitTest.Test(ArrayUtils.IsEqual(VirtualDrive.Load(pngFileName), TestTags.demoPicturePng));
        }
        static void Test_TagModelItemPicture_LoadPicture_AllSupported()
        {
            string[] fileNames =
            {
                VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.png"),
                VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.jpg"),
                VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.bmp")
            };
            byte[][] images =
            {
                TestTags.demoPicturePng,
                TestTags.demoPictureJpg,
                TestTags.demoPictureBmp,
            };
            for (int i = 0; i < fileNames.Length; i++)
            {
                VirtualDrive.Store(fileNames[i], images[i]);
            }

            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];

            TagModelItemPicture item = DocNode.Create<TagModelItemPicture>();
            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            item.Frame = frame;

            History.Instance.Root = item;

            item.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == DocBase.PropertyName(item, m => m.Image)
                    && !Object.ReferenceEquals(item.Image, null))
                {
                    Logger.WriteLine(Tokens.InfoVerbose, item.Image.ToString());
                }
            };

            for (int i = 0; i < fileNames.Length; i++)
            {
                History.Instance.ExecuteInTransaction(() => item.LoadPicture(fileNames[i]), i, "Dummy");
            }
            for (int i = 0; i < fileNames.Length; i++)
            {
                History.Instance.Undo();
            }
        }

        static void Test_TagModel()
        {
            TagModel model = DocNode.Create<TagModel>();

            UnitTest.Test(String.IsNullOrEmpty(model.File.Value));
            UnitTest.Test(String.IsNullOrEmpty(model.FileNameFull));
            UnitTest.Test(String.IsNullOrEmpty(model.FileName));
            UnitTest.Test(model.IsSelected.Value == true);
            UnitTest.Test(model.Version.ValueVersion == ID3.Preferences.PreferredVersion);
            UnitTest.Test(model.Items.Count == 0);
        }
        static void Test_TagModel_Accessors()
        {
            TagModel model = DocNode.Create<TagModel>();
            model.FileNameFull = testFileName0_2_3;

            UnitTest.Test(model.Contains("TPE1"));
            UnitTest.Test(model.Contains("TALB"));
            UnitTest.Test(model.Contains("TIT2"));
            UnitTest.Test(!model.Contains("TXXX"));

            UnitTest.Test(model["TPE1"].Text.Value == "Artist");
            UnitTest.Test(model["TALB"].Text.Value == "Album");
            UnitTest.Test(model["TIT2"].Text.Value == "Title 1");

            UnitTest.Test(model[FrameMeaning.Artist].Text.Value == "Artist");
            UnitTest.Test(model[FrameMeaning.Album].Text.Value == "Album");
            UnitTest.Test(model[FrameMeaning.Title].Text.Value == "Title 1");

            UnitTest.Test(model[0].Text.Value == "Artist");
            UnitTest.Test(model[1].Text.Value == "Album");
            UnitTest.Test(model[2].Text.Value == "Title 1");
        }
        static void Test_TagModel_Load()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName2_2_0, 0, "Dummy");
            UnitTest.Test(model.File.Value == testFileName2_2_0);
            UnitTest.Test(model.FileNameFull == testFileName2_2_0);
            UnitTest.Test(model.FileName == "test2.mp3");
            UnitTest.Test(model.IsSelected.Value == true);
            UnitTest.Test(model.Version.ValueVersion == ID3.Version.v2_0);
            UnitTest.Test(model.Items.Count > 0);

            History.Instance.Undo();
            UnitTest.Test(String.IsNullOrEmpty(model.File.Value));
            UnitTest.Test(String.IsNullOrEmpty(model.FileNameFull));
            UnitTest.Test(String.IsNullOrEmpty(model.FileName));
            UnitTest.Test(model.IsSelected.Value == true);
            UnitTest.Test(model.Version.ValueVersion == ID3.Preferences.PreferredVersion);
            UnitTest.Test(model.Items.Count == 0);

            History.Instance.Redo();
            UnitTest.Test(model.File.Value == testFileName2_2_0);
            UnitTest.Test(model.FileNameFull == testFileName2_2_0);
            UnitTest.Test(model.FileName == "test2.mp3");
            UnitTest.Test(model.IsSelected.Value == true);
            UnitTest.Test(model.Version.ValueVersion == ID3.Version.v2_0);
            UnitTest.Test(model.Items.Count > 0);
        }
        static void Test_TagModel_Save()
        {
            FileInfo fileInfo = new FileInfo(testFileName0_2_3);

            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName0_2_3, 0, "Dummy");
            History.Instance.ExecuteInTransaction(() => model[FrameMeaning.Artist].Text.Value = "Hello world", 1, "Dummy");
            UnitTest.Test(model[FrameMeaning.Artist].Text.Value == "Hello world");

            History.Instance.ExecuteInTransaction(() => model.Save(), 2, "Dummy");
            UnitTest.Test(TagUtils.ReadTag(fileInfo)[FrameMeaning.Artist].Content.Text == "Hello world");

            History.Instance.Undo();
            UnitTest.Test(TagUtils.ReadTag(fileInfo)[FrameMeaning.Artist].Content.Text == "Artist");

            History.Instance.ExecuteInTransaction(() => model.Save(), 3, "Dummy");
            UnitTest.Test(TagUtils.ReadTag(fileInfo)[FrameMeaning.Artist].Content.Text == "Hello world");

            History.Instance.Undo();
            UnitTest.Test(TagUtils.ReadTag(fileInfo)[FrameMeaning.Artist].Content.Text == "Artist");
        }
        static void Test_TagModel_ToTag()
        {
            TagModel model = DocNode.Create<TagModel>();
            model.FileNameFull = testFileName0_2_3;

            Tag tag = model.ToTag();

            UnitTest.Test(model[FrameMeaning.Artist].Text.Value == tag[FrameMeaning.Artist].Content.Text);
            UnitTest.Test(model[FrameMeaning.Album].Text.Value == tag[FrameMeaning.Album].Content.Text);
            UnitTest.Test(model[FrameMeaning.Title].Text.Value == tag[FrameMeaning.Title].Content.Text);
        }
        static void Test_TagModel_Create_ByFrameId()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName0_2_3, 0, "Dummy");
            UnitTest.Test(model.Items.Count == 5);
            UnitTest.Test(!model.Contains("TXXX"));

            History.Instance.ExecuteInTransaction(delegate()
            {
                model.Create("TXXX");
                model["TXXX"].Text.Value = "Hello World";
            }, 1, "Dummy");
            UnitTest.Test(model.Items.Count == 6);
            UnitTest.Test(model.Contains("TXXX"));

            History.Instance.Undo();
            UnitTest.Test(model.Items.Count == 5);
            UnitTest.Test(!model.Contains("TXXX"));

            History.Instance.Redo();
            UnitTest.Test(model.Items.Count == 6);
            UnitTest.Test(model.Contains("TXXX"));
        }
        static void Test_TagModel_Create_ByMeaning()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName0_2_3, 0, "Dummy");
            UnitTest.Test(model.Items.Count == 5);
            UnitTest.Test(!model.Contains(FrameMeaning.Composer));

            History.Instance.ExecuteInTransaction(delegate()
            {
                model.Create(FrameMeaning.Composer);
                model[FrameMeaning.Composer].Text.Value = "Hello World";
            }, 1, "Dummy");
            UnitTest.Test(model.Items.Count == 6);
            UnitTest.Test(model.Contains(FrameMeaning.Composer));

            History.Instance.Undo();
            UnitTest.Test(model.Items.Count == 5);
            UnitTest.Test(!model.Contains(FrameMeaning.Composer));

            History.Instance.Redo();
            UnitTest.Test(model.Items.Count == 6);
            UnitTest.Test(model.Contains(FrameMeaning.Composer));
        }
        static void Test_TagModel_Remove()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName0_2_3, 0, "Dummy");
            UnitTest.Test(model.Items.Count == 5);

            History.Instance.ExecuteInTransaction(() => model.Remove(model.Items.First()), 1, "Dummy");
            UnitTest.Test(model.Items.Count == 4);

            History.Instance.Undo();
            UnitTest.Test(model.Items.Count == 5);

            History.Instance.Redo();
            UnitTest.Test(model.Items.Count == 4);

            History.Instance.ExecuteInTransaction(() => model.Remove(model.Items.ToArray()), 2, "Dummy");
            UnitTest.Test(model.Items.Count == 0);

            History.Instance.Undo();
            UnitTest.Test(model.Items.Count == 4);

            History.Instance.Redo();
            UnitTest.Test(model.Items.Count == 0);
        }
        static void Test_TagModel_Clear()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName0_2_3, 0, "Dummy");
            UnitTest.Test(model.Items.Count == 5);

            History.Instance.ExecuteInTransaction(() => model.Clear(), 1, "Dummy");
            UnitTest.Test(model.Items.Count == 0);

            History.Instance.Undo();
            UnitTest.Test(model.Items.Count == 5);

            History.Instance.Redo();
            UnitTest.Test(model.Items.Count == 0);
        }
        static void Test_TagModel_ConvertVersion()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            TagEditor src = new TagEditor(TagUtils.ReadTag(new FileInfo(testFileName2_2_0)));

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName2_2_0, 0, "Dummy");
            UnitTest.Test(model.Version.ValueVersion == ID3.Version.v2_0);
            UnitTest.Test(new TagEditor(model.ToTag()).Equals(src));

            foreach (var version in ID3.Version.Versions)
            {
                History.Instance.ExecuteInTransaction(() => model.ConvertVersion(version), 1, "Dummy");
                UnitTest.Test(model.Version.ValueVersion == version);
                UnitTest.Test(new TagEditor(model.ToTag()).Equals(src));

                History.Instance.Undo();
                UnitTest.Test(model.Version.ValueVersion == ID3.Version.v2_0);
                UnitTest.Test(new TagEditor(model.ToTag()).Equals(src));

                History.Instance.Redo();
                UnitTest.Test(model.Version.ValueVersion == version);
                UnitTest.Test(new TagEditor(model.ToTag()).Equals(src));

                History.Instance.Undo();
                UnitTest.Test(model.Version.ValueVersion == ID3.Version.v2_0);
                UnitTest.Test(new TagEditor(model.ToTag()).Equals(src));
            }
        }

        static void Test_TagModelList()
        {
            TagModelList tml = DocNode.Create<TagModelList>();

            UnitTest.Test(!tml.HasModels);
            UnitTest.Test(!tml.HasSelection.Value);
            UnitTest.Test(tml.FileName.Value == null);
            UnitTest.Test(tml.Version.IsUndefined);
        }
        static void Test_TagModelList_Load_Add()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(() => tml.Add(testFileName0_2_3), 0, "Dummy");
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(tml.Contains(testFileName0_2_3));
            UnitTest.Test(!tml.Contains(testFileName1_2_3));
            UnitTest.Test(!Object.ReferenceEquals(tml[0], null));
            UnitTest.Test(!Object.ReferenceEquals(tml[testFileName0_2_3], null));
            UnitTest.Test(tml.FileName.Value == "test0.mp3");
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);

            History.Instance.Undo();
            UnitTest.Test(!tml.HasModels);
            UnitTest.Test(!tml.HasSelection.Value);
            UnitTest.Test(!tml.Contains(testFileName0_2_3));
            UnitTest.Test(String.IsNullOrEmpty(tml.FileName.Value));
            UnitTest.Test(tml.Version.IsUndefined);

            History.Instance.Redo();
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(tml.Contains(testFileName0_2_3));
            UnitTest.Test(!tml.Contains(testFileName1_2_3));
            UnitTest.Test(!Object.ReferenceEquals(tml[0], null));
            UnitTest.Test(!Object.ReferenceEquals(tml[testFileName0_2_3], null));
            UnitTest.Test(tml.FileName.Value == "test0.mp3");
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);
        }
        static void Test_TagModelList_Load_SetFiles()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    tml.SetFiles(new string[] { testFileName0_2_3, testFileName1_2_3 });
                }, 0, "Dummy");
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(tml.Contains(testFileName0_2_3));
            UnitTest.Test(tml.Contains(testFileName1_2_3));
            UnitTest.Test(tml.FileName.Value == "*");
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);

            History.Instance.ExecuteInTransaction(() => tml.Remove(testFileName0_2_3), 1, "Dummy");
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(!tml.Contains(testFileName0_2_3));
            UnitTest.Test(tml.Contains(testFileName1_2_3));
            UnitTest.Test(tml.FileName.Value == "test1.mp3");
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);

            History.Instance.Undo();
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(tml.Contains(testFileName0_2_3));
            UnitTest.Test(tml.Contains(testFileName1_2_3));
            UnitTest.Test(tml.FileName.Value == "*");
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);

            History.Instance.Redo();
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(!tml.Contains(testFileName0_2_3));
            UnitTest.Test(tml.Contains(testFileName1_2_3));
            UnitTest.Test(tml.FileName.Value == "test1.mp3");
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);
        }
        static void Test_TagModelList_Load_SetFiles_DifferentVersion()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(
                () => tml.SetFiles(new string[] { testFileName3_1_0, testFileName1_2_3 }), 0, "Dummy");
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(tml.Contains(testFileName3_1_0));
            UnitTest.Test(tml.Contains(testFileName1_2_3));
            UnitTest.Test(tml.FileName.Value == "*");
            UnitTest.Test(tml.Version.IsMultiple);

            History.Instance.ExecuteInTransaction(() => tml.Remove(testFileName1_2_3), 1, "Dummy");
            UnitTest.Test(tml.HasModels);
            UnitTest.Test(tml.HasSelection.Value);
            UnitTest.Test(tml.Contains(testFileName3_1_0));
            UnitTest.Test(tml.FileName.Value == "test3.mp3");
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v1_0);
        }
        static void Test_TagModelList_Save()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(() => tml.Add(testFileName0_2_3), 0, "Dummy");
            History.Instance.ExecuteInTransaction(() => tml[0][0].Text.Value = "DOOD", 1, "Dummy");
            History.Instance.ExecuteInTransaction(() => tml.SelectedModels.Save(2), 2, "Dummy");

            History.Instance.Undo();
            History.Instance.Undo();
            UnitTest.Test(tml[0][0].Text.Value != "DOOD");
        }
        static void Test_TagModelList_Version_Selection()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "Dummy");
            UnitTest.Test(tml.Version.IsMultiple);
            UnitTest.Test(tml.Version.ValueVersion == null);

            tml[1].IsSelected.Value = false;
            tml[2].IsSelected.Value = false;
            UnitTest.Test(!tml.Version.IsMultiple);
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);
        }
        static void Test_TagModelList_Version_Set()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "Dummy");
            UnitTest.Test(tml.Version.IsMultiple);

            foreach (var version in ID3.Version.Versions)
            {
                tml.Version.ValueVersion = version;

                UnitTest.Test(tml.Version.ValueVersion == version);
                UnitTest.Test(tml.SelectedModels.Where(n => n.Version.ValueVersion == version).Count() == 3);

                History.Instance.Undo();
                UnitTest.Test(tml.Version.IsMultiple);

                History.Instance.Redo();
                UnitTest.Test(tml.Version.ValueVersion == version);
                UnitTest.Test(tml.SelectedModels.Where(n => n.Version.ValueVersion == version).Count() == 3);

                History.Instance.Undo();
                UnitTest.Test(tml.Version.IsMultiple);
            }
        }
        static void Test_TagModelList_ConvertThroughSelectedModels()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "Dummy");
            UnitTest.Test(tml.Version.IsMultiple);

            int counter = 1;
            foreach (var version in ID3.Version.Versions)
            {
                History.Instance.ExecuteInTransaction(
                    () => tml.SelectedModels.ConvertToVersion(version), counter++, "Dummy");

                UnitTest.Test(tml.Version.ValueVersion == version);
                UnitTest.Test(tml.SelectedModels.Where(n => n.Version.ValueVersion == version).Count() == 3);

                History.Instance.Undo();
                UnitTest.Test(tml.Version.IsMultiple);

                History.Instance.Redo();
                UnitTest.Test(tml.Version.ValueVersion == version);
                UnitTest.Test(tml.SelectedModels.Where(n => n.Version.ValueVersion == version).Count() == 3);

                History.Instance.Undo();
                UnitTest.Test(tml.Version.IsMultiple);
            }
        }
        static void Test_TagModelList_DropTarget()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(() => tml.Add(testFileName3_1_0), 0, "Dummy");
            UnitTest.Test(tml.DropTarget.SupportedTypes.Contains(DropTypes.Picture));
            UnitTest.Test(tml.DropTarget.AllowDrop(null));
            UnitTest.Test(Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));

            string pngFileName = VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.png");
            VirtualDrive.Store(pngFileName, TestTags.demoPicturePng);
            History.Instance.ExecuteInTransaction(() => tml.DropTarget.Drop(pngFileName), 1, "Dummy");
            UnitTest.Test(!Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));

            History.Instance.Undo();
            UnitTest.Test(Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));

            History.Instance.Redo();
            UnitTest.Test(!Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));
        }
        static void Test_TagModelList_DropTarget_DifferenVersion()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            History.Instance.ExecuteInTransaction(
                () => tml.SetFiles(new string[] { testFileName3_1_0, testFileName1_2_3 }), 0, "Dummy");
            UnitTest.Test(tml.DropTarget.SupportedTypes.Contains(DropTypes.Picture));
            UnitTest.Test(tml.DropTarget.AllowDrop(null));
            UnitTest.Test(Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));
            UnitTest.Test(tml.Version.IsMultiple);

            string pngFileName = VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.png");
            VirtualDrive.Store(pngFileName, TestTags.demoPicturePng);
            History.Instance.ExecuteInTransaction(() => tml.DropTarget.Drop(pngFileName), 1, "Dummy");
            UnitTest.Test(!Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);

            History.Instance.Undo();
            UnitTest.Test(Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));
            UnitTest.Test(tml.Version.IsMultiple);

            History.Instance.Redo();
            UnitTest.Test(!Object.ReferenceEquals(tml[0][FrameMeaning.Picture], null));
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);
        }

        static void Test_MultiTagModelItem()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;

            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName0_2_3, 0, "Dummy");

            MultiTagModelItem multiItem = DocNode.Create<MultiTagModelItem>();
            UnitTest.Test(multiItem.Items.Count == 0);
            UnitTest.Test(multiItem.IsClassIdUnique == true);
            UnitTest.Test(multiItem.IsFrameUnique == true);
            UnitTest.Test(multiItem.IsTextUnique == true);
            UnitTest.Test(multiItem.IsEnabled == false);

            // test single UPDATE
            multiItem.Items.Add(model.Items[0]);
            UnitTest.Test(multiItem.Items.Count == 1);
            UnitTest.Test(multiItem.IsClassIdUnique == true);
            UnitTest.Test(multiItem.IsFrameUnique == true);
            UnitTest.Test(multiItem.IsTextUnique == true);
            UnitTest.Test(multiItem.IsEnabled == true);
            UnitTest.Test(multiItem.FirstText == model.Items[0].Text.Value);
            UnitTest.Test(multiItem.Text.Value == model.Items[0].Text.Value);

            // test single SET
            History.Instance.ExecuteInTransaction(() => multiItem.Text.Value = "Text0", 1, "Dummy");
            UnitTest.Test(multiItem.Text.Value == "Text0");
            UnitTest.Test(multiItem.FirstText == model.Items[0].Text.Value);
            UnitTest.Test(model.Items[0].Text.Value == "Text0");

            History.Instance.Undo();

            // test multi UPDATE
            multiItem.Items.Add(model.Items[1]);
            UnitTest.Test(multiItem.Items.Count == 2);
            UnitTest.Test(multiItem.IsClassIdUnique == true);
            UnitTest.Test(multiItem.IsFrameUnique == false);
            UnitTest.Test(multiItem.IsTextUnique == false);
            UnitTest.Test(multiItem.IsEnabled == true);
            UnitTest.Test(multiItem.Text.Value == "*");
            UnitTest.Test(multiItem.FirstText == "Artist");
            UnitTest.Test(model.Items[0].Text.Value == "Artist");
            UnitTest.Test(model.Items[1].Text.Value == "Album");

            // test multi SET
            History.Instance.ExecuteInTransaction(() => multiItem.Text.Value = "Hello World", 1, "Dummy");
            UnitTest.Test(multiItem.IsClassIdUnique == true);
            UnitTest.Test(multiItem.IsFrameUnique == true);
            UnitTest.Test(multiItem.IsTextUnique == true);
            UnitTest.Test(multiItem.IsEnabled == true);
            UnitTest.Test(multiItem.Text.Value == "Hello World");
            UnitTest.Test(multiItem.FirstText == "Hello World");
            UnitTest.Test(model.Items[0].Text.Value == "Hello World");
            UnitTest.Test(model.Items[1].Text.Value == "Hello World");

            History.Instance.Undo();
            History.Instance.Redo();

            // test multi UPDATE
            History.Instance.ExecuteInTransaction(() => model.Items[0].Text.Value = "Hello World2", 2, "Dummy");
            UnitTest.Test(multiItem.IsClassIdUnique == true);
            UnitTest.Test(multiItem.IsFrameUnique == false);
            UnitTest.Test(multiItem.IsTextUnique == false);
            UnitTest.Test(multiItem.IsEnabled == true);
            UnitTest.Test(multiItem.Text.Value == "*");
            UnitTest.Test(multiItem.FirstText == "Hello World2");
            UnitTest.Test(model.Items[0].Text.Value == "Hello World2");
            UnitTest.Test(model.Items[1].Text.Value == "Hello World");
        }
        static void Test_MultiTagModelItem_Availability()
        {
            MultiTagModelItem multiItem = DocNode.Create<MultiTagModelItem>();
            UnitTest.Test(!multiItem.DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiItem.DuplicateCommand.CanExecute(null));

            TagModelItem item = DocNode.Create<TagModelItem>();
            item.Frame = new Frame(TagDescriptionMap.Instance[ID3.Version.v2_3], FrameMeaning.Artist);

            multiItem.Items.Add(item);

            UnitTest.Test(multiItem.DeleteCommand.CanExecute(null));
            UnitTest.Test(multiItem.DuplicateCommand.CanExecute(null));
        }

        static void Test_MultiTagModelItemPicture()
        {
            TagModel model = DocNode.Create<TagModel>();
            History.Instance.Root = model;
            History.Instance.ExecuteInTransaction(() => model.FileNameFull = testFileName0_2_3, 0, "Dummy");

            MultiTagModelItemPicture multiItem = DocNode.Create<MultiTagModelItemPicture>();

            TagDescription tagDescription = TagDescriptionMap.Instance[ID3.Version.v2_3];
            Frame frame = new Frame(tagDescription, FrameMeaning.Picture);
            FrameContentPicture content = frame.Content as FrameContentPicture;
            content.Content = TestTags.demoPicturePng;
            content.Description = "Description";
            content.MimeType = Images.MimeType.Png;
            content.PictureType = 0;

            TagModelItemPicture item = DocNode.Create<TagModelItemPicture>();
            item.Frame = frame;

            PropertyChangedTest propertyChangedTest = new PropertyChangedTest();
            multiItem.PropertyChanged += propertyChangedTest.PropertyChanged;

            multiItem.Items.Add(item);

            propertyChangedTest.TestArgs<PropertyChangedEventArgs>(n => n.PropertyName == "Image");
            UnitTest.Test(!Object.ReferenceEquals(multiItem.Image, null));
        }

        static void Test_MultiTagModel_AddRemove()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(new string[] { testFileName0_2_3, testFileName1_2_3 }), 0, "Dummy");
            UnitTest.Test(multiModel.TagModels.Count == 2);
            UnitTest.Test(multiModel.MultiTagItems.Count == 5);

            History.Instance.ExecuteInTransaction(() => tml.Remove(testFileName0_2_3), 1, "Dummy");
            UnitTest.Test(multiModel.TagModels.Count == 1);
            UnitTest.Test(multiModel.MultiTagItems.Count == 5);

            History.Instance.Undo();
            UnitTest.Test(multiModel.TagModels.Count == 2);
            UnitTest.Test(multiModel.MultiTagItems.Count == 5);

            History.Instance.Redo();
            UnitTest.Test(multiModel.TagModels.Count == 1);
            UnitTest.Test(multiModel.MultiTagItems.Count == 5);
        }
        static void Test_MultiTagModel_DeleteItemCommand()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(new string[] { testFileName0_2_3, testFileName1_2_3 }), 0, "Dummy");
            UnitTest.Test(multiModel.MultiTagItems.Count == 5);

            for (int i = 0; i < 5; ++i)
            {
                History.Instance.ExecuteInTransaction(() => multiModel.MultiTagItems[0].DeleteCommand.Execute(null), 1 + i, "Dummy");
                UnitTest.Test(multiModel.MultiTagItems.Count == 5 - i - 1);
            }
            UnitTest.Test(multiModel.MultiTagItems.Count == 0);

            for (int i = 0; i < 5; ++i)
            {
                History.Instance.Undo();
            }
            UnitTest.Test(multiModel.MultiTagItems.Count == 5);

            for (int i = 0; i < 5; ++i)
            {
                History.Instance.Redo();
            }
            UnitTest.Test(multiModel.MultiTagItems.Count == 0);
        }
        static void Test_MultiTagModel_CreateFrameCommand()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(new string[] { testFileName0_2_3, testFileName2_2_0 }), 0, "Dummy");
            UnitTest.Test(multiModel.MultiTagItems.Count == 10);
            UnitTest.Test(tml.Version.IsMultiple);

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    multiModel.CreateFrameCommands.Where(
                        n => (n as CreateFrameCommand).FrameDesc.FrameId == "TPE3").First().Execute(null);
                },
                1, "Dummy");
            UnitTest.Test(multiModel.MultiTagItems.Count == 6); // implicit version conversion
            UnitTest.Test(tml.Version.ValueVersion == ID3.Version.v2_3);

            History.Instance.Undo();
            UnitTest.Test(multiModel.MultiTagItems.Count == 10);
            UnitTest.Test(tml.Version.IsMultiple);

            History.Instance.Undo();
            UnitTest.Test(multiModel.MultiTagItems.Count == 0);
            UnitTest.Test(tml.Version.IsUndefined);
        }
        static void Test_MultiTagModel_CreateFramePictureCommand()
        {
            TagModelList tml = DocNode.Create<TagModelList>();

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            tml.Add(testFileName0_2_3);
            tml.Add(testFileName1_2_3);
            tml.Add(testFileName3_1_0);

            multiModel.CreateFrameCommands.Where(
                n => (n as CreateFrameCommand).FrameDesc.Meaning == FrameMeaning.Picture).First().Execute(null);

            tml.Items.ForEach(model => UnitTest.Test(model.Version.ValueVersion == ID3.Version.v2_3));
        }
        static void Test_MultiTagModel_Example()
        {
            // How to edit two tags at once.

            // Setup file list
            TagModelList tml = DocNode.Create<TagModelList>();
            tml.Add(testFileName0_2_3);
            tml.Add(testFileName1_2_3);
            History.Instance.Root = tml;

            // Setup multi editor
            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            // Edit frames
            multiModel[FrameMeaning.Album].Text.Value = "Hello World";
        }
        static void Test_MultiTagModel_CreateTagInMultiselection()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    tml.Add(testFileName2_2_0);
                    tml.Add(testFileName0_2_3);
                    tml.Add(testFileName1_2_3);
                }, 1, "Dummy");

            tml.AllModels.CheckVersion();

            History.Instance.ExecuteInTransaction(() => multiModel[FrameMeaning.Album].Text.Value = "Hello World", 2, "Dummy");
            tml.AllModels.CheckVersion();

            History.Instance.ExecuteInTransaction(() => multiModel[FrameMeaning.Album].DuplicateCommand.Execute(null), 3, "Dummy");
            tml.AllModels.CheckVersion();

            History.Instance.ExecuteInTransaction(() => multiModel[FrameMeaning.Comment].DuplicateCommand.Execute(null), 4, "Dummy");
            tml.AllModels.CheckVersion();
        }
        static void Test_MultiTagModel_OnBlankMp3()
        {
            string testFileMp3 = VirtualDrive.VirtualFileName("TestMultiTagModelOnBlankMp3.mp3");
            byte[] mpegFrameDummy = new byte[] { 0xFF, 0xFF, 1, 2, 3, 4 };
            ID3.Utils.Id3FileUtils.SaveFileBinary(testFileMp3, mpegFrameDummy);

            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.Add(testFileMp3), 0, "Dummy");

            History.Instance.ExecuteInTransaction(
                () => tml.SelectedModels.CreateItemByFrameId(tml.SelectedModels.CreatableFramesIds()[0].FrameId),
                2, "Dummy");

            History.Instance.ExecuteInTransaction(() => tml.SelectedModels.Save(3), 3, "Dummy");
            UnitTest.Test(VirtualDrive.FileLength(testFileMp3) == 21 + 6);

            History.Instance.Undo();
            UnitTest.Test(VirtualDrive.FileLength(testFileMp3) == 6);

            History.Instance.Undo();
            History.Instance.Undo();

            byte[] mpegFrameReload = ID3.Utils.Id3FileUtils.LoadFileBinary(testFileMp3);
            UnitTest.Test(ArrayUtils.IsEqual(mpegFrameReload, mpegFrameDummy));
        }
        static void Test_MultiTagModel_WithTransaction()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    List<string> files = new List<string>();
                    files.Add(testFileName0_2_3);
                    files.Add(testFileName1_2_3);

                    tml.SetFiles(files);
                }, 123,
                "Dummy");

            History.Instance.Undo();
        }
        static void Test_MultiTagModel_WithSelection()
        {
            CreateTestFiles();

            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    List<string> files = new List<string>();
                    files.Add(testFileName0_2_3);
                    files.Add(testFileName1_2_3);
                    files.Add(testFileName2_2_0);

                    tml.SetFiles(files);
                }, 0, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 3);

            History.Instance.ExecuteInTransaction(() => tml.Items[1].IsSelected.Value = false, 1, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 2);

            History.Instance.ExecuteInTransaction(() => tml.Items[0].IsSelected.Value = false, 2, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 1);

            History.Instance.ExecuteInTransaction(() => tml.Items[0].IsSelected.Value = true, 3, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 2);

            PathUtils.CheckParentChildrenLink(tml, null);

            foreach (var m in tml.Items)
            {
                UnitTest.Test(Object.ReferenceEquals(m.Parent.Parent, tml));
            }

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    List<string> files = new List<string>();
                    files.Add(testFileName0_2_3);
                    files.Add(testFileName1_2_3);

                    tml.SetFiles(files);
                }, 0,
                "Dummy");

            UnitTest.Test(multiModel.NumTagModels() == 2);
        }

        static void Test_MultiTagEditor_Fixed()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "TestMultiTagEditorFixed");

            UnitTest.Test(multiModel[FrameMeaning.Artist].Text.Value == "Artist");
            UnitTest.Test(multiModel[FrameMeaning.Artist].FrameDescription == "Artist");
            UnitTest.Test(multiModel[FrameMeaning.Album].Text.Value == "Album");
            UnitTest.Test(multiModel[FrameMeaning.Album].FrameDescription == "Album");
            UnitTest.Test(multiModel[FrameMeaning.Title].Text.Value == "*");
            UnitTest.Test(multiModel[FrameMeaning.Title].FrameDescription == "Title");

            History.Instance.ExecuteInTransaction(
                () => multiModel[FrameMeaning.Title].Text.Value = "Hello World", 1, "TestTagModel.TestMultiTagModel");

            UnitTest.Test(multiModel[FrameMeaning.Title].Text.Value == "Hello World");
            tml.Items.ForEach(n => UnitTest.Test(n[FrameMeaning.Title].Text.Value == "Hello World"));

            History.Instance.Undo();

            tml.Items.ForEach(n => UnitTest.Test(n[FrameMeaning.Title].Text.Value != "Hello World"));

            foreach (var file in testFileNames)
            {
                History.Instance.ExecuteInTransaction(
                    () => tml.Remove(file), 2, "TestTagModel.TestMultiTagModel");
            }
        }
        static void Test_MultiTagEditor_Fixed_WithSelection()
        {
            CreateTestFiles();

            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 3);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountNonTemplateItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[0].Text.Value == "Title 1");
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[1].Text.Value == "Title 2");
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[2].Text.Value == "Title 3");

            History.Instance.ExecuteInTransaction(() => tml.Items[1].IsSelected.Value = false, 1, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 2);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountItems == 2);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountNonTemplateItems == 2);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[0].Text.Value == "Title 1");
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[1].Text.Value == "Title 3");

            History.Instance.ExecuteInTransaction(() => tml.Items[0].IsSelected.Value = false, 2, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 1);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountItems == 1);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountNonTemplateItems == 1);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[0].Text.Value == "Title 3");

            History.Instance.ExecuteInTransaction(() => tml.Items[0].IsSelected.Value = true, 3, "Dummy");
            UnitTest.Test(multiModel.NumTagModels() == 2);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountItems == 2);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountNonTemplateItems == 2);
            UnitTest.Test(multiModel[FrameMeaning.Title].CountTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[0].Text.Value == "Title 1");
            UnitTest.Test(multiModel[FrameMeaning.Title].Items[1].Text.Value == "Title 3");

            PathUtils.CheckParentChildrenLink(tml, null);
            foreach (var m in tml.Items)
            {
                UnitTest.Test(Object.ReferenceEquals(m.Parent.Parent, tml));
            }

            History.Instance.ExecuteInTransaction(
                () => tml.SetFiles(new string[] { testFileName0_2_3, testFileName1_2_3 }),
                0, "Dummy");

            UnitTest.Test(multiModel.NumTagModels() == 2);
        }
        static void Test_MultiTagEditor_Fixed_EditNonExisting_Multi()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 0);

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "TestTagModel.TestMultiTagModel");

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 3);

            int undos00 = History.Instance.UndoCount;
            multiModel[FrameMeaning.PartOfSet].Text.Value = "0";
            int undos01 = History.Instance.UndoCount;
            UnitTest.Test(undos01 - undos00 == 1);

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "0");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 0);

            History.Instance.Undo();

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 3);
        }
        static void Test_MultiTagEditor_Fixed_EditNonExisting_Single()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 0);

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "TestTagModel.TestMultiTagModel");

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 3);

            int undos00 = History.Instance.UndoCount;
            multiModel[FrameMeaning.PartOfSet].Items[0].Text.Value = "0";
            int undos01 = History.Instance.UndoCount;
            UnitTest.Test(undos01 - undos00 == 1);

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "0");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 1);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 2);

            int undos10 = History.Instance.UndoCount;
            multiModel[FrameMeaning.PartOfSet].Items[1].Text.Value = "1";
            int undos11 = History.Instance.UndoCount;
            UnitTest.Test(undos11 - undos10 == 1);

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "*");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 2);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 1);

            int undos20 = History.Instance.UndoCount;
            multiModel[FrameMeaning.PartOfSet].Items[2].Text.Value = "2";
            int undos21 = History.Instance.UndoCount;
            UnitTest.Test(undos21 - undos20 == 1);

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "*");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 0);

            History.Instance.Undo();
            History.Instance.Undo();

            multiModel[FrameMeaning.PartOfSet].Items[1].Text.Value = "0";
            multiModel[FrameMeaning.PartOfSet].Items[2].Text.Value = "0";

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "0");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 0);

            History.Instance.Undo();
            History.Instance.Undo();
            History.Instance.Undo();

            UnitTest.Test(!tml.Items.IsVersionUnique());
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DeleteCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].DuplicateCommand.CanExecute(null));
            UnitTest.Test(!multiModel[FrameMeaning.PartOfSet].Items[0].DeleteCommand.CanExecute(null));
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].IsTextUnique);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].Text.Value == "");
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountItems == 3);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountNonTemplateItems == 0);
            UnitTest.Test(multiModel[FrameMeaning.PartOfSet].CountTemplateItems == 3);
        }
        static void Test_MultiTagEditor_FixedEditNonExistingFrameWithConversion()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(
                () => tml.Add(testFileName3_1_0), 0, "TestMultiTagEditorFixedEditNonExistingFrameWithConversion");

            UnitTest.Test(multiModel.TagModels.MaxVersion() == ID3.Version.v1_0);

            History.Instance.ExecuteInTransaction(
                () => multiModel[FrameMeaning.Picture].Text.Value = "My Picture", 1, "TestMultiTagEditorFixedEditNonExistingFrameWithConversion");

            UnitTest.Test(multiModel.TagModels.First().Version.ValueVersion == ID3.Preferences.PreferredVersion);
            UnitTest.Test(multiModel.TagModels.First()[FrameMeaning.Picture] != null);

            History.Instance.Undo();

            UnitTest.Test(multiModel.TagModels.MaxVersion() == ID3.Version.v1_0);
        }
        static void Test_MultiTagEditor_FixedAvailability()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            PropertyChangedTest isEnabledTest = new PropertyChangedTest();
            multiModel[FrameMeaning.Artist].PropertyChanged += isEnabledTest.PropertyChanged;

            ICommand duplicateCommand = multiModel[FrameMeaning.Artist].DuplicateCommand;
            PropertyChangedTest duplicateChangedTest = new PropertyChangedTest();
            duplicateCommand.CanExecuteChanged += duplicateChangedTest.PropertyChanged;

            ICommand deleteCommand = multiModel[FrameMeaning.Artist].DeleteCommand;
            PropertyChangedTest deleteChangedTest = new PropertyChangedTest();
            deleteCommand.CanExecuteChanged += deleteChangedTest.PropertyChanged;

            UnitTest.Test(multiModel[FrameMeaning.Artist].IsEnabled == false);
            UnitTest.Test(duplicateCommand.CanExecute(null) == false);
            UnitTest.Test(deleteCommand.CanExecute(null) == false);

            foreach (var file in testFileNames)
            {
                History.Instance.ExecuteInTransaction(
                    () => tml.Add(file), 0, "TestMultiTagEditorFixedAvailability");
            }

            duplicateChangedTest.TestWasCalled(3);
            deleteChangedTest.TestWasCalled(3);
            isEnabledTest.TestArgs<PropertyChangedEventArgs>(e => e.PropertyName == "IsEnabled");

            UnitTest.Test(multiModel[FrameMeaning.Artist].IsEnabled == true);
            UnitTest.Test(duplicateCommand.CanExecute(null) == false);
            UnitTest.Test(deleteCommand.CanExecute(null) == true);

            foreach (var file in testFileNames)
            {
                History.Instance.ExecuteInTransaction(
                    () => tml.Remove(file), 0, "TestMultiTagEditorFixedAvailability");
            }

            duplicateChangedTest.TestWasCalled(4);
            deleteChangedTest.TestWasCalled(4);
            isEnabledTest.TestArgs<PropertyChangedEventArgs>(e => e.PropertyName == "IsEnabled");

            UnitTest.Test(multiModel[FrameMeaning.Artist].IsEnabled == false);
            UnitTest.Test(duplicateCommand.CanExecute(null) == false);
            UnitTest.Test(deleteCommand.CanExecute(null) == false);

        }
        static void Test_MultiTagEditor_Fixed_ConvertFrame()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(testFileNames), 0, "Dummy");

            var commands = multiModel[FrameMeaning.Artist].ConvertToCommands;

            var cmd = commands.Where(n => (n as ConvertMultiTagModelItemCommand).Description.Meaning
                == FrameMeaning.BandOrchestraAccompaniment).First();

            History.Instance.ExecuteInTransaction(
                () => cmd.Execute(null),
                1,
                "Dummy");
        }

        static void Test_MultiTagModelItem_Delete_Availability_Selection()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            int callCounter = 0;
            bool available = multiModel[FrameMeaning.Artist].DeleteCommand.CanExecute(null);
            UnitTest.Test(!available);
            multiModel[FrameMeaning.Artist].DeleteCommand.CanExecuteChanged
                += delegate(object sender, EventArgs e)
                {
                    callCounter++;
                    available = multiModel[FrameMeaning.Artist].DeleteCommand.CanExecute(null);
                };

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(
                new string[] { testFileName0_2_3, testFileName3_1_0 }),
                0,
                "Dummy");
            UnitTest.Test(callCounter == 1);
            UnitTest.Test(available);

            History.Instance.ExecuteInTransaction(
                () => tml.Items.ForEach(n => n.IsSelected.Value = false),
                1, "Dummy");
            UnitTest.Test(callCounter == 4);
            UnitTest.Test(!available);

            History.Instance.ExecuteInTransaction(
                () => tml.Items.ForEach(n => n.IsSelected.Value = true),
                2, "Dummy");
            UnitTest.Test(callCounter == 7);
            UnitTest.Test(available);

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(
                new string[] { }),
                3,
                "Dummy");
            UnitTest.Test(callCounter == 9);
            UnitTest.Test(!available);
        }
        static void Test_MultiTagModelItemPicture_DropTarget_Fixed()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.IsFixed.Value = true;
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(
                new string[] { testFileName0_2_3, testFileName3_1_0 }),
                0,
                "Dummy");

            MultiTagModelItemPicture multiItem = multiModel[FrameMeaning.Picture] as MultiTagModelItemPicture;
            UnitTest.Test(multiItem.DropTarget.SupportedTypes.Contains(DropTypes.Picture));
            UnitTest.Test(multiItem.DropTarget.AllowDrop(null));

            PropertyChangedTest imagePropTest = new PropertyChangedTest();
            multiItem.PropertyChanged += imagePropTest.PropertyChanged;

            string pngFileName = VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.png");
            VirtualDrive.Store(pngFileName, TestTags.demoPicturePng);
            History.Instance.ExecuteInTransaction(() => multiItem.DropTarget.Drop(pngFileName), 1, "Dummy");
            UnitTest.Test(!Object.ReferenceEquals(multiItem.Image, null));

            imagePropTest.TestWasCalled(8);

            History.Instance.Undo();
            UnitTest.Test(Object.ReferenceEquals(multiItem.Image, null));

            History.Instance.Redo();
            UnitTest.Test(!Object.ReferenceEquals(multiItem.Image, null));
        }
        static void Test_MultiTagModelItemPicture_DropTarget()
        {
            TagModelList tml = DocNode.Create<TagModelList>();
            History.Instance.Root = tml;

            MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
            multiModel.TagModelList = tml;

            History.Instance.ExecuteInTransaction(() => tml.SetFiles(
                new string[] { testFileName0_2_3, testFileName3_1_0 }),
                0,
                "Dummy");

            string jpgFileName = VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.jpg");
            VirtualDrive.Store(jpgFileName, TestTags.demoPictureJpg);
            tml.DropTarget.Drop(jpgFileName);

            MultiTagModelItemPicture multiItem = multiModel[FrameMeaning.Picture] as MultiTagModelItemPicture;
            UnitTest.Test(multiItem.DropTarget.SupportedTypes.Contains(DropTypes.Picture));
            UnitTest.Test(multiItem.DropTarget.AllowDrop(null));

            string pngFileName = VirtualDrive.VirtualFileName(@"Test_TagModelItemPicture_LoadPicture\test.png");
            VirtualDrive.Store(pngFileName, TestTags.demoPicturePng);
            History.Instance.ExecuteInTransaction(() => multiItem.DropTarget.Drop(pngFileName), 1, "Dummy");

            multiItem = multiModel[FrameMeaning.Picture] as MultiTagModelItemPicture;
            UnitTest.Test(!Object.ReferenceEquals(multiItem.Image, null));

            History.Instance.Undo();
            multiItem = multiModel[FrameMeaning.Picture] as MultiTagModelItemPicture;
            UnitTest.Test(!Object.ReferenceEquals(multiItem.Image, null));

            History.Instance.Redo();
            multiItem = multiModel[FrameMeaning.Picture] as MultiTagModelItemPicture;
            UnitTest.Test(!Object.ReferenceEquals(multiItem.Image, null));
        }

        static void Test_TagModelEditor_MultiThreaded()
        {
            CallbackQueue queue = new CallbackQueue();

            WorkerThreadPool.Instance.SingleThreaded = false;
            WorkerThreadPool.Instance.InvokingThread = queue;

            TagModelEditor editor = DocNode.Create<TagModelEditor>();
            History.Instance.Root = editor;

            editor.Dirty.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                DocObj<bool>.DocObjChangedEventArgs args =
                    e as DocObj<bool>.DocObjChangedEventArgs;

                if (args.OldValue == true && args.NewValue == false)
                {
                    queue.Exit();
                }
            };

            History.Instance.ExecuteInTransaction(() => editor.Path.Value = VirtualDrive.VirtualPrefix(), 0, "Dummy");

            queue.Run();

            WorkerThreadPool.Instance.SingleThreaded = true;
        }
        static void Test_TagModelEditor_SaveCommand()
        {
            TagModelEditor editor = DocNode.Create<TagModelEditor>();
            History.Instance.Root = editor;

            testFileNames.ForEach(n => History.Instance.ExecuteInTransaction(() => editor.TagModelList.Add(n), 0, "Dummy"));

            History.Instance.ExecuteInTransaction(() => editor.TagModelList.Items.ConvertToVersion(ID3.Version.v1_0), 1, "Dummy");

            editor.TagModelList.Items.ForEach(n => UnitTest.Test(n.Version.ValueVersion == ID3.Version.v1_0));

            History.Instance.ExecuteInTransaction(() => editor.SaveCommand.Execute(null), 2, "Dummy");

            foreach (var file in testFileNames)
            {
                UnitTest.Test(TagUtils.HasTagV1(new FileInfo(file)));
                UnitTest.Test(!TagUtils.HasTagV2(new FileInfo(file)));
            }

            History.Instance.Undo();

            foreach (var file in testFileNames)
            {
                UnitTest.Test(!TagUtils.HasTagV1(new FileInfo(file)));
                UnitTest.Test(TagUtils.HasTagV2(new FileInfo(file)));
            }
        }

        //static void Test_TagModelEditor_Performance()
        //{
        //    TagModelList tml = DocNode.Create<TagModelList>();
        //    History.Instance.Root = tml;

        //    MultiTagModel multiModel = DocNode.Create<MultiTagModel>();
        //    multiModel.IsFixed.Value = true;
        //    multiModel.TagModelList = tml;

        //    long totalMillis = 0;
        //    int numCalls = 0;

        //    DateTime startTime = DateTime.Now;

        //    for (int i = 0; i < 2000; i++)
        //    {
        //        History.Instance.ExecuteInTransaction(() => tml.SetFiles(
        //            ntestFileNames,
        //            i,
        //            "Dummy");

        //        DateTime stopTime = DateTime.Now;

        //        numCalls++;
        //        totalMillis += (long)(stopTime - startTime).TotalMilliseconds;
        //    }

        //    Console.WriteLine(
        //        "Average of " + ((double)totalMillis / (double)numCalls) + " ms");
        //}
    }
}
