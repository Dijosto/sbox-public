namespace Sandbox.Rendering;

public enum Stage
{
	AfterDepthPrepass = 1000,
	AfterOpaque = 2000,
	AfterSkybox = 3000,
	AfterTransparent = 4000,
	AfterViewmodel = 5000,
	BeforePostProcess = 6000,
	Tonemapping = 6500,
	AfterPostProcess = 7000,
	AfterUI = 8000,
}
