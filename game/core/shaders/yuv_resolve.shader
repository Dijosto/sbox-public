HEADER
{
	DevShader = true;
	Description = "YUV Resolve";
}

MODES
{
	Default();
}

FEATURES
{
}

COMMON
{
	#include "system.fxc"
}

struct VS_INPUT
{
	float2 vPositionPs : POSITION < Semantic( PosXyz ); >;
};

struct PS_INPUT
{
	float2 vTexCoord : TEXCOORD0;

	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs		: SV_Position;
	#endif
};

VS
{
	PS_INPUT MainVs( const VS_INPUT i )
	{
		PS_INPUT o;

		o.vPositionPs.xyzw = float4( i.vPositionPs.xy, 0.0, 1.0 );
		o.vTexCoord = (i.vPositionPs.xy + 1.0) * 0.5;
		o.vTexCoord.y = 1.0f - o.vTexCoord.y;

		return o;
	}
}

PS
{
	struct PS_OUTPUT
	{
		float4 vColor : SV_Target0;
	};

	SamplerState g_sSampler0 : register(s0);
	Texture2D g_tTextureY : register(t0) < Attribute( "TextureY" ); >;
	Texture2D g_tTextureU : register(t1) < Attribute( "TextureU" ); >;
	Texture2D g_tTextureV : register(t2) < Attribute( "TextureV" ); >;

	PS_OUTPUT MainPs( PS_INPUT i )
	{
		PS_OUTPUT o;

		float Y = g_tTextureY.Sample( g_sSampler0, i.vTexCoord ).r;
		float U = g_tTextureU.Sample( g_sSampler0, i.vTexCoord ).r;
		float V = g_tTextureV.Sample( g_sSampler0, i.vTexCoord ).r;

		float R = Y + 1.402 * (V - 0.5);
		float G = Y - 0.344 * (U - 0.5) - 0.714 * (V - 0.5);
		float B = Y + 1.772 * (U - 0.5);

		o.vColor = float4( R, G, B, 1.0 );

		return o;
	}
}