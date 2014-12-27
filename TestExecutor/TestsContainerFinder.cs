using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TestExecutor
{
 public class TestsContainerFinder
    {
        public List<string> Execute(string buildPath)
        {
            AppDomainSetup setup = new AppDomainSetup()
            {
                AppDomainInitializer = Analyze,
                AppDomainInitializerArguments = new[] { buildPath },
                ShadowCopyFiles = "true"
            };
            AppDomain testDomain = AppDomain.CreateDomain("test", AppDomain.CurrentDomain.Evidence, setup);

            var data = testDomain.GetData("result") as List<string>;

            AppDomain.Unload(testDomain);

            return data;
        }

        private static void Analyze(string[] args)
        {
            AppDomain.CurrentDomain.DoCallBack(() =>
            {
                var buildPath = args[0];
                var result = InternalDllAnalyse(buildPath);
                AppDomain.CurrentDomain.SetData("result", result);
            });
        }

        /// <summary>
        /// Start to search for dlls in the given build path and check which one of them 
        /// is a tests container.
        /// </summary>
        /// <param name="buildPath">The build path.</param>
        /// <returns>List of dll paths which are tests container.</returns>
        private static List<string> InternalDllAnalyse(string buildPath)
        {
            var dllFiles = Directory.GetFiles(buildPath, "*.dll", SearchOption.TopDirectoryOnly);

            var result = (from container
                          in dllFiles
                          let modules = (from t in AnalyzeDlls(container)
                                         select t).ToList()
                          where modules.Any()
                          select container).ToList();

            return result;
        }

        private static IEnumerable<Type> AnalyzeDlls(string assemblyPath)
        {
            Assembly assembly;
            try
            {
                assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(assemblyPath));

                // Check if we need to exclude this project
                var assemblyMetadataAttribute = assembly
                    .GetCustomAttributes(typeof(AssemblyMetadataAttribute)) as IEnumerable<AssemblyMetadataAttribute>;

                // Check we have the MetaDataAttribute - if so, ignore this dll
                if (assemblyMetadataAttribute != null && assemblyMetadataAttribute.Any())
                {
                    if (assemblyMetadataAttribute.Any(
                            t => t.Key.Equals("RunTestsBeforePush") &&
                                 t.Value.Equals("false", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return new List<Type>();
                    }
                }
            }
            catch
            {
                return new List<Type>();
            }

            return GetAssemblyClasses(assembly)
                .Where(IsTestMethod).ToList();
        }

        /// <summary>
        /// Get all assembly classes from given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The list of types.</returns>
        private static IEnumerable<Type> GetAssemblyClasses(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes().AsEnumerable();
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// Check if this type has a <see cref="TestClassAttribute"/> attribute.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if this type has the <see cref="TestClassAttribute"/>, else False.</returns>
        private static bool IsTestMethod(Type type)
        {
            var moduleHasClassAttribute = type.GetCustomAttributes(false)
                                   .Select(t => t.GetType().FullName)
                                   .Contains(typeof(TestClassAttribute).FullName);

            if (moduleHasClassAttribute)
            {
                Message.WriteInformation("Found tests class: {0}", type.FullName);
                return true;
            }

            return false;
        }
    }
}
