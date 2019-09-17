using System;

namespace Dauber.Azure.DocumentDb
{
    /// <summary>
    /// To perform ranged queries on dates, use this class and query off the Epoch value instead of DateTime.
    /// DocumentDB does not support DateTime querying, but does support ranged querying over numbers. 
    /// </summary>
    /// <remarks>See https://azure.microsoft.com/en-us/blog/working-with-dates-in-azure-documentdb-4/ for additional information.</remarks>
    public class DateEpoch
    {
        public DateTime Date { get; set; }
        public int Epoch => Date.Equals(null) || Date.Equals(DateTime.MinValue) ? int.MinValue : Date.ToEpoch();
    }
}