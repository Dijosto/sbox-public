namespace Sandbox;

/// <summary>
/// A container for the current language, allowing access to translated phrases and language information.
/// </summary>
public class LanguageContainer
{
	/// <summary>
	/// The abbreviation for the language the user wants. This is set by the user in the options menu.
	/// </summary>
	public string SelectedCode => Application.LanguageCode;

	/// <summary>
	/// Information about the current selected language. Will default to English if the current language isn't found.
	/// </summary>
	public Sandbox.Localization.LanguageInformation Current { get; internal set; }

	Sandbox.Localization.PhraseCollection lang;

	BaseFileSystem filesystem;
	FileWatch _watcher;

	internal LanguageContainer( BaseFileSystem filesystem )
	{
		this.filesystem = filesystem;
		Tick();

		_watcher = filesystem.Watch();
		_watcher.OnChanges += x => OnFileChanged();
	}

	string _previousLanguage;

	internal void Tick()
	{
		var language = Application.LanguageCode;

		language ??= "en";
		language = language.ToLower();

		if ( _previousLanguage == language )
			return;

		_previousLanguage = language;

		// Add english first for fallbacks
		lang = new Localization.PhraseCollection();
		AddFromPath( "en" );

		// Switch to new language
		AddFromPath( language );
	}

	void AddFromPath( string shortName )
	{
		if ( string.IsNullOrWhiteSpace( shortName ) ) return;
		if ( shortName.Contains( "." ) ) return;
		if ( shortName.Contains( ":" ) ) return;
		if ( shortName.Contains( "/" ) ) return;

		var language = Sandbox.Localization.Languages.Find( shortName );
		if ( language != null ) Current = language;


		foreach ( var file in filesystem.FindFile( shortName, "*.json" ) )
		{
			AddFile( $"{shortName}/{file}" );
		}

	}

	void AddFile( string path )
	{
		try
		{
			var entries = filesystem.ReadJson<Dictionary<string, string>>( path );
			if ( entries == null ) return;

			foreach ( var entry in entries )
			{
				lang.Set( entry.Key, entry.Value );
			}

		}
		catch ( Exception e )
		{
			Log.Warning( $"Couldn't read localization file {path}: {e}" );
		}
	}

	/// <summary>
	/// Called when a localization file has changed (and we should reload)
	/// </summary>
	internal void OnFileChanged()
	{
		// setting this to null will cause a reload in tick
		_previousLanguage = null;
	}

	/// <summary>
	/// Look up a phrase
	/// </summary>
	/// <param name="textToken">The token used to identify the phrase</param>
	/// <param name="data">Key values of data used by the string. Example: {Variable} -> { "Variable", someVar }</param>
	/// <returns>If found will return the phrase, else will return the token itself</returns>
	public string GetPhrase( string textToken, Dictionary<string, object> data = null )
	{
		if ( lang == null ) return textToken;
		return lang.GetPhrase( textToken, data );
	}

	internal void Shutdown()
	{
		_watcher?.Dispose();
		_watcher = null;

		filesystem?.Dispose();
		filesystem = null;
	}
}

/// <summary>
/// Allows access to translated phrases, allowing the translation of gamemodes etc
/// </summary>
[SkipHotload]
public static class Language
{
	/// <summary>
	/// The abbreviation for the language the user wants. This is set by the user in the options menu.
	/// </summary>
	public static string SelectedCode => Game.Language.SelectedCode;

	/// <summary>
	/// Information about the current selected language. Will default to English if the current language isn't found.
	/// </summary>
	public static Sandbox.Localization.LanguageInformation Current => Game.Language.Current;

	/// <summary>
	/// Look up a phrase
	/// </summary>
	/// <param name="textToken">The token used to identify the phrase</param>
	/// <param name="data">Key values of data used by the string. Example: {Variable} -> { "Variable", someVar }</param>
	/// <returns>If found will return the phrase, else will return the token itself</returns>
	public static string GetPhrase( string textToken, Dictionary<string, object> data = null ) => Game.Language.GetPhrase( textToken, data );

}
