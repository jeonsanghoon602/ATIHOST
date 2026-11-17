using System;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class SecsItemDataTests
    {
        [Fact]
        public void CreateList_with_children()
        {
            var child1 = SecsItemData.Ascii("hello");
            var child2 = SecsItemData.U1(1, 2, 3);
            var list = SecsItemData.CreateList(child1, child2);

            Assert.Equal(SecsDataFormat.List, list.Format);
            Assert.Equal(2, list.Children.Count);
            Assert.Null(list.Value);
        }

        [Fact]
        public void CreateList_empty()
        {
            var list = SecsItemData.CreateList();
            Assert.Equal(SecsDataFormat.List, list.Format);
            Assert.Empty(list.Children);
        }

        [Fact]
        public void Ascii_stores_value()
        {
            var item = SecsItemData.Ascii("test");
            Assert.Equal(SecsDataFormat.Ascii, item.Format);
            Assert.Equal("test", item.Value);
            Assert.Null(item.Children);
        }

        [Fact]
        public void Binary_stores_value()
        {
            var bytes = new byte[] { 0x01, 0x02 };
            var item = SecsItemData.Binary(bytes);
            Assert.Equal(SecsDataFormat.Binary, item.Format);
            Assert.Equal(bytes, item.Value);
        }

        [Fact]
        public void Boolean_stores_value()
        {
            var item = SecsItemData.Boolean(true, false);
            Assert.Equal(SecsDataFormat.Boolean, item.Format);
            Assert.Equal(new[] { true, false }, (bool[])item.Value);
        }

        [Fact]
        public void U1_stores_value()
        {
            var item = SecsItemData.U1(10, 20);
            Assert.Equal(SecsDataFormat.U1, item.Format);
            Assert.Equal(new byte[] { 10, 20 }, (byte[])item.Value);
        }

        [Fact]
        public void U2_stores_value()
        {
            var item = SecsItemData.U2(1000, 2000);
            Assert.Equal(SecsDataFormat.U2, item.Format);
            Assert.Equal(new ushort[] { 1000, 2000 }, (ushort[])item.Value);
        }

        [Fact]
        public void U4_stores_value()
        {
            var item = SecsItemData.U4(100000);
            Assert.Equal(SecsDataFormat.U4, item.Format);
            Assert.Equal(new uint[] { 100000 }, (uint[])item.Value);
        }

        [Fact]
        public void I1_stores_value()
        {
            var item = SecsItemData.I1(-1, 0, 1);
            Assert.Equal(SecsDataFormat.I1, item.Format);
            Assert.Equal(new sbyte[] { -1, 0, 1 }, (sbyte[])item.Value);
        }

        [Fact]
        public void I2_stores_value()
        {
            var item = SecsItemData.I2(-100, 100);
            Assert.Equal(SecsDataFormat.I2, item.Format);
            Assert.Equal(new short[] { -100, 100 }, (short[])item.Value);
        }

        [Fact]
        public void I4_stores_value()
        {
            var item = SecsItemData.I4(42);
            Assert.Equal(SecsDataFormat.I4, item.Format);
            Assert.Equal(new int[] { 42 }, (int[])item.Value);
        }

        [Fact]
        public void Create_with_list_format_throws()
        {
            Assert.Throws<ArgumentException>(() => SecsItemData.Create(SecsDataFormat.List, "value"));
        }

        [Fact]
        public void Create_with_null_value_throws()
        {
            Assert.Throws<ArgumentNullException>(() => SecsItemData.Create(SecsDataFormat.Ascii, null));
        }

        [Fact]
        public void ToString_ascii()
        {
            var item = SecsItemData.Ascii("hello");
            Assert.Contains("A", item.ToString());
            Assert.Contains("hello", item.ToString());
        }

        [Fact]
        public void ToString_list()
        {
            var list = SecsItemData.CreateList(SecsItemData.Ascii("a"));
            Assert.Contains("L", list.ToString());
            Assert.Contains("1", list.ToString());
        }

        [Fact]
        public void ToString_binary()
        {
            var item = SecsItemData.Binary(new byte[] { 0xAB, 0xCD });
            Assert.Contains("B", item.ToString());
        }

        [Fact]
        public void ToString_numeric()
        {
            var item = SecsItemData.U4(42);
            Assert.Contains("42", item.ToString());
        }
    }
}
