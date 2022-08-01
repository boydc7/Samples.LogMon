using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DdLogMon.Services
{
    public class FileSystemTailFileDelegate : IFileSystemTailDelegate
    {
        private readonly TailFileInfo _fileInfo;
        private readonly ITailFileFailureDelegate _tailFileFailureDelegate;
        private readonly IOptions<ConfigSettings> _settings;
        private readonly ILogger<FileSystemTailFileDelegate> _logger;
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private int _isTailing;
        private bool _continueTailing = true;

        public FileSystemTailFileDelegate(TailFileInfo fileInfo,
                                          ITailFileFailureDelegate tailFileFailureDelegate,
                                          IOptions<ConfigSettings> settings,
                                          ILogger<FileSystemTailFileDelegate> logger)
        {
            _fileInfo = fileInfo;
            _tailFileFailureDelegate = tailFileFailureDelegate;
            _settings = settings;
            _logger = logger;
        }

        public void StopTail()
        {
            _continueTailing = false;
            _signal?.Set();
        }

        public void StartTail()
        {
            var isTailing = Interlocked.CompareExchange(ref _isTailing, 1, 0);

            if (isTailing > 0 || !_continueTailing)
            {   // Already tailing...nothing to be done
                return;
            }

            var inactiveTimeoutSeconds = _fileInfo.TailInactivityTimeout.Gz(3600);

            try
            {
                var path = Path.GetDirectoryName(_fileInfo.FileToTail);
                var file = Path.GetFileName(_fileInfo.FileToTail);

                // Watch the file for changes (which will signal us to read more lines when they appear while we're waiting)
                using(var fileWatcher = new FileSystemWatcher(path, file)
                                        {
                                            EnableRaisingEvents = true
                                        })
                {
                    fileWatcher.Changed += (s, r) => _signal.Set();

                    // Open the file to read, share with readers and writers (we're just reporting on stuff)
                    using(var fileStream = new FileStream(_fileInfo.FileToTail, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (!_settings.Value.ProcessExistingLogLines)
                        {   // Seek to the end of the file...
                            fileStream.Seek(0, SeekOrigin.End);

                            _logger.LogDebug($"Moved to position {fileStream.Position} in file");
                        }

                        var lastPosition = fileStream.Position;

                        using(var reader = new StreamReader(fileStream))
                        {
                            while (_continueTailing)
                            {
                                var line = reader.ReadLine();

                                if (line == null)
                                {   // Wait till something shows up
                                    if (new FileInfo(_fileInfo.FileToTail).Length < lastPosition)
                                    {   // Deleted entries, recreated file, etc...reset
                                        fileStream.Seek(0, SeekOrigin.End);
                                        _logger.LogDebug($"Reset position in file, was [{lastPosition}], now [{fileStream.Position}].");
                                    }

                                    lastPosition = fileStream.Position;

                                    Task.Delay(250).Wait();
                                    //_signal.WaitOne(TimeSpan.FromSeconds(inactiveTimeoutSeconds));
                                }
                                else
                                {   // Send the line off to the processor
                                    _fileInfo.Processor.ProcessLine(line);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception x)
            {
                _tailFileFailureDelegate.OnFailure(_fileInfo, x);
            }
            finally
            {   // Done tailing
                Interlocked.CompareExchange(ref _isTailing, 0, 1);
            }
        }

        public void Dispose()
        { // Stop tailing and cleanup
            StopTail();

            var loops = 0;

            while (_isTailing > 0 && loops <= 10)
            {
                Task.Delay(500).Wait();
                loops++;
            }

            _signal?.Dispose();
        }
    }
}
