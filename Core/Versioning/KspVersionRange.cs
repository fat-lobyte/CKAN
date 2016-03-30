using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CKAN.Versioning
{
    public sealed class KspVersionRange
    {
        private string _string;

        public static readonly KspVersionRange Any =
            new KspVersionRange(new KspVersionBound(null, false), new KspVersionBound(null, false));

        public KspVersionBound Lower { get; private set; }
        public KspVersionBound Upper { get; private set;  }

        public KspVersionRange(KspVersionBound lower, KspVersionBound upper)
        {
            if (ReferenceEquals(lower, null))
                throw new ArgumentNullException("lower");

            if (ReferenceEquals(upper, null))
                throw new ArgumentNullException("upper");

            Lower = lower;
            Upper = upper;

            _string = DeriveString(this);
        }

        public KspVersionRange(params KspVersion[] versions)
            : this((IEnumerable<KspVersion>)versions) { }

        public KspVersionRange(IEnumerable<KspVersion> versions)
        {
            if (ReferenceEquals(versions, null))
                throw new ArgumentNullException("versions");

            KspVersionRangeInternal(versions.Select(i => i.ToVersionRange()));
        }

        public KspVersionRange(params KspVersionRange[] versionRanges)
            : this((IEnumerable<KspVersionRange>)versionRanges) { }

        public KspVersionRange(IEnumerable<KspVersionRange> versionRanges)
        {
            if (ReferenceEquals(versionRanges, null))
                throw new ArgumentNullException("versionRanges");

            KspVersionRangeInternal(versionRanges);
        }

        public override string ToString()
        {
            return _string;
        }


        public bool IsSupersetOf(KspVersionRange other)
        {
            if (ReferenceEquals(other, null))
                throw new ArgumentNullException("other");

            var lowerIsOkay = (Lower.Value == null)
                || (other.Lower.Value != null && Lower.Value < other.Lower.Value)
                || (other.Lower.Value != null && Lower.Value == other.Lower.Value && (Lower.Inclusive || !other.Lower.Inclusive));

            var upperIsOkay = (Upper.Value == null)
                || (other.Upper.Value != null && other.Upper.Value < Upper.Value)
                || (other.Upper.Value != null && other.Upper.Value == Upper.Value && (Upper.Inclusive || !other.Upper.Inclusive));

            return lowerIsOkay && upperIsOkay;
        }

        private void KspVersionRangeInternal(IEnumerable<KspVersionRange> versionRanges)
        {
            KspVersionBound lower = null;
            KspVersionBound upper = null;

            foreach (var versionRange in versionRanges)
            {
                if (ReferenceEquals(lower, null))
                    lower = versionRange.Lower;

                if (ReferenceEquals(upper, null))
                    upper = versionRange.Upper;

                if (versionRange.Lower.Value < lower.Value)
                {
                    lower = versionRange.Lower;
                }

                if (versionRange.Lower.Value == lower.Value && versionRange.Lower.Inclusive)
                {
                    lower = versionRange.Lower;
                }

                if (versionRange.Upper.Value > upper.Value)
                {
                    upper = versionRange.Upper;
                }

                if (versionRange.Upper.Value == upper.Value && versionRange.Upper.Inclusive)
                {
                    upper = versionRange.Upper;
                }
            }

            Lower = lower ?? new KspVersionBound();
            Upper = upper ?? new KspVersionBound();

            _string = DeriveString(this);
        }

        private static string DeriveString(KspVersionRange versionRange)
        {
            var sb = new StringBuilder();

            sb.Append(versionRange.Lower.Inclusive ? '[' : '(');

            if (versionRange.Lower.Value != null)
                sb.Append(versionRange.Lower.Value);

            sb.Append(',');

            if (versionRange.Upper.Value != null)
                sb.Append(versionRange.Upper.Value);

            sb.Append(versionRange.Upper.Inclusive ? ']' : ')');

            return sb.ToString();
        }
    }
}
