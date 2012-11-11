using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting.Impl;
using PublicBroadcasting;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class Describer
    {
        [TestMethod]
        public void Simple()
        {
            Assert.AreEqual(SimpleTypeDescription.Long, Describer<long>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.ULong, Describer<ulong>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.Int, Describer<int>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.UInt, Describer<uint>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.Short, Describer<short>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.UShort, Describer<ushort>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.Byte, Describer<byte>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.SByte, Describer<sbyte>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.Char, Describer<char>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.String, Describer<string>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.Decimal, Describer<decimal>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.Double, Describer<double>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
            Assert.AreEqual(SimpleTypeDescription.Float, Describer<float>.Get(IncludedMembers.Properties, IncludedVisibility.Public));
        }

        [TestMethod]
        public void Dictionary()
        {
            var strToStr = Describer<Dictionary<string, string>>.Get(IncludedMembers.Properties, IncludedVisibility.Public);
            Assert.AreEqual(typeof(DictionaryTypeDescription), strToStr.GetType());
            Assert.AreEqual(SimpleTypeDescription.String, (strToStr as DictionaryTypeDescription).KeyType);
            Assert.AreEqual(SimpleTypeDescription.String, (strToStr as DictionaryTypeDescription).ValueType);

            var strToInt = Describer<Dictionary<string, int>>.Get(IncludedMembers.Properties, IncludedVisibility.Public);
            Assert.AreEqual(typeof(DictionaryTypeDescription), strToInt.GetType());
            Assert.AreEqual(SimpleTypeDescription.String, (strToInt as DictionaryTypeDescription).KeyType);
            Assert.AreEqual(SimpleTypeDescription.Int, (strToInt as DictionaryTypeDescription).ValueType);
        }

        [TestMethod]
        public void List()
        {
            var ints = Describer<List<int>>.Get(IncludedMembers.Properties, IncludedVisibility.Public);
            Assert.AreEqual(typeof(ListTypeDescription), ints.GetType());
            Assert.AreEqual(SimpleTypeDescription.Int, (ints as ListTypeDescription).Contains);

            var chars = Describer<char[]>.Get(IncludedMembers.Properties, IncludedVisibility.Public);
            Assert.AreEqual(typeof(ListTypeDescription), chars.GetType());
            Assert.AreEqual(SimpleTypeDescription.Char, (chars as ListTypeDescription).Contains);
        }
    }
}
