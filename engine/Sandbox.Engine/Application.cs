using Sandbox.Engine;
using Sandbox.Engine.Settings;
using Sandbox.Utility;
using Sandbox.VR;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Sandbox;

public static class Application
{
	/// <summary>
	/// Prevent double initialization
	/// </summary>
	internal static bool IsInitialized { get; private set; }

	/// <summary>
	/// Steam AppId of S&amp;box.
	/// </summary>
	public static ulong AppId { get; internal set; } = 590830;

	/// <summary>
	/// True if we're running the engine as part of a unit test
	/// </summary>
	public static bool IsUnitTest { get; private set; }


	/// <summary>
	/// True if we're running a live unit test.
	/// </summary>
	internal static bool IsLiveUnitTest { get; private set; }

	/// <summary>
	/// True if running without a graphics window, such as in a terminal.
	/// </summary>
	public static bool IsHeadless { get; private set; }

	/// <summary>
	/// True if running in a terminal like console, instead of a game window or editor.
	/// </summary>
	public static bool IsConsoleApp => IsHeadless;

	/// <summary>
	/// True if this is a dedicated server
	/// </summary>
	public static bool IsDedicatedServer { get; private set; }

	/// <summary>
	/// True if running a benchmark
	/// </summary>
	internal static bool IsBenchmark { get; private set; }

	/// <summary>
	/// True if running with the tools or editor attached
	/// </summary>
	public static bool IsEditor { get; private set; }

	/// <summary>
	/// True if running with -joinlocal. This is an instance that launches and joins
	/// an in process editor session.
	/// </summary>
	internal static bool IsJoinLocal { get; private set; }

	/// <summary>
	/// The engine's version string
	/// </summary>
	public static string Version { get; internal set; }

	/// <summary>
	/// True if this is compiled and published on steam
	/// </summary>
	internal static bool IsRetail { get; private set; }

	/// <summary>
	/// The date of this version, as a UTC datetime.
	/// </summary>
	public static DateTime VersionDate { get; internal set; }

	/// <summary>
	/// Number of exceptions we've had. Resets on game exit.
	/// </summary>
	internal static int ExceptionCount { get; set; }

	/// <summary>
	/// True if the game is running in standalone mode
	/// </summary>
	public static bool IsStandalone { get; internal set; }

	/// <summary>
	/// The language code for the current language
	/// </summary>
	[ConVar( "language", ConVarFlags.Saved | ConVarFlags.Protected, Name = "Language" )]
	public static string LanguageCode { get; internal set; } = "en";

	/// <summary>
	/// True if the game is running in VR mode
	/// </summary>
	public static bool IsVR => VRSystem.IsActive; // garry: I think this is right? But feels like this should be set at startup and never change?

	static CMaterialSystem2AppSystemDict AppSystem;

	/// <summary>
	/// Called from unit test projects to initialize the engine
	/// </summary>
	public static void InitUnitTest<T>( bool withtools = true, bool withRendering = false )
	{
		if ( IsInitialized )
			throw new InvalidOperationException( "Already Initialized" );

		SyncContext.Init();
		SyncContext.Reset();

		ThreadSafe.MarkMainThread();

		var callingAssembly = Assembly.GetCallingAssembly();
		var GameFolder = System.Environment.GetEnvironmentVariable( "FACEPUNCH_ENGINE", IsLiveUnitTest ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process );
		if ( GameFolder is null ) throw new Exception( "FACEPUNCH_ENGINE not found" );

		var nativeDllPath = $"{GameFolder}\\bin\\win64\\";

		var native = NativeLibrary.Load( $"{nativeDllPath}engine2.dll" );

		//
		// Put our native dll path first so that when looking up native dlls we'll
		// always use the ones from our folder first
		//
		var path = System.Environment.GetEnvironmentVariable( "PATH" );
		path = $"{nativeDllPath};{path}";
		System.Environment.SetEnvironmentVariable( "PATH", path );

		Api.Init();
		EngineFileSystem.Initialize( GameFolder );
		Application.InitializeGame( false, false, false, true, false );
		NetCore.InitializeInterop( GameFolder );

		Game.InitUnitTest<T>();

		AppSystem = CMaterialSystem2AppSystemDict.Create( new NativeEngine.MaterialSystem2AppSystemDictCreateInfo()
		{
			iFlags = NativeEngine.MaterialSystem2AppSystemDictFlags.IsGameApp
		} );

		AppSystem.SuppressCOMInitialization();
		AppSystem.SuppressStartupManifestLoad( true );
		AppSystem.SetModGameSubdir( "core" );
		AppSystem.SetInTestMode();

		if ( withRendering )
		{
			AppSystem.SetDefaultRenderSystemOption( "-vulkan" );
		}

		if ( !NativeEngine.EngineGlobal.SourceEnginePreInit( "", AppSystem ) )
		{
			throw new System.Exception( "SourceEnginePreInit failed" );
		}

		AppSystem.InitFinishSetupMaterialSystem();

		AppSystem.AddSystem( "engine2", "SceneSystem_002" );
		AppSystem.AddSystem( "engine2", "SceneUtils_001" );
		AppSystem.AddSystem( "engine2", "WorldRendererMgr001" );

		NativeLibrary.Free( native );

		if ( withtools )
		{
			var sandboxGame = Assembly.Load( "Sandbox.Tools" );
			sandboxGame.GetType( "Editor.AssemblyInitialize", true, true )
					.GetMethod( "InitializeUnitTest", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
					.Invoke( null, new[] { callingAssembly } );
		}
	}

	internal static void InitLiveUnitTest<T>( bool withtools = true, bool withRendering = false )
	{
		Application.IsLiveUnitTest = true;
		InitUnitTest<T>( withtools, withRendering );
	}

	/// <summary>
	/// Called from unit test projects to politely shut down the engine
	/// </summary>
	public static void ShutdownUnitTest()
	{
		if ( !IsUnitTest )
		{
			throw new InvalidOperationException( "Not running a unit test" );
		}

		if ( AppSystem.IsValid )
		{
			NativeEngine.EngineGlobal.SourceEngineShutdown( AppSystem, false );
			AppSystem.Destroy();
			AppSystem = default;
		}
	}

	internal static void InitializeGame( bool dedicated, bool headless, bool toolsMode, bool testMode, bool isRetail )
	{
		if ( IsInitialized )
			throw new InvalidOperationException( "Already Initialized" );

		IsInitialized = true;

		IsDedicatedServer = dedicated;
		IsRetail = isRetail;
		IsUnitTest = testMode;
		IsHeadless = headless;
		IsEditor = toolsMode;
		IsJoinLocal = CommandLine.HasSwitch( "-joinlocal" );
		IsBenchmark = Environment.GetEnvironmentVariable( "SBOX_MODE" ) == "BENCHMARK";
	}

	internal static void TryLoadVersionInfo( string gameFolder )
	{
		Version = "0000000";
		VersionDate = DateTime.Now;

		var versionPath = System.IO.Path.Combine( gameFolder, ".version" );

		if ( System.IO.File.Exists( versionPath ) )
		{
			var text = System.IO.File.ReadAllText( versionPath );
			var split = text.Split( "\n" );

			Version = split[0].Trim();
			VersionDate = DateTime.ParseExact( split[4], "dd/MM/yyyy HH:mm:ss", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal );
		}
	}

	/// <summary>
	/// The currently loaded game package. May be null if no game loaded. 
	/// Controlled by GameMenuDll.
	/// </summary>
	internal static Package GamePackage { get; set; }

	/// <summary>
	/// The currently loaded map package
	/// </summary>
	internal static Package MapPackage { get; set; }


	/// <summary>
	/// The currently loaded game package's ident - if applicable.
	/// </summary>
	internal static string GameIdent { get; set; }

#if DEBUG
	public static bool IsDebug => true;
#endif

#if !DEBUG
	public static bool IsDebug => false;
#endif

	/// <summary>
	/// Returns true if the microphone is currently listening
	/// </summary>
	public static bool IsMicrophoneListening => VoiceManager.IsListening;

	/// <summary>
	/// Returns true if the microphone is currently listening and actually hearing/capturing sounds
	/// </summary>
	public static bool IsMicrophoneRecording => VoiceManager.IsRecording;

	/// <summary>
	/// Is the game window in focus?
	/// </summary>
	public static bool IsFocused => NativeEngine.EngineGlobal.IsWindowFocused();

	internal static bool WantsExit { get; set; }

	/// <summary>
	/// Exits the application if we're running in standalone mode or we are a Dedicated Server.
	/// </summary>
	internal static void Exit()
	{
		WantsExit = true;
	}

	internal static void ClearGame()
	{
		GameIdent = default;
		GamePackage = default;
		ExceptionCount = default;
		MapPackage = default;
	}

	public static bool CheatsEnabled
	{
		get => ConVarSystem.GetValue( "sv_cheats", "false", true ).ToBool();
	}

	/// <summary>
	/// Allows access to the RenderSettings singleton, which contains settings related to rendering in the game.
	/// You're only able to access this when in standalone mode. When accessing in the editor, or in sbox it will return null.
	/// </summary>
	public static RenderSettings RenderSettings
	{
		get
		{
			if ( !IsStandalone ) return null;
			return RenderSettings.Instance;
		}
	}

	/// <summary>
	/// Gets the active scene. This could be in the menu system, or in the game. This is provided
	/// for internal engine, and should never be accessible to the user code.
	/// </summary>
	internal static Scene GetActiveScene()
	{
		if ( IGameInstance.Current?.Scene is Scene gameScene && gameScene.IsValid() )
		{
			return gameScene;
		}

		if ( IMenuDll.Current?.Scene is Scene menuScene && menuScene.IsValid() )
		{
			return menuScene;
		}

		return null;
	}

}
