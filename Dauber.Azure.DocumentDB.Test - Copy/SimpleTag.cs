using System;
using System.Text;

namespace Dauber.Azure.DocumentDb.Test
{
    public class SimpleTag : IEquatable<SimpleTag>
    {        
        public string Slug { get; set; }
        public string CategorySlug { get; set; }

        public bool Equals(SimpleTag other)
        {
            if (other is null)
                return false;

            return this.Slug == other.Slug && this.CategorySlug == other.CategorySlug;
        }
    }
}
