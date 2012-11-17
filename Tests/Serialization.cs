﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class Serialization
    {
        [TestMethod]
        public void Simple()
        {
            var bytes = Serializer.Serialize("Hello World");
            var str = Deserializer.Deserialize<string>(bytes);
            Assert.AreEqual("Hello World", str);

            bytes = Serializer.Serialize(12345);
            var ints = Deserializer.Deserialize<int>(bytes);
            Assert.AreEqual(12345, ints);
        }

        class A
        {
            public string Foo { get; set; }
        }

        class B
        {
            public string Foo { get; set; }
            public string Bar { get; set; }
        }

        private static T AnonHack<T>(T temp, byte[] bytes)
        {
            return Deserializer.Deserialize<T>(bytes);
        }

        [TestMethod]
        public void Class()
        {
            var bytes1 = Serializer.Serialize(new A { Foo = "Hello" });
            var a1 = Deserializer.Deserialize<A>(bytes1);
            Assert.AreEqual("Hello", a1.Foo);

            var b1 = Deserializer.Deserialize<B>(bytes1);
            Assert.AreEqual("Hello", b1.Foo);
            Assert.IsNull(b1.Bar);

            var bytes2 = Serializer.Serialize(new B { Foo = "Hello", Bar = "World" });
            var a2 = Deserializer.Deserialize<A>(bytes2);
            Assert.AreEqual("Hello", a2.Foo);

            var b2 = Deserializer.Deserialize<B>(bytes2);
            Assert.AreEqual("Hello", b2.Foo);
            Assert.AreEqual("World", b2.Bar);
        }

        [TestMethod]
        public void Lists()
        {
            var bytes1 = Serializer.Serialize(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            var list1 = Deserializer.Deserialize<List<int>>(bytes1);
            Assert.AreEqual(10, list1.Count);

            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(i + 1, list1[i]);
            }

            var bytes2 = Serializer.Serialize(new List<A> { new A { Foo = "1" }, new A { Foo = "2" }, new A { Foo = "3" } });
            var list2 = Deserializer.Deserialize<List<A>>(bytes2);
            Assert.AreEqual(3, list2.Count);

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual((i + 1).ToString(), list2[i].Foo);
            }
        }

        class AList
        {
            public string Foo {get;set;}
            public List<int> Bar {get;set;}
        }

        [TestMethod]
        public void ClassesWithLists()
        {
            var bytes = Serializer.Serialize(new AList { Foo = "Hello", Bar = new List<int> { 1, 2, 3 } });
            var alist = Deserializer.Deserialize<AList>(bytes);
            Assert.IsNotNull(alist);
            Assert.AreEqual("Hello", alist.Foo);
            Assert.IsNotNull(alist.Bar);
            Assert.AreEqual(3, alist.Bar.Count);

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(i + 1, alist.Bar[i]);
            }
        }
    }
}
