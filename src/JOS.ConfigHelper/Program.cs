using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JOS.ConfigHelper.Dtos;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Runtime;

namespace JOS.ConfigHelper
{
    public class Program
    {
        private string ApplicationBasePath { get; set; }
        private string BranchName { get; set; }
        private IConfiguration Configuration { get;}
        private ConfigRoot ConfigData { get; set; }

        public Program(IApplicationEnvironment app)
        {
            ApplicationBasePath = app.ApplicationBasePath;
            Configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(app.ApplicationBasePath, "config.json"))
                .AddEnvironmentVariables()
                .Build();

            ConfigData = ConfigurationBinder.Bind<ConfigRoot>(Configuration.GetConfigurationSection("data"));
        }

        public void Main(string[] args)
        {
            args = new[] {"JosefTestar"};
            if (!args.Any())
            {
                Console.WriteLine("Missing branchname argument, you need to pass in a branchname like this: JOS.ConfigHelper.exe myBranchName");
                Console.ReadKey();
                return;
            }

            BranchName = args.Any() ? args.First() : string.Empty;

            Console.WriteLine($"Creating Computerspecific configfiles for computer {Environment.MachineName}...");
            CreateComputerSpecificConfigFiles();

            Console.WriteLine($"Creating SQL database for branch {BranchName}...");
            CreateBranchSpecificSqlDatabase();

            Console.WriteLine($"Creating RavenDB database(s) for branch {BranchName}...");
            CreateBranchSpecificRavenDatabase();

            Console.ReadKey();
        }

        private void CreateBranchSpecificRavenDatabase()
        {
            throw new NotImplementedException();
        }

        private void CreateBranchSpecificSqlDatabase()
        {
            var templateDatabase = ConfigData.DatabaseSettings.TemplateDatabase;
            var sqlNamedInstance = ConfigData.DatabaseSettings.SqlNamedInstance;
            
            var databaseName = !string.IsNullOrEmpty(BranchName) ? $"{templateDatabase}-{BranchName}" : $"{templateDatabase}";
            var backupName = $"{databaseName}.bak";

            MakeBackupOfTemplateDatabase(templateDatabase, sqlNamedInstance, backupName);

            var backupLocation = Path.Combine(ApplicationBasePath, backupName);
            CreateBranchDatabase(sqlNamedInstance, databaseName, backupLocation, templateDatabase);

            RemoveTempBackupFile(backupName);
        }

        private void MakeBackupOfTemplateDatabase(string templateDatabase, string sqlNamedInstance, string backupName)
        {
            var sqlQuery = $"BACKUP DATABASE [{templateDatabase}] TO DISK = '{ApplicationBasePath}\\{backupName}'";
            var arguments = $"-S {sqlNamedInstance} -E -Q \"{sqlQuery}\"";
            var processStartInfo = new ProcessStartInfo("sqlcmd", arguments);
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
        }

        private void CreateBranchDatabase(string sqlNamedInstance, string databaseName, string backupLocation, string templateDatabase)
        {
            var sqlServerDataBasePath = ConfigData.DatabaseSettings.SqlServerDataBasePath;
            var sqlServerLogsBasePath = ConfigData.DatabaseSettings.SqlServerLogsBasePath;
            var fullMdfToPath = Path.Combine(sqlServerDataBasePath, $"{databaseName}.mdf");
            var fullLdfToPath = Path.Combine(sqlServerLogsBasePath, $"{databaseName}_Log.ldf");
            var sqlQuery = $"RESTORE DATABASE [{databaseName}] " +
                           $"FROM DISK = '{backupLocation}' " +
                           $"WITH MOVE '{templateDatabase}' " +
                           $"TO '{fullMdfToPath}', " +
                           $"MOVE '{templateDatabase}_Log' " +
                           $"TO '{fullLdfToPath}'";

            var arguments = $"-S {sqlNamedInstance} -E -Q \"{sqlQuery}\"";

            var processStartInfo = new ProcessStartInfo("sqlcmd", arguments);
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
        }

        private void RemoveTempBackupFile(string backupName)
        {
            var path = Path.Combine(ApplicationBasePath, backupName);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void CreateComputerSpecificConfigFiles()
        {
            foreach (var path in ConfigData.TargetPaths)
            {
                var configFolder = Path.Combine(ApplicationBasePath, ConfigData.BaseConfigFilesLocation, path.Value);
                var target = Path.Combine(ConfigData.ProjectRootFolder, BranchName, path.Value.Replace("{{COMPUTERNAME}}", Environment.MachineName));

                var configFolderPath = new DirectoryInfo(configFolder);
                var targetPath = new DirectoryInfo(target);

                DirectoryHelpers.CopyAll(configFolderPath, targetPath);

                MakeConfigFilesBranchSpecific(target);
            }
        }

        private void MakeConfigFilesBranchSpecific(string path)
        {
            var configFiles = Directory.GetFiles(path, "*.config");
            foreach (var configFile in configFiles)
            {
                var newFileName = configFile.Replace("COMPUTERNAME", Environment.MachineName);
                var fileContent = File.ReadAllText(configFile);
                var updatedFile = ReplaceVariables(fileContent);
                File.WriteAllText(newFileName, updatedFile);
                File.Delete(configFile);
            }
        }

        private string ReplaceVariables(string fileContent)
        {
            var variables = ConfigData.ConfigFileVariables;
            var replacedContent = variables.Aggregate(fileContent, (current, variable) => current.Replace($"{{{variable.Key}}}", variable.Value));
            replacedContent = replacedContent.Replace("{{BRANCH_NAME}}",
                !string.IsNullOrEmpty(BranchName) ? $"-{BranchName}" : string.Empty);

            return replacedContent;
        }
    }
}