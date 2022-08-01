using System;
using System.Collections.Generic;
using DdLogMon.Models;

namespace DdLogMon.Helpers
{
    public static class Extensions
    {
        public static int Gz(this int source, int alternateValue)
            => source > 0
                   ? source
                   : alternateValue;

        public static bool HasValue(this string source)
            => !string.IsNullOrEmpty(source);

        public static void Each<T>(this IEnumerable<T> source, Action<T> block)
        {
            if (source == null)
            {
                return;
            }

            foreach (var t in source)
            {
                block(t);
            }
        }

        public static bool EqualsOrdinalCi(this string source, string compare)
            => source != null && compare != null && source.Equals(compare, StringComparison.OrdinalIgnoreCase);

        public static DateTime ToDateTimeFromC(this string dateTimeString)
        {
            if (!dateTimeString.HasValue())
            {
                return DateTime.MinValue;
            }

            // Pull out the first : and replace with a space....
            var firstColonPosition = dateTimeString.IndexOf(':', StringComparison.OrdinalIgnoreCase);

            if (firstColonPosition >= 0)
            {
                var cStringFormatted = string.Concat(dateTimeString.Substring(0, firstColonPosition), " ", dateTimeString.Substring(firstColonPosition + 1));

                if (DateTime.TryParse(cStringFormatted, out var cdt))
                {
                    return cdt.ToUniversalTime();
                }
            }

            return ToDateTime(dateTimeString, DateTime.MinValue);
        }

        public static DateTime ToDateTime(this string dateTimeString, DateTime defaultValue = default)
        {
            if (!dateTimeString.HasValue())
            {
                return defaultValue;
            }

            return DateTime.TryParse(dateTimeString, out var dt)
                       ? dt.ToUniversalTime()
                       : defaultValue;
        }

        public static int ToInt(this string value, int defaultValue = 0)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }

            return int.TryParse(value, out var i)
                       ? i
                       : defaultValue;
        }

        public static long ToInt64(this string value, long defaultValue = 0)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }

            return long.TryParse(value, out var i)
                       ? i
                       : defaultValue;
        }

        public static string GetStatsSection(this HttpAccessLogLine line)
            => GetStatsSectionFromRequest(line?.Request);

        public static string GetStatsSectionFromRequest(this string line)
        {
            if (!line.HasValue())
            {
                return null;
            }

            var firstSpace = line.IndexOf(' ');

            if (firstSpace < 0)
            {
                return null;
            }

            firstSpace++;

            var secondSpace = line.IndexOf(' ', firstSpace);
            var secondSlash = line.IndexOf('/', line.IndexOf('/', firstSpace) + 1);

            if (secondSlash < firstSpace && secondSlash < firstSpace)
            {
                return null;
            }

            var length = (Math.Min(secondSlash.Gz(0), secondSpace.Gz(0)) - firstSpace - 1);

            var segment = line.Substring(firstSpace + 1, length);

            return segment;
        }
    }
}
