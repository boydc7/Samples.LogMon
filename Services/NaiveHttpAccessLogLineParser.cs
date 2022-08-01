using System;
using System.Collections.Generic;
using System.Linq;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;
using DdLogMon.Models;
using Microsoft.Extensions.Logging;

namespace DdLogMon.Services
{
    public class NaiveHttpAccessLogLineParser : IHttpAccessLogLineParser
    {
        private readonly ILogger<NaiveHttpAccessLogLineParser> _logger;

        private readonly HashSet<char> _startDelimiters = new HashSet<char>
                                                          {
                                                              '"',
                                                              '['
                                                          };

        private readonly Dictionary<char, char> _endDelimiters = new Dictionary<char, char>
                                                                 {
                                                                     { '"', '"' },
                                                                     { '[', ']' }
                                                                 };

        public NaiveHttpAccessLogLineParser(ILogger<NaiveHttpAccessLogLineParser> logger)
        {
            _logger = logger;
        }

        public HttpAccessLogLine Parse(string line)
        {   // Very naive, simple access log parser....
            if (!line.HasValue())
            {
                return null;
            }

            var linePieces = new List<string>();
            var inDelimiter = default(char);
            var currentPiece = new List<char>();

            foreach (var currentChar in line)
            {
                if (inDelimiter != default(char) && currentChar.Equals(_endDelimiters[inDelimiter]))
                {
                    linePieces.Add(new string(currentPiece.ToArray()));
                    currentPiece = new List<char>();
                    inDelimiter = default(char);

                    continue;
                }

                if (currentChar.Equals(' ') && inDelimiter == default(char))
                {
                    if (currentPiece.Count > 0)
                    {
                        linePieces.Add(new string(currentPiece.ToArray()));
                        currentPiece = new List<char>();
                        inDelimiter = default(char);
                    }

                    continue;
                }

                if (_startDelimiters.Contains(currentChar))
                {
                    inDelimiter = currentChar;
                    continue;
                }

                currentPiece.Add(currentChar);
            }

            if (currentPiece.Count > 0)
            {
                linePieces.Add(new string(currentPiece.ToArray()));
            }

            if (linePieces.Count < 7)
            {
                _logger.LogWarning($"Count not deserialize line [{line}] into HttpAccess log model - have pieces count of [{linePieces.Count}]");
                return null;
            }

            var logLine = new HttpAccessLogLine
                          {
                              IpAddress = linePieces[0],
                              Ident = linePieces[1],
                              UserId = linePieces[2],
                              ReceivedOn = linePieces[3].ToDateTimeFromC(),
                              Request = linePieces[4],
                              StatusCode = linePieces[5].ToInt(),
                              ResponseSize = linePieces[6].ToInt64()
                          };

            return logLine;
        }
    }
}
