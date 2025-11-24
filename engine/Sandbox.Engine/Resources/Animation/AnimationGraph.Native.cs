using System.Runtime.InteropServices;

namespace NativeEngine;

internal enum AnimParamType : byte
{
	Unknown = 0,
	Bool,
	Enum,
	Int,
	Float,
	Vector,
	Rotation,
};

[StructLayout( LayoutKind.Explicit, Pack = 1, Size = 17 )]
internal struct AnimVariant
{
	[FieldOffset( 0 )]
	public Vector4 Value;

	[FieldOffset( 16 )]
	public AnimParamType Type;

	public T GetValue<T>()
	{
		return Type switch
		{
			AnimParamType.Bool => (T)Bool,
			AnimParamType.Int => (T)Int,
			AnimParamType.Enum => (T)Enum,
			AnimParamType.Float => (T)Float,
			AnimParamType.Vector => (T)Vector,
			AnimParamType.Rotation => (T)Rotation,
			_ => default,
		};
	}

	internal object Bool => BitConverter.GetBytes( Value.x )[0] != 0;
	internal object Int => BitConverter.ToInt32( BitConverter.GetBytes( Value.x ), 0 );
	internal object Enum => BitConverter.GetBytes( Value.x )[0];
	internal object Float => Value.x;
	internal object Vector => new Vector3( Value.x, Value.y, Value.z );
	internal object Rotation => new Rotation( Value.x, Value.y, Value.z, Value.w );
}
