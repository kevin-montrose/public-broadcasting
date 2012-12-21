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
    public class Issues
    {
        private class TwoTestObject
        {
            public string Property1 { get; set; }
            public string ThisBreaksThings
            {
                get
                {
                    return string.Empty;
                }
            }
        }

        [TestMethod]
        public void Two()
        {
            var bytes = Serializer.Serialize(new TwoTestObject { Property1 = "Foo" });

            var obj = Serializer.Deserialize<TwoTestObject>(bytes);

            Assert.AreEqual("Foo", obj.Property1);
            Assert.AreEqual(string.Empty, obj.ThisBreaksThings);
        }
    }
}
