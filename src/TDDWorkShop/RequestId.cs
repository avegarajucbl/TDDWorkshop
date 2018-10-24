using System;

namespace TDDWorkShop
{
    public class RequestId
    {
        private Guid value { get; }
        internal RequestId(Guid value)
        {
            this.value = value;
        }

        public static implicit operator Guid(RequestId c) => c.value;
        public static implicit operator RequestId(Guid s) => new RequestId(s);

        public static bool operator ==(RequestId a, RequestId b)
        {
            if ((object)a == null || (object)b == null)
            {
                return false;
            }                

            return a.Equals(b);
        }

        public static bool operator !=(RequestId a, RequestId b)
        {
            return !(a == b);
        }

        public override string ToString() => value.ToString();
        public override int GetHashCode() => value.GetHashCode();
        public override bool Equals(object obj)
        {
            if (value == null || obj == null)
                return false;

            if (obj.GetType() == typeof(Guid))
            {
                Guid other = (Guid)obj;
                return Guid.Equals(value, other);
            }

            if (obj.GetType() == this.GetType())
            {
                Guid temp;
                if (Guid.TryParse(obj.ToString(), out temp))
                {
                    return Guid.Equals(value, temp);
                }
            }

            return false;
        }
    }
}
