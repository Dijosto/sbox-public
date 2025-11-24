using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Sandbox;

[Expose]
public struct ParticleGradient
{
	public ParticleGradient()
	{
	}

	public ValueType Type { readonly get; set; }
	public EvaluationType Evaluation { readonly get; set; }

	public Gradient GradientA { readonly get; set; } = Color.White;
	public Gradient GradientB { readonly get; set; } = Color.White;
	public Color ConstantA { readonly get; set; } = Color.White;
	public Color ConstantB { readonly get; set; } = Color.White;

	[JsonIgnore]
	public Color ConstantValue
	{
		readonly get => ConstantA;
		set => ConstantA = value;
	}


	public static implicit operator ParticleGradient( Color color )
	{
		return new ParticleGradient { Type = ValueType.Constant, Evaluation = EvaluationType.Life, ConstantValue = color };
	}

	[Expose]
	public enum ValueType
	{
		Constant,
		Range,
		Gradient
	}

	[Expose]
	public enum EvaluationType
	{
		Life,
		Frame,
		Particle
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public readonly Color Evaluate( in float delta, in float randomFixed )
	{
		var d = Evaluation switch
		{
			EvaluationType.Life => delta,
			EvaluationType.Frame => Random.Shared.Float( 0, 1 ),
			EvaluationType.Particle => randomFixed,
			_ => delta,
		};

		switch ( Type )
		{
			case ValueType.Constant:
				{
					return ConstantValue;
				}

			case ValueType.Range:
				{
					return Color.Lerp( ConstantA, ConstantB, d );
				}

			case ValueType.Gradient:
				{
					return GradientA.Evaluate( d );
				}
		}

		return ConstantValue;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public readonly Color Evaluate( Particle p, int seed, [CallerLineNumber] int line = 0 )
	{
		return Evaluate( p.LifeDelta, p.Rand( seed, line ) );
	}
}
