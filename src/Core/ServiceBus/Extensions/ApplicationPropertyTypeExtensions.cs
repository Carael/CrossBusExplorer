using System.Globalization;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Extensions
{
    public static class ApplicationPropertyTypeExtensions
    {
        public static ApplicationPropertyType GetApplicationPropertyType(this object value) =>
            value switch
            {
                string _ => ApplicationPropertyType.String,
                bool _ => ApplicationPropertyType.Bool,
                byte _ => ApplicationPropertyType.Byte,
                sbyte _ => ApplicationPropertyType.Sbyte,
                short _ => ApplicationPropertyType.Short,
                ushort _ => ApplicationPropertyType.Ushort,
                int _ => ApplicationPropertyType.Int,
                uint _ => ApplicationPropertyType.Uint,
                long _ => ApplicationPropertyType.Long,
                ulong _ => ApplicationPropertyType.Ulong,
                float _ => ApplicationPropertyType.Float,
                decimal _ => ApplicationPropertyType.Decimal,
                double _ => ApplicationPropertyType.Double,
                char _ => ApplicationPropertyType.Char,
                Guid _ => ApplicationPropertyType.Guid,
                DateTime _ => ApplicationPropertyType.DateTime,
                DateTimeOffset _ => ApplicationPropertyType.DateTimeOffset,
                Stream _ => ApplicationPropertyType.Stream,
                Uri _ => ApplicationPropertyType.Uri,
                TimeSpan _ => ApplicationPropertyType.TimeSpan,
                _ => throw new ArgumentOutOfRangeException("Unsupported type")
            };

        public static object GetApplicationPropertyValue(
            this string value,
            ApplicationPropertyType type)
        {
            switch (type)
            {
                case ApplicationPropertyType.String:
                    return value;
                case ApplicationPropertyType.Bool:
                {
                    if (value.Equals("1", StringComparison.Ordinal) ||
                        value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }

                    if (value.Equals("0", StringComparison.Ordinal) ||
                        value.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return false;
                    }

                    return int.Parse(value, CultureInfo.InvariantCulture);
                }
                case ApplicationPropertyType.Byte:
                    return byte.Parse(value);
                case ApplicationPropertyType.Sbyte:
                    return sbyte.Parse(value);
                case ApplicationPropertyType.Short:
                    return short.Parse(value);
                case ApplicationPropertyType.Ushort:
                    return ushort.Parse(value);
                case ApplicationPropertyType.Int:
                    return int.Parse(value);
                case ApplicationPropertyType.Uint:
                    return uint.Parse(value);
                case ApplicationPropertyType.Long:
                    return long.Parse(value);
                case ApplicationPropertyType.Ulong:
                    return ulong.Parse(value);
                case ApplicationPropertyType.Float:
                    return float.Parse(value);
                case ApplicationPropertyType.Decimal:
                    return decimal.Parse(value);
                case ApplicationPropertyType.Double:
                    return double.Parse(value);
                case ApplicationPropertyType.Char:
                    return char.Parse(value);
                case ApplicationPropertyType.Guid:
                    return Guid.Parse(value);
                case ApplicationPropertyType.DateTime:
                    return DateTime.Parse(value);
                case ApplicationPropertyType.DateTimeOffset:
                    return DateTimeOffset.Parse(value);
                case ApplicationPropertyType.Stream:
                    throw new InvalidOperationException("Cannot parse Stream from string.");
                case ApplicationPropertyType.Uri:
                    return new Uri(value);
                case ApplicationPropertyType.TimeSpan:
                    return TimeSpan.Parse(value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported type");
            }
        }
    }
}
