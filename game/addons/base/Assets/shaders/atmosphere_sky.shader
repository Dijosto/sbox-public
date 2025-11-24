HEADER
{
	DevShader = true;
	Description = "Dynamic Sky Shader";
	Version = 1;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
MODES
{
	Forward();
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
FEATURES
{
	#include "vr_common_features.fxc"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
COMMON
{
	#include "system.fxc"
	#include "vr_common.fxc"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
struct VS_INPUT
{
	float4 vPositionOs : POSITION < Semantic( PosXyz ); >;
};

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
struct PS_INPUT
{
	float3 vPositionWs : TEXCOORD1;

	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs	: SV_Position;
	#endif
	#if ( PROGRAM == VFX_PROGRAM_PS )
		float4 vPositionSs : SV_Position;
	#endif
};

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
VS
{
	// Includes -----------------------------------------------------------------------------------------------------------------------------------------------
	#define IS_SPRITECARD 1
	#include "system.fxc"
	#include "vr_lighting.fxc"

	// Combos -------------------------------------------------------------------------------------------------------------------------------------------------

	// Constants ----------------------------------------------------------------------------------------------------------------------------------------------

	// Main ---------------------------------------------------------------------------------------------------------------------------------------------------
	PS_INPUT MainVs( const VS_INPUT i )
	{

		PS_INPUT o;

		o.vPositionWs = i.vPositionOs.xyz;

        float3 vPositionWs = g_vCameraPositionWs.xyz + i.vPositionOs.xyz * 3.1415f;

		o.vPositionPs.xyzw = Position3WsToPs( vPositionWs.xyz );
		o.vPositionWs.xyz = vPositionWs;
		
		return o;
	}
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
PS
{
	#include "vr_lighting.fxc"
	#include "atmosphere_sky.fxc"
	#include "volumetric_fog.fxc"
	
	// Combos -------------------------------------------------------------------------------------------------------------------------------------------------

	// Render State -------------------------------------------------------------------------------------------------------------------------------------------
	RenderState( CullMode, NONE );
	RenderState( DepthWriteEnable, false );
	RenderState( DepthEnable, true );
	RenderState( DepthFunc, GREATER_EQUAL );
	
	// Attributes ---------------------------------------------------------------------------------------------------------------------------------------------
	BoolAttribute( sky, true );

	// Output -------------------------------------------------------------------------------------------------------------------------------------------------
	struct PS_OUTPUT
	{
		float4 vColor0 : SV_Target0;
	};

	// Constants ----------------------------------------------------------------------------------------------------------------------------------------------

	// Main ---------------------------------------------------------------------------------------------------------------------------------------------------

	// Could be better
	float noise( in float3 x )
	{
		float3 p = floor(x);
		float3 f = frac(x);
		f = f*f*(3.0-2.0*f);
		float2 uv = (p.xy+float2(37.0,17.0) * p.z) + f.xy;
		float2 rg = g_tBlueNoise.Sample( g_sBilinearWrap, (uv + 0.5 )/256.0 ).xy;
		return lerp( rg.x, rg.y, f.z );
	}

    float3 GetSunDir(BinnedLight light)
	{
		return normalize( light.GetPosition() );
		//return normalize( float3( 1,0, ( fmod(g_flTime * 0.1, 0.5) - 0.25 )  ) );
	}

    float3 GetSunColor(BinnedLight light)
    {
        return light.GetColor();
	}

    float3 GetAtmosphere(float3 ray, BinnedLight light ){

		float3 fSunIntensity = GetSunColor( light );
		float fPlanetSize = 6371e3;
		float fAtmosphereSize = 100e3;
		float fSeaLevel = 512.0f;
		float3 uSunPos = GetSunDir( light );

		float3 color = atmosphere
		(
			ray.xzy,           // normalized ray direction
			float3(0,fPlanetSize + g_vCameraPositionWs.z + fSeaLevel,0),               // ray origin
			uSunPos.xzy,                        // position of the sun
			50,                           // intensity of the sun
			fPlanetSize,                         // radius of the planet in meters
			fPlanetSize + fAtmosphereSize,                         // radius of the atmosphere in meters
			float3(5.5e-6, 13.0e-6, 22.4e-6), // Rayleigh scattering coefficient
			21e-6,                          // Mie scattering coefficient
			8e3,                            // Rayleigh scale height
			1.2e3,                          // Mie scale height
			0.758                           // Mie preferred scattering direction
		);

		return color * fSunIntensity;
		
	}

	float Stars( in float3 vRay )
	{
		//vRay.z = abs(vRay.z);

		const float fStarScale = 0.3;
		const float fStarAmount = 1.0;

		float vStars = noise(vRay * ( g_vViewportSize.y * fStarScale ) * 0.75 );
		vStars += noise(vRay * ( g_vViewportSize.y * fStarScale ) * 0.5 );
		vStars += noise(vRay * ( g_vViewportSize.y * fStarScale ) * 0.25);
		vStars += noise(vRay * ( g_vViewportSize.y * fStarScale ) * 0.1 );
		vStars += noise(vRay * ( g_vViewportSize.y * fStarScale ) ) * (1.0 - fStarAmount);

		vStars = clamp(vStars, 0.0, 1.0);
		vStars = (1.0 - vStars);

		vStars *= saturate( vRay.z * 100 );

		return vStars;
	}

	float3 Sun( in float3 vRay, BinnedLight light )
	{
		float fSun = pow( saturate(dot( vRay, GetSunDir( light ) ) + 0.00025 ), 10000.0f ) * 10;

		fSun *= saturate( vRay.z * 100  );
		return GetSunColor( light ) * fSun * 5;
	}

	PS_OUTPUT MainPs( PS_INPUT i )
	{
		PS_OUTPUT o;
		// Generate Object->World matrix and animation scale
		float3 vPositionWs = i.vPositionWs.xyz;
		float3 vRay = normalize( vPositionWs - g_vCameraPositionWs );
		float3 vCamDir = g_vCameraDirWs;
        float3 vColor;

		vColor = Stars( vRay );
		
        for (uint j = 0; j < NumDynamicLights; j++)
        {
            BinnedLight light = BinnedLightBuffer[j];

            if (length( light.GetPosition() ) < 10000.0f)
                continue;

            vColor += GetAtmosphere(vRay, light);
            vColor += Sun(vRay, light);
        }

		//vColor += GetAtmosphere( vRay );
		//vColor += Sun( vRay );

		o.vColor0.rgba = float4( vColor , 1.0 );
		o.vColor0.rgb = ApplyVolumetricFog( o.vColor0.rgb, i.vPositionWs, i.vPositionSs.xy );
		return o;
	}
}
