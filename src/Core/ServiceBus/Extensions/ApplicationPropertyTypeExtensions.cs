using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Extensions;

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
}
