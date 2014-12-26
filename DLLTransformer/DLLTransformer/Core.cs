using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using System.CodeDom;
using Microsoft.CSharp;
using System.IO;
using System.Linq;

namespace DLLTransformer
{
    public class Core
    {
        List<Assembly> assemblies;

        public Core()
        {
            assemblies = new List<Assembly>();

            string dllInputFolder = @"C:\Users\davidl01\Documents\visual studio 2010\Projects\DLLTransformer\DLLTransformer\OriginalDLLs\";
            string generateFolder = @"C:\DLLTransformer\Generated\";

            var dllFiles = Directory.GetFiles(dllInputFolder, "*.dll").ToArray();


            //give the dlls an order to avoid 'null reference' situation.
            List<string> OriginalDllList = new List<string>();
            OriginalDllList.Add("Keysight.CommandExpert.DataModel.dll");
            OriginalDllList.Add("Keysight.CommandExpert.Common.dll");
            OriginalDllList.Add("Keysight.CommandExpert.InstrumentAbstraction.dll");
            OriginalDllList.Add("Keysight.CommandExpert.SequenceExecution.dll");
            OriginalDllList.Add("Keysight.CommandExpert.Addons.dll");
            OriginalDllList.Add("Keysight.CommandExpert.Scpi.dll");

            foreach (var dllName in OriginalDllList)
            {
                System.Reflection.Assembly myDllAssembly = System.Reflection.Assembly.LoadFile(dllInputFolder+dllName);
                assemblies.Add(myDllAssembly);
            }

            foreach (var assembly in assemblies)
            {
                Reflector reflector = new Reflector(assembly);
                List<ClassTemplate> reflectedClasses = reflector.ReflectedClasses;
                Generator generator = new Generator(reflectedClasses, generateFolder);
                generator.ReferencedAssemblies = reflector.ReferencedAssemblies;
                generator.AssemblyName = assembly.GetName().Name.Replace("Keysight","Agilent");
                generator.GenerateDLL();
            }
        }
    }
}
