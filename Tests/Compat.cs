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
    public class Compat
    {
        private static string ToString(dynamic dyn)
        {
            if (dyn == null)
            {
                return "null";
            }
            else
            {
                if (dyn is ValueType)
                {
                    return dyn.ToString();
                }
                else
                {
                    if (dyn is string)
                    {
                        return "'" + dyn + "'";
                    }
                    else
                    {
                        if (dyn is System.Collections.IList)
                        {
                            var asList = new System.Collections.ArrayList();
                            foreach (var x in dyn)
                            {
                                asList.Add(ToString(x));
                            }

                            return "[" + string.Join(", ", asList.Cast<string>()) + "]";
                        }
                        else
                        {
                            if (dyn is System.Collections.IDictionary)
                            {
                                var asList = new System.Collections.ArrayList();
                                foreach (var x in dyn)
                                {
                                    asList.Add(ToString(x.Key) + ": " + ToString(x.Value));
                                }

                                return "{" + string.Join(", ", asList.Cast<string>()) + "}";
                            }
                        }
                    }
                }
            }

            var ret = new StringBuilder();

            var first = true;

            ret.Append("{");

            foreach (var r in dyn)
            {
                var key = r.Key;
                var val = r.Value;

                if (!first)
                {
                    ret.Append(", ");
                }

                ret.Append(ToString(key) + ": " + ToString(val));

                first = false;
            }

            ret.Append("}");

            return ret.ToString();
        }

        [TestMethod]
        public void One()
        {
            const string data = "Eq0BIqoBCgcKAUESAhoACgkKAUISBBoCCAkKCQoBQxIEGgIIBAoJCgFEEgQaAggKCgkKAUUSBBoCCAsKCQoBRhIEGgIIDAoJCgFHEgQaAggFCgkKAUgSBBoCCBEKCQoBSRIEGgIIBgoJCgFKEgQaAggBCgsKAUsSBioECgIaAAoNCgFMEggqBgoEGgIIBAoTCgFNEg4yDAoEGgIICxIEGgIIBQoJCgFOEgRCAggBEAEaxAgIoK/lt/r/////ARjQq93C1bnxkJ0BKKW9/v///////wE4jwFAZ0iUQFIOO8W+WTfCsVlBxbjFoWFYkaDFvQRYg4GOlQJYmMPmLVjU1JW1/v////8BWMKeudb7/////wFYnLiAyP7/////AVid0ZmF+v////8BWKCb08D7/////wFYvtnNjf3/////AVjsve1wYMTpjdTZ/NTxR2Czv4COn5r4ovUBYNCOp9Wh24DBygFgjYGcwu6Wtf3kAWCz/ta/itOZgklghb+xh8jByPPeAWCn/IKkwraAmsEBYPLxtqDOwb7I3gFgpbj3jaG9567pAWDDwdHU56LA3mpqBwiZ0AEQwAFqBwj3qQEQ1QFqBgivZBCLAWoOCLPq/v///////wEQ3wFqBQjZPhA8ag0Iip3/////////ARANagUI22AQZGoOCNXn/////////wEQiQFqDQjhrP7///////8BEGFqBwjT2AEQuQFy1gUIy7fkyfv/////ARi0792iwtbL3Q4omUQ4JkBkSAxSEWzigJQ14oSiecO8y4Yow6gmWMnakR9Y6beIgQRYkvXOkAFYyujutvn/////AVid1PJKWMWNrSxYnYjLifr/////AVi84Kys//////8BWO76r4P8/////wFY15W8MWCDxuiLu6Dh9eUBYJ7NrfPX5PewAmDhvZenkr2ggmJgrryQ5YTJht8SYPqqlYi6ufaAmAFg2JmNsM24w+eOAWCc0f3Gz+TCjoIBYJ+wjKyO/OfOgwFgp7ON+Kz7r5sHYN2ZreLZz8vOIWoGCM4sEK8BagYI33UQxgFqBgjt3wEQOGoOCIKF/v///////wEQswFqBgjWOBC4AWoOCLne/v///////wEQgAFqBgiu1gEQCmoOCIDo/v///////wEQ3AFqDQiyrv////////8BEBtqDgjX//7///////8BEIABcvYCCPe95+P8/////wEYl7Pa+r7T5ap+KIzL/v///////wE4vgFAYUiaQFIQw7PigJjDvEHCoAtOw4IyMFjI79na+P////8BWLOJ+xZY3I2y8/j/////AViTmJmcBViT0pGC/f////8BWPD0q5AFWOv388T9/////wFYwJPZngJYvda17v3/////AViogsTP//////8BYIKBgu/G5KD09QFgme7uyeXduODNAWDthvzEi6XBoJMBYLCRxcLqj+qVEWD3lLWCr4Pnlmhgks7+xdCh35paYKqBwKOSrM7ofGC+mvXlreaPu3BgjNOEn8qStNnQAWDW75SF6IjOwjNqDgiS7P////////8BEP4BagUI/XAQBmoFCK97EHlqDgjR4/7///////8BEM0BagYI36MBEDVqBQjTYxBPag4I+8j/////////ARCbAWoOCJKL/////////wEQ2gFqDQjNwP7///////8BEE5qDQiqqP7///////8BEFE=";
            var bytes = Convert.FromBase64String(data);

            var obj = Serializer.Deserialize(bytes);

            Assert.IsNotNull(obj);

            var asStr = ToString(obj);

            Assert.IsNotNull(asStr);
            Assert.AreEqual(
                "{'A': -1493608544, 'B': 0, 'C': -7124195649782327856, 'D': 0, 'E': -24923, 'F': 0, 'G': 143, 'H': 103, 'I': —, 'J': ';žY7±YAŸša', 'K': [1202802705, 581140611, 96051608, -425366956, -1160884414, -385868772, -1599706979, -1206596192, -776770370, 236674796], 'L': [5180076242011124932, -772964568715485261, -3854515195302262960, -1947010006838214515, 5261443070514544435, -2384900040571461755, -4524989852382347737, -2409150867370559246, -1630973861392688091, 7691304934676127939], 'M': {26649: 192, 21751: 213, 12847: 139, -19149: 223, 8025: 60, -12662: 13, 12379: 100, -3115: 137, -27039: 97, 27731: 185}, 'N': {'A': -1187439669, 'B': 0, 'C': 1061493488414586804, 'D': 0, 'E': 8729, 'F': 0, 'G': 38, 'H': 100, 'I': , 'J': 'l—5™yüˆ(è&', 'K': [65301833, 1075977193, 303282834, -1763986358, 157067805, 93013701, -1590508515, -175427524, -1066664594, 103746263], 'L': [-1879262168588082429, 171663612944541342, 7062912953970188001, 1350545836083519022, -7493468836364724870, -8156285261089387304, -9071081821032191844, -8962831895125354465, 519813753504749991, 2422143291623165149], 'M': {5710: 175, 15071: 198, 28653: 56, -32126: 179, 7254: 184, -20679: 128, 27438: 10, -19456: 220, -10446: 27, -16425: 128}, 'N': {'A': -864428297, 'B': 0, 'C': 9103347818278853015, 'D': 0, 'E': -23156, 'F': 0, 'G': 190, 'H': 97, 'I': ‚, 'J': 'ó‘üA NÂ20', 'K': [-1957267512, 48153779, -1905490212, 1401310227, -800823021, 1376451184, -660800533, 601246144, -573740227, -101646040], 'L': [-727187147334844286, -3620644587078699239, -7835976204193692819, 1237267784586578096, 7506827793411164791, 6500239132114003730, 8994033020221522090, 8103734067184094526, -3408432631142864500, 3712435343294019542], 'M': {-2542: 254, 14461: 6, 15791: 121, -20015: 205, 20959: 53, 12755: 79, -7045: 155, -14958: 218, -24499: 78, -27606: 81}, 'N': null}}}",
                asStr
            );
        }

        class Fast
        {
            public int A { get; set; }
            public List<byte> B { get; set; }
            public Fast C { get; set; }
        }

        class Slow
        {
            public int A { get; set; }
            public byte[] B { get; set; }
            public Slow C { get; set; }
        }

        [TestMethod]
        public void SlowFast()
        {
            var b1 = Serializer.Serialize(new Fast { A = 123, B = new List<byte> { 128, 129, 130 }, C = new Fast { A = 456, B = new List<byte> { 1 } } });
            var b2 = Serializer.Serialize(new Slow { A = 255, B = new byte[] { 10, 32, 68 }, C = new Slow { A = 789, B = new byte[] { 15, 10 } } });

            var f1 = Serializer.Deserialize<Fast>(b2);
            var s1 = Serializer.Deserialize<Slow>(b1);

            Assert.IsNotNull(f1);
            Assert.IsNotNull(s1);

            Assert.AreEqual(123, s1.A);
            Assert.AreEqual(3, s1.B.Length);
            Assert.AreEqual(128, s1.B[0]);
            Assert.AreEqual(129, s1.B[1]);
            Assert.AreEqual(130, s1.B[2]);
            Assert.IsNotNull(s1.C);
            Assert.AreEqual(456, s1.C.A);
            Assert.AreEqual(1, s1.C.B.Length);
            Assert.AreEqual(1, s1.C.B[0]);
            Assert.IsNull(s1.C.C);

            Assert.AreEqual(255, f1.A);
            Assert.AreEqual(3, f1.B.Count);
            Assert.AreEqual(10, f1.B[0]);
            Assert.AreEqual(32, f1.B[1]);
            Assert.AreEqual(68, f1.B[2]);
            Assert.AreEqual(789, f1.C.A);
            Assert.AreEqual(2, f1.C.B.Count);
            Assert.AreEqual(15, f1.C.B[0]);
            Assert.AreEqual(10, f1.C.B[1]);
            Assert.IsNull(f1.C.C);
        }

        [TestMethod]
        public void ListArray()
        {
            const string oldArr = "EgYqBAoCGgAaDwgCCBAI/P//////////AQ==";
            const string oldList = "EgYqBAoCGgAaBggBCAUICw==";

            var list = Serializer.Deserialize<List<int>>(Convert.FromBase64String(oldList));
            var arr = Serializer.Deserialize<int[]>(Convert.FromBase64String(oldArr));

            var toList = Serializer.Deserialize<List<int>>(Convert.FromBase64String(oldArr));
            var toArr = Serializer.Deserialize<int[]>(Convert.FromBase64String(oldList));

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, arr.Length);
            Assert.AreEqual(3, toList.Count);
            Assert.AreEqual(3, toArr.Length);

            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(5, list[1]);
            Assert.AreEqual(11, list[2]);

            Assert.AreEqual(2, arr[0]);
            Assert.AreEqual(16, arr[1]);
            Assert.AreEqual(-4, arr[2]);

            Assert.AreEqual(2, toList[0]);
            Assert.AreEqual(16, toList[1]);
            Assert.AreEqual(-4, toList[2]);

            Assert.AreEqual(1, toArr[0]);
            Assert.AreEqual(5, toArr[1]);
            Assert.AreEqual(11, toArr[2]);

        }
    }
}
