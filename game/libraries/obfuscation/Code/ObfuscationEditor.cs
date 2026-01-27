namespace Obfuscation;

/// <summary>
/// Editor integration for code obfuscation.
/// Provides menu items and UI for obfuscating code during publish.
/// </summary>
public static class ObfuscationEditor
{
	/// <summary>
	/// Apply obfuscation to a CompilerOutput's CodeArchive.
	/// This modifies the archive's syntax trees in place.
	/// </summary>
	public static void ObfuscateArchive( CompilerOutput output, ObfuscationOptions options = null )
	{
		if ( output?.Archive == null ) return;

		ObfuscationPipeline.ProcessArchive( output.Archive, options );
	}

	/// <summary>
	/// Apply obfuscation to all CompilerOutputs.
	/// </summary>
	public static void ObfuscateAll( IEnumerable<CompilerOutput> outputs, ObfuscationOptions options = null )
	{
		foreach ( var output in outputs )
		{
			ObfuscateArchive( output, options );
		}
	}

	/// <summary>
	/// Compile and obfuscate a project, ready for publishing.
	/// </summary>
	public static async Task<CompilerOutput[]> CompileObfuscated( Project project, ObfuscationOptions options = null, Action<string> logOutput = null )
	{
		options ??= new ObfuscationOptions();

		logOutput?.Invoke( "Compiling project..." );
		var outputs = await EditorUtility.Projects.Compile( project, logOutput );

		if ( outputs == null || outputs.Length == 0 )
		{
			logOutput?.Invoke( "Compilation failed or produced no output." );
			return outputs;
		}

		logOutput?.Invoke( "Applying obfuscation..." );

		foreach ( var output in outputs )
		{
			if ( output?.Archive == null ) continue;

			var treeCount = output.Archive.SyntaxTrees.Count;
			logOutput?.Invoke( $"Obfuscating {output.Compiler.AssemblyName} ({treeCount} files)..." );

			ObfuscateArchive( output, options );
		}

		logOutput?.Invoke( "Obfuscation complete." );
		return outputs;
	}
}

/// <summary>
/// Popup window for configuring and running obfuscated publish.
/// </summary>
public class ObfuscationPublishPopup : PopupDialogWidget
{
	Project _project;
	ObfuscationOptions _options;

	// UI elements
	CheckBox _stripCommentsCheck;
	CheckBox _renameSymbolsCheck;
	CheckBox _encryptStringsCheck;
	CheckBox _minifyWhitespaceCheck;

	public ObfuscationPublishPopup( Project project ) : base( "obfuscation_publish" )
	{
		_project = project;
		_options = new ObfuscationOptions();

		WindowTitle = "Publish with Obfuscation";
		FixedWidth = 500;

		BuildUI();
	}

	void BuildUI()
	{
		var layout = new Layout( LayoutMode.TopToBottom );
		layout.Margin = 16;
		layout.Spacing = 8;

		// Header
		var header = new Label( $"Publishing: {_project.Config.Title}" );
		header.SetStyles( "font-size: 16px; font-weight: bold;" );
		layout.Add( header );

		layout.AddSpacingCell( 8 );

		// Options section
		var optionsLabel = new Label( "Obfuscation Options:" );
		optionsLabel.SetStyles( "font-weight: bold;" );
		layout.Add( optionsLabel );

		_stripCommentsCheck = new CheckBox( "Strip Comments" );
		_stripCommentsCheck.Value = _options.StripComments;
		_stripCommentsCheck.ValueChanged += v => _options.StripComments = v;
		layout.Add( _stripCommentsCheck );

		_renameSymbolsCheck = new CheckBox( "Rename Private Symbols" );
		_renameSymbolsCheck.Value = _options.RenamePrivateSymbols;
		_renameSymbolsCheck.ValueChanged += v => _options.RenamePrivateSymbols = v;
		layout.Add( _renameSymbolsCheck );

		_encryptStringsCheck = new CheckBox( "Encrypt String Literals" );
		_encryptStringsCheck.Value = _options.EncryptStrings;
		_encryptStringsCheck.ValueChanged += v => _options.EncryptStrings = v;
		layout.Add( _encryptStringsCheck );

		_minifyWhitespaceCheck = new CheckBox( "Minify Whitespace" );
		_minifyWhitespaceCheck.Value = _options.MinifyWhitespace;
		_minifyWhitespaceCheck.ValueChanged += v => _options.MinifyWhitespace = v;
		layout.Add( _minifyWhitespaceCheck );

		layout.AddStretchCell();

		// Buttons
		ButtonLayout.Spacing = 4;
		ButtonLayout.AddStretchCell();

		ButtonLayout.Add( new Button( "Cancel" )
		{
			Clicked = Close
		} );

		ButtonLayout.Add( new Button.Primary( "Publish", "cloud_upload" )
		{
			Clicked = StartPublish
		} );

		Layout = layout;
	}

	async void StartPublish()
	{
		try
		{
			Log.Info( "Starting obfuscated publish..." );

			// Compile with obfuscation
			var outputs = await ObfuscationEditor.CompileObfuscated( _project, _options, msg => Log.Info( msg ) );

			if ( outputs == null || !outputs.Any( x => x.Successful ) )
			{
				Log.Warning( "Compilation failed." );
				return;
			}

			// Create publisher and proceed with normal publish flow
			var publisher = await ProjectPublisher.FromProject( _project );
			if ( publisher == null )
			{
				Log.Warning( "Failed to create publisher." );
				return;
			}

			// Add the obfuscated code archives to the manifest
			foreach ( var output in outputs.Where( x => x.Archive != null ) )
			{
				var archiveData = output.Archive.Serialize();
				var archivePath = $"/.bin/{output.Compiler.AssemblyName}.cll";
				await publisher.AddFile( archiveData, archivePath );
			}

			await publisher.PrePublish();
			await publisher.UploadFiles();
			await publisher.Publish();

			Log.Info( "Obfuscated publish complete!" );
			Close();
		}
		catch ( Exception ex )
		{
			Log.Warning( ex, "Publish failed" );
		}
	}
}
