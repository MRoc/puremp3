using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using CoreTest;
using CoreUtils;

namespace CoreDocument
{
    public class TestCoreDocument
    {
        public static UnitTest Tests()
        {
            return new UnitTest(typeof(TestCoreDocument));
        }

        class TestDocNodeClassBase : DocNode
        {
            public TestDocNodeClassBase()
            {
                M0 = new DocNode();
                M1 = new DocNode();
            }

            public DocNode M0
            {
                get;
                private set;
            }
            private DocNode M1
            {
                get;
                set;
            }
        }
        class TestDocNodeClassDerived : TestDocNodeClassBase
        {
            public TestDocNodeClassDerived()
            {
                M2 = new DocNode();
                M3 = new DocNode();
            }

            public DocNode M2
            {
                get;
                private set;
            }
            private DocNode M3
            {
                get;
                set;
            }
        }
        public class TestSubDoc : DocNode
        {
            public TestSubDoc()
            {
                TestInt0 = new DocObj<int>(2);
                TestInt1 = new DocObj<int>(4);
            }

            public DocObj<int> TestInt0
            {
                get;
                private set;
            }
            public DocObj<int> TestInt1
            {
                get;
                private set;
            }
        }
        public class TestDocument : DocNode
        {
            public TestDocument()
            {
                TestList = new DocList<IDocLeaf>();
                TestList.Add(new DocObj<int>(0));
                TestList.Add(new DocObj<int>(1));
                TestList.Add(new DocObj<int>(2));

                TestBool = new DocObj<bool>(true);

                SubDoc = new TestSubDoc();

                TestRef = new DocObj<int>();                
            }

            public DocList<IDocLeaf> TestList
            {
                get;
                private set;
            }
            public DocObj<bool> TestBool
            {
                get;
                private set;
            }
            public TestSubDoc SubDoc
            {
                get;
                private set;
            }

            [DocObjRef]
            public DocObj<int> TestRef
            {
                get;
                private set;
            }
        }

        static void Test_DocObj_Int()
        {
            DocObj<int> obj = new DocObj<int>();

            obj.Value = 1;
            UnitTest.Test(obj.Value == 1);

            obj.Value = 2;
            UnitTest.Test(obj.Value == 2);
        }
        static void Test_DocObj_String()
        {
            DocObj<string> obj = new DocObj<string>();

            obj.Value = "Hello";
            UnitTest.Test(obj.Value == "Hello");

            obj.Value = "World";
            UnitTest.Test(obj.Value == "World");
        }
        static void Test_DocObj_ByteArray()
        {
            DocObj<byte[]> obj = new DocObj<byte[]>();

            byte[] arr0 = { 0, 1, 2 };
            obj.Value = arr0;
            UnitTest.Test(ArrayUtils.IsEqual(obj.Value, arr0));

            byte[] arr1 = { 3, 4, 5 };
            obj.Value = arr1;
            UnitTest.Test(ArrayUtils.IsEqual(obj.Value, arr1));
        }
        static void Test_DocEnum()
        {
            string[] myList = { "Hello", "World" };

            DocEnum myEnum = new DocEnum(myList);

            for (int i = 0; i < myList.Length; i++)
            {
                myEnum.Value = i;
                UnitTest.Test(myEnum.ValueStr == myList[i]);

                myEnum.ValueStr = myList[i];
                UnitTest.Test(myEnum.ValueStr == myList[i]);
            }

            myEnum.Value = DocEnum.multiple;
            UnitTest.Test(myEnum.IsMultiple);
            UnitTest.Test(myEnum.ValueStr == "*");

            myEnum.Value = DocEnum.undefined;
            UnitTest.Test(myEnum.IsUndefined);
            UnitTest.Test(myEnum.ValueStr == "");
        }
        static void Test_DocObj_Notifier()
        {
            DocObj<int> obj = new DocObj<int>();

            PropertyChangedTest pct = new PropertyChangedTest();
            obj.PropertyChanged += pct.PropertyChanged;

            obj.Value = 1;

            pct.TestWasCalledOnce();
        }

        static void TestDocBaseResolveParent()
        {
            IDocNode parent = new DocNode();
            IDocLeaf child = new DocBase();

            child.ResolveParentLink(parent, "child");

            UnitTest.Test(child.Parent == parent);
            UnitTest.Test(child.Name == "child");
        }
        static void TestDocNodeChildrenNames()
        {
            IDocNode t0 = DocNode.Create<TestDocNodeClassBase>();

            IList<string> childrenNames0 = t0.ChildrenNames() as IList<string>;
            UnitTest.Test(childrenNames0.Count == 2);
            UnitTest.Test(childrenNames0[0] == "M0");
            UnitTest.Test(childrenNames0[1] == "M1");

            IDocNode t1 = DocNode.Create<TestDocNodeClassDerived>();

            IList<string> childrenNames1 = t1.ChildrenNames() as IList<string>;
            UnitTest.Test(childrenNames1.Count == 4);
            UnitTest.Test(childrenNames1[0] == "M2");
            UnitTest.Test(childrenNames1[1] == "M3");
            UnitTest.Test(childrenNames1[2] == "M0");
            UnitTest.Test(childrenNames1[3] == "M1");
        }
        static void TestDocNodeResolveChildrenLinks()
        {
            IDocNode t0 = DocNode.Create<TestDocNodeClassBase>();
            UnitTest.Test(t0.Children().Count() == 2);
            foreach (IDocLeaf doc in t0.Children())
            {
                UnitTest.Test(doc.Parent == t0);
            }

            IDocNode t1 = DocNode.Create<TestDocNodeClassDerived>();
            UnitTest.Test(t1.Children().Count() == 4);
            foreach (IDocLeaf doc in t1.Children())
            {
                UnitTest.Test(doc.Parent == t1);
            }
        }
        static void TestDocListResolveChildrenLinks()
        {
            DocList<DocNode> parent = new DocList<DocNode>();

            parent.Add(DocNode.Create<TestDocNodeClassBase>());
            parent.Add(DocNode.Create<TestDocNodeClassBase>());
            parent.Add(DocNode.Create<TestDocNodeClassBase>());

            foreach (IDocLeaf doc in ((IDocNode)parent).Children())
            {
                UnitTest.Test(doc.Parent == parent);
            }
        }

        static void TestDocObjSerialization()
        {
            DocObj<int> obj0 = new DocObj<int>(777);
            DocObj<int> obj1 = SerializeDeserialize(obj0);

            UnitTest.Test(obj0.Value == obj1.Value);
        }
        static void TestSubDocSerialization()
        {
            TestSubDoc doc0 = DocNode.Create<TestSubDoc>();
            doc0.TestInt0.Value = 777;
            doc0.TestInt1.Value = 888;

            TestSubDoc doc1 = (TestSubDoc)SerializeDeserializeNode(doc0);

            UnitTest.Test(doc0.TestInt0.Value == doc1.TestInt0.Value);
            UnitTest.Test(doc0.TestInt1.Value == doc1.TestInt1.Value);

            PathUtils.CheckParentChildrenLink(doc1, null);
        }
        static void TestDocListSerialization()
        {
            DocList<IDocLeaf> list0 = new DocList<IDocLeaf>();
            list0.Add(new DocObj<int>(1));
            list0.Add(new DocObj<bool>(true));

            DocList<IDocLeaf> list1 = SerializeDeserialize(list0);
            UnitTest.Test(list1.Count == 2);
            UnitTest.Test((list1[0] as DocObj<int>).Value == 1);
            UnitTest.Test((list1[1] as DocObj<bool>).Value == true);

            PathUtils.CheckParentChildrenLink(list1, null);
        }
        static void TestDocListSerialization2()
        {
            DocList<IDocLeaf> list0 = new DocList<IDocLeaf>();
            DocList<IDocLeaf> list01 = new DocList<IDocLeaf>();
            list01.Add(new DocObj<int>(0));
            list01.Add(new DocObj<int>(1));
            list0.Add(list01);

            DocList<IDocLeaf> list02 = new DocList<IDocLeaf>();
            list02.Add(new DocObj<int>(0));
            list02.Add(new DocObj<int>(1));
            list0.Add(list02);

            DocList<IDocLeaf> list1 = SerializeDeserialize(list0);
            UnitTest.Test(list1.Count == 2);
            UnitTest.Test((list1[0] as DocList<IDocLeaf>).Count == 2);
            UnitTest.Test((list1[1] as DocList<IDocLeaf>).Count == 2);

            PathUtils.CheckParentChildrenLink(list1, null);
        }
        static void TestTreeSerialization()
        {
            TestDocument doc0 = DocNode.Create<TestDocument>();
            TestDocument doc1 = (TestDocument)SerializeDeserializeNode(doc0);

            PathUtils.CheckParentChildrenLink(doc1, null);
        }
        static void TestDocPropertyUtils()
        {
            TestDocument document = DocNode.Create<TestDocument>();

            string[] names = PropertyUtils.NamesByType(document.GetType());

            UnitTest.Test(names.Length == 3);
            UnitTest.Test(names[0] == "TestList");
            UnitTest.Test(names[1] == "TestBool");
            UnitTest.Test(names[2] == "SubDoc");
        }
        static void TestDocDocObjRef()
        {
            TestDocument document = DocNode.Create<TestDocument>();

            string[] names = FieldUtils.NamesByType(document.GetType());

            foreach (string name in names)
            {
                UnitTest.Test(name != "testRef");
            }
        }
        static void TestHistoryBasics()
        {
            DocList<IDocLeaf> testDoc = new DocList<IDocLeaf>();
            History.Instance.Root = testDoc;
            UnitTest.Test(History.Instance.HasUndo == false);
            UnitTest.Test(History.Instance.HasRedo == false);

            DocObj<int> testInt = new DocObj<int>(0);
            testDoc.Add(testInt);
            UnitTest.Test(History.Instance.HasUndo == true);
            UnitTest.Test(History.Instance.HasRedo == false);

            testInt.Value = 1;
            UnitTest.Test(testInt.Value == 1);
            testInt.Value = 2;
            UnitTest.Test(testInt.Value == 2);
            testInt.Value = 3;
            UnitTest.Test(testInt.Value == 3);

            History.Instance.Undo();
            UnitTest.Test(testInt.Value == 0);
            UnitTest.Test(History.Instance.HasRedo == true);
            History.Instance.Redo();
            UnitTest.Test(testInt.Value == 3);
        }
        static void TestListUndoRedo()
        {
            DocList<DocObj<int>> testDoc = new DocList<DocObj<int>>();
            History.Instance.Root = testDoc;

            History.Instance.ExecuteInTransaction(delegate()
            {
                testDoc.Add(new DocObj<int>(1));
                UnitTest.Test(testDoc.Count == 1);
                testDoc.Add(new DocObj<int>(2));
                UnitTest.Test(testDoc.Count == 2);
                testDoc.Add(new DocObj<int>(3));
                UnitTest.Test(testDoc.Count == 3);
            }, History.Instance.NextFreeTransactionId(), "Dummy");

            UnitTest.Test(testDoc[0].Value == 1);
            UnitTest.Test(testDoc[1].Value == 2);
            UnitTest.Test(testDoc[2].Value == 3);

            History.Instance.Undo();
            UnitTest.Test(testDoc.Count == 0);

            History.Instance.Redo();
            UnitTest.Test(testDoc.Count == 3);
            UnitTest.Test(testDoc[0].Value == 1);
            UnitTest.Test(testDoc[1].Value == 2);
            UnitTest.Test(testDoc[2].Value == 3);

            testDoc.RemoveAt(0);
            UnitTest.Test(testDoc.Count == 2);
            UnitTest.Test(testDoc[0].Value == 2);
            UnitTest.Test(testDoc[1].Value == 3);

            testDoc.Clear();
            UnitTest.Test(testDoc.Count == 0);
        }
        static void TestPathBasics()
        {
            IDocLeaf doc = DocNode.Create<TestDocument>();

            string pathToMyInt = "TestList.2";
            DocObj<int> myInt = PathUtils.ChildByPath<DocObj<int>>(
                doc as IDocNode, pathToMyInt);
            UnitTest.Test(myInt.Value == 2);
            UnitTest.Test(PathUtils.PathByChild(myInt) == pathToMyInt);

            string pathToMyBool = "TestBool";
            DocObj<bool> myBool = PathUtils.ChildByPath<DocObj<bool>>(
                doc as IDocNode, pathToMyBool);
            UnitTest.Test(myBool.Value == true);
            UnitTest.Test(PathUtils.PathByChild(myBool) == pathToMyBool);

            string pathToMyInt2 = "SubDoc.TestInt0";
            DocObj<int> myInt2 = PathUtils.ChildByPath<DocObj<int>>(
                doc as IDocNode, pathToMyInt2);
            UnitTest.Test(myInt2.Value == 2);
            UnitTest.Test(PathUtils.PathByChild(myInt2) == pathToMyInt2);
        }

        static void TestHistoryTransactions()
        {
            DocList<IDocLeaf> testDoc = new DocList<IDocLeaf>();
            History.Instance.Root = testDoc;
            UnitTest.Test(History.Instance.HasUndo == false);
            UnitTest.Test(History.Instance.HasRedo == false);

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    DocObj<int> testInt0 = new DocObj<int>(0);
                    testDoc.Add(testInt0);
                    UnitTest.Test(History.Instance.HasUndo == false);
                    UnitTest.Test(History.Instance.HasRedo == false);

                    DocObj<int> testInt1 = new DocObj<int>(1);
                    testDoc.Add(testInt1);
                    UnitTest.Test(History.Instance.HasUndo == false);
                    UnitTest.Test(History.Instance.HasRedo == false);
                },
                0,
                "TestHistoryTransactions");

            UnitTest.Test(testDoc.Count == 2);
            UnitTest.Test(History.Instance.HasUndo == true);
            UnitTest.Test(History.Instance.HasRedo == false);

            History.Instance.Undo();
            UnitTest.Test(testDoc.Count == 0);
            UnitTest.Test(History.Instance.HasUndo == false);
            UnitTest.Test(History.Instance.HasRedo == true);

            History.Instance.Redo();
            UnitTest.Test(testDoc.Count == 2);
            UnitTest.Test(History.Instance.HasUndo == true);
            UnitTest.Test(History.Instance.HasRedo == false);
        }
        static void TestHistoryRestoreTransactions()
        {
            DocList<IDocLeaf> testDoc = new DocList<IDocLeaf>();
            History.Instance.Root = testDoc;
            UnitTest.Test(History.Instance.HasUndo == false);
            UnitTest.Test(History.Instance.HasRedo == false);

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    DocObj<int> testInt0 = new DocObj<int>(0);
                    testDoc.Add(testInt0);
                },
                0,
                "TestHistoryRestoreTransactions");
            UnitTest.Test(testDoc.Count == 1);
            UnitTest.Test(History.Instance.HasUndo == true);
            UnitTest.Test(History.Instance.HasRedo == false);

            History.Instance.ExecuteInTransaction(
                delegate()
                {
                    DocObj<int> testInt1 = new DocObj<int>(0);
                    testDoc.Add(testInt1);
                },
                0,
                "TestHistoryRestoreTransactions");
            UnitTest.Test(testDoc.Count == 2);
            UnitTest.Test(History.Instance.HasUndo == true);
            UnitTest.Test(History.Instance.HasRedo == false);

            History.Instance.Undo();
            UnitTest.Test(testDoc.Count == 0);
            UnitTest.Test(History.Instance.HasUndo == false);
            UnitTest.Test(History.Instance.HasRedo == true);
        }
        static void TestHistoryRollback()
        {
            DocList<DocObj<int>> testDoc = new DocList<DocObj<int>>();
            testDoc.Add(new DocObj<int>(0));
            testDoc.Add(new DocObj<int>(1));

            History.Instance.Root = testDoc;

            bool exceptionCaught = false;

            try
            {
                History.Instance.ExecuteInTransaction(
                    delegate()
                    {
                        testDoc[0].Value = -1;
                        testDoc[1].Value = -2;

                        UnitTest.Test(testDoc[0].Value == -1);
                        UnitTest.Test(testDoc[1].Value == -2);

                        throw new Exception("Hello World");
                    },
                    0,
                    "TestHistoryRollback");
            }
            catch (Exception ex)
            {
                exceptionCaught = true;
                UnitTest.Test(ex.Message == "Hello World");
            }

            UnitTest.Test(exceptionCaught);
            UnitTest.Test(testDoc[0].Value == 0);
            UnitTest.Test(testDoc[1].Value == 1);

            UnitTest.Test(History.Instance.HasUndo == false);
            UnitTest.Test(History.Instance.HasRedo == false);
        }

        static void TestDocListHooks()
        {
            DocList<IDocLeaf> list = new DocList<IDocLeaf>();
            list.Hook += TestDocListHook;

            DocObj<string> obj0 = new DocObj<string>("Hello World");
            DocObj<string> obj1 = new DocObj<string>("Hello World");

            list.Add(obj0);
            list.Remove(obj0);
            list.Add(obj0);
            list.Add(obj1);
            list.Clear();
            list.Clear();
        }
        static void TestDocListHook(object sender, EventArgs e)
        {
            NotifyCollectionChangedEventArgs args =
                (NotifyCollectionChangedEventArgs)e;

            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                UnitTest.Test(args.NewItems != null && args.NewItems.Count > 0);
                UnitTest.Test(args.OldItems == null);
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                UnitTest.Test(args.OldItems != null && args.OldItems.Count > 0);
                UnitTest.Test(args.NewItems == null);
            }
            else
            {
                UnitTest.Test(false);
            }
        }

        static void TestDocListCollectionChanged()
        {
            DocList<IDocLeaf> list = new DocList<IDocLeaf>();
            list.CollectionChanged += TestDocListOnCollectionChanged;
            ((INotifyPropertyChanged)list).PropertyChanged += TestDocListOnCollectionChanged2;

            DocObj<string> obj0 = new DocObj<string>("Hello World");
            DocObj<string> obj1 = new DocObj<string>("Hello World");

            list.Add(obj0);
            list.Remove(obj0);
            list.Add(obj0);
            list.Add(obj1);
            list.Clear();
            list.Clear();
        }
        static void TestDocListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UnitTest.Test(e.Action == NotifyCollectionChangedAction.Add
                || e.Action == NotifyCollectionChangedAction.Remove);

            UnitTest.Test((e.OldItems == null && e.NewItems != null)
                || (e.OldItems != null && e.NewItems == null));
        }
        static void TestDocListOnCollectionChanged2(
            Object sender,
            PropertyChangedEventArgs e)
        {
        }
        static void TestDocListTransaction()
        {
            DocList<IDocLeaf> list = new DocList<IDocLeaf>();
            History.Instance.Root = list;

            list.Transaction.Value += 1;

            list.Add(new DocObj<string>("Hello World"));

            list.Transaction.Value -= 1;
        }

        private static DocNode SerializeDeserializeNode(DocNode obj0)
        {
            byte[] binary;
            using (MemoryStream stream = new MemoryStream())
            {
                XmlUtils.Save(obj0, stream);
                binary = stream.ToArray();
            }

            DocNode obj1 = DocNode.Create(obj0.GetType());
            using (MemoryStream stream = new MemoryStream(binary))
            {
                XmlUtils.Load(obj1, stream);
            }

            return obj1;
        }
        private static T SerializeDeserialize<T>(T obj0) where T : IXml, new()
        {
            byte[] binary;
            using (MemoryStream stream = new MemoryStream())
            {
                XmlUtils.Save(obj0, stream);
                binary = stream.ToArray();
            }

            T obj1 = new T();
            using (MemoryStream stream = new MemoryStream(binary))
            {
                obj1 = XmlUtils.SafeLoad<T>(stream);
            }

            return obj1;
        }
    }
}
