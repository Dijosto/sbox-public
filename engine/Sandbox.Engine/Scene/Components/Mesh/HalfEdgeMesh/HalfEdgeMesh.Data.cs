using System.Collections;

namespace HalfEdgeMesh;

partial class Mesh
{
	public VertexData<TData> CreateVertexData<TData>( string name ) where TData : struct =>
		VertexList.CreateDataStream<VertexData<TData>>( name );

	public FaceData<TData> CreateFaceData<TData>( string name ) where TData : struct =>
		FaceList.CreateDataStream<FaceData<TData>>( name );

	public HalfEdgeData<TData> CreateHalfEdgeData<TData>( string name ) where TData : struct =>
		HalfEdgeList.CreateDataStream<HalfEdgeData<TData>>( name );
}

internal interface IDataStream
{
	internal void Allocate( IHandle hSource );
	internal void AllocateMultiple( int count );
}

internal sealed class VertexData<TData> : ComponentData<TData, VertexHandle> where TData : struct
{
}

internal sealed class FaceData<TData> : ComponentData<TData, FaceHandle> where TData : struct
{
}

internal sealed class HalfEdgeData<TData> : ComponentData<TData, HalfEdgeHandle> where TData : struct
{
}

internal abstract class ComponentData<T, THandle> : IEnumerable<T>, IDataStream where T : struct where THandle : IHandle
{
	internal readonly List<T> _list = new();

	public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int Count => _list.Count;

	public T this[THandle hHandle]
	{
		get => hHandle.Index >= 0 && hHandle.Index < Count ? _list[hHandle.Index] : default;
		set
		{
			if ( hHandle.Index >= 0 && hHandle.Index < Count )
				_list[hHandle.Index] = value;
		}
	}

	public void CopyFrom( T[] source )
	{
		int count = Math.Min( _list.Count, source.Length );
		for ( int i = 0; i < count; i++ )
			_list[i] = source[i];
	}

	void IDataStream.Allocate( IHandle hSource )
	{
		_list.Add( hSource is not null && hSource.IsValid && hSource.Index < Count ? _list[hSource.Index] : default );
	}

	void IDataStream.AllocateMultiple( int count )
	{
		_list.Capacity += count;
		for ( var i = 0; i < count; i++ )
			_list.Add( default );
	}
}

internal class ComponentList<T> : IEnumerable<T>
{
	private readonly List<T> _list = new();
	private readonly List<bool> _active = new();
	private readonly Dictionary<string, IDataStream> _streams = new();

	public int Count => _list.Count;

	public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public IEnumerable<int> ActiveList => Enumerable.Range( 0, Count ).Where( x => _active[x] );

	public T this[int index]
	{
		get => _list[index];
		internal set => _list[index] = value;
	}

	public int Allocate( T component, IHandle hSource )
	{
		_list.Add( component );
		_active.Add( true );

		foreach ( var stream in _streams.Values )
			stream.Allocate( hSource );

		return Count - 1;
	}

	public void AllocateMultiple( int count, T component )
	{
		_list.Capacity += count;
		for ( var i = 0; i < count; i++ )
		{
			_list.Add( component );
			_active.Add( true );
		}

		foreach ( var stream in _streams.Values )
			stream.AllocateMultiple( count );
	}

	public void Deallocate( IHandle hHandle )
	{
		_active[hHandle.Index] = false;
	}

	public bool IsAllocated( IHandle hHandle )
	{
		var index = hHandle.Index;
		if ( index < 0 || index >= _active.Count )
			return false;

		return _active[hHandle.Index];
	}

	public TDataStream CreateDataStream<TDataStream>( string name ) where TDataStream : IDataStream, new()
	{
		if ( _streams.ContainsKey( name ) )
			throw new ArgumentException( "A stream with the same name already exists.", nameof( name ) );

		var stream = new TDataStream();
		stream.AllocateMultiple( _list.Count );
		_streams[name] = stream;

		return stream;
	}
}
