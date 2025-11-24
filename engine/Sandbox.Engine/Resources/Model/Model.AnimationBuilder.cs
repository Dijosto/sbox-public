using System.Runtime.InteropServices;

namespace Sandbox;

/// <summary>
/// Provides ability to generate animations for a <see cref="Model"/> at runtime.
/// See <see cref="ModelBuilder.AddAnimation(string, float)"/>
/// </summary>
public sealed class AnimationBuilder
{
	/// <summary>
	/// The name of the animation.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The frames per second of the animation.
	/// </summary>
	public float FrameRate { get; set; }

	/// <summary>
	/// This animation loops.
	/// </summary>
	public bool Looping { get; set; }

	/// <summary>
	/// This animation "adds" to the base result.
	/// </summary>
	public bool Delta { get; set; }

	/// <summary>
	/// This animation disables interpolation between frames.
	/// </summary>
	public bool DisableInterpolation { get; set; }

	/// <summary>
	/// The number of frames in the animation.
	/// </summary>
	public int FrameCount => _frames.Count;

	private struct Frame( int offset, int length )
	{
		public int Offset = offset;
		public int Length = length;
	}

	private readonly List<Transform> _boneTransforms = [];
	private readonly List<Frame> _frames = [];

	internal AnimationBuilder()
	{
	}

	/// <summary>
	/// Sets the name of the animation.
	/// </summary>
	public AnimationBuilder WithName( string name )
	{
		Name = name;
		return this;
	}

	/// <summary>
	/// Sets the frames per second of the animation.
	/// </summary>
	public AnimationBuilder WithFrameRate( float frameRate )
	{
		FrameRate = frameRate;
		return this;
	}

	/// <summary>
	/// Sets whether the animation loops.
	/// </summary>
	public AnimationBuilder WithLooping( bool looping = true )
	{
		Looping = looping;
		return this;
	}

	/// <summary>
	/// Sets whether the animation adds to the base result.
	/// </summary>
	public AnimationBuilder WithDelta( bool delta = true )
	{
		Delta = delta;
		return this;
	}

	/// <summary>
	/// Sets whether interpolation between frames is disabled.
	/// </summary>
	public AnimationBuilder WithInterpolationDisabled( bool disableInterpolation = true )
	{
		DisableInterpolation = disableInterpolation;
		return this;
	}

	/// <summary>
	/// Add bone transforms for a frame of animation.
	/// </summary>
	public AnimationBuilder AddFrame( Span<Transform> boneTransforms )
	{
		if ( boneTransforms.IsEmpty )
			return this;

		_frames.Add( new Frame( _boneTransforms.Count, boneTransforms.Length ) );
		_boneTransforms.AddRange( boneTransforms );

		return this;
	}

	/// <summary>
	/// Add bone transforms for a frame of animation.
	/// </summary>
	public AnimationBuilder AddFrame( List<Transform> boneTransforms )
	{
		if ( boneTransforms is null || boneTransforms.Count == 0 )
			return this;

		AddFrame( CollectionsMarshal.AsSpan( boneTransforms ) );

		return this;
	}

	internal ReadOnlySpan<Transform> GetFrame( int frameIndex )
	{
		if ( frameIndex < 0 || frameIndex >= FrameCount )
			throw new ArgumentOutOfRangeException( nameof( frameIndex ), $"Frame index {frameIndex} is out of range. Must be between 0 and {FrameCount - 1}." );

		var frame = _frames[frameIndex];
		return CollectionsMarshal.AsSpan( _boneTransforms ).Slice( frame.Offset, frame.Length );
	}
}

partial class ModelBuilder
{
	private readonly List<AnimationBuilder> _animations = [];

	/// <summary>
	/// Adds an animation to this model and returns a builder to construct the animation.
	/// </summary>
	/// <param name="name">The name of the animation.</param>
	/// <param name="frameRate">The frames per second of the animation.</param>
	/// <returns>An <see cref="AnimationBuilder"/> instance to construct the animation.</returns>
	public AnimationBuilder AddAnimation( string name, float frameRate )
	{
		var uniqueName = name;
		var suffix = 1;

		while ( _animations.Any( b => b.Name == uniqueName ) )
		{
			uniqueName = $"{name}_{suffix++}";
		}

		var builder = new AnimationBuilder { Name = uniqueName, FrameRate = frameRate };
		_animations.Add( builder );
		return builder;
	}

	private unsafe CAnimationGroupBuilder CreateAnimationGroup()
	{
		if ( _animations.Count == 0 )
			return default;

		if ( _animations.Sum( x => x.FrameCount ) == 0 )
			return default;

		var builder = CAnimationGroupBuilder.Create();

		foreach ( var anim in _animations )
		{
			if ( anim.FrameCount == 0 )
				continue;

			var i = builder.AddAnimation();
			builder.SetName( i, anim.Name );
			builder.SetFrameRate( i, anim.FrameRate );
			builder.SetLooping( i, anim.Looping );
			builder.SetDelta( i, anim.Delta );
			builder.SetDisableInterpolation( i, anim.DisableInterpolation );

			for ( var frame = 0; frame < anim.FrameCount; frame++ )
			{
				var boneTransforms = anim.GetFrame( frame );
				fixed ( Transform* pFrame = boneTransforms )
				{
					builder.AddFrame( i, (IntPtr)pFrame, boneTransforms.Length );
				}
			}
		}

		return builder;
	}
}
