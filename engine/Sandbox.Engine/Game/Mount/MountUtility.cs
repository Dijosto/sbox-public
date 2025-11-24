namespace Sandbox.Mounting;

public static class MountUtility
{
	static readonly Dictionary<string, Texture> _cache = new();
	static readonly List<RenderJob> _jobs = new();
	static readonly HashSet<RenderJob> _activeJobs = new();

	/// <summary>
	/// Create a preview texture for the given resource loader.
	/// </summary>
	public static Texture GetPreviewTexture( ResourceLoader loader )
	{
		Assert.NotNull( loader, "loader was null" );

		var path = loader.Path;

		// In-memory cache
		if ( _cache.TryGetValue( path, out var cached ) )
			return cached;

		// Disk cache
		var cacheKey = $"preview/icons/{path.Md5()}.png";
		if ( FileSystem.Cache.TryGet( cacheKey, out var data ) )
		{
			using var bitmap = Bitmap.CreateFromBytes( data );
			var tex = bitmap.ToTexture( true );
			_cache[path] = tex;
			return tex;
		}

		// Queue render job
		var job = new RenderJob( path, cacheKey );
		_jobs.Add( job );
		_cache[path] = job.Texture;

		return job.Texture;
	}

	internal static void TickPreviewRenders()
	{
		_activeJobs.RemoveWhere( x => x.IsFinished );

		while ( _jobs.Count > 0 && _activeJobs.Count < 2 )
		{
			var job = _jobs.OrderBy( x => x.Texture.LastUsed ).First();
			_jobs.Remove( job );

			_activeJobs.Add( job );
			job.Start();
		}
	}

	internal static void FlushCache()
	{
		_jobs.Clear();
		_activeJobs.Clear();
		_cache.Clear();
	}
}

class RenderJob
{
	public string Path { get; }
	public Texture Texture { get; }

	Task _task;
	readonly string _cacheKey;

	public bool IsFinished => _task is null || _task.IsCompleted;

	public RenderJob( string path, string cacheKey )
	{
		_cacheKey = cacheKey;
		Path = path;

		using var bitmap = new Bitmap( 256, 256 );
		bitmap.Clear( Color.Transparent );
		Texture = bitmap.ToTexture( true );
	}

	public void Start()
	{
		_task = Run();
	}

	public async Task Run()
	{
		var modelResource = await Model.LoadAsync( Path );
		if ( !modelResource.IsValid() ) return;

		using var bitmap = new Bitmap( 512, 512 );
		SceneUtility.RenderModelBitmap( modelResource, bitmap );

		using var resized = bitmap.Resize( 256, 256 );
		Texture.Update( resized );

		FileSystem.Cache.Set( _cacheKey, resized.ToPng() );
	}
}
