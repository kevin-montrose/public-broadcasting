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
            Assert.AreEqual(SimpleTypeDescription.Long, AllPublicDescriber<long>.Get());
            Assert.AreEqual(SimpleTypeDescription.ULong, AllPublicDescriber<ulong>.Get());
            Assert.AreEqual(SimpleTypeDescription.Int, AllPublicDescriber<int>.Get());
            Assert.AreEqual(SimpleTypeDescription.UInt, AllPublicDescriber<uint>.Get());
            Assert.AreEqual(SimpleTypeDescription.Short, AllPublicDescriber<short>.Get());
            Assert.AreEqual(SimpleTypeDescription.UShort, AllPublicDescriber<ushort>.Get());
            Assert.AreEqual(SimpleTypeDescription.Byte, AllPublicDescriber<byte>.Get());
            Assert.AreEqual(SimpleTypeDescription.SByte, AllPublicDescriber<sbyte>.Get());
            Assert.AreEqual(SimpleTypeDescription.Char, AllPublicDescriber<char>.Get());
            Assert.AreEqual(SimpleTypeDescription.String, AllPublicDescriber<string>.Get());
            Assert.AreEqual(SimpleTypeDescription.Decimal, AllPublicDescriber<decimal>.Get());
            Assert.AreEqual(SimpleTypeDescription.Double, AllPublicDescriber<double>.Get());
            Assert.AreEqual(SimpleTypeDescription.Float, AllPublicDescriber<float>.Get());
        }

        [TestMethod]
        public void Dictionary()
        {
            var strToStr = AllPublicDescriber<Dictionary<string, string>>.Get();
            Assert.AreEqual(typeof(DictionaryTypeDescription), strToStr.GetType());
            Assert.AreEqual(SimpleTypeDescription.String, (strToStr as DictionaryTypeDescription).KeyType);
            Assert.AreEqual(SimpleTypeDescription.String, (strToStr as DictionaryTypeDescription).ValueType);

            var strToInt = AllPublicDescriber<Dictionary<string, int>>.Get();
            Assert.AreEqual(typeof(DictionaryTypeDescription), strToInt.GetType());
            Assert.AreEqual(SimpleTypeDescription.String, (strToInt as DictionaryTypeDescription).KeyType);
            Assert.AreEqual(SimpleTypeDescription.Int, (strToInt as DictionaryTypeDescription).ValueType);
        }

        [TestMethod]
        public void List()
        {
            var ints = AllPublicDescriber<List<int>>.Get();
            Assert.AreEqual(typeof(ListTypeDescription), ints.GetType());
            Assert.AreEqual(SimpleTypeDescription.Int, (ints as ListTypeDescription).Contains);

            var chars = AllPublicDescriber<char[]>.Get();
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
            var c = AllPublicDescriber<Dummy>.Get();
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
            var c = AllPublicDescriber<Circular>.GetForUse(true);
            Assert.AreEqual(typeof(ClassTypeDescription), c.GetType());
            var asClass = c as ClassTypeDescription;

            Assert.AreEqual(2, asClass.Members.Count);
            Assert.IsTrue(asClass.Members.ContainsKey("Str"));
            Assert.IsTrue(asClass.Members.ContainsKey("Next"));

            Assert.AreEqual(SimpleTypeDescription.String, asClass.Members["Str"]);

            var backRef = asClass.Members["Next"] as BackReferenceTypeDescription;
            Assert.IsNotNull(backRef);
            Assert.AreEqual(asClass.Id, backRef.Id);
        }

        enum E
        {
            A,
            B
        }
        #pragma warning disable 0649
        class MultiEnum
        {
            public class Sub
            {
                public MultiEnum Outer;
                public E? Buzz;
            }

            public E Foo;
            public E Bar;
            public Sub Fizz;
        }
        #pragma warning restore 0649

        [TestMethod]
        public void MultiUseEnums()
        {
            var c = AllPublicDescriber<MultiEnum>.GetForUse(true);
            Assert.AreEqual(typeof(ClassTypeDescription), c.GetType());

            var asClass = (ClassTypeDescription)c;
            Assert.AreEqual(3, asClass.Members.Count);
            Assert.AreEqual(typeof(EnumTypeDescription), asClass.Members["Foo"].GetType());
            Assert.AreEqual(typeof(BackReferenceTypeDescription), asClass.Members["Bar"].GetType());
            Assert.AreEqual(typeof(ClassTypeDescription), asClass.Members["Fizz"].GetType());

            var sub = (ClassTypeDescription)asClass.Members["Fizz"];
            var bar = (BackReferenceTypeDescription)asClass.Members["Bar"];

            Assert.AreEqual(2, sub.Members.Count);
            Assert.AreEqual(typeof(BackReferenceTypeDescription), sub.Members["Outer"].GetType());
            Assert.AreEqual(typeof(NullableTypeDescription), sub.Members["Buzz"].GetType());

            var buzz = (NullableTypeDescription)sub.Members["Buzz"];
            Assert.AreEqual(typeof(BackReferenceTypeDescription), buzz.InnerType.GetType());

            var buzzInner = (BackReferenceTypeDescription)buzz.InnerType;
        }
    }
}
