using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TestExecutor
{
    /// <summary>
    /// Find all the tests containers that in the given path.
    /// Tests container are dlls that have the 'TestClass' attribute.
    /// If the dll information contains also an attribute 'RunTestsBeforePush' with value 'false',
    /// the dll is being ignored.
    /// </summary>
    public class TestsContainerFinder
    {
        private readonly ILogger _logger;

        public TestsContainerFinder(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all tests containers in the given build path.
        /// </summary>
        /// <param name="buildPath">The build path.</param>
        /// <returns>The list of tests containers (dlls paths).</returns>
        public List<string> GetTestsContainers(string buildPath)
        {
            AppDomainSetup setup = new AppDomainSetup()
            {
                AppDomainInitializer = Analyze,
                AppDomainInitializerArguments = new[] { buildPath },
                ShadowCopyFiles = "true"
            };

            AppDomain testDomain = AppDomain.CreateDomain("TestsContainersFinder", AppDomain.CurrentDomain.Evidence, setup);

            // Get the results of tests containers from the created app domain
            var data = testDomain.GetData("result") as List<string>;

            AppDomain.Unload(testDomain);

            return data;
        }

        /// <summary>
        /// Entry point for the tests containers finder app domain.
        /// </summary>
        /// <param name="args">The app domain's args.</param>
        private void Analyze(string[] args)
        {
            AppDomain.CurrentDomain.DoCallBack(() =>
            {
                var buildPath = args[0];
                var result = InternalDllAnalyze(buildPath);

                AppDomain.CurrentDomain.SetData("result", result);
            });
        }

        /// <summary>
        /// Start to search for dlls in the given build path and check which one of them 
        /// is a tests container.
        /// </summary>
        /// <param name="buildPath">The build path.</param>
        /// <returns>List of dll paths which are tests container.</returns>
        private List<string> InternalDllAnalyze(string buildPath)
        {
            var dllFiles = Directory.GetFiles(buildPath, "*.dll", SearchOption.TopDirectoryOnly);

            _logger.Log("Found {0} to go over", dllFiles.Count());

            var result = (from container
                          in dllFiles
                          let modules = (from t in CheckIfAssemblyIsTestsContainer(container)
                                         select t).ToList()
                          where modules.Any()
                          select container).ToList();

            return result;
        }

        private IEnumerable<Type> CheckIfAssemblyIsTestsContainer(string assemblyPath)
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
            catch (Exception e)
            {
                _logger.Log("Failed to analyze assembly {0}, exception: {1}", assemblyPath, e);
                return new List<Type>();
            }

            return GetAssemblyClasses(assembly)
                .Where(IsContainsTestsClasses).ToList();
        }

        /// <summary>
        /// Get all assembly classes from given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The list of types.</returns>
        private IEnumerable<Type> GetAssemblyClasses(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes().AsEnumerable();
            }
            catch (Exception e)
            {
                _logger.Log("Failed to get assembly classes: {0}, exception: {1}", assembly, e);
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// Check if this type has a <see cref="TestClassAttribute"/> attribute.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if this type has the <see cref="TestClassAttribute"/>, else False.</returns>
        private bool IsContainsTestsClasses(Type type)
        {
            var moduleHasClassAttribute = type.GetCustomAttributes(false)
                                   .Select(t => t.GetType().FullName)
                                   .Contains(typeof(TestClassAttribute).FullName);

            if (moduleHasClassAttribute)
            {
                _logger.Log("Type {0} does contain test class attribute", type);
                Message.WriteInformation("Found tests class: {0}", type.FullName);
                return true;
            }

            _logger.Log("Type {0} doesn't contain test class attribute", type);

            return false;
        }
    }
}
