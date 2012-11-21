using System;
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

    class Program
    {
        private static FieldsPoco BuildFieldsPoco(int seed)
        {
            var rand = new Random(seed);

            return
                new FieldsPoco
                {
                    A = rand.Next<int>(),
                    B = rand.Next<uint>(),
                    C = rand.Next<long>(),
                    D = rand.Next<ulong>(),
                    E = rand.Next<short>(),
                    F = rand.Next<ushort>(),
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

        private static void ProtoBufNet<T>(T obj)
        {
            using (var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(mem, obj);
            }
        }

        private static void PB<T>(T obj)
        {
            using (var mem = new MemoryStream())
            {
                PublicBroadcasting.Serializer.Serialize(mem, obj);
            }
        }

        private static void MessagePack<T>(T obj, MsgPack.Serialization.MessagePackSerializer<T> serializer)
        {
            using (var mem = new MemoryStream())
            {
                serializer.Pack(mem, obj);
            }
        }

        static void Main(string[] args)
        {
            var fields = BuildFieldsPoco(0);

            var mpSerializer = MsgPack.Serialization.MessagePackSerializer.Create<FieldsPoco>();

            // warmup
            ProtoBufNet(fields);
            PB(fields);
            MessagePack(fields, mpSerializer);

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("ProtoBuf"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    ProtoBufNet(fields);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("MessagePack"))
            {
                for (var i = 0; i < 10000; i++)
                {
                    MessagePack(fields, mpSerializer);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete(-1);

            using (new Timer("PublicBroadcasting"))
            {
                for (var i = 0; i < 1000; i++)
                {
                    PB(fields);
                }
            }

            Console.ReadKey();
        }
    }
}
