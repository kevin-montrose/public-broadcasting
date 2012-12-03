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

            var all = Serializer.Deserialize<A>(allBytes);
            var field = Serializer.Deserialize<A>(fieldBytes);

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

            var prop = Serializer.Deserialize<A>(propBytes);
            var all = Serializer.Deserialize<A>(allBytes);

            Assert.AreEqual("Hello", all.Foo);
            Assert.AreEqual("World", all.Bar);
            Assert.AreEqual("Buzz", all.Next.Foo);

            Assert.AreEqual("Hello", prop.Foo);
            Assert.IsNull(prop.Bar);
            Assert.IsNull(prop.Next);
        }

        [TestMethod]
        public void All()
        {
            var obj = new A { Foo = "Hello", Bar = "World", Next = new A { Foo = "Buzz" } };
            byte[] bytes;
            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Public);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Public);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public);
            
            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Protected);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Private);

            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Protected | IncludedVisibility.Private);

            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Protected);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Protected);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Protected);

            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Protected | IncludedVisibility.Private);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Protected | IncludedVisibility.Internal);

            bytes = Serializer.Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Protected | IncludedVisibility.Internal | IncludedVisibility.Private);

            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Internal);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Internal);
            bytes = Serializer.Serialize(obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Internal);

            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Internal | IncludedVisibility.Private);

            bytes = Serializer.Serialize(obj, IncludedMembers.Fields, IncludedVisibility.Private);
        }

        [TestMethod]
        public void Interface()
        {
            var exposedTypes = typeof(Serializer).Assembly.GetTypes().Where(t => t.IsPublic).ToList();

            Assert.IsTrue(exposedTypes.Where(w => w == typeof(Serializer)).Count() == 1);
            Assert.IsTrue(exposedTypes.Where(w => w == typeof(IncludedVisibility)).Count() == 1);
            Assert.IsTrue(exposedTypes.Where(w => w == typeof(IncludedMembers)).Count() == 1);

            Assert.AreEqual(3, exposedTypes.Count);
        }
    }
}
