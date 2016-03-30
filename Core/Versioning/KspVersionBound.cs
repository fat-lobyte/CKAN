namespace CKAN.Versioning
{
    public sealed class KspVersionBound
    {
        public KspVersion Value { get; private set; }
        public bool Inclusive { get; private set; }

        public KspVersionBound()
        {
            Value = null;
            Inclusive = false;
        }

        public KspVersionBound(KspVersion value, bool inclusive)
        {
            Value = value;
            Inclusive = inclusive;
        }
    }
}
