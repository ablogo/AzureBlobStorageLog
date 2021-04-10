using Microsoft.Extensions.Logging;

namespace AzureBlobStorageLog
{
    public class AzureStorageLoggerProvider : ILoggerProvider
    {
        private ILogger _logger;
        private AzureStorageLoggerConfiguration _config;
        private bool disposedValue = false;

        public AzureStorageLoggerProvider(AzureStorageLoggerConfiguration logConfiguration)
        {
            _config = logConfiguration;
        }

        public ILogger CreateLogger(string categoryName)
        {
            _config.Validate();
            _logger = new AzureStorageLogger(categoryName, _config);
            return _logger;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _logger = null;
                }
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
