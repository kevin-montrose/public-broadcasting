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

        class Dummy
        {
            public string Str { get; set; }
            public int Int { get; set; }
            public double Double { get; set; }
        }

        [TestMethod]
        public void Class()
        {
            var c = Describer<Dummy>.Get(IncludedMembers.Properties, IncludedVisibility.Public);
            Assert.AreEqual(typeof(ClassTypeDescription), c.GetType());
            var asClass = c as ClassTypeDescription;

            Assert.AreEqual(3, asClass.Members.Count);
            Assert.IsTrue(asClass.Members.ContainsKey("Str"));
            Assert.IsTrue(asClass.Members.ContainsKey("Int"));
            Assert.IsTrue(asClass.Members.ContainsKey("Double"));

            Assert.AreEqual(SimpleTypeDescription.String, asClass.Members["Str"]);
            Assert.AreEqual(SimpleTypeDescription.Int, asClass.Members["Int"]);
            Assert.AreEqual(SimpleTypeDescription.Double, asClass.Members["Double"]);
        }

        class Circular
        {
            public string Str { get; set; }
            public Circular Next { get; set; }
        }

        [TestMethod]
        public void CircularClass()
        {
            var c = Describer<Circular>.Get(IncludedMembers.Properties, IncludedVisibility.Public);
            Assert.AreEqual(typeof(ClassTypeDescription), c.GetType());
            var asClass = c as ClassTypeDescription;

            Assert.AreEqual(2, asClass.Members.Count);
            Assert.IsTrue(asClass.Members.ContainsKey("Str"));
            Assert.IsTrue(asClass.Members.ContainsKey("Next"));

            Assert.AreEqual(SimpleTypeDescription.String, asClass.Members["Str"]);

            var backRef = asClass.Members["Next"] as BackReferenceTypeDescription;
            Assert.IsNotNull(backRef);
            Assert.AreEqual(asClass.Id, backRef.ClassId);
        }
    }
}
