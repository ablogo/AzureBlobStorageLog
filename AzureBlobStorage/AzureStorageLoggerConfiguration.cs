using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace AzureBlobStorageLog
{
    public class AzureStorageLoggerConfiguration
    {
        private string _azConnectionString = null;
        private string _containerName = "logs";
        private Regex _regexContainerName = new Regex("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$");

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public int EventId { get; set; } = 0;

        public string AzureStorageConnectionString
        {
            get { return _azConnectionString; }
            set 
            {
                if (string.IsNullOrEmpty(value))
                {
                    ArgumentException("The connection string for Azure cannot be empty.");
                }
                else 
                {
                    _azConnectionString = value;
                }
            } 
        }

        public string ContainerName
        { 
            get { return _containerName; } 
            set 
            {
                if (_regexContainerName.IsMatch(value))
                {
                    _containerName = value;
                }
                else 
                {
                    ArgumentException("Invalid container name.");
                }
            }
        }

        public string FileName { get; set; } = "app-log";

        public string Extension { get; set; } = ".txt";

        public Periodicity Periodicity { get; set; } = Periodicity.Weekly;

        public void Validate() 
        {
            if (string.IsNullOrEmpty(_azConnectionString)) 
            {
                ArgumentException("The connection string for Azure cannot be empty.");
            }

            if (!_regexContainerName.IsMatch(_containerName))
            {
                ArgumentException("Invalid container name.");
            }
        }

        private ArgumentException ArgumentException(string message) 
        {
            throw new ArgumentException(message);
        }
    }
}
