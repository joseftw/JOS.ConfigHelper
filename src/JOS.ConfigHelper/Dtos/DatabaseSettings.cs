﻿namespace JOS.ConfigHelper.Dtos
{
    public class DatabaseSettings
    {
        public string SqlNamedInstance { get; set; }
        public string TemplateDatabase { get; set; }

        public string SqlServerDataBasePath { get; set; }
        public string SqlServerLogsBasePath { get; set; }
    }
}
