using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using HostSimulator.Domain;
using Secs4Net;

namespace HostSimulator.Protocol
{
    internal sealed class SimpleBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer = new byte[256];
        private int _written;

        public void Advance(int count) { _written += count; }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Memory<byte>(_buffer, _written, _buffer.Length - _written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Span<byte>(_buffer, _written, _buffer.Length - _written);
        }

        public byte[] ToArray()
        {
            var result = new byte[_written];
            Array.Copy(_buffer, 0, result, 0, _written);
            return result;
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint < 1) sizeHint = 1;
            if (_written + sizeHint > _buffer.Length)
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _written + sizeHint));
        }
    }
}

namespace HostSimulator.Protocol
{
    public static class SecsMessageConverter
    {
        public static SecsMessage ToSecs4Net(SecsMessageData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var item = data.Item != null ? ConvertItemToSecs4Net(data.Item) : null;
            return new SecsMessage(data.Stream, data.Function, data.WBit)
            {
                SecsItem = item
            };
        }

        public static SecsMessageData FromSecs4Net(SecsMessage message, uint systemByte)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var item = message.SecsItem != null ? ConvertItemFromSecs4Net(message.SecsItem) : null;
            byte[] rawBytes = null;
            try
            {
                rawBytes = EncodeRawBytes(message);
            }
            catch
            {
            }

            return new SecsMessageData(
                message.S,
                message.F,
                message.ReplyExpected,
                systemByte,
                item,
                rawBytes);
        }

        private static Item ConvertItemToSecs4Net(SecsItemData data)
        {
            switch (data.Format)
            {
                case SecsDataFormat.List:
                    return Item.L(data.Children.Select(ConvertItemToSecs4Net).ToArray());
                case SecsDataFormat.Ascii:
                    return Item.A((string)data.Value);
                case SecsDataFormat.Binary:
                    return Item.B((byte[])data.Value);
                case SecsDataFormat.Boolean:
                    return Item.Boolean((bool[])data.Value);
                case SecsDataFormat.U1:
                    return Item.U1((byte[])data.Value);
                case SecsDataFormat.U2:
                    return Item.U2((ushort[])data.Value);
                case SecsDataFormat.U4:
                    return Item.U4((uint[])data.Value);
                case SecsDataFormat.I1:
                    return Item.I1((sbyte[])data.Value);
                case SecsDataFormat.I2:
                    return Item.I2((short[])data.Value);
                case SecsDataFormat.I4:
                    return Item.I4((int[])data.Value);
                case SecsDataFormat.F4:
                    return Item.F4((float[])data.Value);
                case SecsDataFormat.F8:
                    return Item.F8((double[])data.Value);
                case SecsDataFormat.U8:
                    return Item.U8((ulong[])data.Value);
                case SecsDataFormat.I8:
                    return Item.I8((long[])data.Value);
                case SecsDataFormat.Jis8:
                    return Item.A((string)data.Value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(data), $"Unsupported format: {data.Format}");
            }
        }

        public static SecsItemData ConvertItemFromSecs4Net(Item item)
        {
            if (item == null)
                return null;

            switch (item.Format)
            {
                case SecsFormat.List:
                    var children = new List<SecsItemData>();
                    foreach (var child in item.Items)
                        children.Add(ConvertItemFromSecs4Net(child));
                    return SecsItemData.CreateList(children.ToArray());

                case SecsFormat.ASCII:
                    return SecsItemData.Ascii(item.GetString());

                case SecsFormat.Binary:
                    return SecsItemData.Binary(item.GetMemory<byte>().ToArray());

                case SecsFormat.Boolean:
                    return SecsItemData.Boolean(item.GetMemory<bool>().ToArray());

                case SecsFormat.U1:
                    return SecsItemData.U1(item.GetMemory<byte>().ToArray());

                case SecsFormat.U2:
                    return SecsItemData.U2(item.GetMemory<ushort>().ToArray());

                case SecsFormat.U4:
                    return SecsItemData.Create(SecsDataFormat.U4, item.GetMemory<uint>().ToArray());

                case SecsFormat.U8:
                    return SecsItemData.Create(SecsDataFormat.U8, item.GetMemory<ulong>().ToArray());

                case SecsFormat.I1:
                    return SecsItemData.I1(item.GetMemory<sbyte>().ToArray());

                case SecsFormat.I2:
                    return SecsItemData.I2(item.GetMemory<short>().ToArray());

                case SecsFormat.I4:
                    return SecsItemData.I4(item.GetMemory<int>().ToArray());

                case SecsFormat.I8:
                    return SecsItemData.Create(SecsDataFormat.I8, item.GetMemory<long>().ToArray());

                case SecsFormat.F4:
                    return SecsItemData.Create(SecsDataFormat.F4, item.GetMemory<float>().ToArray());

                case SecsFormat.F8:
                    return SecsItemData.Create(SecsDataFormat.F8, item.GetMemory<double>().ToArray());

                case SecsFormat.JIS8:
                    return SecsItemData.Create(SecsDataFormat.Jis8, item.GetString());

                default:
                    return SecsItemData.Binary(Array.Empty<byte>());
            }
        }

        private static byte[] EncodeRawBytes(SecsMessage message)
        {
            if (message.SecsItem == null)
                return Array.Empty<byte>();

            var writer = new SimpleBufferWriter();
            message.SecsItem.EncodeTo(writer);
            return writer.ToArray();
        }
    }
}
