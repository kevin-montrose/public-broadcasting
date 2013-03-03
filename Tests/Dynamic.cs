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
    public class Dynamic
    {
        [TestMethod]
        public void Dictionaries()
        {
            var bytes = Serializer.Serialize(new Dictionary<string, int>() { { "Hello", 1 }, { "World", 2 } });
            dynamic dynDict = Serializer.Deserialize(bytes);

            Assert.AreEqual(2, dynDict.Count);
            Assert.AreEqual(1, dynDict["Hello"]);
            Assert.AreEqual(2, dynDict["World"]);
        }

        [TestMethod]
        public void Anon()
        {
            try
            {
                var bytes = Serializer.Serialize(new { A = "Foo", B = "Bar", C = "Bazz", D = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } });
                dynamic dynAnon = Serializer.Deserialize(bytes);

                Assert.AreEqual("Foo", dynAnon.A);
                Assert.AreEqual("Bar", dynAnon.B);
                Assert.AreEqual("Bazz", dynAnon.C);
                Assert.AreEqual(10, dynAnon.D.Count);
                for (var i = 0; i < 10; i++)
                {
                    Assert.AreEqual(i + 1, dynAnon.D[i]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [TestMethod]
        public void Indexer()
        {
            var bytes = Serializer.Serialize(new { A = "Foo", B = 123, C = (int?)2, C2 = (int?)null, D = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } });
            var dyn = Serializer.Deserialize(bytes);

            Assert.AreEqual("Foo", dyn["A"]);
            Assert.AreEqual(123, dyn["B"]);
            Assert.AreEqual(2, dyn["C"]);
            Assert.AreEqual(null, dyn["C2"]);
            Assert.AreEqual(10, dyn["D"].Count);

            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(i + 1, dyn["D"][i]);
            }
        }

        [TestMethod]
        public void ForEach()
        {
            var bytes = Serializer.Serialize(new { A = "Foo", B = 123, C = (int?)2, C2 = (int?)null, D = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } });
            var dyn = Serializer.Deserialize(bytes);

            foreach (var kv in dyn)
            {
                switch ((string)kv.Key)
                {
                    case "A": Assert.AreEqual("Foo", kv.Value); break;
                    case "B": Assert.AreEqual(123, kv.Value); break;
                    case "C": Assert.AreEqual((int?)2, kv.Value); break;
                    case "C2": Assert.AreEqual((int?)null, kv.Value); break;
                    case "D":
                        for (var i = 0; i < 10; i++)
                        {
                            Assert.AreEqual(i + 1, kv.Value[i]);
                        }
                        break;
                    default: Assert.Fail("Unexpected element " + kv.Key); break;
                }
            }
        }

        class ToStringObj
        {
            public bool Bool;
            public char Char;
            public byte Byte;
            public sbyte SByte;
            public short Short;
            public ushort UShort;
            public int Int;
            public uint UInt;
            public long Long;
            public ulong ULong;
            public float Float;
            public double Double;
            public decimal Decimal;
            public string String;

            public bool? NBool;
            public char? NChar;
            public byte? NByte;
            public sbyte? NSByte;
            public short? NShort;
            public ushort? NUShort;
            public int? NInt;
            public uint? NUInt;
            public long? NLong;
            public ulong? NULong;
            public float? NFloat;
            public double? NDouble;
            public decimal? NDecimal;

            public Guid Guid;
            public Guid? NGuid;

            public Uri AbsoluteUri;
            public Uri RelativeUri;

            public TimeSpan TimeSpan;
            public TimeSpan? NTimeSpan;

            public DateTime DateTime;
            public DateTime? NDateTime;

            public List<int> ListInt;
            public List<ToStringObj> ListToStringObj;

            public Dictionary<int, int> DictIntInt;
            public Dictionary<int, ToStringObj> DictIntToStringObj;
            public Dictionary<ToStringObj, int> DictToStringObjInt;
            public Dictionary<ToStringObj, ToStringObj> DictToStringObjToStringObj;
            public Dictionary<double, List<int>> DictDoubleListInt;
            public Dictionary<List<int>, double> DictListIntDouble;

            public ToStringObj RecursiveRef;

            public ToStringObj Clone()
            {
                return (ToStringObj)this.MemberwiseClone();
            }
        }

        [TestMethod]
        public void ToStringMethod()
        {
            var obj =
                new ToStringObj
                {
                    Bool = true,
                    Char = 'a',
                    Byte = 1,
                    SByte = -2,
                    Short = 3,
                    UShort = 4,
                    Int = 5,
                    UInt = 6,
                    Long = 7,
                    ULong = 8,
                    Float = 9,
                    Double = 10,
                    Decimal = 11,
                    String = "Hello \"World\" Test",

                    NBool = null,
                    NChar = 'b',
                    NByte = null,
                    NSByte = -3,
                    NShort = null,
                    NUShort = 5,
                    NInt = null,
                    NUInt = 7,
                    NLong = null,
                    NULong = 9,
                    NFloat = null,
                    NDouble = 11,
                    NDecimal = null,

                    Guid = Guid.Parse("B94F865C-82F5-4A60-BD14-20BD4CEA5BB8"),
                    NGuid = null,

                    TimeSpan = new TimeSpan(1, 2, 3, 4),
                    NTimeSpan = new TimeSpan(5, 6, 7, 8),

                    DateTime = DateTime.Parse("1989-11-08 20:17:00"),
                    NDateTime = null,

                    AbsoluteUri = new Uri("http://example.com/example"),
                    //RelativeUri = new Uri("/example/path", UriKind.Relative),

                    DictIntInt = 
                        new Dictionary<int, int>
                        {
                            { 1, 2 },
                            { 3, 4 },
                            { 5, 6 }
                        },

                   ListInt = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 },

                   DictDoubleListInt = new Dictionary<double,List<int>> { { 1.038, new List<int> { 8,6,7,6,5,3,0,9 } }, { 1.618, new List<int> { 1, 1, 2, 6 } } },
                   DictListIntDouble = new Dictionary<List<int>,double> { { new List<int> { 1, 0, 3, 8 },  8675309 }, { new List<int> { 1, 6, 1, 8 }, 1126 } }
                };

            var clone = obj.Clone();
            obj.RecursiveRef = clone;

            obj.ListToStringObj = new List<ToStringObj> { clone, clone, clone };

            obj.DictIntToStringObj = new Dictionary<int, ToStringObj> { { 100, clone } };
            obj.DictToStringObjToStringObj = new Dictionary<ToStringObj, ToStringObj> { { clone, clone } };
            obj.DictToStringObjInt = new Dictionary<ToStringObj, int> { { clone, 200 } };

            var bytes = Serializer.Serialize(obj);

            var dyn = Serializer.Deserialize(bytes);

            var str = dyn.ToString();

            Assert.AreEqual(
                "{\r\n AbsoluteUri: http://example.com/example,\r\n Bool: True,\r\n Byte: 1,\r\n Char: a,\r\n DateTime: 1989-11-08 20:17:00Z,\r\n Decimal: 11,\r\n DictDoubleListInt: \r\n  {\r\n   {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n   {1.618 -> [1, 1, 2, 6]}\r\n  },\r\n DictIntInt: \r\n  {\r\n   {1 -> 2},\r\n   {3 -> 4},\r\n   {5 -> 6}\r\n  },\r\n DictIntToStringObj: \r\n  {\r\n   {\r\n    100\r\n     ->\r\n    {\r\n     AbsoluteUri: http://example.com/example,\r\n     Bool: True,\r\n     Byte: 1,\r\n     Char: a,\r\n     DateTime: 1989-11-08 20:17:00Z,\r\n     Decimal: 11,\r\n     DictDoubleListInt: \r\n      {\r\n       {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n       {1.618 -> [1, 1, 2, 6]}\r\n      },\r\n     DictIntInt: \r\n      {\r\n       {1 -> 2},\r\n       {3 -> 4},\r\n       {5 -> 6}\r\n      },\r\n     DictIntToStringObj: null,\r\n     DictListIntDouble: \r\n      {\r\n       {[1, 0, 3, 8] -> 8675309},\r\n       {[1, 6, 1, 8] -> 1126}\r\n      },\r\n     DictToStringObjInt: null,\r\n     DictToStringObjToStringObj: null,\r\n     Double: 10,\r\n     Float: 9,\r\n     Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n     Int: 5,\r\n     ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n     ListToStringObj: null,\r\n     Long: 7,\r\n     NBool: null,\r\n     NByte: null,\r\n     NChar: b,\r\n     NDateTime: null,\r\n     NDecimal: null,\r\n     NDouble: 11,\r\n     NFloat: null,\r\n     NGuid: null,\r\n     NInt: null,\r\n     NLong: null,\r\n     NSByte: -3,\r\n     NShort: null,\r\n     NTimeSpan: 5.06:07:08,\r\n     NUInt: 7,\r\n     NULong: 9,\r\n     NUShort: 5,\r\n     RecursiveRef: null,\r\n     RelativeUri: null,\r\n     SByte: -2,\r\n     Short: 3,\r\n     String: \"Hello \\\"World\\\" Test\",\r\n     TimeSpan: 1.02:03:04,\r\n     UInt: 6,\r\n     ULong: 8,\r\n     UShort: 4\r\n    }\r\n   }\r\n  },\r\n DictListIntDouble: \r\n  {\r\n   {[1, 0, 3, 8] -> 8675309},\r\n   {[1, 6, 1, 8] -> 1126}\r\n  },\r\n DictToStringObjInt: \r\n  {\r\n   {\r\n    {\r\n     AbsoluteUri: http://example.com/example,\r\n     Bool: True,\r\n     Byte: 1,\r\n     Char: a,\r\n     DateTime: 1989-11-08 20:17:00Z,\r\n     Decimal: 11,\r\n     DictDoubleListInt: \r\n      {\r\n       {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n       {1.618 -> [1, 1, 2, 6]}\r\n      },\r\n     DictIntInt: \r\n      {\r\n       {1 -> 2},\r\n       {3 -> 4},\r\n       {5 -> 6}\r\n      },\r\n     DictIntToStringObj: null,\r\n     DictListIntDouble: \r\n      {\r\n       {[1, 0, 3, 8] -> 8675309},\r\n       {[1, 6, 1, 8] -> 1126}\r\n      },\r\n     DictToStringObjInt: null,\r\n     DictToStringObjToStringObj: null,\r\n     Double: 10,\r\n     Float: 9,\r\n     Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n     Int: 5,\r\n     ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n     ListToStringObj: null,\r\n     Long: 7,\r\n     NBool: null,\r\n     NByte: null,\r\n     NChar: b,\r\n     NDateTime: null,\r\n     NDecimal: null,\r\n     NDouble: 11,\r\n     NFloat: null,\r\n     NGuid: null,\r\n     NInt: null,\r\n     NLong: null,\r\n     NSByte: -3,\r\n     NShort: null,\r\n     NTimeSpan: 5.06:07:08,\r\n     NUInt: 7,\r\n     NULong: 9,\r\n     NUShort: 5,\r\n     RecursiveRef: null,\r\n     RelativeUri: null,\r\n     SByte: -2,\r\n     Short: 3,\r\n     String: \"Hello \\\"World\\\" Test\",\r\n     TimeSpan: 1.02:03:04,\r\n     UInt: 6,\r\n     ULong: 8,\r\n     UShort: 4\r\n    }\r\n     ->\r\n    200\r\n   }\r\n  },\r\n DictToStringObjToStringObj: \r\n  {\r\n   {\r\n    {\r\n     AbsoluteUri: http://example.com/example,\r\n     Bool: True,\r\n     Byte: 1,\r\n     Char: a,\r\n     DateTime: 1989-11-08 20:17:00Z,\r\n     Decimal: 11,\r\n     DictDoubleListInt: \r\n      {\r\n       {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n       {1.618 -> [1, 1, 2, 6]}\r\n      },\r\n     DictIntInt: \r\n      {\r\n       {1 -> 2},\r\n       {3 -> 4},\r\n       {5 -> 6}\r\n      },\r\n     DictIntToStringObj: null,\r\n     DictListIntDouble: \r\n      {\r\n       {[1, 0, 3, 8] -> 8675309},\r\n       {[1, 6, 1, 8] -> 1126}\r\n      },\r\n     DictToStringObjInt: null,\r\n     DictToStringObjToStringObj: null,\r\n     Double: 10,\r\n     Float: 9,\r\n     Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n     Int: 5,\r\n     ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n     ListToStringObj: null,\r\n     Long: 7,\r\n     NBool: null,\r\n     NByte: null,\r\n     NChar: b,\r\n     NDateTime: null,\r\n     NDecimal: null,\r\n     NDouble: 11,\r\n     NFloat: null,\r\n     NGuid: null,\r\n     NInt: null,\r\n     NLong: null,\r\n     NSByte: -3,\r\n     NShort: null,\r\n     NTimeSpan: 5.06:07:08,\r\n     NUInt: 7,\r\n     NULong: 9,\r\n     NUShort: 5,\r\n     RecursiveRef: null,\r\n     RelativeUri: null,\r\n     SByte: -2,\r\n     Short: 3,\r\n     String: \"Hello \\\"World\\\" Test\",\r\n     TimeSpan: 1.02:03:04,\r\n     UInt: 6,\r\n     ULong: 8,\r\n     UShort: 4\r\n    }\r\n     ->\r\n    {\r\n     AbsoluteUri: http://example.com/example,\r\n     Bool: True,\r\n     Byte: 1,\r\n     Char: a,\r\n     DateTime: 1989-11-08 20:17:00Z,\r\n     Decimal: 11,\r\n     DictDoubleListInt: \r\n      {\r\n       {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n       {1.618 -> [1, 1, 2, 6]}\r\n      },\r\n     DictIntInt: \r\n      {\r\n       {1 -> 2},\r\n       {3 -> 4},\r\n       {5 -> 6}\r\n      },\r\n     DictIntToStringObj: null,\r\n     DictListIntDouble: \r\n      {\r\n       {[1, 0, 3, 8] -> 8675309},\r\n       {[1, 6, 1, 8] -> 1126}\r\n      },\r\n     DictToStringObjInt: null,\r\n     DictToStringObjToStringObj: null,\r\n     Double: 10,\r\n     Float: 9,\r\n     Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n     Int: 5,\r\n     ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n     ListToStringObj: null,\r\n     Long: 7,\r\n     NBool: null,\r\n     NByte: null,\r\n     NChar: b,\r\n     NDateTime: null,\r\n     NDecimal: null,\r\n     NDouble: 11,\r\n     NFloat: null,\r\n     NGuid: null,\r\n     NInt: null,\r\n     NLong: null,\r\n     NSByte: -3,\r\n     NShort: null,\r\n     NTimeSpan: 5.06:07:08,\r\n     NUInt: 7,\r\n     NULong: 9,\r\n     NUShort: 5,\r\n     RecursiveRef: null,\r\n     RelativeUri: null,\r\n     SByte: -2,\r\n     Short: 3,\r\n     String: \"Hello \\\"World\\\" Test\",\r\n     TimeSpan: 1.02:03:04,\r\n     UInt: 6,\r\n     ULong: 8,\r\n     UShort: 4\r\n    }\r\n   }\r\n  },\r\n Double: 10,\r\n Float: 9,\r\n Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n Int: 5,\r\n ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n ListToStringObj: \r\n  [\r\n   {\r\n    AbsoluteUri: http://example.com/example,\r\n    Bool: True,\r\n    Byte: 1,\r\n    Char: a,\r\n    DateTime: 1989-11-08 20:17:00Z,\r\n    Decimal: 11,\r\n    DictDoubleListInt: \r\n     {\r\n      {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n      {1.618 -> [1, 1, 2, 6]}\r\n     },\r\n    DictIntInt: \r\n     {\r\n      {1 -> 2},\r\n      {3 -> 4},\r\n      {5 -> 6}\r\n     },\r\n    DictIntToStringObj: null,\r\n    DictListIntDouble: \r\n     {\r\n      {[1, 0, 3, 8] -> 8675309},\r\n      {[1, 6, 1, 8] -> 1126}\r\n     },\r\n    DictToStringObjInt: null,\r\n    DictToStringObjToStringObj: null,\r\n    Double: 10,\r\n    Float: 9,\r\n    Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n    Int: 5,\r\n    ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n    ListToStringObj: null,\r\n    Long: 7,\r\n    NBool: null,\r\n    NByte: null,\r\n    NChar: b,\r\n    NDateTime: null,\r\n    NDecimal: null,\r\n    NDouble: 11,\r\n    NFloat: null,\r\n    NGuid: null,\r\n    NInt: null,\r\n    NLong: null,\r\n    NSByte: -3,\r\n    NShort: null,\r\n    NTimeSpan: 5.06:07:08,\r\n    NUInt: 7,\r\n    NULong: 9,\r\n    NUShort: 5,\r\n    RecursiveRef: null,\r\n    RelativeUri: null,\r\n    SByte: -2,\r\n    Short: 3,\r\n    String: \"Hello \\\"World\\\" Test\",\r\n    TimeSpan: 1.02:03:04,\r\n    UInt: 6,\r\n    ULong: 8,\r\n    UShort: 4\r\n   },\r\n   {\r\n    AbsoluteUri: http://example.com/example,\r\n    Bool: True,\r\n    Byte: 1,\r\n    Char: a,\r\n    DateTime: 1989-11-08 20:17:00Z,\r\n    Decimal: 11,\r\n    DictDoubleListInt: \r\n     {\r\n      {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n      {1.618 -> [1, 1, 2, 6]}\r\n     },\r\n    DictIntInt: \r\n     {\r\n      {1 -> 2},\r\n      {3 -> 4},\r\n      {5 -> 6}\r\n     },\r\n    DictIntToStringObj: null,\r\n    DictListIntDouble: \r\n     {\r\n      {[1, 0, 3, 8] -> 8675309},\r\n      {[1, 6, 1, 8] -> 1126}\r\n     },\r\n    DictToStringObjInt: null,\r\n    DictToStringObjToStringObj: null,\r\n    Double: 10,\r\n    Float: 9,\r\n    Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n    Int: 5,\r\n    ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n    ListToStringObj: null,\r\n    Long: 7,\r\n    NBool: null,\r\n    NByte: null,\r\n    NChar: b,\r\n    NDateTime: null,\r\n    NDecimal: null,\r\n    NDouble: 11,\r\n    NFloat: null,\r\n    NGuid: null,\r\n    NInt: null,\r\n    NLong: null,\r\n    NSByte: -3,\r\n    NShort: null,\r\n    NTimeSpan: 5.06:07:08,\r\n    NUInt: 7,\r\n    NULong: 9,\r\n    NUShort: 5,\r\n    RecursiveRef: null,\r\n    RelativeUri: null,\r\n    SByte: -2,\r\n    Short: 3,\r\n    String: \"Hello \\\"World\\\" Test\",\r\n    TimeSpan: 1.02:03:04,\r\n    UInt: 6,\r\n    ULong: 8,\r\n    UShort: 4\r\n   },\r\n   {\r\n    AbsoluteUri: http://example.com/example,\r\n    Bool: True,\r\n    Byte: 1,\r\n    Char: a,\r\n    DateTime: 1989-11-08 20:17:00Z,\r\n    Decimal: 11,\r\n    DictDoubleListInt: \r\n     {\r\n      {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n      {1.618 -> [1, 1, 2, 6]}\r\n     },\r\n    DictIntInt: \r\n     {\r\n      {1 -> 2},\r\n      {3 -> 4},\r\n      {5 -> 6}\r\n     },\r\n    DictIntToStringObj: null,\r\n    DictListIntDouble: \r\n     {\r\n      {[1, 0, 3, 8] -> 8675309},\r\n      {[1, 6, 1, 8] -> 1126}\r\n     },\r\n    DictToStringObjInt: null,\r\n    DictToStringObjToStringObj: null,\r\n    Double: 10,\r\n    Float: 9,\r\n    Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n    Int: 5,\r\n    ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n    ListToStringObj: null,\r\n    Long: 7,\r\n    NBool: null,\r\n    NByte: null,\r\n    NChar: b,\r\n    NDateTime: null,\r\n    NDecimal: null,\r\n    NDouble: 11,\r\n    NFloat: null,\r\n    NGuid: null,\r\n    NInt: null,\r\n    NLong: null,\r\n    NSByte: -3,\r\n    NShort: null,\r\n    NTimeSpan: 5.06:07:08,\r\n    NUInt: 7,\r\n    NULong: 9,\r\n    NUShort: 5,\r\n    RecursiveRef: null,\r\n    RelativeUri: null,\r\n    SByte: -2,\r\n    Short: 3,\r\n    String: \"Hello \\\"World\\\" Test\",\r\n    TimeSpan: 1.02:03:04,\r\n    UInt: 6,\r\n    ULong: 8,\r\n    UShort: 4\r\n   }\r\n  ],\r\n Long: 7,\r\n NBool: null,\r\n NByte: null,\r\n NChar: b,\r\n NDateTime: null,\r\n NDecimal: null,\r\n NDouble: 11,\r\n NFloat: null,\r\n NGuid: null,\r\n NInt: null,\r\n NLong: null,\r\n NSByte: -3,\r\n NShort: null,\r\n NTimeSpan: 5.06:07:08,\r\n NUInt: 7,\r\n NULong: 9,\r\n NUShort: 5,\r\n RecursiveRef: \r\n  {\r\n   AbsoluteUri: http://example.com/example,\r\n   Bool: True,\r\n   Byte: 1,\r\n   Char: a,\r\n   DateTime: 1989-11-08 20:17:00Z,\r\n   Decimal: 11,\r\n   DictDoubleListInt: \r\n    {\r\n     {1.038 -> [8, 6, 7, 6, 5, 3, 0, 9]},\r\n     {1.618 -> [1, 1, 2, 6]}\r\n    },\r\n   DictIntInt: \r\n    {\r\n     {1 -> 2},\r\n     {3 -> 4},\r\n     {5 -> 6}\r\n    },\r\n   DictIntToStringObj: null,\r\n   DictListIntDouble: \r\n    {\r\n     {[1, 0, 3, 8] -> 8675309},\r\n     {[1, 6, 1, 8] -> 1126}\r\n    },\r\n   DictToStringObjInt: null,\r\n   DictToStringObjToStringObj: null,\r\n   Double: 10,\r\n   Float: 9,\r\n   Guid: b94f865c-82f5-4a60-bd14-20bd4cea5bb8,\r\n   Int: 5,\r\n   ListInt: [3, 1, 4, 1, 5, 9, 2, 6],\r\n   ListToStringObj: null,\r\n   Long: 7,\r\n   NBool: null,\r\n   NByte: null,\r\n   NChar: b,\r\n   NDateTime: null,\r\n   NDecimal: null,\r\n   NDouble: 11,\r\n   NFloat: null,\r\n   NGuid: null,\r\n   NInt: null,\r\n   NLong: null,\r\n   NSByte: -3,\r\n   NShort: null,\r\n   NTimeSpan: 5.06:07:08,\r\n   NUInt: 7,\r\n   NULong: 9,\r\n   NUShort: 5,\r\n   RecursiveRef: null,\r\n   RelativeUri: null,\r\n   SByte: -2,\r\n   Short: 3,\r\n   String: \"Hello \\\"World\\\" Test\",\r\n   TimeSpan: 1.02:03:04,\r\n   UInt: 6,\r\n   ULong: 8,\r\n   UShort: 4\r\n  },\r\n RelativeUri: null,\r\n SByte: -2,\r\n Short: 3,\r\n String: \"Hello \\\"World\\\" Test\",\r\n TimeSpan: 1.02:03:04,\r\n UInt: 6,\r\n ULong: 8,\r\n UShort: 4\r\n}",
                str
            );
        }
    }
}
