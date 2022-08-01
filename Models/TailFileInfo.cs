using System;
using DdLogMon.Helpers;
using DdLogMon.Interfaces;

namespace DdLogMon.Models
{
    public class TailFileInfo : IEquatable<TailFileInfo>
    {
        public TailFileType FileType { get; set; }
        public string FileToTail { get; set; }
        public int TailInactivityTimeout { get; set; }
        public IFileLineProcessor Processor { get; set; }

        public bool Equals(TailFileInfo other)
            => other != null &&
               (ReferenceEquals(this, other) || FileToTail.EqualsOrdinalCi(other.FileToTail));

        public override bool Equals(object obj)
            => obj != null && obj is TailFileInfo tfi && Equals(tfi);

        public override int GetHashCode()
            => FileToTail.GetHashCode();
    }
}
