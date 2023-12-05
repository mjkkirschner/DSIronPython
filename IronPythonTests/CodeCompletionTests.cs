﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dynamo.Logging;
using Dynamo.PythonServices;
using Dynamo.Utilities;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronPythonTests
{
    /*//TODO - this should be tested on Dynamo side? Or will need to be exposed to third parties somehow...
    [TestFixture]
    internal class SharedCodeCompletionProviderTests : CodeCompletionTests
    {
        [Test]
        public void SharedCoreCanFindLoadedProviders()
        {
            var provider = new SharedCompletionProvider(PythonNodeModels.PythonEngineVersion.IronPython2, "");
            Assert.IsNotNull(provider);
        }

        [Test]
        public void SharedCoreCanReturnCLRCompletionData()
        {
            var provider = new SharedCompletionProvider(PythonNodeModels.PythonEngineVersion.IronPython2, "");
            Assert.IsNotNull(provider);
            var str = "\nimport System.Collections\nSystem.Collections.";

            var completionData = provider.GetCompletionData(str);
            var completionList = completionData.Select(d => d.Text);
            Assert.IsTrue(completionList.Any());
            Assert.IsTrue(completionList.Intersect(new[] { "Hashtable", "Queue", "Stack" }).Count() == 3);
            Assert.AreEqual(29, completionData.Length);
        }
    }
    */
    [TestFixture]
    internal class CodeCompletionTests
    {
        private AssemblyHelper assemblyHelper;

        private class SimpleLogger : ILogger
        {

            public string LogPath
            {
                get { return ""; }
            }

            public void Log(string message)
            {
                
            }

            public void Log(string message, LogLevel level)
            {
                
            }

            public void Log(string tag, string message)
            {
                
            }

            public void LogError(string error)
            {
                
            }

            public void LogWarning(string warning, WarningLevel level)
            {
                
            }

            public void Log(Exception e)
            {
                
            }

            public void ClearLog()
            {
                
            }

            public string LogText
            {
                get { return ""; }
            }

            public string Warning
            {
                get
                {
                    return "";
                }
                set { return; }
            }
        }

        private ILogger logger;

        // List of expected default imported types
        private List<string> defaultImports = new List<string>()
        {
            "None",
            "System"
        };

        [OneTimeSetUp]
        public void SetupPythonTests()
        {
            this.logger = new SimpleLogger();

            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var moduleRootFolder = Path.GetDirectoryName(assemblyPath);

            var resolutionPaths = new[]
            {
                // These tests need "DSIronPythonNode.dll" under "nodes" folder.
                Path.Combine(moduleRootFolder, "nodes")
            };
            //for some legacy tests we'll need the DSIronPython binary loaded manually
            //as the types are found using reflection - during normal dynamo use these types are already loaded. 
            Assembly.LoadFrom(Path.Combine(moduleRootFolder, "DSIronPython.dll"));

            assemblyHelper = new AssemblyHelper(moduleRootFolder, resolutionPaths);
            AppDomain.CurrentDomain.AssemblyResolve += assemblyHelper.ResolveAssembly;
        }

        [OneTimeTearDown]
        public void RunAfterAllTests()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= assemblyHelper.ResolveAssembly;
            assemblyHelper = null;
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchBasicNumVarSingleLine()
        {
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matchNumVar = "a = 5.0";
            var matches = provider.FindAllVariables(matchNumVar);
            Assert.AreEqual(1, matches.Count);
            Assert.IsTrue(matches.ContainsKey("a"));
            Assert.AreEqual("5.0", matches["a"].Item1);
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchBasicArrayVarSingleLine()
        {
            var matchArray = "a = []";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariables(matchArray);
            Assert.AreEqual(1, matches.Count);
            Assert.IsTrue(matches.ContainsKey("a"));
            Assert.AreEqual("[]", matches["a"].Item1);
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchBasicDictVarSingleLine()
        {
            var matchDict = "a = {}";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariables(matchDict);
            Assert.AreEqual(1, matches.Count);
            Assert.IsTrue(matches.ContainsKey("a"));
            Assert.AreEqual("{}", matches["a"].Item1);
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchIntSingleLine()
        {
            var matchDict = "a = 2";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariables(matchDict);
            Assert.AreEqual(1, matches.Count);
            Assert.IsTrue(matches.ContainsKey("a"));
            Assert.AreEqual("2", matches["a"].Item1);
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchComplexDictVarSingleLine()
        {
            var matchDict2 = "a = { 'Alice': 7, 'Toby': 'Nuts' }";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariables(matchDict2);
            Assert.IsTrue(matches.ContainsKey("a"));
            Assert.AreEqual("{ 'Alice': 7, 'Toby': 'Nuts' }", matches["a"].Item1);
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchComplexDictVarMultiLine()
        {
            var matchDict2 = "\n\na = { 'Alice': 7, 'Toby': 'Nuts' }\nb = 5.0";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariables(matchDict2);
            Assert.IsTrue(matches.ContainsKey("a"));
            Assert.AreEqual("{ 'Alice': 7, 'Toby': 'Nuts' }", matches["a"].Item1);
        }

        [Test]
        [Category("UnitTests")]
        public void DoesntMatchBadVariable()
        {
            var matchDict2 = "a! = { 'Alice': 7, 'Toby': 'Nuts' }";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariableAssignments(matchDict2);
            Assert.AreEqual(0, matches.Count);
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchAllVariablesSingleLine()
        {
            var str = "a = { 'Alice': 7, 'Toby': 'Nuts' }";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariables(str);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual(typeof(IronPython.Runtime.PythonDictionary), matches["a"].Item3);
        }

        [Test]
        [Category("UnitTests")]
        public void CanMatchAllVariableTypes()
        {
            var str = "a = { 'Alice': 7, 'Toby': 'Nuts' }\nb = {}\nc = 5.0\nd = 'pete'\ne = []";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = provider.FindAllVariables(str);

            Assert.AreEqual(5, matches.Count);
            Assert.AreEqual(typeof(IronPython.Runtime.PythonDictionary), matches["a"].Item3);
            Assert.AreEqual(typeof(IronPython.Runtime.PythonDictionary), matches["b"].Item3);
            Assert.AreEqual(typeof(double), matches["c"].Item3);
            Assert.AreEqual(typeof(string), matches["d"].Item3);
            Assert.AreEqual(typeof(IronPython.Runtime.List), matches["e"].Item3);
        }

        [Test]
        [Category("UnitTests")]
        public void CanImportLibrary()
        {
            var str = "\nimport System\n";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            provider.UpdateImportedTypes(str);

            Assert.AreEqual(2, provider.ImportedTypes.Count);
            Assert.IsTrue(provider.ImportedTypes.ContainsKey("System"));
        }

        [Test]
        [Category("UnitTests")]
        public void DuplicateCallsToImportShouldBeFine()
        {
            var str = "\nimport System\nimport System";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            provider.UpdateImportedTypes(str);

            Assert.AreEqual(2, provider.ImportedTypes.Count);
            Assert.IsTrue(defaultImports.SequenceEqual(provider.ImportedTypes.Keys.ToList()));
        }

        [Test]
        [Category("UnitTests")]
        public void CanImportSystemLibraryAndGetCompletionData()
        {
            var str = "\nclr.AddReference('System.Console')\nimport System\nSystem.";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();

            var completionData = provider.GetCompletionData(str);

            // Randomly verify some namepsaces are in the completion list
            var completionList = completionData.Select(d => d.Text);
            Assert.IsTrue(completionList.Any());
            Assert.IsTrue(completionList.Intersect(new[] { "IO", "Console", "Reflection" }).Count() == 3);
            Assert.AreEqual(2, provider.ImportedTypes.Count);
            Assert.IsTrue(defaultImports.SequenceEqual(provider.ImportedTypes.Keys.ToList()));
        }

        [Test]
        [Category("UnitTests")]
        public void CanImportSystemCollectionsLibraryAndGetCompletionData()
        {
            var str = "\nclr.AddReference('System.Collections.NonGeneric')\nimport System.Collections\nSystem.Collections.";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();

            var completionData = provider.GetCompletionData(str);
            var completionList = completionData.Select(d => d.Text);
            Assert.IsTrue(completionList.Any());
            Assert.IsTrue(completionList.Intersect(new[] { "Hashtable", "Queue", "Stack" }).Count() == 3);
            Assert.AreEqual(28, completionData.Length);
        }


        [Test]
        [Category("UnitTests")]
        public void CanMatchImportSystemAndLoadLibraryAndWithComment()
        {
            var str = "# Write your script here.\r\nimport System.";
            var completionProvider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            completionProvider.UpdateImportedTypes(str);

            Assert.AreEqual(2, completionProvider.ImportedTypes.Count);
            Assert.IsTrue(defaultImports.SequenceEqual(completionProvider.ImportedTypes.Keys.ToList()));
        }

        [Test]
        [Category("UnitTests")]
        public void CanIdentifyVariableTypeAndGetCompletionData()
        {
            var str = "a = 5.0\na.";

            var completionProvider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var completionData = completionProvider.GetCompletionData(str);

            Assert.AreNotEqual(0, completionData.Length);
        }

        [Test]
        [Category("UnitTests")]
        public void CanFindTypeSpecificImportsMultipleTypesSingleLine()
        {
            var str = "from math import sin, cos\n";
            var provider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            provider.UpdateImportedTypes(str);
            Assert.AreEqual(2,provider.ImportedTypes.Count);
        }
        /*finish
        [Test]
        [Category("UnitTests")]
        public void CanFindTypeSpecificImportsSingleTypeSingleLine()
        {
            var str = "from math import sin\n";

            var imports = IronPythonCompletionProvider.FindTypeSpecificImportStatements(str);
            Assert.IsTrue(imports.ContainsKey("sin"));
            Assert.AreEqual("from math import sin", imports["sin"]);
        }

        [Test]
        [Category("UnitTests")]
        public void CanFindTypeSpecificAutodeskImportsSingleTypeSingleLine()
        {
            var str = "from Autodesk.Revit.DB import Events\n";

            var imports = IronPythonCompletionProvider.FindTypeSpecificImportStatements(str);
            Assert.IsTrue(imports.ContainsKey("Events"));
            Assert.AreEqual("from Autodesk.Revit.DB import Events", imports["Events"]);
        }

        [Test]
        [Category("UnitTests")]
        public void CanFindAllTypeImports()
        {
            var str = "from Autodesk.Revit.DB import *\n";

            var imports = IronPythonCompletionProvider.FindAllTypeImportStatements(str);
            Assert.IsTrue(imports.ContainsKey("Autodesk.Revit.DB"));
            Assert.AreEqual("from Autodesk.Revit.DB import *", imports["Autodesk.Revit.DB"]);
        }
        */
        [Test]
        [Category("UnitTests")]
        public void CanFindDifferentTypesOfImportsAndLoad()
        {
            var str = "from itertools import *\nimport math\nfrom sys import callstats\n";

            var completionProvider =  new DSIronPython.IronPythonCodeCompletionProviderCore();
            completionProvider.UpdateImportedTypes(str);

            Assert.AreEqual(3, completionProvider.ImportedTypes.Count);
            Assert.IsTrue((completionProvider.Scope as ScriptScope).ContainsVariable("repeat"));
            Assert.IsTrue((completionProvider.Scope as ScriptScope).ContainsVariable("izip"));
            Assert.IsTrue((completionProvider.Scope as ScriptScope).ContainsVariable("math"));
            Assert.IsTrue((completionProvider.Scope as ScriptScope).ContainsVariable("callstats"));
        }

        [Test]
        [Category("UnitTests")]
        public void CanFindSystemCollectionsAssignmentAndType()
        {
            var str = "from System.Collections import ArrayList\na = ArrayList()\n";
            var completionProvider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            completionProvider.UpdateImportedTypes(str);
            completionProvider.UpdateVariableTypes(str);

            Assert.IsTrue(completionProvider.VariableTypes.ContainsKey("a"));
            Assert.AreEqual(typeof(System.Collections.ArrayList), completionProvider.VariableTypes["a"]);
        }

        [Test]
        [Category("UnitTests")]
        public void CanGetCompletionDataForArrayListVariable()
        {
            var str = "from System.Collections import ArrayList\na = ArrayList()\na.";
            var completionProvider = new DSIronPython.IronPythonCodeCompletionProviderCore();
            var matches = completionProvider.GetCompletionData(str);

            Assert.AreNotEqual(0, matches.Length);
            //TODO this was commented out.
            //Assert.AreEqual(typeof(IronPython.Runtime.PythonDictionary), matches[0].);
        }

        [Test]
        [Category("UnitTests")]
        public void VerifyIronPythonLoadedAssemblies()
        {
            // Verify IronPython assebmlies are loaded a single time
            List<string> matches = new List<string>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("IronPython"))
                {
                    if (matches.Contains(assembly.FullName))
                    {
                        Assert.Fail("Attempted to load an IronPython assembly multiple times: " + assembly.FullName);
                    }
                    else
                    {
                        matches.Add(assembly.FullName);
                    }
                }
            }
        }
    }
}
