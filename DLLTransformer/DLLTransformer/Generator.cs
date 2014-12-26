using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.CodeDom;
using Microsoft.CSharp;
using System.IO;
using System.Reflection;

namespace DLLTransformer
{
    public class Generator
    {
        public List<ClassTemplate> ReflectedClasses { get; set; }
        public string AssemblyName { get; set; }
        public string GenerateFolder { get; set; }
        public List<AssemblyName> ReferencedAssemblies { get; set; }
        public Generator(List<ClassTemplate> reflectedClasses, string GenerateFolderPath)
        {
            ReflectedClasses = reflectedClasses;
            GenerateFolder = GenerateFolderPath;
        }

        CodeCompileUnit myassembly;
        CodeNamespace mynamespace;
        string outputClassesPath;
        public bool IsDelegate(Type type)
        {
            return type.IsSubclassOf(typeof(Delegate)) || type == typeof(Delegate);
        }
        public void GenerateDLL()
        {
            mynamespace = new CodeNamespace(AssemblyName);

            outputClassesPath = GenerateFolder + AssemblyName + "\\";
            if (!Directory.Exists(outputClassesPath))
            {
                Directory.CreateDirectory(outputClassesPath);
            }
            foreach (ClassTemplate c in ReflectedClasses)
            {
                if (c.ClassType.IsEnum)
                {
                    CreateEnum(c);
                }
                if (IsDelegate(c.ClassType))
                {
                    CreateDelegate(c);
                }
                if (c.ClassType.IsInterface)
                {
                    CreateInterface(c);
                }

                if (c.ClassType.IsClass)
                {
                    CreateClass(c);
                }

            }

           SaveAssembly();
        }

        public void CreateEnum(ClassTemplate c)
        {
            string newClassnamespace = c.ClassNamespace.Replace("Keysight", "Agilent");
            CodeCompileUnit classUnit = new CodeCompileUnit();
            CodeNamespace classNamespace = new CodeNamespace(newClassnamespace);

            CodeTypeDeclaration myEnum;
            myEnum = new CodeTypeDeclaration();
            myEnum.Name = c.ClassName;
            myEnum.IsEnum = true;
            myEnum.Attributes = MemberAttributes.Public;
            mynamespace.Types.Add(myEnum);

            classNamespace.Types.Add(myEnum);
            classUnit.Namespaces.Add(classNamespace);

            foreach (var enumMember in c.ClassType.GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                // Creates the enum member
                CodeMemberField f = new CodeMemberField(c.ClassType.Name, enumMember.Name);
                myEnum.Members.Add(f);
            }

            //generate .cs file into the folder
            using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                string fileName = outputClassesPath + c.ClassName + ".cs";
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";
                using (StreamWriter sourceWriter = new StreamWriter(fileName))
                {
                    provider.GenerateCodeFromCompileUnit(
                        classUnit, sourceWriter, options);
                }
            }
        }

        public void CreateDelegate(ClassTemplate c)
        {
            string newClassnamespace = c.ClassNamespace.Replace("Keysight", "Agilent");
            CodeCompileUnit classUnit = new CodeCompileUnit();
            CodeNamespace classNamespace = new CodeNamespace(newClassnamespace);

            CodeTypeDelegate myDelegate = new CodeTypeDelegate();
            myDelegate.Name = c.ClassName;
            //delegate1.Parameters.Add(new CodeParameterDeclarationExpression("System.Object", "sender"));
            //delegate1.Parameters.Add(new CodeParameterDeclarationExpression("System.EventArgs", "e"));

            MethodInfo method = c.ClassType.GetMethod("Invoke");
            myDelegate.ReturnType = new CodeTypeReference(method.ReflectedType);
            foreach (ParameterInfo param in method.GetParameters())
            {
                myDelegate.Parameters.Add(new CodeParameterDeclarationExpression(param.ParameterType.FullName.Replace("Keysight", "Agilent"), param.Name));
            }

            //generate .cs file into the folder
            using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                string fileName = outputClassesPath + c.ClassName + ".cs";
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";
                using (StreamWriter sourceWriter = new StreamWriter(fileName))
                {
                    provider.GenerateCodeFromCompileUnit(
                        classUnit, sourceWriter, options);
                }
            }
        }


        public void CreateInterface(ClassTemplate c)
        {
            string newClassnamespace = c.ClassNamespace.Replace("Keysight", "Agilent");
            CodeCompileUnit classUnit = new CodeCompileUnit();
            CodeNamespace classNamespace = new CodeNamespace(newClassnamespace);

            CodeTypeDeclaration myInterface;
            myInterface = new CodeTypeDeclaration();
            myInterface.Name = c.ClassName;
            myInterface.IsInterface = true;
            myInterface.Attributes = MemberAttributes.Public;
            mynamespace.Types.Add(myInterface);

            classNamespace.Types.Add(myInterface);
            classUnit.Namespaces.Add(classNamespace);

            foreach (var interfaceMember in c.ClassType.GetMembers(BindingFlags.FlattenHierarchy
                | BindingFlags.Public
                | BindingFlags.Instance))
            {

                CodeTypeMember f = null;
                //Handle methods in interface
                if (interfaceMember.MemberType == MemberTypes.Method)
                {
                    MethodInfo iMember = interfaceMember as MethodInfo;
                    if (!iMember.IsSpecialName)
                    {
                        f = new CodeMemberMethod();
                        f.Name = iMember.Name;
                        CodeTypeReference ctr = new CodeTypeReference(iMember.ReturnType.FullName.Replace("Keysight", "Agilent"));
                        ((CodeMemberMethod)f).ReturnType = ctr;
                        foreach (ParameterInfo p in iMember.GetParameters())
                        {
                            CodeParameterDeclarationExpression pExpress = new CodeParameterDeclarationExpression(p.ParameterType.FullName.Replace("Keysight", "Agilent").Replace("&",""), p.Name);
                        
                            if (p.ParameterType.IsByRef)
                            {
                                pExpress.Direction = FieldDirection.Ref;
                            }
                            if (p.IsOut)
                            {
                                pExpress.Direction = FieldDirection.Out;
                            }
                            if (p.IsIn)
                            {
                                pExpress.Direction = FieldDirection.In;

                            }
                            ((CodeMemberMethod)f).Parameters.Add(pExpress);
                        }
                    }
                }

                //Handle properties in interface
                if (interfaceMember.MemberType == MemberTypes.Property)
                {
                    PropertyInfo iMember = interfaceMember as PropertyInfo;
                    f = new CodeMemberProperty();
                    f.Name = interfaceMember.Name;
                    CodeTypeReference ctr = new CodeTypeReference(iMember.PropertyType.FullName.Replace("Keysight", "Agilent"));
                    ((CodeMemberProperty)f).Type = ctr;

                    string strGetSet = "";
                    if (iMember.CanRead)
                    {
                        strGetSet = "get; ";
                        CodeSnippetStatement snippet = new CodeSnippetStatement(strGetSet);
                        ((CodeMemberProperty)f).GetStatements.Add(snippet);
                    }
                    if (iMember.CanWrite)
                    {
                        strGetSet = "set;";
                        CodeSnippetStatement snippet = new CodeSnippetStatement(strGetSet);
                        ((CodeMemberProperty)f).SetStatements.Add(snippet);
                    }
                }


                //Handle events in interface
                if (interfaceMember.MemberType == MemberTypes.Event)
                {
                    EventInfo iMember = interfaceMember as EventInfo;
                    f = new CodeMemberEvent();
                    f.Name = interfaceMember.Name;
                    CodeTypeReference ctr = new CodeTypeReference(iMember.EventHandlerType.FullName.Replace("Keysight", "Agilent"));
                    ((CodeMemberEvent)f).Type = ctr;
                    ((CodeMemberEvent)f).Attributes = MemberAttributes.Static;
                }

                if (f != null)
                {
                    myInterface.Members.Add(f);
                }
            }

            //generate .cs file into the folder
            using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                string fileName = outputClassesPath + c.ClassName + ".cs";
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";
                using (StreamWriter sourceWriter = new StreamWriter(fileName))
                {
                    provider.GenerateCodeFromCompileUnit(
                        classUnit, sourceWriter, options);
                }
            }
        }

        public void CreateClass(ClassTemplate c)
        {
            string newClassnamespace = c.ClassNamespace.Replace("Keysight", "Agilent");
            CodeCompileUnit classUnit = new CodeCompileUnit();
            CodeNamespace classNamespace = new CodeNamespace(newClassnamespace);

            CodeTypeDeclaration myclass;
            myclass = new CodeTypeDeclaration();
            myclass.Name = c.ClassName;
            myclass.IsClass = true;
            myclass.Attributes = MemberAttributes.Public;
            mynamespace.Types.Add(myclass);

            classNamespace.Types.Add(myclass);
            classUnit.Namespaces.Add(classNamespace);

            CreateOriginalInstance(c, ref myclass);
            CreateConstructor(c, ref myclass);
            CreateMemberField(c, ref myclass);
            CreateProperty(c, ref myclass);


            //generate .cs file into the folder
            using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                string fileName = outputClassesPath + c.ClassName + ".cs";
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";
                using (StreamWriter sourceWriter = new StreamWriter(fileName))
                {
                    provider.GenerateCodeFromCompileUnit(
                        classUnit, sourceWriter, options);
                }
            }
        }

        public void SaveAssembly()
        {
            myassembly = new CodeCompileUnit();
            myassembly.Namespaces.Add(mynamespace);
            CompilerParameters compparams = new CompilerParameters();
            compparams.ReferencedAssemblies.Add("System.dll");
            compparams.ReferencedAssemblies.Add(@"C:\Users\davidl01\Documents\visual studio 2010\Projects\DLLTransformer\DLLTransformer\OriginalDLLs\" + AssemblyName.Replace("Agilent", "Keysight") + ".dll");
            //References other:
            foreach (AssemblyName assem in ReferencedAssemblies)
            {
                if (assem.FullName.Contains("Keysight"))
                {
                    compparams.ReferencedAssemblies.Add(@"C:\Users\davidl01\Documents\visual studio 2010\Projects\DLLTransformer\DLLTransformer\OriginalDLLs\" + assem.Name + ".dll");
                }
            }

            compparams.GenerateInMemory = false;
            compparams.GenerateExecutable = false;

            compparams.OutputAssembly = GenerateFolder + AssemblyName + ".dll";
            Microsoft.CSharp.CSharpCodeProvider csharp =
            new Microsoft.CSharp.CSharpCodeProvider();


            var settings = new Dictionary<string, string>();
            settings.Add("CompilerVersion", "v3.5");

            using (var provider = new CSharpCodeProvider(settings))
            {
                // To debug generated code, uncomment below lines.
                //FileInfo fileInfo = new FileInfo(GenerateFolder);
                //compparams.IncludeDebugInformation = true;
                //compparams.TempFiles = new TempFileCollection(fileInfo.Directory.ToString(), true);
                //compparams.TempFiles.KeepFiles = true;

                // provider.CompileAssemblyFromDom(compparams, myassembly);
                string[] generatedCSfiles = Directory.GetFiles(outputClassesPath, "*.cs").ToArray();
                CompilerResults compresult = provider.CompileAssemblyFromFile(compparams, generatedCSfiles);

                if (compresult == null || compresult.Errors.Count > 0)
                {
                    for (int i = 0; i < compresult.Errors.Count; i++)
                    {
                        Console.WriteLine(compresult.Errors[i]);
                    }
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }
        }

        public void CreateOriginalInstance(ClassTemplate c, ref CodeTypeDeclaration myclass)
        {
            if (!c.ClassType.IsAbstract)
            {
                CodeMemberField mymemberfield =
                     new CodeMemberField(c.ClassType, c.ClassName + "_Origin");
                mymemberfield.Attributes = MemberAttributes.Private;
                myclass.Members.Add(mymemberfield);
            }
        }

        public void CreateConstructor(ClassTemplate c, ref CodeTypeDeclaration myclass)
        {
            List<MemberInfo> constructors = c.Constructors;

            foreach (ConstructorInfo con in constructors)
            {
                CodeConstructor constructor = new CodeConstructor();
                constructor.Attributes = MemberAttributes.Public;
                ParameterInfo[] conParams = con.GetParameters();
                if (conParams.Count() > 0)
                {
                    List<CodeExpression> paramExpress = new List<CodeExpression>();
                    foreach (ParameterInfo p in conParams)
                    {
                        constructor.Parameters.Add(new CodeParameterDeclarationExpression(
                            p.ParameterType, p.Name));

                        paramExpress.Add(new CodeVariableReferenceExpression(p.Name));
                    }

                    CodeExpression thisExpr = new CodeThisReferenceExpression();
                    CodeStatement initializeOriginObject = new CodeAssignStatement(
                        new CodeFieldReferenceExpression(thisExpr, c.ClassName + "_Origin"), new CodeObjectCreateExpression(c.ClassType, paramExpress.ToArray()));
                    constructor.Statements.Add(initializeOriginObject);
                }
                else
                {
                    if (!c.ClassType.IsAbstract)
                    {
                        CodeExpression thisExpr = new CodeThisReferenceExpression();
                        CodeStatement abc = new CodeAssignStatement(
                            new CodeFieldReferenceExpression(thisExpr, c.ClassName + "_Origin"), new CodeObjectCreateExpression(c.ClassType));
                        constructor.Statements.Add(abc);
                    }
                }

                //initialize fields of non-static class
                if (!(c.ClassType.IsAbstract && c.ClassType.IsSealed))//What C# calls a static class, is an abstract, sealed class to the CLR. 
                {
                    foreach (FieldInfo field in c.Fields)
                    {
                        CodeExpression thisExpr = new CodeThisReferenceExpression();
                        if ((field.IsLiteral && !field.IsInitOnly) || field.IsStatic)
                        {
                            CodeExpression originType = new CodeTypeReferenceExpression(c.ClassType);
                            CodeStatement initializeOriginObject = new CodeAssignStatement(
                                new CodeFieldReferenceExpression(thisExpr, field.Name), new CodeFieldReferenceExpression(originType, field.Name));
                            constructor.Statements.Add(initializeOriginObject);
                        }
                        else
                        {
                            CodeFieldReferenceExpression refOrigin = new CodeFieldReferenceExpression(thisExpr, c.ClassName + "_Origin");
                            CodeStatement initializeOriginObject = new CodeAssignStatement(
                           new CodeFieldReferenceExpression(thisExpr, field.Name), new CodeFieldReferenceExpression(refOrigin, field.Name));
                            constructor.Statements.Add(initializeOriginObject);
                        }
                    }
                }
                myclass.Members.Add(constructor);
            }
        }

        public void CreateMemberField(ClassTemplate c, ref CodeTypeDeclaration myclass)
        {
            foreach (FieldInfo field in c.Fields)
            {
                CodeMemberField mymemberfield =
                new CodeMemberField((field.FieldType), field.Name);
                mymemberfield.Attributes = MemberAttributes.Public;
                if (c.ClassType.IsAbstract && c.ClassType.IsSealed)//What C# calls a static class, is an abstract, sealed class to the CLR. 
                {
                    CodeExpression originType = new CodeTypeReferenceExpression(c.ClassType);
                    mymemberfield.InitExpression = new CodeFieldReferenceExpression(originType, field.Name);
                }
                myclass.Members.Add(mymemberfield);
            }
        }

        public void CreateProperty(ClassTemplate c, ref CodeTypeDeclaration myclass)
        {
            foreach (PropertyInfo property in c.Properties)
            {
                if (!property.PropertyType.ContainsGenericParameters)
                {
                    CodeMemberProperty myproperty = new CodeMemberProperty();
                    CodeTypeReference ctr = new CodeTypeReference(property.PropertyType.FullName.Replace("Keysight", "Agilent"));
                    myproperty.Type = ctr;
                    myproperty.Name = property.Name;
                    if (property.PropertyType.IsPublic)
                    {
                        myproperty.Attributes = MemberAttributes.Public;
                    }

                    if (property.CanRead)
                    {
                        CodeExpression thisExpr = new CodeThisReferenceExpression();
                        CodeFieldReferenceExpression refOrigin = new CodeFieldReferenceExpression(thisExpr, c.ClassName + "_Origin");
                        CodePropertyReferenceExpression refOriginProp = new CodePropertyReferenceExpression(refOrigin, property.Name);
                        CodeMethodReturnStatement getsnippet = new CodeMethodReturnStatement(refOriginProp);
                        myproperty.GetStatements.Add(getsnippet);
                    }
                    if (property.CanWrite)
                    {
                        //CodeSnippetExpression setsnippet =
                        //new CodeSnippetExpression(c.ClassNamespace + "." + c.ClassName + "." + property.Name + "value");
                        CodeExpression thisExpr = new CodeThisReferenceExpression();
                        CodeFieldReferenceExpression refOrigin = new CodeFieldReferenceExpression(thisExpr, c.ClassName + "_Origin");
                      //  CodePropertyReferenceExpression refOriginProp = new CodePropertyReferenceExpression(refOrigin, property.Name);

                        CodeStatement setsnippet = new CodeAssignStatement(
                                  new CodeFieldReferenceExpression(refOrigin, property.Name), new CodeSnippetExpression("value"));
                        myproperty.SetStatements.Add(setsnippet);
                    }
                    myclass.Members.Add(myproperty);
                }
                else
                {

                }
            }
        }


        //public void CreateMethod()
        //{
        //    CodeMemberMethod mymethod = new CodeMemberMethod();
        //    mymethod.Name = "AddNumbers";
        //    CodeParameterDeclarationExpression cpd1 =
        //    new CodeParameterDeclarationExpression(typeof(int), "a");
        //    CodeParameterDeclarationExpression cpd2 =
        //    new CodeParameterDeclarationExpression(typeof(int), "b");
        //    mymethod.Parameters.Add(cpd1);
        //    mymethod.Parameters.Add(cpd2);
        //    CodeTypeReference ctr =
        //    new CodeTypeReference("System.Int32");
        //    mymethod.ReturnType = ctr;
        //    CodeSnippetExpression snippet1 =
        //    new CodeSnippetExpression("System.Console.WriteLine(\"Adding :\" + a + \" And \" + b )");
        //    CodeSnippetExpression snippet2 =
        //    new CodeSnippetExpression("return a+b");
        //    CodeExpressionStatement stmt1 =
        //    new CodeExpressionStatement(snippet1);
        //    CodeExpressionStatement stmt2 =
        //    new CodeExpressionStatement(snippet2);
        //    mymethod.Statements.Add(stmt1);
        //    mymethod.Statements.Add(stmt2);
        //    mymethod.Attributes = MemberAttributes.Public;
        //    myclass.Members.Add(mymethod);
        //}

    }
}
