using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            bytes = Serializer.Serialize(true);
            var @bool = Deserializer.Deserialize<bool>(bytes);
            Assert.IsTrue(@bool);
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

        [TestMethod]
        public void Dictionaries()
        {
            var bytes1 = Serializer.Serialize(new Dictionary<string, int> { {"1", 1}, {"2", 2}, {"3", 3 } });
            var dicts1 = Deserializer.Deserialize<Dictionary<string, int>>(bytes1);
            Assert.AreEqual(3, dicts1.Count);

            for (var i = 1; i <= 3; i++)
            {
                Assert.AreEqual(i, dicts1[i.ToString()]);
            }

            var bytes2 = Serializer.Serialize(new Dictionary<string, A> { { "1", new A { Foo = "2" } }, { "2", new A { Foo = "3" } }, { "3", new A { Foo = "4" } } });
            var dicts2 = Deserializer.Deserialize<Dictionary<string, A>>(bytes2);
            Assert.AreEqual(3, dicts2.Count);

            for (var i = 1; i <= 3; i++)
            {
                Assert.AreEqual((i+1).ToString(), dicts2[i.ToString()].Foo);
            }
        }

        class ADict
        {
            public string Foo {get;set;}
            public Dictionary<string, int> Bar {get;set;}
        }

        [TestMethod]
        public void ClassesWithDictionaries()
        {
            var bytes = Serializer.Serialize(new ADict { Foo = "Hello", Bar = new Dictionary<string, int> { { "1", 11 }, { "2", 22 } } });
            var adict = Deserializer.Deserialize<ADict>(bytes);
            Assert.IsNotNull(adict);
            Assert.AreEqual("Hello", adict.Foo);
            Assert.IsNotNull(adict.Bar);
            Assert.AreEqual(2, adict.Bar.Count);
            Assert.IsTrue(adict.Bar.Keys.Contains("1"));
            Assert.IsTrue(adict.Bar.Keys.Contains("2"));
            Assert.AreEqual(11, adict.Bar["1"]);
            Assert.AreEqual(22, adict.Bar["2"]);
        }

        class Circle
        {
            public string Text {get;set;}
            public Circle Next {get;set;}
        }

        [TestMethod]
        public void CircularClass()
        {
            var bytes = Serializer.Serialize(new Circle { Text = "1", Next = new Circle { Text = "2", Next = new Circle { Text = "3" } } });
            var circle = Deserializer.Deserialize<Circle>(bytes);
            Assert.IsNotNull(circle);
            Assert.AreEqual("1", circle.Text);
            Assert.IsNotNull(circle.Next);
            Assert.AreEqual("2", circle.Next.Text);
            Assert.IsNotNull(circle.Next.Next);
            Assert.AreEqual("3", circle.Next.Next.Text);
            Assert.IsNull(circle.Next.Next.Next);
        }

        class BDict
        {
            public string Bar { get; set; }
            public Dictionary<string, A> Buzz { get; set; }
        }

        [TestMethod]
        public void MoreDictionaries()
        {
            var bytes = Serializer.Serialize(new BDict { Bar = "Hello", Buzz = new Dictionary<string, A> { { "1", new A { Foo = "Bar" } } } });
            var bdict = Deserializer.Deserialize<BDict>(bytes);
        }

        class Compl
        {
            public class Sub
            {
                public string Hello { get; set; }
                public Compl SubOther { get; set; }
            }

            public int Foo;
            public string Bar;
            public char Car;
            public Dictionary<string, int> Bits { get; set; }
            public List<sbyte> Stuff { get; set; }

            public Dictionary<Compl, Compl> Pairs;

            public Compl Other { get; set; }

            public Sub SubObj { get; set; }
        }

        [TestMethod]
        public void Complicated()
        {
            Compl obj = new Compl();
            obj.Foo = 123;
            obj.Bar = "Bizz";
            obj.Car = 'c';
            obj.Bits = new Dictionary<string, int> { { "Do Stuff", 321 }, { "Other Stuff", 456 } };
            obj.Pairs = new Dictionary<Compl, Compl> { { new Compl { Foo = 3141519 }, new Compl { SubObj = new Compl.Sub { Hello = "World" } } } };
            obj.Other = new Compl { Car = 'd', SubObj = new Compl.Sub { SubOther = new Compl { Bar = "Foo" }, Hello = "!!!" } };
            obj.SubObj = new Compl.Sub { Hello = "Thing" };

            var bytes = Serializer.Serialize(obj);
            var de = Deserializer.Deserialize<Compl>(bytes);

            Assert.IsNotNull(de);
            Assert.AreEqual(obj.Foo, de.Foo);
            Assert.AreEqual(obj.Bar, de.Bar);
            Assert.AreEqual(obj.Car, de.Car);
            Assert.AreEqual(2, de.Bits.Count);
            Assert.AreEqual(321, de.Bits["Do Stuff"]);
            Assert.AreEqual(456, de.Bits["Other Stuff"]);
            Assert.AreEqual(3141519, de.Pairs.Single().Key.Foo);
            Assert.AreEqual("World", de.Pairs.Single().Value.SubObj.Hello);
            Assert.AreEqual('d', de.Other.Car);
            Assert.AreEqual("Foo", de.Other.SubObj.SubOther.Bar);
            Assert.AreEqual("!!!", de.Other.SubObj.Hello);
            Assert.AreEqual("Thing", de.SubObj.Hello);
        }

        class NullObj
        {
            public int Foo { get; set; }
            public int? Bar { get; set; }
            public byte? Fizz { get; set; }
            public string Buzz { get; set; }

            public NullObj Back { get; set; }
        }

        [TestMethod]
        public void Nullable()
        {
            var bytes = Serializer.Serialize(new NullObj { Bar = 123, Back = new NullObj { Foo = 456, Buzz = "World", Back = new NullObj { Fizz = 222 } } });
            var n = Deserializer.Deserialize<NullObj>(bytes);

            Assert.IsNotNull(n);
            Assert.AreEqual(0, n.Foo);
            Assert.AreEqual(123, n.Bar);
            Assert.IsFalse(n.Fizz.HasValue);
            Assert.IsNull(n.Buzz);
            Assert.IsNotNull(n.Back);

            Assert.AreEqual(456, n.Back.Foo);
            Assert.IsFalse(n.Back.Bar.HasValue);
            Assert.IsFalse(n.Back.Fizz.HasValue);
            Assert.AreEqual("World", n.Back.Buzz);
            Assert.IsNotNull(n.Back.Back);

            Assert.AreEqual(222, n.Back.Back.Fizz.Value);
        }
    }
}
