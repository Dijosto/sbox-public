using System.Text.Json.Serialization;

namespace Sandbox.SolutionGenerator;

public class VSCodeExtensions
{
	[JsonPropertyName( "recommendations" )]
	public string[] Recommendations { get; set; } = [];
}
