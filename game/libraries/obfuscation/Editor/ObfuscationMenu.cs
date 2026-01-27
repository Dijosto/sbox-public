namespace Obfuscation;

/// <summary>
/// Editor menu integration for code obfuscation.
/// </summary>
public static class ObfuscationMenu
{
	/// <summary>
	/// Open the obfuscation publish dialog for the current project.
	/// </summary>
	[Menu( "Editor", "Build/Publish with Obfuscation", "shield" )]
	public static void OpenObfuscationPublish()
	{
		var project = Project.Current;
		if ( project == null )
		{
			Log.Warning( "No project is currently open." );
			return;
		}

		// Only allow for games and addons that have code
		if ( project.Config.Type != "game" && project.Config.Type != "addon" )
		{
			Log.Warning( "Obfuscation is only supported for games and addons." );
			return;
		}

		if ( !project.HasCodePath() )
		{
			Log.Warning( "Project has no code to obfuscate." );
			return;
		}

		var popup = new ObfuscationPublishPopup( project );
		popup.SetModal( true, true );
		popup.Show();
	}

	/// <summary>
	/// Quick obfuscation test - strips comments and shows a preview.
	/// </summary>
	[Menu( "Editor", "Build/Preview Obfuscation", "visibility" )]
	public static async void PreviewObfuscation()
	{
		var project = Project.Current;
		if ( project == null )
		{
			Log.Warning( "No project is currently open." );
			return;
		}

		if ( !project.HasCodePath() )
		{
			Log.Warning( "Project has no code." );
			return;
		}

		Log.Info( "Compiling to preview obfuscation..." );

		try
		{
			var outputs = await EditorUtility.Projects.Compile( project, msg => Log.Info( msg ) );

			if ( outputs == null || outputs.Length == 0 )
			{
				Log.Warning( "Compilation failed." );
				return;
			}

			foreach ( var output in outputs )
			{
				if ( output?.Archive == null ) continue;

				var originalCount = output.Archive.SyntaxTrees.Count;
				var originalSize = output.Archive.SyntaxTrees.Sum( t => t.GetText().Length );

				// Apply obfuscation
				var options = new ObfuscationOptions
				{
					StripComments = true,
					RenamePrivateSymbols = false, // Keep names for preview readability
					EncryptStrings = false,
					MinifyWhitespace = true
				};

				ObfuscationEditor.ObfuscateArchive( output, options );

				var obfuscatedSize = output.Archive.SyntaxTrees.Sum( t => t.GetText().Length );
				var reduction = ((float)(originalSize - obfuscatedSize) / originalSize) * 100;

				Log.Info( $"Assembly: {output.Compiler.AssemblyName}" );
				Log.Info( $"  Files: {originalCount}" );
				Log.Info( $"  Original size: {originalSize:n0} chars" );
				Log.Info( $"  Obfuscated size: {obfuscatedSize:n0} chars" );
				Log.Info( $"  Reduction: {reduction:F1}%" );

				// Show a sample of the first file
				if ( output.Archive.SyntaxTrees.Count > 0 )
				{
					var firstTree = output.Archive.SyntaxTrees[0];
					var preview = firstTree.GetText().ToString();
					if ( preview.Length > 500 )
						preview = preview.Substring( 0, 500 ) + "...";

					Log.Info( $"\nSample preview from {firstTree.FilePath}:\n{preview}" );
				}
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( ex, "Preview failed" );
		}
	}
}
