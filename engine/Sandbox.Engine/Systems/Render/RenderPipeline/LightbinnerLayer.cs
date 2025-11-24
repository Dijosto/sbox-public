namespace Sandbox.Rendering;

internal class LightbinnerLayer : RenderLayer
{
	public LightbinnerLayer()
	{
		Name = "Lightbinner";

		Flags |= LayerFlags.NeverRemove;
		Flags |= LayerFlags.LightBinnerSetupLayer;

		ObjectFlagsRequired = SceneObjectFlags.IsLight;
	}

	/// <summary>
	/// Configures the lightbinner to react to mat_fullbright and more
	/// </summary>
	/// <param name="pipelineAttributes"></param>
	public void Setup( RenderAttributes pipelineAttributes )
	{
		bool directLighting = pipelineAttributes.GetBool( "directLighting", true );
		bool indirectLighting = pipelineAttributes.GetBool( "indirectLighting", true );
		bool environmentMaps = pipelineAttributes.GetBool( "environmentMaps", true );
		bool lightProbeVolumes = pipelineAttributes.GetBool( "lightProbeVolumes", true );
		bool renderSun = pipelineAttributes.GetBool( "renderSun", true );

		ObjectFlagsExcluded = SceneObjectFlags.None;

		if ( !directLighting )
		{
			ObjectFlagsExcluded |= SceneObjectFlags.IsDirectLight;
		}

		if ( !indirectLighting )
		{
			ObjectFlagsExcluded |= SceneObjectFlags.IsIndirectLight;
		}

		if ( !renderSun )
		{
			ObjectFlagsExcluded |= SceneObjectFlags.IsSunLight;
		}

		if ( !environmentMaps )
		{
			ObjectFlagsExcluded |= SceneObjectFlags.IsEnvMap;
		}

		if ( !lightProbeVolumes )
		{
			ObjectFlagsExcluded |= SceneObjectFlags.IsLightVolume;
		}
	}
}
