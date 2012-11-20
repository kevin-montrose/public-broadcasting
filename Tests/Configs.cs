using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class Configs
    {
        class A
        {
            public string Foo { get; set; }
            public string Bar;

            public A Next;
        }

        [TestMethod]
        public void FieldsPublic()
        {
            var allBytes = Serializer.Serialize(new A { Foo = "Hello", Bar = "World", Next = new A { Foo = "Buzz" } });
            var fieldBytes = Serializer.Serialize(new A { Foo = "Hello", Bar = "World", Next = new A { Foo = "Buzz" } }, IncludedMembers.Fields);

            var all = Deserializer.Deserialize<A>(allBytes);
            var field = Deserializer.Deserialize<A>(fieldBytes);

            Assert.AreEqual("Hello", all.Foo);
            Assert.AreEqual("World", all.Bar);
            Assert.AreEqual("Buzz", all.Next.Foo);

            Assert.IsNull(field.Foo);
            Assert.AreEqual("World", field.Bar);
            Assert.IsNotNull(field.Next);
            Assert.IsNull(field.Next.Foo);
        }

        [TestMethod]
        public void PropertiesPublic()
        {
            var propBytes = Serializer.Serialize(new A { Foo = "Hello", Bar = "World", Next = new A { Foo = "Buzz" } }, IncludedMembers.Properties);
            var allBytes = Serializer.Serialize(new A { Foo = "Hello", Bar = "World", Next = new A { Foo = "Buzz" } });

            var prop = Deserializer.Deserialize<A>(propBytes);
            var all = Deserializer.Deserialize<A>(allBytes);

            Assert.AreEqual("Hello", all.Foo);
            Assert.AreEqual("World", all.Bar);
            Assert.AreEqual("Buzz", all.Next.Foo);

            Assert.AreEqual("Hello", prop.Foo);
            Assert.IsNull(prop.Bar);
            Assert.IsNull(prop.Next);
        }
    }
}
