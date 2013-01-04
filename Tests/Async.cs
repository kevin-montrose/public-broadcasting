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
    public class Async
    {
        class A
        {
            public int C;
            public bool B;
        }

        [TestMethod]
        public void AwaitBatches()
        {
            var x = new List<A>();
            x.Add(new A { C = 123, B = false });
            x.Add(new A { C = 456, B = true });
            x.Add(new A { C = 789, B = false });
            x.Add(new A { C = 012, B = true });
            x.Add(new A { C = 345, B = true });
            x.Add(new A { C = 678, B = false });
            x.Add(new A { C = 901, B = false });
            x.Add(new A { C = 234, B = true });
            x.Add(new A { C = 567, B = true });
            x.Add(new A { C = 890, B = true });

            var all =
                new[] {
                    Serializer.SerializeAsync(x[0]),
                    Serializer.SerializeAsync(x[1]),
                    Serializer.SerializeAsync(x[2]),
                    Serializer.SerializeAsync(x[3]),
                    Serializer.SerializeAsync(x[4]),
                    Serializer.SerializeAsync(x[5]),
                    Serializer.SerializeAsync(x[6]),
                    Serializer.SerializeAsync(x[7]),
                    Serializer.SerializeAsync(x[8]),
                    Serializer.SerializeAsync(x[9])
                };

            Task.WaitAll(all);

            foreach (var y in all)
            {
                var sub = Serializer.Deserialize<A>(y.Result);
                Assert.IsNotNull(sub);
            }
        }
    }
}
