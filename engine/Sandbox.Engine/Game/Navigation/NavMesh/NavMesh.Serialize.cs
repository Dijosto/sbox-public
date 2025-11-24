using System.Text.Json.Nodes;

namespace Sandbox.Navigation;

public sealed partial class NavMesh
{
	/// <summary>
	/// Data saved in a Scene file
	/// </summary>
	internal JsonObject Serialize()
	{
		JsonObject jso = new JsonObject();

		jso["Enabled"] = IsEnabled;
		jso["IncludeStaticBodies"] = IncludeStaticBodies;
		jso["IncludeKeyframedBodies"] = IncludeKeyframedBodies;
		jso["EditorAutoUpdate"] = EditorAutoUpdate;
		jso["AgentHeight"] = AgentHeight;
		jso["AgentRadius"] = AgentRadius;
		jso["AgentStepSize"] = AgentStepSize;
		jso["AgentMaxSlope"] = AgentMaxSlope;
		jso["ExcludedBodies"] = Json.ToNode( ExcludedBodies, typeof( TagSet ) );
		jso["IncludedBodies"] = Json.ToNode( IncludedBodies, typeof( TagSet ) );

		return jso;
	}

	/// <summary>
	/// Data loaded from a Scene file
	/// </summary>
	internal void Deserialize( JsonObject jso )
	{
		if ( jso is null )
			return;

		IsEnabled = (bool)(jso["Enabled"] ?? IsEnabled);
		IncludeStaticBodies = (bool)(jso["IncludeStaticBodies"] ?? IncludeStaticBodies);
		IncludeKeyframedBodies = (bool)(jso["IncludeKeyframedBodies"] ?? IncludeKeyframedBodies);
		EditorAutoUpdate = (bool)(jso["EditorAutoUpdate"] ?? EditorAutoUpdate);
		AgentHeight = (float)(jso["AgentHeight"] ?? AgentHeight);
		AgentRadius = (float)(jso["AgentRadius"] ?? AgentRadius);
		AgentStepSize = (float)(jso["AgentStepSize"] ?? AgentStepSize);
		AgentMaxSlope = (float)(jso["AgentMaxSlope"] ?? AgentMaxSlope);

		ExcludedBodies = Json.FromNode( jso["ExcludedBodies"], typeof( TagSet ) ) as TagSet ?? ExcludedBodies;
		IncludedBodies = Json.FromNode( jso["IncludedBodies"], typeof( TagSet ) ) as TagSet ?? IncludedBodies;
	}
}
