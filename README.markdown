##Public Broadcasting

A self-describing wrapper around [protobuf-net](http://code.google.com/p/protobuf-net/).

#Work In Progress, Use At Own Risk!

Public Broadcasting sacrifices *some* of the compactness of [Protocol Buffers](http://en.wikipedia.org/wiki/Protocol_Buffers) and *some*
of the performance of [protobuf-net](http://code.google.com/p/protobuf-net/) to avoid the hassle of .proto files or manually adding
attributes to your types.  Addition and removal of members is handled gracefully, avoiding versioning headaches.

###Structurally Typed

Public Broadcasting encodes the structure of your data, not type names.  Thus there's no requirement to deserialize to a type of the same
type or which implements the same interfaces.

The following code exploits structural typing:  
```
class A
{
	public string Prop { get; set; }
	public int Field;

	public byte NotInB { get; set; }
}

struct B
{
	public string Prop;				// Not actually a property!
	public int Field { get; set; }	// Nor is this a field!
}

var aBytes = Serializer.Serialize(new A { Prop = "Value", Field = 123, NotInB = 5 });
var b = Serializer.Deserialize<B>(aBytes);

// b.Prop == "Value" && b.Field == 123
```

###Aware Of Common Types
Like protobuf-net, Public Broadcasting is aware of the following types and serializes them efficiently:

  - Int32
  - UInt32
  - Int64
  - UInt64
  - Int16
  - UInt16
  - Byte
  - SByte
  - Bool
  - Double
  - Single
  - Decimal
  - String
  - Char
  - DateTime
  - TimeSpan
  - GUID
  - URI
  - IList&lt;T&gt;
  - IDictionary&lt;TKey, TValue&gt;

###Collections

Public Broadcasting can serialize any collections which implement IList&lt;T&gt; or IDictionary&lt;TKey, TValue&gt;, including one-dimensional arrays.
No type information beyond "is a list" or "is a dictionary" is encoded, meaning that any implementation of IList&lt;T&gt; or IDictionary&lt;TKey, TValue&gt;
is a legal deserialization target.

In other words, the following is legal:
```
var dBytes = Serializer.Serialize(new Dictionary<int, string> { { 123, "Hello" } });
var iDict = Serializer.Deserialize<IDictionary<int, string>>(dBytes);
var myDict = Serializer.Deserialize<MyDictImpl<int, string>>(dBytes);

var lBytes = Serializer.Serialize(new List<string> { "Hello", "World" });
var iList = Serializer.Deserialize<IList<string>>(lBytes);
var myList = Serializer.Deserialize<MyListImpl<string>>(lBytes);
```

The only constraint on implementations of IList&lt;T&gt; and IDictionary&lt;TKey, TValue&gt; is that they must have a zero parameter constructor.

###Type Conversions

All widening conversions between numeric types are legal.  This means that conversions such as `byte -> int` during deserialization are legal,
but `sbyte -> uint` and `uint -> int` are not.

Public Broadcasting will ignore the distinction between user-defined ValueTypes and ReferenceTypes, conversion between any combination of them
is legal.

###Enumerations

Enumerations are encoded as their string equivalents, underlying types (`int`, `byte`, etc.) and values are discarded during serialization.
Both strings and other enumeration types are legal targets for deserialization, and while a different enumeration type **must** contain the
actual value being deserialized it need not include all possible values.

###Nullable Types

Nullable types freely convert to their non-nullable equivalents, null being replaced with default values.

Therefore:  
```
var val = Serializer.Deserialize<int>(Serializer.Serialize((int?)null));
```
Would result in `val` being equal to 0.  This holds true for enumerations as well, with null strings or nullable enumeration types becoming
the default value of target non-nullable enumerations.

###Dynamism

While types are necessary when serializing (though they will usually be inferred), they can be omitted when deserializing which results in a 
`dynamic` value being returned.

In addition to the expected "properties" being exposed on classes, a string indexer is also provided.

In the following, `equiv` will be true:  
```
dynamic dyn = Serializer.Deserialize(Serializer.Serialize(new { B = "C" }))

var equiv = dyn.B == dyn["B"]
```

Note that generally speaking types **will not** map to the originally serialized types, even if they are theoretically available.  In particular,
be aware that ValueTypes will not necessarily be ValueTypes and enumerations will not have the same underlying values (though they will have the same
names).

###Compactness

Public Broadcasting encodes a "structural type map" when serializing, and will typically add a few bytes of overhead for each member plus
the length of all relevant strings (such as property names, field names, and enumeration values).  Types and enumerations that are used multiple
times are only encoded a single time, with further uses replaced by references.

When serializing many instances of each type (such as in a collection) this overhead is typically dwarfed by the size of data.  For single
instances, the overhead is comparable (if usually somewhat smaller) to JSON.

Naturally, if you need the absolute smallest sizes possible you should use protobuf-net directly.

###Speed

At time of writing, Public Broadcasting is typically within an order of magnitude of directly using protobuf-net.  Dynamic deserialization is faster
than static deserialization, due to skipping a fair number of allocations and type mappings.

As with extreme compactness, a need for extreme speed would be best served by directly using protobuf-net.

#Reminder, Work In Progress!  Use At Your Own Risk, Because It Could All Break Tomorrow!