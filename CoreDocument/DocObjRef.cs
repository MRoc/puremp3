using System.Reflection;

namespace CoreDocument
{
    public class DocObjRef : System.Attribute
    {
        public static bool IsDocObjRef(MemberInfo t)
        {
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(t);

            foreach (System.Attribute attr in attrs)
            {
                if (attr is DocObjRef)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
