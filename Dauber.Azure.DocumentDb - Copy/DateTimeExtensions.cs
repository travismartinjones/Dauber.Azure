using System;
using System.Globalization;

namespace Dauber.Azure.DocumentDb
{
    public static class DateTimeExtensions
    {
        public static int ToEpoch(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1);
            var epochTimeSpan = date - epoch;
            return (int)epochTimeSpan.TotalSeconds;
        }

        public static string ToIso8601(this DateTime date)
        {
            return date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);            
        }
    }
}