using System;
using Sandbox.MovieMaker;
using Sandbox.MovieMaker.Compiled;

namespace TestMovieMaker;

[TestClass]
public sealed class CompiledTests
{
	public IMovieClip CreateExampleClip()
	{
		var rootTrack = MovieClip.RootGameObject( "Camera" );
		var cameraTrack = rootTrack.Component<CameraComponent>();

		return MovieClip.FromTracks( rootTrack, cameraTrack,
			rootTrack.Property<Vector3>( nameof( GameObject.LocalPosition ) )
				.WithConstant( (0f, 2f), new Vector3( 100f, 200f, 300f ) ),
			cameraTrack.Property<float>( nameof( CameraComponent.FieldOfView ) )
				.WithSamples( (1f, 3f), sampleRate: 2, [60f, 75f, 65f, 90f, 50f] ) );
	}

	public IMovieClip RoundTripSerialize( IMovieClip clip )
	{
		return Json.Deserialize<MovieClip>( Json.Serialize( clip ) );
	}

	[TestMethod]
	public void Serialize()
	{
		var clip = CreateExampleClip();
		var json = Json.Serialize( clip );

		Console.WriteLine( json );

		clip = Json.Deserialize<MovieClip>( json );

		Assert.AreEqual( 3d, clip.Duration.TotalSeconds );

		var cameraPosTrack = clip.GetProperty<Vector3>( "Camera", nameof( GameObject.LocalPosition ) );
		var fovTrack = clip.GetProperty<float>( "Camera", nameof( CameraComponent ), nameof( CameraComponent.FieldOfView ) );

		Assert.IsNotNull( cameraPosTrack );

		Assert.IsTrue( cameraPosTrack.TryGetValue( 1.5, out var position ) );
		Assert.IsFalse( cameraPosTrack.TryGetValue( 2.5, out _ ) );

		Assert.AreEqual( new Vector3( 100f, 200f, 300f ), position );

		Assert.IsNotNull( fovTrack );

		Assert.IsTrue( fovTrack.TryGetValue( 1.25, out var fov ) );
		Assert.IsFalse( fovTrack.TryGetValue( 0.5, out _ ) );

		Assert.AreEqual( (60f + 75f) / 2f, fov );
	}

	[TestMethod]
	public void SerializeStringTrackConstant()
	{
		var srcTrack = MovieClip.RootGameObject( "Object" )
			.Property<string>( nameof( GameObject.Name ) )
			.WithConstant( (0f, 1f), "Terry" );

		var clip = MovieClip.FromTracks( srcTrack );
		var json = Json.Serialize( clip );

		Console.WriteLine( json );

		clip = Json.Deserialize<MovieClip>( json );

		var dstTrack = clip.GetProperty<string>( "Object", nameof( GameObject.Name ) );

		Assert.IsNotNull( dstTrack );
		Assert.IsTrue( dstTrack.TryGetValue( 0.5f, out var name ) );
		Assert.AreEqual( "Terry", name );
	}

	[TestMethod]
	public void SerializeStringTrackSamples()
	{
		var srcTrack = MovieClip.RootGameObject( "Object" )
			.Property<string>( nameof( GameObject.Name ) )
			.WithSamples( (0f, 1f), 3, ["Larry", "Terry", "Jerry"] );

		var clip = MovieClip.FromTracks( srcTrack );
		var json = Json.Serialize( clip );

		Console.WriteLine( json );

		clip = Json.Deserialize<MovieClip>( json );

		var dstTrack = clip.GetProperty<string>( "Object", nameof( GameObject.Name ) );

		Assert.IsNotNull( dstTrack );
		Assert.IsTrue( dstTrack.TryGetValue( 0.5f, out var name ) );
		Assert.AreEqual( "Terry", name );
	}

	[TestMethod]
	public void SerializeTextScopeTrack()
	{
		var srcTrack = MovieClip.RootGameObject( "Object" )
			.Component<TextRenderer>()
			.Property<TextRendering.Scope>( nameof( TextRenderer.TextScope ) )
			.Property<string>( nameof( TextRendering.Scope.Text ) )
			.WithConstant( (0f, 1f), "Terry" );

		var clip = MovieClip.FromTracks( srcTrack );
		var json = Json.Serialize( clip );

		Console.WriteLine( json );

		clip = Json.Deserialize<MovieClip>( json );

		var dstTrack = clip.GetProperty<string>(
			"Object",
			nameof( TextRenderer ),
			nameof( TextRenderer.TextScope ),
			nameof( TextRendering.Scope.Text ) );

		Assert.IsNotNull( dstTrack );
		Assert.IsTrue( dstTrack.TryGetValue( 0.5f, out var name ) );
		Assert.AreEqual( "Terry", name );
	}

	[TestMethod]
	public void ValidateBlocks()
	{
		var track = MovieClip.RootGameObject( "Example" )
			.Property<Vector3>( "LocalPosition" )
			.WithConstant( (0d, 2d), default );

		Assert.ThrowsException<ArgumentException>( () => track.WithConstant( (1d, 3d), default ),
			"Overlapping blocks" );
	}
}
