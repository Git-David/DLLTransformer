using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace DLLTransformer
{
    public class Reflector
    {
        public List<ClassTemplate> ReflectedClasses { get; set; }
        public List<AssemblyName> ReferencedAssemblies { get; set; }
        public Reflector(Assembly myAssembly)
        {
            ReflectedClasses = new List<ClassTemplate>();
            ReflectTypesFromAssembly(myAssembly);
        }

        public void ReflectTypesFromAssembly(Assembly myAssembly)
        {
            var Classes=myAssembly.GetExportedTypes();
            ReferencedAssemblies=myAssembly.GetReferencedAssemblies().ToList();
            foreach (Type c in Classes)
            {
                if (IsDelegate(c))
                {
                    //TODO: handle delegates.
                    return;
                }
                ClassTemplate myClass = new ClassTemplate(); 
                const BindingFlags bf = BindingFlags.DeclaredOnly | BindingFlags.Public |
                   BindingFlags.Instance | BindingFlags.Static;
                
                myClass.ClassName = c.Name;
                myClass.ClassNamespace= c.Namespace;
                myClass.ClassType = c;
                foreach (MemberInfo mi in c.GetMembers(bf))
                {
                    String typeName = String.Empty;
                    if (mi is Type)
                    {
                        myClass.NestedTypes.Add(mi);
                    }
                    if (mi is FieldInfo)
                    {
                        myClass.Fields.Add(mi);
                    }
                    if (mi is MethodInfo)
                    {
                        myClass.Methods.Add(mi);
                    }
                    if (mi is ConstructorInfo)
                    {
                        myClass.Constructors.Add(mi);
                    }
                    if (mi is PropertyInfo)
                    {
                        myClass.Properties.Add(mi);
                    }
                    if (mi is EventInfo)
                    {
                        myClass.Events.Add(mi);
                    }
                }
                ReflectedClasses.Add(myClass);
            }
        }

        public bool IsDelegate(Type type)
        {
          
            return type.IsSubclassOf(typeof(Delegate)) || type == typeof(Delegate);
        }
    }
}
