using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobStorageLog
{
    public class AzureStorageLogger : ILogger
    {
        private readonly AzureStorageLoggerConfiguration _config;
        private readonly string _nameCategory;
        private ConcurrentQueue<string> _logMessages;
        private string _fileName = "";

        public AzureStorageLogger(string name, AzureStorageLoggerConfiguration logConfiguration)
        {
            _nameCategory = name;
            _config = logConfiguration;
            _logMessages = new ConcurrentQueue<string>();
        }

        private void Log<TState>(string date, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.Append(logLevel.ToString() + " : ");
            sb.Append(date);
            sb.AppendLine();
            sb.AppendLine(_nameCategory);
            sb.AppendLine(formatter(state, exception));

            if (exception != null)
            {
                sb.AppendLine(exception.ToString());
            }
            _logMessages.Enqueue(sb.ToString());
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _config.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                Task.Factory.StartNew(GetFileName);
                Task.Factory.StartNew(() => Log(DateTime.Now.ToString(), logLevel, eventId, state, exception, formatter));
                Task.Factory.StartNew(WriteLogAsync);
            }
        }

        private async Task WriteLogAsync()
        {
            try
            {
                BlobContainerClient blobContainerClient = await CreateContainer();
                AppendBlobClient appendBlobClient = await CreateBlob(blobContainerClient);

                using (var ms = new MemoryStream())
                {
                    string message = "";
                    TextWriter tw = new StreamWriter(ms, Encoding.UTF8, appendBlobClient.AppendBlobMaxAppendBlockBytes);
                    while (!_logMessages.IsEmpty)
                    {
                        _logMessages.TryDequeue(out message);
                        tw.Write(message);
                    }
                    tw.Flush();
                    ms.Position = 0;
                    appendBlobClient.AppendBlock(ms);
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private async Task<BlobContainerClient> CreateContainer()
        {
            var blobServiceClient = new BlobServiceClient(_config.AzureStorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_config.ContainerName);
            if (!containerClient.Exists())
            {
                await containerClient.CreateAsync();
            }
            return containerClient;
        }

        private async Task<AppendBlobClient> CreateBlob(BlobContainerClient containerClient)
        {
            AppendBlobClient appendBlobClient = containerClient.GetAppendBlobClient(_fileName);
            await appendBlobClient.CreateIfNotExistsAsync();
            
            return appendBlobClient;
        }

        private void GetFileName()
        {
            string fileName = _config.FileName + "-";
            switch (_config.Periodicity) 
            {
                case Periodicity.Daily:
                    fileName += DateTime.Now.ToString("yyyy-MM-dd");
                    break;
                case Periodicity.Weekly:
                    int day = DateTime.Now.DayOfYear;
                    var weekNumber = decimal.Round(((decimal)day / 7) + (decimal).4);
                    fileName += DateTime.Now.ToString("yyyy-MM-" + weekNumber);
                    break;
                case Periodicity.Monthly:
                    fileName += DateTime.Now.ToString("yyyy-MM");
                    break;
                case Periodicity.Yearly:
                    fileName += DateTime.Now.ToString("yyyy"); 
                    break;
            }
            if (string.IsNullOrEmpty(_fileName) || (_fileName != fileName))
            {
                _fileName = fileName;
            }
            _fileName += _config.Extension;
        }
    }
}
