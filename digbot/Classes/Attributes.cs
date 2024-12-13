namespace digbot.Classes
{
    public class Attributes<TType, TValue>
        where TType : struct, Enum
        where TValue : notnull
    {
        private TValue Generic { get; set; }
        private Dictionary<TType, TValue> Lib = [];

        public TValue this[TType type]
        {
            get
            {
                if (type.Equals(default(TType)))
                    return Generic;

                TValue? specificValue = Lib.TryGetValue(type, out var value) ? value : default;
                if (specificValue == null)
                {
                    return Generic;
                }
                return (dynamic)specificValue + Generic;
            }
            set
            {
                if (type.Equals(default(TType)))
                    Generic = value;
                else
                    Lib[type] = value;
            }
        }

        public TValue this[string name]
        {
            get
            {
                if (Enum.TryParse<TType>(name, out TType type))
                {
                    return this[type];
                }
                return Generic;
            }
            set
            {
                if (Enum.TryParse<TType>(name, out TType type))
                {
                    if (value == null)
                        throw new ArgumentNullException(nameof(value), "Value cannot be null.");
                    this[type] = value;
                }
            }
        }

        protected Attributes(TValue genericValue)
        {
            Generic = genericValue;
        }

        public static Attributes<TType, TValue> operator +(
            Attributes<TType, TValue> a,
            Attributes<TType, TValue> b
        )
        {
            var combinedLib = new Dictionary<TType, TValue>(a.Lib);
            foreach (var key in b.Lib.Keys)
            {
                combinedLib[key] = (dynamic)a.Lib[key] + b.Lib[key];
            }
            return new Attributes<TType, TValue>((dynamic)a.Generic + b.Generic)
            {
                Lib = combinedLib,
            };
        }

        public static Attributes<TType, TValue> operator -(
            Attributes<TType, TValue> a,
            Attributes<TType, TValue> b
        )
        {
            var combinedLib = new Dictionary<TType, TValue>(a.Lib);
            foreach (var key in b.Lib.Keys)
            {
                combinedLib[key] = (dynamic)a.Lib[key] - b.Lib[key];
            }
            return new Attributes<TType, TValue>((dynamic)a.Generic - b.Generic)
            {
                Lib = combinedLib,
            };
        }

        public static Attributes<TType, TValue> operator *(Attributes<TType, TValue> a, dynamic b)
        {
            var combinedLib = new Dictionary<TType, TValue>(a.Lib);
            if (b is Attributes<TType, TValue>)
            {
                foreach (var key in a.Lib.Keys)
                {
                    combinedLib[key] = (dynamic)a.Lib[key] * b.Lib[key];
                }
                return new Attributes<TType, TValue>((dynamic)a.Generic * b.Generic)
                {
                    Lib = combinedLib,
                };
            }
            else if (b is int || b is float || b is double)
            {
                foreach (var key in a.Lib.Keys)
                {
                    combinedLib[key] = (dynamic)a.Lib[key] * b;
                }
                return new Attributes<TType, TValue>((dynamic)a.Generic * b) { Lib = combinedLib };
            }
            throw new ArgumentException("Unsupported type for multiplication");
        }
    }
}
