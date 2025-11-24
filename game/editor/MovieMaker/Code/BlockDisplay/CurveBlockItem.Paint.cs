using Sandbox.MovieMaker;
using Sandbox.MovieMaker.Compiled;

namespace Editor.MovieMaker.BlockDisplays;

#nullable enable

partial class CurveBlockItem<T>
{
	private readonly List<CurveTile> _tiles = new();
	private bool _rangesDirty;

	IEnumerable<MovieTimeRange> ICurveBlockItem.GetPaintHints( MovieTimeRange timeRange )
	{
		return Block switch
		{
			IPaintHintBlock paintHintBlock => paintHintBlock.GetPaintHints( timeRange ),
			CompiledSampleBlock<T> => [timeRange.Clamp( Block.TimeRange )],
			_ => []
		};
	}

	/// <summary>
	/// Make sure we have the right number of tiles given the current width of this block.
	/// </summary>
	private void UpdateTiles()
	{
		var width = LocalRect.Width;

		// Round pixelsPerSecond to a power of 2 so that we don't need
		// to resize each tile every frame while smoothly zooming

		var pixelsPerSecond = Parent.Session.PixelsPerSecond.NearestPowerOfTwo();
		var scale = Parent.Session.PixelsPerSecond / pixelsPerSecond;

		var tileWidth = CurveTile.TileWidth * scale;
		var tileCount = (int)MathF.Ceiling( width / tileWidth );
		var tileDurationSeconds = (double)CurveTile.TileWidth / pixelsPerSecond;

		while ( _tiles.Count > tileCount )
		{
			_tiles.Pop().Destroy();
		}

		while ( _tiles.Count < tileCount )
		{
			_tiles.Add( new CurveTile( this, _tiles.Count ) );
		}

		var prev = TimeRange.Start;

		foreach ( var tile in _tiles )
		{
			var next = TimeRange.Start + MovieTime.FromSeconds( (tile.Index + 1) * tileDurationSeconds );
			var left = tileWidth * tile.Index;

			tile.PrepareGeometryChange();

			tile.Position = new Vector2( left, 0f );
			tile.Width = tileWidth;
			tile.TimeRange = (prev, next);

			prev = next;
		}
	}

	protected override void OnBlockChanged( MovieTimeRange timeRange )
	{
		UpdateTiles();

		foreach ( var tile in _tiles )
		{
			if ( tile.TimeRange.Intersect( timeRange ) is { IsEmpty: false } )
			{
				tile.MarkDirty( timeRange );
			}
		}

		_rangesDirty = true;
	}

	protected override void OnResize()
	{
		UpdateTiles();
	}
}
