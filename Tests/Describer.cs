using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting.Impl;
using PublicBroadcasting;

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
    }
}
