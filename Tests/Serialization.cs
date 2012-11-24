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
        class SingleC
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
        }

        struct SingleS
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
        }

        class SingleCC
        {
            public string Foo;
            public int Bar;
        }

        struct SingleSS
        {
            public string Foo;
            public int Bar;
        }

        [TestMethod]
        public void Widen()
        {
            //Byte -> Short, UShort, Integer, UInteger, Long, ULong, Decimal, Single, Double

            var b = Serializer.Serialize((byte)123);

            Assert.AreEqual(123, Deserializer.Deserialize<short>(b));
            Assert.AreEqual((ushort)123, Deserializer.Deserialize<ushort>(b));
            Assert.AreEqual(123, Deserializer.Deserialize<int>(b));
            Assert.AreEqual((uint)123, Deserializer.Deserialize<uint>(b));
            Assert.AreEqual(123, Deserializer.Deserialize<long>(b));
            Assert.AreEqual((ulong)123, Deserializer.Deserialize<ulong>(b));
            Assert.AreEqual(123.0f, Deserializer.Deserialize<float>(b));
            Assert.AreEqual(123.0, Deserializer.Deserialize<double>(b));
            Assert.AreEqual((decimal)123.0, Deserializer.Deserialize<decimal>(b));

            // SByte -> Short, Integer, Long, Decimal, Single, Double

            var sb = Serializer.Serialize((sbyte)123);

            Assert.AreEqual((short)123, Deserializer.Deserialize<short>(sb));
            Assert.AreEqual((int)123, Deserializer.Deserialize<int>(sb));
            Assert.AreEqual((long)123, Deserializer.Deserialize<long>(sb));
            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(sb));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(sb));
            Assert.AreEqual((decimal)123, Deserializer.Deserialize<decimal>(sb));

            // Short -> Integer, Long, Decimal, Single, Double

            var s = Serializer.Serialize((short)123);

            Assert.AreEqual((int)123, Deserializer.Deserialize<int>(s));
            Assert.AreEqual((long)123, Deserializer.Deserialize<long>(s));
            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(s));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(s));
            Assert.AreEqual((decimal)123, Deserializer.Deserialize<decimal>(s));

            // UShort -> Integer, UInteger, Long, ULong, Decimal, Single, Double

            var us = Serializer.Serialize((ushort)123);

            Assert.AreEqual((int)123, Deserializer.Deserialize<int>(us));
            Assert.AreEqual((uint)123, Deserializer.Deserialize<uint>(us));
            Assert.AreEqual((long)123, Deserializer.Deserialize<long>(us));
            Assert.AreEqual((ulong)123, Deserializer.Deserialize<ulong>(us));
            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(us));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(us));
            Assert.AreEqual((decimal)123, Deserializer.Deserialize<decimal>(us));

            // Integer -> Long, Decimal, Single, Double

            var i = Serializer.Serialize((int)123);

            Assert.AreEqual((long)123, Deserializer.Deserialize<long>(i));
            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(i));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(i));
            Assert.AreEqual((decimal)123, Deserializer.Deserialize<decimal>(i));

            // UInteger -> Long, ULong, Decimal, Single, Double

            var ui = Serializer.Serialize((uint)123);

            Assert.AreEqual((long)123, Deserializer.Deserialize<long>(ui));
            Assert.AreEqual((ulong)123, Deserializer.Deserialize<ulong>(ui));
            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(ui));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(ui));
            Assert.AreEqual((decimal)123, Deserializer.Deserialize<decimal>(ui));

            // Long -> Decimal, Single, Double

            var l = Serializer.Serialize((long)123);

            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(l));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(l));
            Assert.AreEqual((decimal)123, Deserializer.Deserialize<decimal>(l));

            // ULong -> Decimal, Single, Double

            var ul = Serializer.Serialize((ulong)123);

            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(ul));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(ul));
            Assert.AreEqual((decimal)123, Deserializer.Deserialize<decimal>(ul));

            // Decimal -> Single, Double

            var m = Serializer.Serialize((decimal)123);

            Assert.AreEqual((float)123, Deserializer.Deserialize<float>(m));
            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(m));

            // Single -> Double

            var f = Serializer.Serialize((float)123);

            Assert.AreEqual((double)123, Deserializer.Deserialize<double>(f));
        }

        [TestMethod]
        public void Narrows()
        {
            Action<Action> narrowThrows =
                act =>
                {
                    try
                    {
                        act();
                        Assert.Fail("Should have thrown narrowing exception");
                    }
                    catch (Exception e)
                    {
                        while(e.InnerException != null) e = e.InnerException;

                        Assert.IsTrue(e.Message.StartsWith("Illegal narrowing conversion "));
                    }
                };

            var b = Serializer.Serialize((byte)123);
            var sb = Serializer.Serialize((sbyte)123);
            var s = Serializer.Serialize((short)123);
            var us = Serializer.Serialize((ushort)123);
            var i = Serializer.Serialize((int)123);
            var ui = Serializer.Serialize((uint)123);
            var l = Serializer.Serialize((long)123);
            var ul = Serializer.Serialize((ulong)123);
            var f = Serializer.Serialize((float)123);
            var d = Serializer.Serialize((double)123);
            var m = Serializer.Serialize((decimal)123);

            narrowThrows(() => Deserializer.Deserialize<byte>(sb));
            narrowThrows(() => Deserializer.Deserialize<byte>(s));
            narrowThrows(() => Deserializer.Deserialize<byte>(us));
            narrowThrows(() => Deserializer.Deserialize<byte>(i));
            narrowThrows(() => Deserializer.Deserialize<byte>(ui));
            narrowThrows(() => Deserializer.Deserialize<byte>(l));
            narrowThrows(() => Deserializer.Deserialize<byte>(ul));
            narrowThrows(() => Deserializer.Deserialize<byte>(f));
            narrowThrows(() => Deserializer.Deserialize<byte>(d));
            narrowThrows(() => Deserializer.Deserialize<byte>(m));

            narrowThrows(() => Deserializer.Deserialize<sbyte>(b));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(s));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(us));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(i));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(ui));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(l));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(ul));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(f));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(d));
            narrowThrows(() => Deserializer.Deserialize<sbyte>(m));

            narrowThrows(() => Deserializer.Deserialize<short>(us));
            narrowThrows(() => Deserializer.Deserialize<short>(i));
            narrowThrows(() => Deserializer.Deserialize<short>(ui));
            narrowThrows(() => Deserializer.Deserialize<short>(l));
            narrowThrows(() => Deserializer.Deserialize<short>(ul));
            narrowThrows(() => Deserializer.Deserialize<short>(f));
            narrowThrows(() => Deserializer.Deserialize<short>(d));
            narrowThrows(() => Deserializer.Deserialize<short>(m));

            narrowThrows(() => Deserializer.Deserialize<ushort>(s));
            narrowThrows(() => Deserializer.Deserialize<ushort>(i));
            narrowThrows(() => Deserializer.Deserialize<ushort>(ui));
            narrowThrows(() => Deserializer.Deserialize<ushort>(l));
            narrowThrows(() => Deserializer.Deserialize<ushort>(ul));
            narrowThrows(() => Deserializer.Deserialize<ushort>(f));
            narrowThrows(() => Deserializer.Deserialize<ushort>(d));
            narrowThrows(() => Deserializer.Deserialize<ushort>(m));

            narrowThrows(() => Deserializer.Deserialize<int>(ui));
            narrowThrows(() => Deserializer.Deserialize<int>(l));
            narrowThrows(() => Deserializer.Deserialize<int>(ul));
            narrowThrows(() => Deserializer.Deserialize<int>(f));
            narrowThrows(() => Deserializer.Deserialize<int>(d));
            narrowThrows(() => Deserializer.Deserialize<int>(m));

            narrowThrows(() => Deserializer.Deserialize<uint>(i));
            narrowThrows(() => Deserializer.Deserialize<uint>(l));
            narrowThrows(() => Deserializer.Deserialize<uint>(ul));
            narrowThrows(() => Deserializer.Deserialize<uint>(f));
            narrowThrows(() => Deserializer.Deserialize<uint>(d));
            narrowThrows(() => Deserializer.Deserialize<uint>(m));

            narrowThrows(() => Deserializer.Deserialize<long>(ul));
            narrowThrows(() => Deserializer.Deserialize<long>(f));
            narrowThrows(() => Deserializer.Deserialize<long>(d));
            narrowThrows(() => Deserializer.Deserialize<long>(m));

            narrowThrows(() => Deserializer.Deserialize<ulong>(l));
            narrowThrows(() => Deserializer.Deserialize<ulong>(f));
            narrowThrows(() => Deserializer.Deserialize<ulong>(d));
            narrowThrows(() => Deserializer.Deserialize<ulong>(m));

            narrowThrows(() => Deserializer.Deserialize<decimal>(f));
            narrowThrows(() => Deserializer.Deserialize<decimal>(d));

            narrowThrows(() => Deserializer.Deserialize<float>(d));
        }

        [TestMethod]
        public void ILConversion()
        {
            var bytes = Serializer.Serialize(new SingleC { Foo = "Bar", Bar = 123 });
            var c = Deserializer.Deserialize<SingleC>(bytes);
            var s = Deserializer.Deserialize<SingleS>(bytes);
            var cc = Deserializer.Deserialize<SingleCC>(bytes);
            var ss = Deserializer.Deserialize<SingleSS>(bytes);

            Assert.AreEqual("Bar", c.Foo);
            Assert.AreEqual(123, c.Bar);

            Assert.AreEqual("Bar", s.Foo);
            Assert.AreEqual(123, s.Bar);

            Assert.AreEqual("Bar", cc.Foo);
            Assert.AreEqual(123, cc.Bar);

            Assert.AreEqual("Bar", ss.Foo);
            Assert.AreEqual(123, ss.Bar);

            Assert.IsTrue(bytes.SequenceEqual(Serializer.Serialize(new SingleS { Foo = "Bar", Bar = 123 })));
            Assert.IsTrue(bytes.SequenceEqual(Serializer.Serialize(new SingleCC { Foo = "Bar", Bar = 123 })));
            Assert.IsTrue(bytes.SequenceEqual(Serializer.Serialize(new SingleSS { Foo = "Bar", Bar = 123 })));
        }

        class HasNull
        {
            public int? Foo { get; set; }
        }

        class NoNull
        {
            public int Foo { get; set; }
        }

        [TestMethod]
        public void NullableConversions()
        {
            var b1 = Serializer.Serialize(new HasNull { Foo = 123 });
            var b2 = Serializer.Serialize(new HasNull());
            var b3 = Serializer.Serialize(new NoNull { Foo = 456 });
            var b4 = Serializer.Serialize(new NoNull());

            var nn1 = Deserializer.Deserialize<NoNull>(b1);
            var nn2 = Deserializer.Deserialize<NoNull>(b2);
            var hn1 = Deserializer.Deserialize<HasNull>(b3);
            var hn2 = Deserializer.Deserialize<HasNull>(b4);

            Assert.AreEqual(123, nn1.Foo);
            Assert.AreEqual(0, nn2.Foo);
            Assert.AreEqual(456, hn1.Foo.Value);
            Assert.AreEqual(0, hn2.Foo.Value);
        }

        class ConstDict : Dictionary<int, string> { }
        class OneDict<Key> : Dictionary<Key, string> { }
        class TwoDict<Key, Value> : Dictionary<Key, Value> { }

        [TestMethod]
        public void DictionaryConversion()
        {
            var b1 = Serializer.Serialize(new Dictionary<int, string> { { 1, "foo" }, { 2, "bar" } });
            var d1 = Deserializer.Deserialize<Dictionary<int, string>>(b1);
            var d2 = Deserializer.Deserialize<IDictionary<int, string>>(b1);
            var d3 = Deserializer.Deserialize<ConstDict>(b1);
            var d4 = Deserializer.Deserialize<OneDict<int>>(b1);
            var d5 = Deserializer.Deserialize<TwoDict<int, string>>(b1);

            var b2 = Serializer.Serialize(new ConstDict { { 1, "123" } });
            var b3 = Serializer.Serialize(new OneDict<int> { { 1, "123" } });
            var b4 = Serializer.Serialize(new TwoDict<int, string> { { 1, "123" } });

            var d6 = Deserializer.Deserialize<Dictionary<int, string>>(b2);
            var d7 = Deserializer.Deserialize<Dictionary<int, string>>(b3);
            var d8 = Deserializer.Deserialize<Dictionary<int, string>>(b4);

            try
            {
                Deserializer.Deserialize<System.Collections.IDictionary>(b1);
                Assert.Fail("Shouldn't be able to deserialize to a non-generic IDictionary");
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;

                Assert.IsTrue(e.Message.EndsWith(" is not a valid deserialization target, expected an IDictionary<Key, Value>"));
            }

            Assert.AreEqual(2, d1.Count);
            Assert.AreEqual("foo", d1[1]);
            Assert.AreEqual("bar", d1[2]);

            Assert.AreEqual(2, d2.Count);
            Assert.AreEqual("foo", d2[1]);
            Assert.AreEqual("bar", d2[2]);

            Assert.AreEqual(2, d3.Count);
            Assert.AreEqual("foo", d3[1]);
            Assert.AreEqual("bar", d3[2]);

            Assert.AreEqual(2, d4.Count);
            Assert.AreEqual("foo", d4[1]);
            Assert.AreEqual("bar", d4[2]);

            Assert.AreEqual(2, d5.Count);
            Assert.AreEqual("foo", d5[1]);
            Assert.AreEqual("bar", d5[2]);

            Assert.AreEqual(1, d6.Count);
            Assert.AreEqual("123", d6[1]);

            Assert.AreEqual(1, d7.Count);
            Assert.AreEqual("123", d7[1]);

            Assert.AreEqual(1, d8.Count);
            Assert.AreEqual("123", d8[1]);
        }

        class ConstList : List<string> { }
        class OneList<T> : List<T> { }

        [TestMethod]
        public void ListConversion()
        {
            var b1 = Serializer.Serialize(new List<string> { "foo", "bar" });
            var d1 = Deserializer.Deserialize<List<string>>(b1);
            var d2 = Deserializer.Deserialize<IList<string>>(b1);
            var d3 = Deserializer.Deserialize<ConstList>(b1);
            var d4 = Deserializer.Deserialize<OneList<string>>(b1);

            var b2 = Serializer.Serialize(new ConstList { "123" });
            var b3 = Serializer.Serialize(new OneList<string> { "123" });

            var d5 = Deserializer.Deserialize<List<string>>(b2);
            var d6 = Deserializer.Deserialize<List<string>>(b3);

            try
            {
                Deserializer.Deserialize<System.Collections.IList>(b1);
                Assert.Fail("Shouldn't be able to deserialize to a non-generic IList");
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;

                Assert.IsTrue(e.Message.EndsWith(" is not a valid deserialization target, expected an IList<T>"));
            }

            Assert.AreEqual(2, d1.Count);
            Assert.AreEqual("foo", d1[0]);
            Assert.AreEqual("bar", d1[1]);

            Assert.AreEqual(2, d2.Count);
            Assert.AreEqual("foo", d2[0]);
            Assert.AreEqual("bar", d2[1]);

            Assert.AreEqual(2, d3.Count);
            Assert.AreEqual("foo", d3[0]);
            Assert.AreEqual("bar", d3[1]);

            Assert.AreEqual(2, d4.Count);
            Assert.AreEqual("foo", d4[0]);
            Assert.AreEqual("bar", d4[1]);

            Assert.AreEqual(1, d5.Count);
            Assert.AreEqual("123", d5[0]);

            Assert.AreEqual(1, d6.Count);
            Assert.AreEqual("123", d6[0]);
        }

        struct S
        {
            public struct Blah
            {
                public int Foo { get; set; }
                public string Bar { get; set; }
            }

            public int Foo { get; set; }

            public string Bar { get; set; }

            public Blah Next { get; set; }
        }

        [TestMethod]
        public void Structs()
        {
            var bytes = Serializer.Serialize(new S { Foo = 123, Bar = "Hello", Next = new S.Blah { Foo = 456, Bar = "World" } });
            var s = Deserializer.Deserialize<S>(bytes);

            Assert.AreEqual(123, s.Foo);
            Assert.AreEqual("Hello", s.Bar);
            Assert.AreNotEqual(default(S.Blah), s.Next);
            Assert.AreEqual(456, s.Next.Foo);
            Assert.AreEqual("World", s.Next.Bar);
        }

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

        [TestMethod]
        public void DateTimes()
        {
            var lastWeek = DateTime.UtcNow - TimeSpan.FromDays(7);

            var bytes = Serializer.Serialize(lastWeek);
            var dt = Deserializer.Deserialize<DateTime>(bytes);

            Assert.AreEqual(lastWeek, dt);
        }

        [TestMethod]
        public void Uris()
        {
            var uri = new Uri("http://www.example.com/test");

            var bytes = Serializer.Serialize(uri);
            var u = Deserializer.Deserialize<Uri>(bytes);

            Assert.AreEqual(uri, u);
        }

        [TestMethod]
        public void Guids()
        {
            var guid = Guid.NewGuid();

            var bytes = Serializer.Serialize(guid);
            var g = Deserializer.Deserialize<Guid>(bytes);

            Assert.AreEqual(guid, g);
        }

        [TestMethod]
        public void TimeSpans()
        {
            var twoDays = TimeSpan.FromDays(2);

            var bytes = Serializer.Serialize(twoDays);
            var ts = Deserializer.Deserialize<TimeSpan>(bytes);

            Assert.AreEqual(twoDays, ts);
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

            Assert.AreEqual("Hello", bdict.Bar);
            Assert.AreEqual(1, bdict.Buzz.Count);
            Assert.AreEqual("Bar", bdict.Buzz["1"].Foo);
        }

        class BList
        {
            public string Bar {get; set;}
            public List<BList> Others { get; set; }
        }

        [TestMethod]
        public void MoreLists()
        {
            var bytes = Serializer.Serialize(new BList { Bar = "Hello", Others = new List<BList> { new BList { Bar = "Foo" }, new BList { Bar = "Bizz", Others = new List<BList> { new BList { Bar = "Bazz" } } } } });
            var blist = Deserializer.Deserialize<BList>(bytes);

            Assert.AreEqual(blist.Bar, "Hello");
            Assert.AreEqual(2, blist.Others.Count);
            Assert.AreEqual("Foo", blist.Others[0].Bar);
            Assert.AreEqual("Bizz", blist.Others[1].Bar);
            Assert.AreEqual(1, blist.Others[1].Others.Count);
            Assert.AreEqual("Bazz", blist.Others[1].Others[0].Bar);
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
            var obj = new NullObj { Bar = 123, Back = new NullObj { Foo = 456, Buzz = "World", Back = new NullObj { Fizz = 222 } } };

            var bytes = Serializer.Serialize(obj);
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

        private T DeserializeByExample<T>(T example, byte[] bytes)
        {
            return Deserializer.Deserialize<T>(bytes);
        }

        [TestMethod]
        public void Anon()
        {
            var anonExample = new { String = "str", Int = 123 };

            var bytes = Serializer.Serialize(new { String = "Foo", Int = 456 });
            var de = DeserializeByExample(anonExample, bytes);

            Assert.AreEqual("Foo", de.String);
            Assert.AreEqual(456, de.Int);
        }

        class Arr
        {
            public string Foo { get; set; }
            public double[] Bar { get; set; }
        }

        class NoArr
        {
            public string Foo { get; set; }
            public IList<double> Bar { get; set; }
        }

        [TestMethod]
        public void Arrays()
        {
            var b1 = Serializer.Serialize(new Arr { Foo = "Hello World", Bar = new double[] { 1.0, 2.5, 3.75 } });
            var a1 = Deserializer.Deserialize<Arr>(b1);
            var n1 = Deserializer.Deserialize<NoArr>(b1);

            Assert.AreEqual("Hello World", a1.Foo);
            Assert.AreEqual("Hello World", n1.Foo);

            Assert.AreEqual(3, a1.Bar.Length);
            Assert.AreEqual(3, n1.Bar.Count);

            Assert.AreEqual(1.0, a1.Bar[0]);
            Assert.AreEqual(1.0, n1.Bar[0]);

            Assert.AreEqual(2.5, a1.Bar[1]);
            Assert.AreEqual(2.5, n1.Bar[1]);

            Assert.AreEqual(3.75, a1.Bar[2]);
            Assert.AreEqual(3.75, n1.Bar[2]);

            var b2 = Serializer.Serialize(new NoArr { Foo = "Fizz Buzz", Bar = new List<double> { 10, 9.25, 8.5, 7.75 } });
            var a2 = Deserializer.Deserialize<Arr>(b2);
            var n2 = Deserializer.Deserialize<NoArr>(b2);

            Assert.AreEqual("Fizz Buzz", a2.Foo);
            Assert.AreEqual("Fizz Buzz", n2.Foo);

            Assert.AreEqual(4, a2.Bar.Length);
            Assert.AreEqual(4, n2.Bar.Count);

            Assert.AreEqual(10, a2.Bar[0]);
            Assert.AreEqual(10, n2.Bar[0]);

            Assert.AreEqual(9.25, a2.Bar[1]);
            Assert.AreEqual(9.25, n2.Bar[1]);

            Assert.AreEqual(8.5, a2.Bar[2]);
            Assert.AreEqual(8.5, n2.Bar[2]);

            Assert.AreEqual(7.75, a2.Bar[3]);
            Assert.AreEqual(7.75, n2.Bar[3]);
        }
    }
}
