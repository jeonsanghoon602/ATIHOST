using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace HostSimulator.Domain
{
    public sealed class SecsItemData
    {
        public SecsDataFormat Format { get; }
        public object Value { get; }
        public ReadOnlyCollection<SecsItemData> Children { get; }

        private SecsItemData(SecsDataFormat format, object value, IList<SecsItemData> children)
        {
            Format = format;
            Value = value;
            Children = children != null
                ? new ReadOnlyCollection<SecsItemData>(children)
                : null;
        }

        public static SecsItemData CreateList(params SecsItemData[] children)
        {
            return new SecsItemData(SecsDataFormat.List, null, children ?? Array.Empty<SecsItemData>());
        }

        public static SecsItemData Create(SecsDataFormat format, object value)
        {
            if (format == SecsDataFormat.List)
                throw new ArgumentException("Use CreateList for List format.");
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return new SecsItemData(format, value, null);
        }

        public static SecsItemData Ascii(string value)
        {
            return Create(SecsDataFormat.Ascii, value ?? string.Empty);
        }

        public static SecsItemData Binary(byte[] value)
        {
            return Create(SecsDataFormat.Binary, value ?? Array.Empty<byte>());
        }

        public static SecsItemData Boolean(params bool[] value)
        {
            return Create(SecsDataFormat.Boolean, value ?? Array.Empty<bool>());
        }

        public static SecsItemData U1(params byte[] value)
        {
            return Create(SecsDataFormat.U1, value);
        }

        public static SecsItemData U2(params ushort[] value)
        {
            return Create(SecsDataFormat.U2, value);
        }

        public static SecsItemData U4(params uint[] value)
        {
            return Create(SecsDataFormat.U4, value);
        }

        public static SecsItemData I1(params sbyte[] value)
        {
            return Create(SecsDataFormat.I1, value);
        }

        public static SecsItemData I2(params short[] value)
        {
            return Create(SecsDataFormat.I2, value);
        }

        public static SecsItemData I4(params int[] value)
        {
            return Create(SecsDataFormat.I4, value);
        }

        public override string ToString()
        {
            if (Format == SecsDataFormat.List)
            {
                var sb = new StringBuilder();
                sb.Append("<L [");
                sb.Append(Children.Count);
                sb.Append("]");
                foreach (var child in Children)
                {
                    sb.Append(" ");
                    sb.Append(child.ToString());
                }
                sb.Append(">");
                return sb.ToString();
            }

            if (Format == SecsDataFormat.Ascii)
                return $"<A \"{Value}\">";

            if (Format == SecsDataFormat.Binary && Value is byte[] bytes)
                return $"<B [{ bytes.Length}] {BitConverter.ToString(bytes)}>";

            return $"<{Format} {FormatValue()}>";
        }

        private string FormatValue()
        {
            if (Value is Array arr)
                return string.Join(" ", arr.Cast<object>());
            return Value?.ToString() ?? "";
        }
    }
}
