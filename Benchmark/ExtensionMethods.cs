using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    public static class ExtensionMethods
    {
        public static T Next<T>(this Random rand) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));

            var bytes = new byte[size];
            rand.NextBytes(bytes);

            var ret = Activator.CreateInstance<T>();

            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(bytes, 0, ptr, size);

            ret = (T)Marshal.PtrToStructure(ptr, ret.GetType());
            Marshal.FreeHGlobal(ptr);

            return ret;
        }

        public static string NextString(this Random rand, int length)
        {
            var builder = new StringBuilder();

            while (builder.Length < length)
            {
                var c = rand.Next<char>();

                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c) || char.IsSymbol(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public static T[] NextArray<T>(this Random rand, int length) where T : struct
        {
            var ret = new T[length];

            for (var i = 0; i < length; i++)
            {
                ret[i] = rand.Next<T>();
            }

            return ret;
        }

        public static Dictionary<T, V> NextDictionary<T,V>(this Random rand, int length) 
            where T : struct
            where V : struct
        {
            var ret = new Dictionary<T, V>(length);

            for (var i = 0; i < length; i++)
            {
                var key = rand.Next<T>();
                if (ret.ContainsKey(key)) continue;

                ret[key] = rand.Next<V>();
            }

            return ret;
        }
    }
}
