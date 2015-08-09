using System.Collections.Generic;

namespace JOS.ConfigHelper.Dtos
{
    public class ConfigRoot
    {
        public Dictionary<string, string> TargetPaths { get; set; } 
        public Dictionary<string, string> ConfigFileVariables { get; set; }
        public DatabaseSettings DatabaseSettings { get; set; } 
        public string ProjectRootFolder { get; set; }
        public string BaseConfigFilesLocation { get; set; }
    }
}
