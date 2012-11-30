using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [ProtoBuf.ProtoContract]
    public class FieldsPoco
    {
        [ProtoBuf.ProtoMember(1)]
        public int A;
        [ProtoBuf.ProtoMember(2)]
        public uint B;
        [ProtoBuf.ProtoMember(3)]
        public long C;
        [ProtoBuf.ProtoMember(4)]
        public ulong D;
        [ProtoBuf.ProtoMember(5)]
        public short E;
        [ProtoBuf.ProtoMember(6)]
        public ushort F;
        [ProtoBuf.ProtoMember(7)]
        public byte G;
        [ProtoBuf.ProtoMember(8)]
        public sbyte H;
        [ProtoBuf.ProtoMember(9)]
        public char I;
        [ProtoBuf.ProtoMember(10)]
        public string J;
        [ProtoBuf.ProtoMember(11)]
        public int[] K;
        [ProtoBuf.ProtoMember(12)]
        public List<long> L;
        [ProtoBuf.ProtoMember(13)]
        public Dictionary<short, byte> M;

        [ProtoBuf.ProtoMember(14)]
        public FieldsPoco N;

        public override bool Equals(object obj)
        {
            var other = obj as FieldsPoco;
            if (other == null) return false;

            if (A != other.A) return false;
            if (B != other.B) return false;
            if (C != other.C) return false;
            if (D != other.D) return false;
            if (E != other.E) return false;
            if (F != other.F) return false;
            if (G != other.G) return false;
            if (H != other.H) return false;
            if (I != other.I) return false;
            if (J != other.J) return false;

            if (K == null && other.K != null) return false;
            if (K != null && other.K == null) return false;

            if (K != null)
            {
                if (K.Length != other.K.Length) return false;

                for (var i = 0; i < K.Length; i++)
                {
                    if (K[i] != other.K[i]) return false;
                }
            }

            if (L == null && other.L != null) return false;
            if (L != null && other.L == null) return false;

            if (L != null)
            {
                if (L.Count != other.L.Count) return false;

                for (var i = 0; i < L.Count; i++)
                {
                    if (L[i] != other.L[i]) return false;
                }
            }

            if (M == null && other.M != null) return false;
            if (M != null && other.M == null) return false;

            if (M != null)
            {
                if (M.Count != other.M.Count) return false;

                foreach (var kv in M)
                {
                    if (!other.M.ContainsKey(kv.Key)) return false;

                    if(kv.Value != other.M[kv.Key]) return false;
                }
            }

            if(N == null && other.N != null) return false;

            if(N != null) return N.Equals(other.N);

            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    [ProtoBuf.ProtoContract]
    public class PropsPoco
    {
        [ProtoBuf.ProtoMember(1)]
        public int A { get; set; }
        [ProtoBuf.ProtoMember(2)]
        public uint B { get; set; }
        [ProtoBuf.ProtoMember(3)]
        public long C { get; set; }
        [ProtoBuf.ProtoMember(4)]
        public ulong D { get; set; }
        [ProtoBuf.ProtoMember(5)]
        public short E { get; set; }
        [ProtoBuf.ProtoMember(6)]
        public ushort F { get; set; }
        [ProtoBuf.ProtoMember(7)]
        public byte G { get; set; }
        [ProtoBuf.ProtoMember(8)]
        public sbyte H { get; set; }
        [ProtoBuf.ProtoMember(9)]
        public char I { get; set; }
        [ProtoBuf.ProtoMember(10)]
        public string J { get; set; }
        [ProtoBuf.ProtoMember(11)]
        public int[] K { get; set; }
        [ProtoBuf.ProtoMember(12)]
        public List<long> L { get; set; }
        [ProtoBuf.ProtoMember(13)]
        public Dictionary<short, byte> M { get; set; }

        [ProtoBuf.ProtoMember(14)]
        public PropsPoco N { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as PropsPoco;
            if (other == null) return false;

            if (A != other.A) return false;
            if (B != other.B) return false;
            if (C != other.C) return false;
            if (D != other.D) return false;
            if (E != other.E) return false;
            if (F != other.F) return false;
            if (G != other.G) return false;
            if (H != other.H) return false;
            if (I != other.I) return false;
            if (J != other.J) return false;

            if (K == null && other.K != null) return false;
            if (K != null && other.K == null) return false;

            if (K != null)
            {
                if (K.Length != other.K.Length) return false;

                for (var i = 0; i < K.Length; i++)
                {
                    if (K[i] != other.K[i]) return false;
                }
            }

            if (L == null && other.L != null) return false;
            if (L != null && other.L == null) return false;

            if (L != null)
            {
                if (L.Count != other.L.Count) return false;

                for (var i = 0; i < L.Count; i++)
                {
                    if (L[i] != other.L[i]) return false;
                }
            }

            if (M == null && other.M != null) return false;
            if (M != null && other.M == null) return false;

            if (M != null)
            {
                if (M.Count != other.M.Count) return false;

                foreach (var kv in M)
                {
                    if (!other.M.ContainsKey(kv.Key)) return false;

                    if (kv.Value != other.M[kv.Key]) return false;
                }
            }

            if (N == null && other.N != null) return false;

            if (N != null) return N.Equals(other.N);

            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    class Program
    {
        private static PropsPoco BuildPropsPoco(int seed)
        {
            var rand = new Random(seed);

            return
                new PropsPoco
                {
                    A = rand.Next<int>(),
                    //B = rand.Next<uint>(),    // Json.Net doesn't deal with unsigned very well
                    C = rand.Next<long>(),
                    //D = rand.Next<ulong>(),   // Json.Net doesn't deal with unsigned very well
                    E = rand.Next<short>(),
                    //F = rand.Next<ushort>(),  // Json.Net doesn't deal with unsigned very well
                    G = rand.Next<byte>(),
                    H = rand.Next<sbyte>(),
                    I = rand.Next<char>(),
                    J = rand.NextString(10),
                    K = rand.NextArray<int>(10),
                    L = rand.NextArray<long>(10).ToList(),
                    M = rand.NextDictionary<short, byte>(10),
                    N = rand.Next() % 2 == 0 ? null : BuildPropsPoco(seed + 1)
                };
        }

        private static FieldsPoco BuildFieldsPoco(int seed)
        {
            var rand = new Random(seed);

            return
                new FieldsPoco
                {
                    A = rand.Next<int>(),
                    //B = rand.Next<uint>(),    // Json.Net doesn't deal with unsigned very well
                    C = rand.Next<long>(),
                    //D = rand.Next<ulong>(),   // Json.Net doesn't deal with unsigned very well
                    E = rand.Next<short>(),
                    //F = rand.Next<ushort>(),  // Json.Net doesn't deal with unsigned very well
                    G = rand.Next<byte>(),
                    H = rand.Next<sbyte>(),
                    I = rand.Next<char>(),
                    J = rand.NextString(10),
                    K = rand.NextArray<int>(10),
                    L = rand.NextArray<long>(10).ToList(),
                    M = rand.NextDictionary<short, byte>(10),
                    N = rand.Next() % 2 == 0 ? null : BuildFieldsPoco(seed + 1)
                };
        }

        private static void Json<T>(T obj)
        {
            T copy;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            copy = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        private static T JsonD<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }

        private static string JsonS<T>(T obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        private static void ProtoBufNet<T>(T obj)
        {
            T copy;
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, obj);

                mem.Seek(0, SeekOrigin.Begin);
                copy = ProtoBuf.Serializer.Deserialize<T>(mem);
            }
        }

        private static T ProtoBufNetD<T>(byte[] bytes)
        {
            using (var mem = new MemoryStream(bytes))
            {
                return ProtoBuf.Serializer.Deserialize<T>(mem);
            }
        }

        private static byte[] ProtoBufNetS<T>(T obj)
        {
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, obj);

                return mem.ToArray();
            }
        }

        private static void PB<T>(T obj)
        {
            T copy;
            using (var mem = new MemoryStream())
            {
                PublicBroadcasting.Serializer.Serialize(mem, obj);

                mem.Seek(0, SeekOrigin.Begin);
                copy = PublicBroadcasting.Serializer.Deserialize<T>(mem);
            }
        }

        private static byte[] PBS<T>(T obj)
        {
            using (var mem = new MemoryStream())
            {
                PublicBroadcasting.Serializer.Serialize(mem, obj);

                return mem.ToArray();
            }
        }

        private static T PBD<T>(byte[] bytes)
        {
            using (var mem = new MemoryStream(bytes))
            {
                return PublicBroadcasting.Serializer.Deserialize<T>(mem);
            }
        }

        private static dynamic PBDD(byte[] bytes)
        {
            using (var mem = new MemoryStream(bytes))
            {
                return PublicBroadcasting.Serializer.Deserialize(mem);
            }
        }

        private static void MessagePack<T>(T obj, MsgPack.Serialization.MessagePackSerializer<T> serializer)
        {
            T copy;
            using (var mem = new MemoryStream())
            {
                serializer.Pack(mem, obj);

                mem.Seek(0, SeekOrigin.Begin);
                copy = serializer.Unpack(mem);
            }
        }

        private static byte[] MessagePackS<T>(T obj, MsgPack.Serialization.MessagePackSerializer<T> serializer)
        {
            using (var mem = new MemoryStream())
            {
                serializer.Pack(mem, obj);

                return mem.ToArray();
            }
        }

        private static T MessagePackD<T>(MsgPack.Serialization.MessagePackSerializer<T> serializer, byte[] bytes)
        {
            using (var mem = new MemoryStream(bytes))
            {
                return serializer.Unpack(mem);
            }
        }

        static void Time<T>(string title, Func<T> build)
        {
            Console.WriteLine(title);
            for (var i = 0; i < title.Length; i++)
            {
                Console.Write('=');
            }
            Console.WriteLine();

            var obj = build();

            var mpSerializer = MsgPack.Serialization.MessagePackSerializer.Create<T>();

            // warmup
            for (var i = 0; i < 100; i++)
            {
                Json(obj);
                ProtoBufNet(obj);
                PB(obj);
                MessagePack(obj, mpSerializer);
            }

            Console.WriteLine();
            Console.WriteLine("Both");
            Console.WriteLine("----");

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("Json"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    Json(obj);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("ProtoBuf"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    ProtoBufNet(obj);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("MessagePack"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    MessagePack(obj, mpSerializer);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("PublicBroadcasting"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    PB(obj);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Serialization");
            Console.WriteLine("-------------");

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("Json"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    JsonS(obj);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("ProtoBuf"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    ProtoBufNetS(obj);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("MessagePack"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    MessagePackS(obj, mpSerializer);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("PublicBroadcasting"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    PBS(obj);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Deserialization");
            Console.WriteLine("---------------");

            byte[] protoBs, msgPackBs, PBBs;
            string jsonBs;

            jsonBs = JsonS(obj);
            protoBs = ProtoBufNetS(obj);
            msgPackBs = MessagePackS(obj, mpSerializer);
            PBBs = PBS(obj);

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("Json"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    JsonD<T>(jsonBs);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("ProtoBuf"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    ProtoBufNetD<T>(protoBs);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("MessagePack"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    MessagePackD(mpSerializer, msgPackBs);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("PublicBroadcastingStatic"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    PBD<T>(PBBs);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("PublicBroadcastingDynamic"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    PBDD(PBBs);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Size");
            Console.WriteLine("----");
            Console.WriteLine("Json: " + Encoding.UTF8.GetBytes(jsonBs).Length + " bytes");
            Console.WriteLine("ProtoBuf: " + protoBs.Length + " bytes");
            Console.WriteLine("MessagePack: " + msgPackBs.Length + " bytes");
            Console.WriteLine("PublicBroadcasting: " + PBBs.Length + " bytes");
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            Time("Fields POCO", () => BuildFieldsPoco(0));
            Time("Props POCO", () => BuildPropsPoco(0));
            Time("Fields List", 
                () =>
                {
                    var ret = new List<FieldsPoco>();
                    for (var i = 0; i < 100; i++)
                    {
                        ret.Add(BuildFieldsPoco(100 + i));
                    }

                    return ret;
                }
            );
            Time("Props List",
                () =>
                {
                    var ret = new List<PropsPoco>();
                    for (var i = 0; i < 100; i++)
                    {
                        ret.Add(BuildPropsPoco(100 + i));
                    }

                    return ret;
                }
            );
#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}