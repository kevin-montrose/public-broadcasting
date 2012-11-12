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
        }
    }
}
