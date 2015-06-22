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
    public class TestsContainerFinder : MarshalByRefObject
    {
        /// <summary>
        /// Contains the remote object for 'Tracer'.
        /// </summary>
        private TracerWrapper _tracerWrapper;

        /// <summary>
        /// Get all tests containers in the given build path.
        /// </summary>
        /// <param name="buildPath">The build path.</param>
        /// <returns>The list of tests containers (dlls paths).</returns>
        public List<string> GetTestsContainers(string buildPath, TracerWrapper tracerWrapper)
        {
            _tracerWrapper = tracerWrapper;

            return InternalDllAnalyze(buildPath);
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

            _tracerWrapper.TraceInformation("Found {0} dlls to go over", dllFiles.Count());

            var result = (from container
                          in dllFiles
                          let modules = (from t in CheckIfAssemblyIsTestsContainer(container)
                                         select t).ToList()
                          where modules.Any()
                          select container).ToList();

            return result;
        }

        /// <summary>
        /// Check if the given assembly is a test container by the following:
        ///     1. Check it doesn't have the attribute 'RunTestsbeforePush' with the value 'false'.
        ///     2. Check if this assembly contains the 'TestMethod' attribute.
        /// </summary>
        /// <param name="assemblyPath">The assembly's path.</param>
        /// <returns>True if the assembly is a test container, else false.</returns>
        private IEnumerable<Type> CheckIfAssemblyIsTestsContainer(string assemblyPath)
        {
            Assembly assembly;
            try
            {
                _tracerWrapper.TraceInformation("Starting to analyze assembly: {0}", assemblyPath);

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

                return GetAssemblyClasses(assembly).Where(IsContainsTestsClasses).ToList();
            }
            catch (Exception e)
            {
                _tracerWrapper.TraceError("Failed to analyze assembly {0}, exception: {1}", assemblyPath, e.Message);
                return new List<Type>();
            }
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
                _tracerWrapper.TraceInformation("Failed to get assembly classes, exception: {0}", e.Message);
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
                _tracerWrapper.TraceInformation("Type {0} does contain test class attribute", type.FullName);
                Message.WriteInformation("Found tests class: {0}", type.FullName);
                return true;
            }

            return false;
        }
    }
}
