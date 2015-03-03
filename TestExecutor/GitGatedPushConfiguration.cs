using System.Collections.Generic;
using System.Xml.Serialization;

namespace TestExecutor
{
    /// <summary>
    /// Represents a solution item in case the user wants to indicates 
    /// if the gated push should be only on specific solutions.
    /// </summary>
    public class SolutionItem
    {
        /// <summary>
        /// Gets or sets the solution's path.
        /// </summary>
        public string SolutionPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicates whether we should be the solution.
        /// </summary>
        public bool BuildSolution { get; set; }

        /// <summary>
        /// Gets or sets a value indicates whether we should run the solution's tests.
        /// If the 'BuildSolution' value is false, this value will not be consider.
        /// </summary>
        public bool RunTests { get; set; }
    }

    /// <summary>
    /// Represents the git gated push required configuration.
    /// </summary>
    public class GitGatedPushConfiguration
    {
        #region Configuration Parameters

        /// <summary>
        /// Gets or sets the MSTest component for running the unit tests.
        /// </summary>
        public string MsTestPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicates whether we should run this component on develop
        /// branch only, or not.
        /// </summary>
        public bool OnDevelopOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicates whether we should run this component for each solution in 
        /// the given repository, or we should do it only for specific ones.
        /// </summary>
        public bool ProcessAllSolutions { get; set; }

        /// <summary>
        /// Gets or sets a value indicates whether we should find which solution we need to process by
        /// checking the unsynced commits for current branch.
        /// </summary>
        public bool FindSolutionByChanges { get; set; }

        /// <summary>
        /// Gets or sets the solution items that we should process in case the user doesn't 
        /// want to process all solutions.
        /// </summary>
        public List<SolutionItem> SolutionsItems { get; set; } 

        #endregion

        /// <summary>
        /// Load configuration from XML to configuration object.
        /// </summary>
        /// <param name="xmlFilePath">The configuration file path.</param>
        /// <returns>The git gated push configuration.</returns>
        public static GitGatedPushConfiguration LoadFromXmlFile(string xmlFilePath)
        {
            var streamReader = new System.IO.StreamReader(xmlFilePath);
            var serializer = new XmlSerializer(typeof(GitGatedPushConfiguration));
            return serializer.Deserialize(streamReader) as GitGatedPushConfiguration;
        }

        /// <summary>
        /// Convert the git gated push configuration object to XML file.
        /// </summary>
        /// <returns>The configuration in XML format.</returns>
        public string ToXml()
        {
            var stringwriter = new System.IO.StringWriter();
            var serializer = new XmlSerializer(this.GetType());
            serializer.Serialize(stringwriter, this);
            return stringwriter.ToString();
        }
    }
}
