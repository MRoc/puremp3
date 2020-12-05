using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace CoreTest
{
    public class UnitTest
    {
        public UnitTest(Type type)
        {
            Test(type);
        }
        public UnitTest(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                Test(type);
            }
        }

        private void Test(Type type)
        {
            Console.WriteLine("UnitTest: " + type.Namespace + "." + type.Name);

            CallFunctionsWhereNamesBeginWith(type, "Init");
            CallFunctionsWhereNamesBeginWith(type, "Test");
            CallFunctionsWhereNamesBeginWith(type, "Exit");
        }

        private void CallFunctionsWhereNamesBeginWith(Type type, string name)
        {
            IEnumerable<MethodInfo> methods =
                from
                    method
                in
                    type.GetMethods(System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic)
                where
                    method.Name.StartsWith(name)
                    && method.ReturnType == typeof(void)
                    && method.GetParameters().Length == 0
                select
                    method;

            foreach (var i in methods)
            {
                Console.WriteLine("  " + i.Name);
                i.Invoke(null, new object[] { });
            }
        }

        public static void Test(bool value)
        {
            if (!value)
            {
                throw new Exception("Test failed");
            }
        }
        public static void TestException(Action t, Type exception)
        {
            try
            {
                t();
            }
            catch (Exception e)
            {
                if (e.GetType() != exception)
                {
                    throw e;
                }
                else
                {
                    return;
                }
            }

            Test(false);
        }
    }
}
