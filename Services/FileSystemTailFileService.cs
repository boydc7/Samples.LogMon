using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DdLogMon.Services
{
    internal class FileSystemTailFileService : ITailFileService, IDisposable
    {
        private readonly ConcurrentDictionary<TailFileInfo, IFileSystemTailDelegate> _tailDelegateMap = new ConcurrentDictionary<TailFileInfo, IFileSystemTailDelegate>();

        private readonly IOptions<ConfigSettings> _settings;
        private readonly Func<TailFileType, IFileLineProcessor> _lineProcessorFactory;
        private readonly Func<TailFileInfo, IFileSystemTailDelegate> _tailDelegateFactory;

        public FileSystemTailFileService(IOptions<ConfigSettings> settings,
                                         Func<TailFileType, IFileLineProcessor> lineProcessorFactory,
                                         Func<TailFileInfo, IFileSystemTailDelegate> tailDelegateFactory)
        {
            _settings = settings;
            _lineProcessorFactory = lineProcessorFactory;
            _tailDelegateFactory = tailDelegateFactory;
        }

        public void Tail(string fileToTail, TailFileType fileType)
        {   // Ensure inputs are valid
            if (!fileToTail.HasValue())
            {
                throw new ArgumentNullException(nameof(fileToTail));
            }

            if (!File.Exists(fileToTail))
            {
                throw new FileNotFoundException($"[{fileToTail}] does not exist", fileToTail);
            }

            var lineProcessor = _lineProcessorFactory(fileType);

            if (lineProcessor == null)
            {
                throw new ArgumentOutOfRangeException(nameof(fileType), $"FileType [{fileType}] does not have a mapped line processor");
            }

            IFileSystemTailDelegate startNewTail(TailFileInfo fileInfo)
            {   // Simple internal wrap object (via local method) for actual processing
                var tailDelegate = _tailDelegateFactory(fileInfo);

                Task.Factory
                    .StartNew(() => tailDelegate.StartTail(),
                              TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)
                    .ContinueWith(t => StopTail(fileInfo));

                return tailDelegate;
            }

            // Meta-data for tracking what this tail is doing, etc
            var mapInfo = new TailFileInfo
                          {
                              FileType = fileType,
                              FileToTail = fileToTail,
                              Processor = lineProcessor,
                              TailInactivityTimeout = _settings.Value.TailInactivityTimeout
                          };

            // Background the tailing of the file...
            if (_tailDelegateMap.TryAdd(mapInfo, startNewTail(mapInfo)))
            {
                return;
            }

            throw new ApplicationException($"File [{fileToTail}] is already being tailed elsewhere");
        }

        public void StopTail(string tailFile)
        {
            var tailMapKvp = _tailDelegateMap.SingleOrDefault(t => t.Key.FileToTail.EqualsOrdinalCi(tailFile));

            if (tailMapKvp.Equals(default(KeyValuePair<TailFileInfo, IFileSystemTailDelegate>)))
            {
                return;
            }

            StopTail(tailMapKvp.Key);
        }

        public void StopTail(TailFileInfo fileInfo)
        {   // Stop tailing for the file in question
            if (!_tailDelegateMap.TryRemove(fileInfo, out var tailDelegate))
            {
                return;
            }

            tailDelegate.StopTail();
            tailDelegate.Dispose();
        }

        public void Dispose()
            => _tailDelegateMap.Each(kvp => StopTail(kvp.Key));
    }
}
