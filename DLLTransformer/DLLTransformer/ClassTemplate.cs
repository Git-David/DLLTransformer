using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace DLLTransformer
{
    public class ClassTemplate
    {
        public ClassTemplate()
        {
            Constructors = new List<MemberInfo>();
            NestedTypes = new List<MemberInfo>();
            Fields = new List<MemberInfo>();
            Methods = new List<MemberInfo>();
            Properties = new List<MemberInfo>();
            Events = new List<MemberInfo>();

        }
        public string ClassName { get; set; }
        public string ClassNamespace { get; set; }
        public Type ClassType { get; set; }
        public List<MemberInfo> Constructors { get; set; }
        public List<MemberInfo> NestedTypes { get; set; }
        public List<MemberInfo> Fields { get; set; }
        public List<MemberInfo> Methods { get; set; }
        public List<MemberInfo> Properties { get; set; }
        public List<MemberInfo> Events { get; set; }
    }
}
