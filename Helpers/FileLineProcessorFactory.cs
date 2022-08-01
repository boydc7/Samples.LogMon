using System;
using System.Collections.Generic;
using DdLogMon.Interfaces;
using DdLogMon.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DdLogMon.Helpers
{
    public class FileLineProcessorFactory
    {
        private readonly Dictionary<TailFileType, Func<IServiceProvider, IFileLineProcessor>> _fileTypeProcessorMap = new Dictionary<TailFileType, Func<IServiceProvider, IFileLineProcessor>>
                                                                                                    {
                                                                                                        { TailFileType.Unknown, (s) => NullFileLineProcessor.Instance },
                                                                                                        { TailFileType.HttpAccessLog, (s) => s.GetRequiredService<HttpAccessLogFileLineProcessor>() }
                                                                                                    };

        private FileLineProcessorFactory() { }

        public static FileLineProcessorFactory Instance { get; } = new FileLineProcessorFactory();

        public IFileLineProcessor ResolveProcessor(TailFileType fileType, IServiceProvider serviceProvider)
        {
            if (!_fileTypeProcessorMap.ContainsKey(fileType))
            {
                throw new ArgumentOutOfRangeException(nameof(fileType), $"Missing processor map for fileType of [{fileType}]");
            }

            return _fileTypeProcessorMap[fileType](serviceProvider);
        }
    }
}
