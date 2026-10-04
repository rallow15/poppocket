// Upgrade NOTE: upgraded instancing buffer 'SymphonieSlime' to new syntax.

// Made with Amplify Shader Editor v1.9.9.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Symphonie/Slime"
{
	Properties
	{
		[Gamma][Group(Common, 0)] _Color( "Color", Color ) = ( 0.3764706, 0.4862745, 0.7372549, 1 )
		[Group(Surface, 10)] _SurfaceDiffuseWeight( "Surface Diffuse Weight", Range( 0, 1 ) ) = 0.04
		[Group(Common)] _Smoothness( "Smoothness", Range( 0, 1 ) ) = 1
		[Group(Common)] _Size( "Size", Float ) = 1
		[Group(Scattering,10)] _RefractionScatter( "Refraction Scatter", Range( 0, 2 ) ) = 1
		[Group(Scattering)] _RefractETA( "Refract ETA", Range( 0, 1 ) ) = 0.85
		[Group(Scattering)][Toggle( _USESCATTERTINT_ON )] _UseScatterTint( "Use Scatter Tint", Float ) = 0
		[HDR][Group(Scattering)][ShowIf(_UseScatterTint)] _ScatterTintColor( " • Tint Color", Color ) = ( 1, 0.7698327, 0, 1 )
		[Group(Scattering)][ShowIf(_UseScatterTint)] _ScatterTintAbsortion( " •🔵🔵 Absortion", Range( 0, 1 ) ) = 0.95
		[Group(Core,30)][Toggle( _SHOWCORE_ON )] _ShowCore( "Show", Float ) = 1
		[NoScaleOffset][Group(Core)][ShowIf(_ShowCore)] _ScatterLUT( "Scatter LUT", 2D ) = "white" {}
		[Group(Core)][ShowIf(_ShowCore)] _CoreRadius( "Core Radius", Range( 0, 1 ) ) = 0.1
		[Group(Core)][ShowIf(_ShowCore)] _CoreScatterScale( "Core Scatter Scale", Float ) = 0.005
		[Group(Core)][ShowIf(_ShowCore)] _CoreScatterCurve( "Core Scatter Curve", Float ) = 3
		[HDR][Group(Core)][ShowIf(_ShowCore)] _CoreTint( "Core Tint", Color ) = ( 0.4156863, 0.4156863, 0.4156863, 1 )
		[HDR][Group(Core)][ShowIf(_ShowCore)] _CoreEmmision( "Core Emmision", Color ) = ( 0, 0, 0, 1 )
		[PerRendererData][Group(Core)][ShowIf(_ShowCore)] _CorePosition( "Core Position", Vector ) = ( 0, 0, 0, 0 )
		[Group(Fake Environment,40)][Toggle( _USEFAKEENVIRONMENT_ON )] _UseFakeEnvironment( "Enable", Float ) = 0
		[HDR][Group(Fake Environment,40)][ShowIf(_UseFakeEnvironment)] _DomeTop( "Dome Top", Color ) = ( 0.06666667, 0.172549, 0.427451, 1 )
		[HDR][Group(Fake Environment,40)][ShowIf(_UseFakeEnvironment)] _DomeBottom( "Dome Bottom", Color ) = ( 0.4235294, 0.572549, 0.6235294, 1 )
		[HDR][Group(Fake Environment,40)][ShowIf(_UseFakeEnvironment)] _Ground( "Ground", Color ) = ( 0.1764706, 0.1686275, 0.1411765, 1 )
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#include "UnityCG.cginc"
		#include "UnityShaderVariables.cginc"
		#pragma target 3.5
		#pragma multi_compile_instancing
		#pragma shader_feature_local _USEFAKEENVIRONMENT_ON
		#pragma shader_feature_local _SHOWCORE_ON
		#pragma shader_feature_local _USESCATTERTINT_ON
		#define ASE_VERSION 19901
		#pragma surface surf StandardCustomLighting keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldRefl;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform float4 _DomeTop;
		uniform float4 _DomeBottom;
		uniform float _Smoothness;
		uniform float4 _Ground;
		uniform sampler2D _ScatterLUT;
		uniform float _RefractETA;
		uniform float _CoreRadius;
		uniform float _CoreScatterCurve;
		uniform float _CoreScatterScale;
		uniform float _Size;
		uniform float4 _CoreEmmision;
		uniform float4 _Color;
		uniform float _RefractionScatter;
		uniform float4 _ScatterTintColor;
		uniform float _ScatterTintAbsortion;
		uniform float4 _CoreTint;
		uniform float _SurfaceDiffuseWeight;

		UNITY_INSTANCING_BUFFER_START(SymphonieSlime)
			UNITY_DEFINE_INSTANCED_PROP(float3, _CorePosition)
#define _CorePosition_arr SymphonieSlime
		UNITY_INSTANCING_BUFFER_END(SymphonieSlime)

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			#ifdef UNITY_PASS_FORWARDBASE
			float ase_lightAtten = data.atten;
			if( _LightColor0.a == 0)
			ase_lightAtten = 0;
			#else
			float3 ase_lightAttenRGB = gi.light.color / ( ( _LightColor0.rgb ) + 0.000001 );
			float ase_lightAtten = max( max( ase_lightAttenRGB.r, ase_lightAttenRGB.g ), ase_lightAttenRGB.b );
			#endif
			#if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
			half bakedAtten = UnitySampleBakedOcclusion(data.lightmapUV.xy, data.worldPos);
			float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
			float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
			ase_lightAtten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
			#endif
			SurfaceOutputStandard s114 = (SurfaceOutputStandard ) 0;
			float3 temp_cast_0 = (0.0).xxx;
			s114.Albedo = temp_cast_0;
			float3 ase_normalWS = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_normalWSNorm = normalize( ase_normalWS );
			s114.Normal = ase_normalWSNorm;
			s114.Emission = float3( 0,0,0 );
			s114.Metallic = 0.0;
			float Smooth_Property32 = _Smoothness;
			s114.Smoothness = Smooth_Property32;
			#ifdef _USEFAKEENVIRONMENT_ON
				float staticSwitch155 = 0.0;
			#else
				float staticSwitch155 = 1.0;
			#endif
			s114.Occlusion = staticSwitch155;

			data.light = gi.light;

			UnityGI gi114 = gi;
			#ifdef UNITY_PASS_FORWARDBASE
			Unity_GlossyEnvironmentData g114 = UnityGlossyEnvironmentSetup( s114.Smoothness, data.worldViewDir, s114.Normal, float3(0,0,0));
			gi114 = UnityGlobalIllumination( data, s114.Occlusion, s114.Normal, g114 );
			#endif

			float3 surfResult114 = LightingStandard ( s114, viewDir, gi114 ).rgb;
			surfResult114 += s114.Emission;

			#ifdef UNITY_PASS_FORWARDADD//114
			surfResult114 -= s114.Emission;
			#endif//114
			float3 ase_positionWS = i.worldPos;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_lightDirWS = 0;
			#else //aseld
			float3 ase_lightDirWS = normalize( UnityWorldSpaceLightDir( ase_positionWS ) );
			#endif //aseld
			float dotResult5_g56 = dot( ase_normalWS , ase_lightDirWS );
			float temp_output_9_0 = (dotResult5_g56*0.5 + 0.5);
			#if defined(LIGHTMAP_ON) && ( UNITY_VERSION < 560 || ( defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) ) )//aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			UnityGI gi73 = gi;
			float3 diffNorm73 = ase_normalWSNorm;
			gi73 = UnityGI_Base( data, 1, diffNorm73 );
			float3 indirectDiffuse73 = gi73.indirect.diffuse + diffNorm73 * 0.0001;
			float3 Color_Property31 = (_Color).rgb;
			float3 Surface_Color69 = ( ( ( min( ( temp_output_9_0 * temp_output_9_0 ) , ase_lightAtten ) * ase_lightColor.rgb ) + indirectDiffuse73 ) * Color_Property31 );
			float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_positionWS );
			float3 ase_viewDirWS = normalize( ase_viewVectorWS );
			float3 normalizeResult10_g55 = normalize( -refract( -ase_viewDirWS , ase_normalWS , _RefractETA ) );
			float3 normalizeResult11_g55 = normalize( ase_normalWS );
			float dotResult8_g55 = dot( normalizeResult10_g55 , normalizeResult11_g55 );
			float temp_output_9_0_g55 = ( dotResult8_g55 * _Size );
			float Est_Refraction_Depth78 = temp_output_9_0_g55;
			float3 temp_cast_1 = (Est_Refraction_Depth78).xxx;
			float3 normalizeResult10_g19 = normalize( ase_viewDirWS );
			float3 normalizeResult11_g19 = normalize( ase_normalWS );
			float dotResult8_g19 = dot( normalizeResult10_g19 , normalizeResult11_g19 );
			float temp_output_9_0_g19 = ( dotResult8_g19 * _Size );
			float Est_Depth79 = temp_output_9_0_g19;
			float temp_output_119_0 = ( 1.0 - saturate( ( Est_Depth79 * _RefractionScatter ) ) );
			float Refract_ETA_Property50 = _RefractETA;
			float3 temp_output_6_0_g61 = ase_normalWS;
			float dotResult1_g61 = dot( refract( ase_viewDirWS , -ase_normalWS , Refract_ETA_Property50 ) , temp_output_6_0_g61 );
			float dotResult2_g61 = dot( temp_output_6_0_g61 , temp_output_6_0_g61 );
			float3 temp_output_245_0 = ( ( dotResult1_g61 / dotResult2_g61 ) * temp_output_6_0_g61 );
			Unity_GlossyEnvironmentData g231 = UnityGlossyEnvironmentSetup( temp_output_119_0, ( ( refract( ase_viewDirWS , -ase_normalWS , Refract_ETA_Property50 ) - temp_output_245_0 ) + -temp_output_245_0 ), ase_normalWSNorm, float3(0,0,0));
			float3 indirectSpecular231 = UnityGI_IndirectSpecular( data, 1.0, ase_normalWSNorm, g231 );
			float4 DomeTopColor159 = _DomeTop;
			float4 DomeBottomColor160 = _DomeBottom;
			float3 ase_reflectionWS = normalize( WorldReflectionVector( i, float3( 0, 0, 1 ) ) );
			float temp_output_4_0_g54 =  (0.0 + ( acos( -ase_reflectionWS.y ) - 0.0 ) * ( 1.0 - 0.0 ) / ( 3.141593 - 0.0 ) );
			float lerpResult5_g54 = lerp( 1.0 , 0.5 , sqrt( temp_output_119_0 ));
			float lerpResult6_g54 = lerp( 0.2 , 0.0 , sqrt( temp_output_119_0 ));
			float temp_output_8_0_g54 = saturate(  (1.0 + ( temp_output_4_0_g54 - lerpResult5_g54 ) * ( lerpResult6_g54 - 1.0 ) / ( 0.0 - lerpResult5_g54 ) ) );
			float3 lerpResult16_g54 = lerp( (DomeTopColor159).rgb , (DomeBottomColor160).rgb , ( temp_output_8_0_g54 * temp_output_8_0_g54 ));
			float4 GroundColor161 = _Ground;
			float lerpResult9_g54 = lerp( 0.0 , 0.499 , sqrt( temp_output_119_0 ));
			float lerpResult10_g54 = lerp( 1.0 , 0.501 , sqrt( temp_output_119_0 ));
			float smoothstepResult37_g54 = smoothstep( 0.0 , 1.0 , saturate(  (0.0 + ( temp_output_4_0_g54 - lerpResult9_g54 ) * ( 1.0 - 0.0 ) / ( lerpResult10_g54 - lerpResult9_g54 ) ) ));
			float3 lerpResult17_g54 = lerp( lerpResult16_g54 , (GroundColor161).rgb , smoothstepResult37_g54);
			float3 FakeRefraction149 = (lerpResult17_g54).xyz;
			#ifdef _USEFAKEENVIRONMENT_ON
				float3 staticSwitch148 = FakeRefraction149;
			#else
				float3 staticSwitch148 = indirectSpecular231;
			#endif
			UnityGI gi14 = gi;
			float3 diffNorm14 = -ase_normalWS;
			gi14 = UnityGI_Base( data, 1, diffNorm14 );
			float3 indirectDiffuse14 = gi14.indirect.diffuse + diffNorm14 * 0.0001;
			float fresnelNdotV19 = dot( ase_normalWS, ase_viewDirWS );
			float fresnelNode19 = ( ( 1.0 - Smooth_Property32 ) + 1.0 * pow( 1.0 - fresnelNdotV19, 5.0 ) );
			float3 lerpResult13 = lerp( staticSwitch148 , indirectDiffuse14 , saturate( fresnelNode19 ));
			float3 temp_output_1_0_g57 = lerpResult13;
			float3 lerpResult8_g57 = lerp( temp_output_1_0_g57 , ( temp_output_1_0_g57 * _ScatterTintColor.rgb ) , pow( _ScatterTintAbsortion , Est_Refraction_Depth78 ));
			#ifdef _USESCATTERTINT_ON
				float3 staticSwitch205 = lerpResult8_g57;
			#else
				float3 staticSwitch205 = lerpResult13;
			#endif
			float3 Refraction_Color29 = ( pow( Color_Property31 , temp_cast_1 ) * staticSwitch205 );
			float3 _CorePosition_Instance = UNITY_ACCESS_INSTANCED_PROP(_CorePosition_arr, _CorePosition);
			float3 temp_output_20_0_g58 = _CorePosition_Instance;
			float dotResult26_g58 = dot( -refract( ase_viewDirWS , -ase_normalWS , Refract_ETA_Property50 ) , ( temp_output_20_0_g58 - ase_positionWS ) );
			float temp_output_2_0_g58 = _CoreRadius;
			float Size_Property49 = _Size;
			float2 appendResult34_g58 = (float2(( 1.0 - ( 4.0 / ( ( distance( ( ( -refract( ase_viewDirWS , -ase_normalWS , Refract_ETA_Property50 ) * dotResult26_g58 ) + ase_positionWS ) , temp_output_20_0_g58 ) / temp_output_2_0_g58 ) + 4.0 ) ) ) , ( 4.0 / ( ( pow( ( distance( temp_output_20_0_g58 , ase_positionWS ) / temp_output_2_0_g58 ) , _CoreScatterCurve ) * ( _CoreScatterScale * Size_Property49 ) ) + 4.0 ) )));
			float3 temp_output_80_0 = (tex2D( _ScatterLUT, appendResult34_g58 )).rgb;
			float3 lerpResult82 = lerp( float3( 1,1,1 ) , (_CoreTint).rgb , ( _CoreTint.a * temp_output_80_0 ));
			#ifdef _SHOWCORE_ON
				float3 staticSwitch53 = ( Refraction_Color29 * lerpResult82 );
			#else
				float3 staticSwitch53 = Refraction_Color29;
			#endif
			float fresnelNdotV60 = dot( ase_normalWS, ase_viewDirWS );
			float fresnelNode60 = ( _SurfaceDiffuseWeight + 1.0 * pow( 1.0 - fresnelNdotV60, 5.0 ) );
			float3 lerpResult58 = lerp( Surface_Color69 , staticSwitch53 , ( 1.0 - saturate( fresnelNode60 ) ));
			c.rgb = ( surfResult114 + lerpResult58 );
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			o.Normal = float3(0,0,1);
			float3 ase_positionWS = i.worldPos;
			float3 ase_viewVectorWS = ( _WorldSpaceCameraPos.xyz - ase_positionWS );
			float3 ase_viewDirWS = normalize( ase_viewVectorWS );
			float3 ase_normalWS = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float fresnelNdotV164 = dot( ase_normalWS, ase_viewDirWS );
			float fresnelNode164 = ( 0.04 + 1.0 * pow( 1.0 - fresnelNdotV164, 5.0 ) );
			float4 DomeTopColor159 = _DomeTop;
			float4 DomeBottomColor160 = _DomeBottom;
			float3 ase_reflectionWS = normalize( WorldReflectionVector( i, float3( 0, 0, 1 ) ) );
			float temp_output_4_0_g59 =  (0.0 + ( acos( ase_reflectionWS.y ) - 0.0 ) * ( 1.0 - 0.0 ) / ( 3.141593 - 0.0 ) );
			float Smooth_Property32 = _Smoothness;
			float lerpResult5_g59 = lerp( 1.0 , 0.5 , sqrt( Smooth_Property32 ));
			float lerpResult6_g59 = lerp( 0.2 , 0.0 , sqrt( Smooth_Property32 ));
			float temp_output_8_0_g59 = saturate(  (1.0 + ( temp_output_4_0_g59 - lerpResult5_g59 ) * ( lerpResult6_g59 - 1.0 ) / ( 0.0 - lerpResult5_g59 ) ) );
			float3 lerpResult16_g59 = lerp( (DomeTopColor159).rgb , (DomeBottomColor160).rgb , ( temp_output_8_0_g59 * temp_output_8_0_g59 ));
			float4 GroundColor161 = _Ground;
			float lerpResult9_g59 = lerp( 0.0 , 0.499 , sqrt( Smooth_Property32 ));
			float lerpResult10_g59 = lerp( 1.0 , 0.501 , sqrt( Smooth_Property32 ));
			float smoothstepResult37_g59 = smoothstep( 0.0 , 1.0 , saturate(  (0.0 + ( temp_output_4_0_g59 - lerpResult9_g59 ) * ( 1.0 - 0.0 ) / ( lerpResult10_g59 - lerpResult9_g59 ) ) ));
			float3 lerpResult17_g59 = lerp( lerpResult16_g59 , (GroundColor161).rgb , smoothstepResult37_g59);
			#ifdef _USEFAKEENVIRONMENT_ON
				float3 staticSwitch158 = ( fresnelNode164 * (lerpResult17_g59).xyz );
			#else
				float3 staticSwitch158 = float3( 0,0,0 );
			#endif
			float Refract_ETA_Property50 = _RefractETA;
			float3 _CorePosition_Instance = UNITY_ACCESS_INSTANCED_PROP(_CorePosition_arr, _CorePosition);
			float3 temp_output_20_0_g58 = _CorePosition_Instance;
			float dotResult26_g58 = dot( -refract( ase_viewDirWS , -ase_normalWS , Refract_ETA_Property50 ) , ( temp_output_20_0_g58 - ase_positionWS ) );
			float temp_output_2_0_g58 = _CoreRadius;
			float Size_Property49 = _Size;
			float2 appendResult34_g58 = (float2(( 1.0 - ( 4.0 / ( ( distance( ( ( -refract( ase_viewDirWS , -ase_normalWS , Refract_ETA_Property50 ) * dotResult26_g58 ) + ase_positionWS ) , temp_output_20_0_g58 ) / temp_output_2_0_g58 ) + 4.0 ) ) ) , ( 4.0 / ( ( pow( ( distance( temp_output_20_0_g58 , ase_positionWS ) / temp_output_2_0_g58 ) , _CoreScatterCurve ) * ( _CoreScatterScale * Size_Property49 ) ) + 4.0 ) )));
			float3 temp_output_80_0 = (tex2D( _ScatterLUT, appendResult34_g58 )).rgb;
			#ifdef _SHOWCORE_ON
				float3 staticSwitch225 = ( temp_output_80_0 * (_CoreEmmision).rgb * _CoreEmmision.a );
			#else
				float3 staticSwitch225 = float3( 0,0,0 );
			#endif
			float3 CoreEmission223 = staticSwitch225;
			o.Emission = ( staticSwitch158 + CoreEmission223 );
		}

		ENDCG
	}
	Fallback "Standard"
	CustomEditor "Symphonie.StoreAssets.Editor.SlimeMaterialGUI"
}
/*ASEBEGIN
Version=19901
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;3;-3392,-1632;Inherit;False;1430.473;657.6536;Estimated Depth;13;79;78;77;76;66;50;49;48;34;12;7;6;5;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;48;-3008,-1184;Inherit;False;Property;_Size;Size;3;0;Create;True;0;0;0;True;1;Group(Common);False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;66;-3312,-1376;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;76;-2544,-1216;Inherit;False;EstimateSphereDepth;-1;;19;37c209bc3af11864b82b72de1fbd4c27;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;7;FLOAT;1;False;2;FLOAT;0;FLOAT3;13
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;34;-3328,-1216;Inherit;False;Property;_RefractETA;Refract ETA;5;0;Create;True;0;0;0;True;1;Group(Scattering);False;0.85;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;4;-4048,-96;Inherit;False;2482.648;1451.549;Refraction Color;37;194;29;26;24;13;25;148;51;14;23;19;150;17;20;111;64;18;119;21;15;33;22;205;209;214;216;217;231;244;247;245;248;249;251;254;255;256;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;79;-2224,-1216;Inherit;False;Est Depth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;50;-3248,-1104;Inherit;False;Refract ETA Property;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;111;-3616,800;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;22;-3664,512;Inherit;False;79;Est Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;33;-3760,592;Inherit;False;Property;_RefractionScatter;Refraction Scatter;4;0;Create;True;0;0;0;True;1;Group(Scattering,10);False;1;0.606;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;255;-3792,832;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;246;-4016,672;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;256;-3984,912;Inherit;False;50;Refract ETA Property;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;147;-3312,1408;Inherit;False;1450.622;864.0862;Fake Refraction;9;149;153;137;135;134;145;159;160;161;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;-3440,544;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RefractOpVec, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;254;-3808,704;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;30;-778.1412,-1821.788;Inherit;False;Property;_Smoothness;Smoothness;2;0;Create;True;1;123;0;0;True;1;Group(Common);False;1;0.903;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;21;-3248,544;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldReflectionVector, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;145;-3296,2080;Inherit;False;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;134;-3248,1504;Inherit;False;Property;_DomeTop;Dome Top;18;1;[HDR];Create;True;0;0;0;True;2;Group(Fake Environment,40);ShowIf(_UseFakeEnvironment);False;0.06666667,0.172549,0.427451,1;0.1091285,0.1650918,0.305,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;135;-3248,1680;Inherit;False;Property;_DomeBottom;Dome Bottom;19;1;[HDR];Create;True;0;0;0;True;2;Group(Fake Environment,40);ShowIf(_UseFakeEnvironment);False;0.4235294,0.572549,0.6235294,1;0.5393208,0.7290818,0.794,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;137;-3248,1856;Inherit;False;Property;_Ground;Ground;20;1;[HDR];Create;True;0;0;0;True;2;Group(Fake Environment,40);ShowIf(_UseFakeEnvironment);False;0.1764706,0.1686275,0.1411765,1;0.1764706,0.1686275,0.1411765,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RelayNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;251;-3568,672;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;5;-3344,-1520;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;32;-256.0296,-1822.965;Inherit;False;Smooth Property;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;159;-2928,1600;Inherit;False;DomeTopColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;160;-2928,1712;Inherit;False;DomeBottomColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;161;-2944,1824;Inherit;False;GroundColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;119;-3088,544;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;-2992,2048;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NegateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;6;-3120,-1568;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;245;-3424,736;Inherit;False;Projection;-1;;61;3249e2c8638c9ef4bbd1902a2d38a67c;0;2;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;191;-2592,1648;Inherit;False;GetFakeReflection;-1;;54;4b1ad5a444c4cdd45ae4a2e53cb3a616;0;5;23;COLOR;0,0,0,0;False;24;COLOR;0,0,0,0;False;25;COLOR;0,0,0,0;False;27;FLOAT;0.9;False;28;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RefractOpVec, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;7;-2912,-1504;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NegateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;248;-3248,800;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;247;-3232,688;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;18;-3648,1152;Inherit;False;32;Smooth Property;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;149;-2176,1904;Inherit;False;FakeRefraction;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;64;-3584,976;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;20;-3280,1136;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;12;-2736,-1504;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;249;-3056,720;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;27;-777.0802,-2021.724;Inherit;False;Property;_Color;Color;0;1;[Gamma];Create;True;0;0;0;True;1;Group(Common, 0);False;0.3764706,0.4862745,0.7372549,1;0.8773585,0.1852117,0.103462,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.NegateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;17;-3280,976;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;150;-2832,560;Inherit;False;149;FakeRefraction;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FresnelNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;19;-3040,1088;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0.04;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;-2576,-1424;Inherit;False;EstimateSphereDepth;-1;;55;37c209bc3af11864b82b72de1fbd4c27;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;7;FLOAT;1;False;2;FLOAT;0;FLOAT3;13
Node;AmplifyShaderEditor.IndirectSpecularLightEx, Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;231;-2848,640;Inherit;False;World;4;0;FLOAT3;0,0,1;False;1;FLOAT;0.5;False;2;FLOAT;1;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;28;-509.9163,-2015.678;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;1;-1440,256;Inherit;False;1535.506;1082.93;Core Color;22;82;81;80;67;47;46;45;44;43;42;41;40;39;38;37;36;35;218;220;221;223;225;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;2;-3360,-928;Inherit;False;1371.11;633.5644;Surface Color;11;75;74;73;72;71;70;69;11;10;9;8;;1,1,1,1;0;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;-3008,976;Inherit;False;World;1;0;FLOAT3;0,0,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;51;-2720,1104;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;78;-2240,-1424;Inherit;False;Est Refraction Depth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;148;-2496,592;Inherit;False;Property;_UseFakeEnvironment;Enable;17;0;Create;False;0;0;0;True;1;Group(Fake Environment,40);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;31;-249.4056,-2011.014;Inherit;False;Color Property;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;49;-2800,-1088;Inherit;False;Size Property;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;67;-1392,480;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;-3296,-880;Inherit;False;Half Lambert Term;-1;;56;86299dc21373a954aa5772333626c9c1;0;1;3;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;-2176,640;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;214;-2288,1008;Inherit;False;78;Est Refraction Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;209;-2272,816;Inherit;False;Property;_ScatterTintColor; • Tint Color;7;1;[HDR];Create;False;0;0;0;False;2;Group(Scattering);ShowIf(_UseScatterTint);False;1,0.7698327,0,1;4.541205,4.018135,0,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;216;-2272,1088;Inherit;False;Property;_ScatterTintAbsortion; •🔵🔵 Absortion;8;0;Create;False;0;0;0;False;2;Group(Scattering);ShowIf(_UseScatterTint);False;0.95;0.073;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;23;-3696,304;Inherit;False;31;Color Property;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NegateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;43;-1136,496;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;44;-1216,576;Inherit;False;50;Refract ETA Property;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;45;-1184,304;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;8;-3024,-864;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightAttenuation, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;70;-3296,-800;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;25;-3664,400;Inherit;False;78;Est Refraction Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;40;-1184,944;Inherit;False;49;Size Property;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;41;-1184,880;Inherit;False;Property;_CoreScatterScale;Core Scatter Scale;12;0;Create;True;0;0;0;False;2;Group(Core);ShowIf(_ShowCore);False;0.005;0.015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;217;-1920,640;Inherit;False;SlimeScatterTint;-1;;57;1183181536166de488844b86ef560327;0;4;1;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT;0;False;9;FLOAT;0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;42;-960,896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RefractOpVec, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;46;-944,448;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LightColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;-3200,-720;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMinOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;-2848,-832;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;24;-2816,416;Inherit;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;205;-1888,528;Inherit;False;Property;_UseScatterTint;Use Scatter Tint;6;0;Create;False;0;0;0;True;1;Group(Scattering);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;37;-1152,1120;Inherit;True;Property;_ScatterLUT;Scatter LUT;10;1;[NoScaleOffset];Create;True;0;0;0;True;2;Group(Core);ShowIf(_ShowCore);False;fa96176ad89090848ae0c7dd49b93399;fa96176ad89090848ae0c7dd49b93399;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;36;-1152,1040;Inherit;False;Property;_CoreScatterCurve;Core Scatter Curve;13;0;Create;True;0;0;0;False;2;Group(Core);ShowIf(_ShowCore);False;3;3.18;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;38;-1264,800;Inherit;False;Property;_CoreRadius;Core Radius;11;0;Create;True;0;0;0;True;2;Group(Core);ShowIf(_ShowCore);False;0.1;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;39;-1184,656;Inherit;False;InstancedProperty;_CorePosition;Core Position;16;1;[PerRendererData];Create;True;0;0;0;True;2;Group(Core);ShowIf(_ShowCore);False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.IndirectDiffuseLighting, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;73;-3232,-608;Inherit;False;Tangent;1;0;FLOAT3;0,0,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;74;-2816,-720;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;80;-672,656;Inherit;False;CalcCoreScatter;-1;;58;772c1a60e2e34aa43bbb471083420c73;0;7;46;FLOAT3;0,0,0;False;20;FLOAT3;0,0,0;False;2;FLOAT;0;False;41;FLOAT;1;False;43;FLOAT;1;False;10;SAMPLER2D;0;False;12;SAMPLERSTATE;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;26;-1792,384;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;47;-656,464;Inherit;False;Property;_CoreTint;Core Tint;14;1;[HDR];Create;True;0;0;0;True;2;Group(Core);ShowIf(_ShowCore);False;0.4156863,0.4156863,0.4156863,1;0,0,0,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;218;-384,848;Inherit;False;Property;_CoreEmmision;Core Emmision;15;1;[HDR];Create;True;0;0;0;True;2;Group(Core);ShowIf(_ShowCore);False;0,0,0,1;1,0.118228,0,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;35;-336,608;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;72;-3168,-512;Inherit;False;31;Color Property;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;75;-2672,-608;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;61;-420.6252,-191.0792;Inherit;False;Property;_SurfaceDiffuseWeight;Surface Diffuse Weight;1;0;Create;True;0;0;0;False;1;Group(Surface, 10);False;0.04;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;29;-1824,288;Inherit;False;Refraction Color;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;172;400,-1440;Inherit;False;820;571;Fake Reflection;8;167;169;168;166;171;164;170;192;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;81;-400,512;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;221;-368,768;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;82;-144,480;Inherit;False;3;0;FLOAT3;1,1,1;False;1;FLOAT3;1,1,1;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;71;-2560,-544;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FresnelNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;60;-94.80231,-239.1622;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0.04;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;54;-281.6602,47.99407;Inherit;False;29;Refraction Color;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;448,-1248;Inherit;False;160;DomeBottomColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;169;480,-1184;Inherit;False;161;GroundColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;168;448,-1312;Inherit;False;159;DomeTopColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;166;448,-1120;Inherit;False;32;Smooth Property;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldReflectionVector, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;171;448,-1056;Inherit;False;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;220;-144,704;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;55;-34.36243,137.8188;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;69;-2352,-656;Inherit;False;Surface Color;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;63;146.7244,-232.7331;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;-48,-640;Inherit;False;Constant;_Float1;Float 0;0;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;156;-48,-560;Inherit;False;Constant;_Float2;Float 0;0;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;816,-1392;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0.04;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;192;720,-1168;Inherit;False;GetFakeReflection;-1;;59;4b1ad5a444c4cdd45ae4a2e53cb3a616;0;5;23;COLOR;0,0,0,0;False;24;COLOR;0,0,0,0;False;25;COLOR;0,0,0,0;False;27;FLOAT;0.9;False;28;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;225;-144,832;Inherit;False;Property;_ShowCore1;Enable;9;0;Create;False;0;0;0;True;1;Group(Core,30);False;0;1;1;True;;Toggle;2;Key0;Key1;Reference;53;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;62;308.2597,-220.3999;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;68;128.9266,-343.8887;Inherit;False;69;Surface Color;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;59;192,-800;Inherit;False;Constant;_Float0;Float 0;0;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;155;128,-608;Inherit;False;Property;_UseFakeReflection1;Use Fake Reflection;17;0;Create;True;0;0;0;True;1;Group(Fake Reflection,40);False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;148;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;115;144,-704;Inherit;False;32;Smooth Property;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;170;1040,-1296;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;223;-128,944;Inherit;False;CoreEmission;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;53;160.562,24.27606;Inherit;False;Property;_ShowCore;Show;9;0;Create;False;0;0;0;True;1;Group(Core,30);False;0;1;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;58;542.5072,-282.8906;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomStandardSurface, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;114;448,-784;Inherit;False;Metallic;Tangent;6;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;158;1264,-720;Inherit;False;Property;_UseFakeReflection2;Use Fake Reflection;17;0;Create;True;0;0;0;True;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;148;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;121;1312,-608;Inherit;False;223;CoreEmission;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;194;-2432,352;Inherit;False;Debug1;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;117;800,-496;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;125;864,-752;Inherit;False;Tangent;1;0;FLOAT3;0,0,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;222;1584,-624;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;193;1424,-224;Inherit;False;194;Debug1;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.IndirectSpecularLight, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;244;-2848,800;Inherit;False;World;3;0;FLOAT3;0,0,1;False;1;FLOAT;0.5;False;2;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;0;1792,-608;Float;False;True;-1;3;Symphonie.StoreAssets.Editor.SlimeMaterialGUI;0;0;CustomLighting;Symphonie/Slime;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;Standard;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;76;5;66;0
WireConnection;76;7;48;0
WireConnection;79;0;76;0
WireConnection;50;0;34;0
WireConnection;255;0;111;0
WireConnection;15;0;22;0
WireConnection;15;1;33;0
WireConnection;254;0;246;0
WireConnection;254;1;255;0
WireConnection;254;2;256;0
WireConnection;21;0;15;0
WireConnection;251;0;254;0
WireConnection;32;0;30;0
WireConnection;159;0;134;0
WireConnection;160;0;135;0
WireConnection;161;0;137;0
WireConnection;119;0;21;0
WireConnection;153;0;145;0
WireConnection;6;0;5;0
WireConnection;245;5;251;0
WireConnection;245;6;111;0
WireConnection;191;23;159;0
WireConnection;191;24;160;0
WireConnection;191;25;161;0
WireConnection;191;27;119;0
WireConnection;191;28;153;0
WireConnection;7;0;6;0
WireConnection;7;1;66;0
WireConnection;7;2;34;0
WireConnection;248;0;245;0
WireConnection;247;0;251;0
WireConnection;247;1;245;0
WireConnection;149;0;191;0
WireConnection;20;0;18;0
WireConnection;12;0;7;0
WireConnection;249;0;247;0
WireConnection;249;1;248;0
WireConnection;17;0;64;0
WireConnection;19;1;20;0
WireConnection;77;1;12;0
WireConnection;77;5;66;0
WireConnection;77;7;48;0
WireConnection;231;1;119;0
WireConnection;231;3;249;0
WireConnection;28;0;27;0
WireConnection;14;0;17;0
WireConnection;51;0;19;0
WireConnection;78;0;77;0
WireConnection;148;1;231;0
WireConnection;148;0;150;0
WireConnection;31;0;28;0
WireConnection;49;0;48;0
WireConnection;13;0;148;0
WireConnection;13;1;14;0
WireConnection;13;2;51;0
WireConnection;43;0;67;0
WireConnection;8;0;9;0
WireConnection;8;1;9;0
WireConnection;217;1;13;0
WireConnection;217;5;209;0
WireConnection;217;6;214;0
WireConnection;217;9;216;0
WireConnection;42;0;41;0
WireConnection;42;1;40;0
WireConnection;46;0;45;0
WireConnection;46;1;43;0
WireConnection;46;2;44;0
WireConnection;11;0;8;0
WireConnection;11;1;70;0
WireConnection;24;0;23;0
WireConnection;24;1;25;0
WireConnection;205;1;13;0
WireConnection;205;0;217;0
WireConnection;74;0;11;0
WireConnection;74;1;10;1
WireConnection;80;46;46;0
WireConnection;80;20;39;0
WireConnection;80;2;38;0
WireConnection;80;41;42;0
WireConnection;80;43;36;0
WireConnection;80;10;37;0
WireConnection;80;12;37;1
WireConnection;26;0;24;0
WireConnection;26;1;205;0
WireConnection;35;0;47;4
WireConnection;35;1;80;0
WireConnection;75;0;74;0
WireConnection;75;1;73;0
WireConnection;29;0;26;0
WireConnection;81;0;47;0
WireConnection;221;0;218;0
WireConnection;82;1;81;0
WireConnection;82;2;35;0
WireConnection;71;0;75;0
WireConnection;71;1;72;0
WireConnection;60;1;61;0
WireConnection;220;0;80;0
WireConnection;220;1;221;0
WireConnection;220;2;218;4
WireConnection;55;0;54;0
WireConnection;55;1;82;0
WireConnection;69;0;71;0
WireConnection;63;0;60;0
WireConnection;192;23;168;0
WireConnection;192;24;167;0
WireConnection;192;25;169;0
WireConnection;192;27;166;0
WireConnection;192;28;171;0
WireConnection;225;0;220;0
WireConnection;62;0;63;0
WireConnection;155;1;116;0
WireConnection;155;0;156;0
WireConnection;170;0;164;0
WireConnection;170;1;192;0
WireConnection;223;0;225;0
WireConnection;53;1;54;0
WireConnection;53;0;55;0
WireConnection;58;0;68;0
WireConnection;58;1;53;0
WireConnection;58;2;62;0
WireConnection;114;0;59;0
WireConnection;114;3;59;0
WireConnection;114;4;115;0
WireConnection;114;5;155;0
WireConnection;158;0;170;0
WireConnection;194;0;231;0
WireConnection;117;0;114;0
WireConnection;117;1;58;0
WireConnection;222;0;158;0
WireConnection;222;1;121;0
WireConnection;244;1;119;0
WireConnection;0;2;222;0
WireConnection;0;13;117;0
ASEEND*/
//CHKSM=9779001BF03D73E5CC586418DA9D85BBF32389C1