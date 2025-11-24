using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Sandbox;

/// <summary>
/// Allows easy SIMD/AVX2 fast math on a span of floats
/// </summary>
public ref struct FloatSpan
{
	Span<float> _span;

	public FloatSpan( Span<float> span )
	{
		_span = span;
	}

	/// <summary>
	/// Uses SIMD/AVX2 to find the maximum value in a span of floats.
	/// </summary>
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public float Max()
	{
		if ( _span.IsEmpty ) return 0.0f;

		int i = 0;
		float max = float.MinValue;

		if ( Avx.IsSupported )
		{
			var maxVector = Vector256.Create( float.MinValue );

			// Get a pointer to the span data
			unsafe
			{
				fixed ( float* ptr = _span )
				{
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						var v = Avx.LoadVector256( ptr + i ); // Correct memory load
						maxVector = Avx.Max( maxVector, v );
					}
				}
			}

			// Reduce maxVector to a single float
			max = Math.Max( max, maxVector.GetElement( 0 ) );
			max = Math.Max( max, maxVector.GetElement( 1 ) );
			max = Math.Max( max, maxVector.GetElement( 2 ) );
			max = Math.Max( max, maxVector.GetElement( 3 ) );
			max = Math.Max( max, maxVector.GetElement( 4 ) );
			max = Math.Max( max, maxVector.GetElement( 5 ) );
			max = Math.Max( max, maxVector.GetElement( 6 ) );
			max = Math.Max( max, maxVector.GetElement( 7 ) );
		}

		// Handle remaining elements
		for ( ; i < _span.Length; i++ )
			max = Math.Max( max, _span[i] );

		return max;
	}

	/// <summary>
	/// Uses SIMD/AVX2 to find the minimum value in a span of floats.
	/// </summary>
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public float Min()
	{
		if ( _span.IsEmpty ) return 0.0f;

		int i = 0;
		float max = float.MaxValue;

		if ( Avx.IsSupported )
		{
			var maxVector = Vector256.Create( float.MaxValue );

			// Get a pointer to the span data
			unsafe
			{
				fixed ( float* ptr = _span )
				{
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						var v = Avx.LoadVector256( ptr + i ); // Correct memory load
						maxVector = Avx.Min( maxVector, v );
					}
				}
			}

			max = Math.Min( max, maxVector.GetElement( 0 ) );
			max = Math.Min( max, maxVector.GetElement( 1 ) );
			max = Math.Min( max, maxVector.GetElement( 2 ) );
			max = Math.Min( max, maxVector.GetElement( 3 ) );
			max = Math.Min( max, maxVector.GetElement( 4 ) );
			max = Math.Min( max, maxVector.GetElement( 5 ) );
			max = Math.Min( max, maxVector.GetElement( 6 ) );
			max = Math.Min( max, maxVector.GetElement( 7 ) );
		}

		// Handle remaining elements
		for ( ; i < _span.Length; i++ )
			max = Math.Min( max, _span[i] );

		return max;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public float Average()
	{
		if ( _span.IsEmpty ) return 0.0f;

		int i = 0;
		float sum = 0f;
		float len = _span.Length;

		if ( Avx.IsSupported )
		{
			var sumVector = Vector256<float>.Zero;

			unsafe
			{
				fixed ( float* ptr = _span )
				{
					// Sum using AVX2
					for ( ; i <= len - 8; i += 8 )
					{
						var v = Avx.LoadVector256( ptr + i );
						sumVector = Avx.Add( sumVector, v );
					}
				}
			}

			// Reduce sumVector to a single float
			sum += sumVector.GetElement( 0 );
			sum += sumVector.GetElement( 1 );
			sum += sumVector.GetElement( 2 );
			sum += sumVector.GetElement( 3 );
			sum += sumVector.GetElement( 4 );
			sum += sumVector.GetElement( 5 );
			sum += sumVector.GetElement( 6 );
			sum += sumVector.GetElement( 7 );
		}

		// Handle remaining elements
		for ( ; i < len; i++ )
			sum += _span[i];

		return sum / len;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public float Sum()
	{
		if ( _span.IsEmpty ) return 0.0f;

		int i = 0;

		float sum = 0f;

		if ( Avx.IsSupported )
		{
			var sumVector = Vector256<float>.Zero;

			unsafe
			{
				fixed ( float* ptr = _span )
				{
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						var v = Avx.LoadVector256( ptr + i );
						sumVector = Avx.Add( sumVector, v );
					}
				}
			}

			// Reduce sumVector using horizontal adds
			var temp = Avx.HorizontalAdd( sumVector, sumVector );
			temp = Avx.HorizontalAdd( temp, temp );
			temp = Avx.HorizontalAdd( temp, temp );

			sum += temp.GetElement( 0 );

		}

		for ( ; i < _span.Length; i++ )
			sum += _span[i];

		return sum;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void Set( float value )
	{
		int i = 0;

		if ( Avx.IsSupported )
		{
			unsafe
			{
				fixed ( float* ptr = _span )
				{
					var v = Vector256.Create( value );
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						Avx.Store( ptr + i, v );
					}
				}
			}
		}

		for ( ; i < _span.Length; i++ )
			_span[i] = value;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public readonly void Set( in Span<float> values )
	{
		if ( _span.Length != values.Length ) throw new ArgumentException( "Source and destination spans must be the same length." );

		unsafe
		{
			var size = _span.Length * sizeof( float );

			fixed ( float* srcPtr = values, dstPtr = _span )
			{
				NativeLowLevel.Copy( (IntPtr)srcPtr, (IntPtr)dstPtr, (uint)size );
			}
		}
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public readonly void CopyScaled( in Span<float> values, float scale )
	{
		if ( _span.Length != values.Length ) throw new ArgumentException( "Source and destination spans must be the same length." );

		int i = 0;

		if ( Avx.IsSupported )
		{
			var scaleVector = Vector256.Create( scale );

			unsafe
			{
				fixed ( float* srcPtr = values, dstPtr = _span )
				{
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						var v = Avx.LoadVector256( srcPtr + i );
						v = Avx.Multiply( v, scaleVector );
						Avx.Store( dstPtr + i, v );
					}
				}
			}
		}

		for ( ; i < _span.Length; i++ )
			_span[i] = values[i] * scale;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public readonly void Add( in Span<float> values )
	{
		if ( _span.Length != values.Length ) throw new ArgumentException( "Source and destination spans must be the same length." );

		int i = 0;

		if ( Avx.IsSupported )
		{
			unsafe
			{
				fixed ( float* srcPtr = values, dstPtr = _span )
				{
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						var v = Avx.LoadVector256( srcPtr + i );
						var dst = Avx.LoadVector256( dstPtr + i );
						dst = Avx.Add( dst, v );
						Avx.Store( dstPtr + i, dst );
					}
				}
			}
		}

		for ( ; i < _span.Length; i++ )
			_span[i] += values[i];
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public readonly void AddScaled( in Span<float> values, float scale )
	{
		if ( _span.Length != values.Length ) throw new ArgumentException( "Source and destination spans must be the same length." );

		int i = 0;

		if ( Avx.IsSupported )
		{
			var scaleVector = Vector256.Create( scale );

			unsafe
			{
				fixed ( float* srcPtr = values, dstPtr = _span )
				{
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						var v = Avx.LoadVector256( srcPtr + i );
						v = Avx.Multiply( v, scaleVector );
						var dst = Avx.LoadVector256( dstPtr + i );
						dst = Avx.Add( dst, v );
						Avx.Store( dstPtr + i, dst );
					}
				}
			}
		}

		for ( ; i < _span.Length; i++ )
			_span[i] += values[i] * scale;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public readonly void Scale( float scale )
	{
		int i = 0;

		if ( Avx.IsSupported )
		{
			var scaleVector = Vector256.Create( scale );

			unsafe
			{
				fixed ( float* dstPtr = _span )
				{
					for ( ; i <= _span.Length - 8; i += 8 )
					{
						var v = Avx.LoadVector256( dstPtr + i );
						v = Avx.Multiply( v, scaleVector );
						Avx.Store( dstPtr + i, v );
					}
				}
			}
		}

		for ( ; i < _span.Length; i++ )
			_span[i] *= scale;
	}
}
