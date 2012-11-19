using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class SimpleTypeDescription : TypeDescription
    {
        private const int IntTag = 0;
        private const int StringTag = 1;
        private const int BoolTag = 2;
        private const int DoubleTag = 3;
        private const int LongTag = 4;
        private const int ByteTag = 5;
        private const int CharTag = 6;
        private const int UIntTag = 7;
        private const int ULongTag = 8;
        private const int ShortTag = 9;
        private const int UShortTag = 10;
        private const int FloatTag = 11;
        private const int DecimalTag = 12;
        private const int SByteTag = 13;

        internal static readonly SimpleTypeDescription Int = new SimpleTypeDescription(IntTag);
        internal static readonly SimpleTypeDescription UInt = new SimpleTypeDescription(UIntTag);
        internal static readonly SimpleTypeDescription Long = new SimpleTypeDescription(LongTag);
        internal static readonly SimpleTypeDescription ULong = new SimpleTypeDescription(ULongTag);
        internal static readonly SimpleTypeDescription Short = new SimpleTypeDescription(ShortTag);
        internal static readonly SimpleTypeDescription UShort = new SimpleTypeDescription(UShortTag);
        internal static readonly SimpleTypeDescription Byte = new SimpleTypeDescription(ByteTag);
        internal static readonly SimpleTypeDescription SByte = new SimpleTypeDescription(SByteTag);

        internal static readonly SimpleTypeDescription Bool = new SimpleTypeDescription(BoolTag);

        internal static readonly SimpleTypeDescription Double = new SimpleTypeDescription(DoubleTag);
        internal static readonly SimpleTypeDescription Float = new SimpleTypeDescription(FloatTag);
        internal static readonly SimpleTypeDescription Decimal = new SimpleTypeDescription(DecimalTag);

        internal static readonly SimpleTypeDescription String = new SimpleTypeDescription(StringTag);
        internal static readonly SimpleTypeDescription Char = new SimpleTypeDescription(CharTag);

        [ProtoMember(1)]
        internal int Type { get; private set; }

        private SimpleTypeDescription() { }

        private SimpleTypeDescription(int tag)
        {
            Type = tag;
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            switch (Type)
            {
                case LongTag: return typeof(long);
                case ULongTag: return typeof(ulong);
                case IntTag: return typeof(int);
                case UIntTag: return typeof(uint);
                case ShortTag: return typeof(short);
                case UShortTag: return typeof(ushort);
                case ByteTag: return typeof(byte);
                case SByteTag: return typeof(sbyte);

                case BoolTag: return typeof(bool);

                case DoubleTag: return typeof(double);
                case FloatTag: return typeof(float);
                case DecimalTag: return typeof(decimal);

                case StringTag: return typeof(string);
                case CharTag: return typeof(char);

                default: throw new Exception("Unexpected Tag [" + Type + "]");
            }
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise = () => { };

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> ignored)
        {
            return this;
        }
    }
}
