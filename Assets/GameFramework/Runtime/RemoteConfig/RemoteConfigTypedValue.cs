using System;
using UnityEngine;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// A single authored/boxed value of one of the types <see cref="RemoteConfigValueType"/> supports
    /// - shared by <see cref="RemoteConfigDefinition"/> (a definition's local default),
    /// <see cref="Providers.Mock.MockRemoteConfigEntry"/> (an authored mock fetch value), and
    /// <see cref="RemoteConfigCacheData"/> (a cached value), so the boxing/unboxing switch exists in
    /// exactly one place. Per-type fields (rather than a single serialized string) so Unity's default
    /// inspector authors each type without ad hoc string parsing - see CLAUDE.md's Phase 17 brief,
    /// section 20 ("do not build a massive custom editor").
    /// </summary>
    [Serializable]
    public struct RemoteConfigTypedValue
    {
        [SerializeField] private RemoteConfigValueType _type;
        [SerializeField] private bool _boolValue;
        [SerializeField] private int _intValue;
        [SerializeField] private long _longValue;
        [SerializeField] private float _floatValue;
        [SerializeField] private double _doubleValue;
        [SerializeField] private string _stringValue;

        public RemoteConfigValueType Type => _type;

        /// <summary>The value boxed as the CLR type matching <see cref="Type"/> - always one of
        /// bool/int/long/float/double/string.</summary>
        public object BoxedValue
        {
            get
            {
                switch (_type)
                {
                    case RemoteConfigValueType.Bool: return _boolValue;
                    case RemoteConfigValueType.Int: return _intValue;
                    case RemoteConfigValueType.Long: return _longValue;
                    case RemoteConfigValueType.Float: return _floatValue;
                    case RemoteConfigValueType.Double: return _doubleValue;
                    default: return _stringValue ?? string.Empty;
                }
            }
        }

        public static RemoteConfigTypedValue FromBool(bool value) => new RemoteConfigTypedValue { _type = RemoteConfigValueType.Bool, _boolValue = value };
        public static RemoteConfigTypedValue FromInt(int value) => new RemoteConfigTypedValue { _type = RemoteConfigValueType.Int, _intValue = value };
        public static RemoteConfigTypedValue FromLong(long value) => new RemoteConfigTypedValue { _type = RemoteConfigValueType.Long, _longValue = value };
        public static RemoteConfigTypedValue FromFloat(float value) => new RemoteConfigTypedValue { _type = RemoteConfigValueType.Float, _floatValue = value };
        public static RemoteConfigTypedValue FromDouble(double value) => new RemoteConfigTypedValue { _type = RemoteConfigValueType.Double, _doubleValue = value };
        public static RemoteConfigTypedValue FromString(string value) => new RemoteConfigTypedValue { _type = RemoteConfigValueType.String, _stringValue = value ?? string.Empty };

        /// <summary>Boxes an already-typed CLR value (bool/int/long/float/double/string) back into a
        /// <see cref="RemoteConfigTypedValue"/> - used when persisting a resolved snapshot value to
        /// cache. Returns a <see cref="RemoteConfigValueType.String"/> wrapping
        /// <see cref="object.ToString"/> for any other type, rather than throwing, since a cache write
        /// must never fail gameplay (see CLAUDE.md's Phase 17 brief, section 55).</summary>
        public static RemoteConfigTypedValue FromBoxed(object value)
        {
            switch (value)
            {
                case bool b: return FromBool(b);
                case int i: return FromInt(i);
                case long l: return FromLong(l);
                case float f: return FromFloat(f);
                case double d: return FromDouble(d);
                case string s: return FromString(s);
                default: return FromString(value?.ToString() ?? string.Empty);
            }
        }
    }
}
