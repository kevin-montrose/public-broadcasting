using Microsoft.VisualStudio.TestTools.UnitTesting;
using PublicBroadcasting;
using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class Errors
    {
        private static void Throws(Action act)
        {
            try
            {
                act();
                Assert.Fail("Should have thrown");
            }
            catch
            {

            }
        }

        [TestMethod]
        public void SerializerCalls()
        {
            Throws(() => Serializer.Serialize<string>(null));
            Throws(() => Serializer.Serialize<string>((Stream)null, ""));
            Throws(() => Serializer.Serialize<string>(null, IncludedVisibility.Private));
            Throws(() => Serializer.Serialize<string>(null, IncludedMembers.Fields));
            Throws(() => Serializer.Serialize<string>(new MemoryStream(), null, IncludedVisibility.Private));
            Throws(() => Serializer.Serialize<string>(new MemoryStream(), null, IncludedMembers.Fields));
            Throws(() => Serializer.Serialize<string>("", IncludedMembers.Properties, (IncludedVisibility)123));
            Throws(() => Serializer.Serialize<string>("", (IncludedMembers)222, IncludedVisibility.Private));
            Throws(() => Serializer.Serialize<string>(new MemoryStream(), "", IncludedMembers.Properties, (IncludedVisibility)123));
            Throws(() => Serializer.Serialize<string>(new MemoryStream(), "", (IncludedMembers)222, IncludedVisibility.Private));

            Throws(() => Serializer.Deserialize((byte[])null));
            Throws(() => Serializer.Deserialize<string>((byte[])null));
            Throws(() => Serializer.Deserialize((Stream)null));
            Throws(() => Serializer.Deserialize<string>((Stream)null));
        }

        [TestMethod]
        public void DescriptionAndBuilder()
        {
            var members = new List<IncludedMembers>();
            var visibility = new List<IncludedVisibility>();

            members.Add(IncludedMembers.Fields);
            members.Add(IncludedMembers.Properties);
            members.Add(IncludedMembers.Fields | IncludedMembers.Properties);

            visibility.Add(IncludedVisibility.Internal);
            visibility.Add(IncludedVisibility.Private);
            visibility.Add(IncludedVisibility.Protected);
            visibility.Add(IncludedVisibility.Public);

            while(true)
            {
                var temp = new List<IncludedVisibility>();

                foreach(var x in visibility)
                {
                    foreach(var y in visibility)
                    {
                        var sub = x | y;

                        if(!visibility.Contains(sub))
                        {
                            temp.Add(sub);
                        }
                    }
                }

                visibility.AddRange(temp);

                if(temp.Count == 0) break;
            }

            foreach (var mem in members)
            {
                foreach (var vis in visibility)
                {
                    TypeDescription desc;
                    PublicBroadcasting.Impl.POCOBuilder build;
                    Serializer.GetDescriptionAndBuilder<string>(mem, vis, out desc, out build);

                    Assert.IsNotNull(desc);
                    Assert.IsNotNull(build);
                }
            }
        }
    }
}
