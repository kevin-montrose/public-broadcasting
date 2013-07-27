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
        internal const int IntTag = 0;
        internal const int StringTag = 1;
        internal const int BoolTag = 2;
        internal const int DoubleTag = 3;
        internal const int LongTag = 4;
        internal const int ByteTag = 5;
        internal const int CharTag = 6;
        internal const int DateTimeTag = 7;
        internal const int TimeSpanTag = 8;
        internal const int UIntTag = 9;
        internal const int ULongTag = 10;
        internal const int ShortTag = 11;
        internal const int UShortTag = 12;
        internal const int FloatTag = 13;
        internal const int DecimalTag = 14;
        internal const int GuidTag = 15;
        internal const int UriTag = 16;
        internal const int SByteTag = 17;

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

        internal static readonly SimpleTypeDescription DateTime = new SimpleTypeDescription(DateTimeTag);
        internal static readonly SimpleTypeDescription TimeSpan = new SimpleTypeDescription(TimeSpanTag);

        internal static readonly SimpleTypeDescription Guid = new SimpleTypeDescription(GuidTag);

        internal static readonly SimpleTypeDescription Uri = new SimpleTypeDescription(UriTag);

        internal override bool NeedsMapping
        {
            get { return false; }
        }

        [ProtoMember(1)]
        internal int Tag { get; private set; }

        private SimpleTypeDescription() { }

        private SimpleTypeDescription(int tag)
        {
            Tag = tag;
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            switch (Tag)
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

                case DateTimeTag: return typeof(DateTime);
                case TimeSpanTag: return typeof(TimeSpan);

                case GuidTag: return typeof(Guid);

                case UriTag: return typeof(Uri);

                default: throw new Exception("Unexpected Tag [" + Tag + "]");
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

        internal override bool ContainsRawObject(out string path)
        {
            path = null;
            return false;
        }
    }
}
