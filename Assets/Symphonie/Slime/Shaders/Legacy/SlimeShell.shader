// Made with Amplify Shader Editor v1.9.3.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Symphonie/Legacy/Slime"
{
	Properties
	{
		_Color("Color", Color) = (0.3775951,0.4910184,0.7132353,1)
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.95
		_DepthBlend("DepthBlend", Range( 0 , 5)) = 1
		_DepthTint("DepthTint", Color) = (0.5019608,0.5019608,0.5019608,1)
		_CorePosition("CorePosition", Vector) = (0,0,0,0)
		_CoreSize("CoreSize", Float) = 0.2
		_CoreSoftnessByDist("CoreSoftnessByDist", Range( 0 , 2)) = 0.1
		_CoreTint("Core Tint", Color) = (0.3689987,0.4704183,0.6691177,1)
		_CoreTransparentByDist("CoreTransparentByDist", Range( 0 , 8)) = 0
		_CoreTransparentBase("CoreTransparentBase", Range( 0.01 , 1)) = 0.99
		_TransColor("TransColor", Color) = (0.2720588,0.4578092,1,1)
		_TransPow("TransPow", Float) = 1
		_TransDistortion("TransDistortion", Float) = 1
		_TransThicknessScale("TransThicknessScale", Float) = 1
		_TransAmbient("TransAmbient", Float) = 0
		_TransScale("TransScale", Float) = 1
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" }
		Cull Back
		ZWrite On
		ZTest LEqual
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
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

		uniform float4 _Color;
		uniform float _Smoothness;
		uniform float4 _DepthTint;
		uniform float4 _CoreTint;
		uniform float3 _CorePosition;
		uniform float _CoreSize;
		uniform float _CoreSoftnessByDist;
		uniform float _CoreTransparentBase;
		uniform float _CoreTransparentByDist;
		uniform float _DepthBlend;
		uniform float4 _TransColor;
		uniform float _TransDistortion;
		uniform float _TransThicknessScale;
		uniform float _TransPow;
		uniform float _TransScale;
		uniform float _TransAmbient;


		float3 BackScatter167( float3 WorldLightDir, float3 WorldViewDir, float3 WorldNormal, float Distribution, float Thickness, float Power, float Scale, float Ambient )
		{
			half3 vEye = normalize(WorldViewDir);
			half3 vLTLight = normalize(WorldLightDir) + normalize(WorldNormal) * Distribution;
			half fLTDot = pow(saturate(dot(vEye, -vLTLight)), Power) * Scale;
			half3 fLT = (fLTDot + Ambient) * Thickness;
			return fLT;
		}


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
			SurfaceOutputStandard s20 = (SurfaceOutputStandard ) 0;
			s20.Albedo = _Color.rgb;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_normWorldNormal = normalize( ase_worldNormal );
			s20.Normal = ase_normWorldNormal;
			s20.Emission = float3( 0,0,0 );
			s20.Metallic = 0.0;
			float Smoothness28 = _Smoothness;
			s20.Smoothness = Smoothness28;
			s20.Occlusion = 1.0;

			data.light = gi.light;

			UnityGI gi20 = gi;
			#ifdef UNITY_PASS_FORWARDBASE
			Unity_GlossyEnvironmentData g20 = UnityGlossyEnvironmentSetup( s20.Smoothness, data.worldViewDir, s20.Normal, float3(0,0,0));
			gi20 = UnityGlobalIllumination( data, s20.Occlusion, s20.Normal, g20 );
			#endif

			float3 surfResult20 = LightingStandard ( s20, viewDir, gi20 ).rgb;
			surfResult20 += s20.Emission;

			#ifdef UNITY_PASS_FORWARDADD//20
			surfResult20 -= s20.Emission;
			#endif//20
			float3 ase_worldPos = i.worldPos;
			float dotResult196 = dot( ase_worldNormal , ( _WorldSpaceCameraPos - ase_worldPos ) );
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float fresnelNdotV24 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode24 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV24, 5.0 ) );
			float temp_output_26_0 = ( 1.0 - fresnelNode24 );
			UnityGI gi2 = gi;
			float3 diffNorm2 = ase_worldNormal;
			gi2 = UnityGI_Base( data, 1, diffNorm2 );
			float3 indirectDiffuse2 = gi2.indirect.diffuse + diffNorm2 * 0.0001;
			float3 worldSpaceViewDir51 = WorldSpaceViewDir( float4( 0,0,0,0 ) );
			float3 normalizeResult52 = normalize( refract( ase_worldNormal , worldSpaceViewDir51 , -0.5 ) );
			UnityGI gi67 = gi;
			float3 diffNorm67 = normalizeResult52;
			gi67 = UnityGI_Base( data, 1, diffNorm67 );
			float3 indirectDiffuse67 = gi67.indirect.diffuse + diffNorm67 * 0.0001;
			float3 lerpResult68 = lerp( indirectDiffuse2 , indirectDiffuse67 , float3( 0.5,0,0 ));
			float4 temp_output_25_0 = (  ( dotResult196 - 0.0 > 0.0 ? temp_output_26_0 : dotResult196 - 0.0 <= 0.0 && dotResult196 + 0.0 >= 0.0 ? temp_output_26_0 : 1.0 )  * float4( lerpResult68 , 0.0 ) * _DepthTint );
			float3 normalizeResult124 = normalize( ( _WorldSpaceCameraPos - ase_worldPos ) );
			float3 temp_output_108_0 = ( _WorldSpaceCameraPos - _CorePosition );
			float3 normalizeResult109 = normalize( temp_output_108_0 );
			float dotResult106 = dot( normalizeResult124 , normalizeResult109 );
			float temp_output_153_0 = distance( ase_worldPos , _CorePosition );
			float temp_output_142_0 = ( _CoreSize * pow( ( _CoreSoftnessByDist * temp_output_153_0 ) , 2.0 ) );
			float smoothstepResult156 = smoothstep( 0.0 , 1.0 , saturate( ( ( ( sin( acos( dotResult106 ) ) * length( temp_output_108_0 ) ) - ( _CoreSize - temp_output_142_0 ) ) / ( temp_output_142_0 * 2.0 ) ) ));
			float CoreMask143 = ( ( 1.0 - smoothstepResult156 ) * pow( _CoreTransparentBase , ( temp_output_153_0 * _CoreTransparentByDist ) ) );
			float4 lerpResult152 = lerp( temp_output_25_0 , ( _CoreTint * temp_output_25_0 ) , CoreMask143);
			SurfaceOutputStandard s62 = (SurfaceOutputStandard ) 0;
			s62.Albedo = float3( 0,0,0 );
			s62.Normal = ase_normWorldNormal;
			s62.Emission = float3( 0,0,0 );
			s62.Metallic = 0.0;
			s62.Smoothness = Smoothness28;
			s62.Occlusion = 1.0;

			data.light = gi.light;

			UnityGI gi62 = gi;
			#ifdef UNITY_PASS_FORWARDBASE
			Unity_GlossyEnvironmentData g62 = UnityGlossyEnvironmentSetup( s62.Smoothness, data.worldViewDir, s62.Normal, float3(0,0,0));
			gi62 = UnityGlobalIllumination( data, s62.Occlusion, s62.Normal, g62 );
			#endif

			float3 surfResult62 = LightingStandard ( s62, viewDir, gi62 ).rgb;
			surfResult62 += s62.Emission;

			#ifdef UNITY_PASS_FORWARDADD//62
			surfResult62 -= s62.Emission;
			#endif//62
			float fresnelNdotV37 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode37 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV37, _DepthBlend ) );
			float DepthBlendPow34 = fresnelNode37;
			float4 lerpResult65 = lerp( float4( surfResult20 , 0.0 ) , ( lerpResult152 + float4( surfResult62 , 0.0 ) ) , DepthBlendPow34);
			#if defined(LIGHTMAP_ON) && ( UNITY_VERSION < 560 || ( defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) ) )//aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = normalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float3 WorldLightDir167 = ase_worldlightDir;
			float3 WorldViewDir167 = ( _WorldSpaceCameraPos - ase_worldPos );
			float3 WorldNormal167 = ase_worldNormal;
			float Distribution167 = _TransDistortion;
			float Thickness167 = _TransThicknessScale;
			float Power167 = _TransPow;
			float Scale167 = _TransScale;
			float Ambient167 = _TransAmbient;
			float3 localBackScatter167 = BackScatter167( WorldLightDir167 , WorldViewDir167 , WorldNormal167 , Distribution167 , Thickness167 , Power167 , Scale167 , Ambient167 );
			float4 BackScatter166 = ( _TransColor * float4( ase_lightColor.rgb , 0.0 ) * ase_lightAtten * float4( localBackScatter167 , 0.0 ) );
			c.rgb = ( lerpResult65 + BackScatter166 ).rgb;
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
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows exclude_path:deferred 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19302
Node;AmplifyShaderEditor.CommentaryNode;146;-2478.967,-1544.644;Inherit;False;2271.41;764.843;Core Mask;31;143;144;110;141;140;136;139;142;154;133;135;153;129;124;109;128;127;125;106;108;121;105;107;118;155;156;157;158;159;160;161;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;118;-2388.134,-1494.644;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldSpaceCameraPos;107;-2428.967,-1330.586;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;105;-2428.768,-1202.286;Float;False;Property;_CorePosition;CorePosition;4;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;121;-2132.134,-1443.644;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;108;-2160.567,-1273.986;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;109;-1991.668,-1282.386;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DistanceOpNode;153;-2124.588,-1001.418;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;124;-1980.135,-1401.644;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;135;-2136.745,-1098.943;Float;False;Property;_CoreSoftnessByDist;CoreSoftnessByDist;6;0;Create;True;0;0;0;False;0;False;0.1;1;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;106;-1820.968,-1318.886;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;154;-1873.589,-1019.418;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ACosOpNode;125;-1676.135,-1309.644;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;155;-1732.386,-1018.432;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;133;-1739.744,-1154.943;Float;False;Property;_CoreSize;CoreSize;5;0;Create;True;0;0;0;False;0;False;0.2;0.15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LengthOpNode;129;-1977.425,-1201.113;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;142;-1574.745,-1085.944;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;127;-1529.134,-1313.644;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;128;-1394.134,-1315.644;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;139;-1416.744,-1151.943;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;140;-1325.613,-1079.222;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;136;-1240.744,-1190.943;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceViewDirHlpNode;51;-1743.621,129.3479;Inherit;False;1;0;FLOAT4;0,0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;43;-1643.87,-208.2289;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleDivideOpNode;141;-1100.744,-1106.943;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;201;-2091.015,-275.375;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RefractOpVec;50;-1480.621,86.3479;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;-0.5;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldPosInputsNode;202;-2063.015,-199.375;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;157;-2169.76,-868.9526;Float;False;Property;_CoreTransparentByDist;CoreTransparentByDist;8;0;Create;True;0;0;0;False;0;False;0;5.5;0;8;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;110;-954.0447,-1102.18;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;194;-1508.39,-638.2051;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;203;-1827.015,-257.375;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FresnelNode;24;-1501.149,-349.1958;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;52;-1331.98,60.19205;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;161;-1347.842,-949.5576;Float;False;Property;_CoreTransparentBase;CoreTransparentBase;9;0;Create;True;0;0;0;False;0;False;0.99;0.8;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;156;-819.3865,-1224.432;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;-1862.789,-903.4204;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;196;-1298.133,-554.8715;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;26;-1228.773,-386.1093;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting;2;-1184.5,-93;Inherit;False;World;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting;67;-1157.124,4.513428;Inherit;False;World;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;163;-1295.845,925.8152;Inherit;False;1292.089;803.8553;Back Scattering;16;170;166;176;99;167;96;101;189;169;172;93;88;171;192;190;191;;1,1,1,1;0;0
Node;AmplifyShaderEditor.PowerNode;158;-1039.511,-908.7018;Inherit;False;False;2;0;FLOAT;0.99;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;144;-661.2694,-1084.646;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCIf;199;-970.5127,-302.5304;Inherit;False;6;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;68;-872.124,-81.48657;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0.5,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;69;-1012.156,96.13788;Float;False;Property;_DepthTint;DepthTint;3;0;Create;True;0;0;0;False;0;False;0.5019608,0.5019608,0.5019608,1;0.5735295,0.9117646,1,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldSpaceCameraPos;191;-1240.758,1082.626;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;190;-1212.758,1158.626;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;160;-586.2085,-955.61;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-2095.017,-540.895;Float;False;Property;_Smoothness;Smoothness;1;0;Create;True;0;0;0;False;0;False;0.95;0.923;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;149;-910.219,-483.3449;Float;False;Property;_CoreTint;Core Tint;7;0;Create;True;0;0;0;False;0;False;0.3689987,0.4704183,0.6691177,1;0.4823411,0.3724048,0.6176471,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-703.9589,-86.8582;Inherit;False;3;3;0;FLOAT;1;False;1;FLOAT3;0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldNormalVector;170;-1013.872,1205.944;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleSubtractOpNode;192;-976.7583,1100.626;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;143;-467.0554,-1097.799;Float;False;CoreMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-2344.919,-414.717;Float;False;Property;_DepthBlend;DepthBlend;2;0;Create;True;0;0;0;False;0;False;1;0.37;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;28;-1784.529,-536.9728;Float;False;Smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;169;-1023.677,998.0954;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;93;-980.5637,1480.863;Float;False;Property;_TransPow;TransPow;11;0;Create;True;0;0;0;False;0;False;1;2.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;189;-979.9697,1555.995;Float;False;Property;_TransScale;TransScale;15;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;171;-1016.993,1411.994;Float;False;Property;_TransThicknessScale;TransThicknessScale;13;0;Create;True;0;0;0;False;0;False;1;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;88;-1017.017,1338.579;Float;False;Property;_TransDistortion;TransDistortion;12;0;Create;True;0;0;0;False;0;False;1;0.35;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;172;-987.6025,1637.405;Float;False;Property;_TransAmbient;TransAmbient;14;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;151;-566.9542,-247.7034;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;145;-699.15,51.08798;Inherit;False;143;CoreMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;63;-1013.114,289.3728;Inherit;False;28;Smoothness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;99;-678.9913,990.2313;Float;False;Property;_TransColor;TransColor;10;0;Create;True;0;0;0;False;0;False;0.2720588,0.4578092,1,1;0.6176471,0.8259634,1,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FresnelNode;37;-2076.675,-463.7559;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightAttenuation;96;-643.7415,1298.731;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.LightColorNode;101;-646.0123,1169.976;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.CustomExpressionNode;167;-690.2047,1386.552;Float;False;half3 vEye = normalize(WorldViewDir)@$half3 vLTLight = normalize(WorldLightDir) + normalize(WorldNormal) * Distribution@$half fLTDot = pow(saturate(dot(vEye, -vLTLight)), Power) * Scale@$half3 fLT = (fLTDot + Ambient) * Thickness@$$return fLT@;3;Create;8;True;WorldLightDir;FLOAT3;0,0,0;In;;Float;False;True;WorldViewDir;FLOAT3;0,0,0;In;;Float;False;True;WorldNormal;FLOAT3;0,0,0;In;;Float;False;True;Distribution;FLOAT;1;In;;Float;False;True;Thickness;FLOAT;1;In;;Float;False;True;Power;FLOAT;1;In;;Float;False;True;Scale;FLOAT;1;In;;Float;False;True;Ambient;FLOAT;0;In;;Float;False;Back Scatter;True;False;0;;False;8;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;1;False;4;FLOAT;1;False;5;FLOAT;1;False;6;FLOAT;1;False;7;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CustomStandardSurface;62;-722.0707,163.6316;Inherit;False;Metallic;Tangent;6;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;7;-729.5393,-692.1389;Float;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;0.3775951,0.4910184,0.7132353,1;0.1191609,0.2102941,0.4264705,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;152;-425.9542,-148.7034;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;30;-619.7273,-424.5746;Inherit;False;28;Smoothness;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;34;-1777.431,-430.7948;Float;False;DepthBlendPow;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;176;-399.2258,1212.004;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;64;-230.1815,-79.70939;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CustomStandardSurface;20;-409.9587,-514.1595;Inherit;False;Metallic;Tangent;6;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;36;-297.6753,122.2441;Inherit;False;34;DepthBlendPow;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;166;-218.0098,1266.262;Float;False;BackScatter;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;165;-120.1692,256.2061;Inherit;False;166;BackScatter;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;65;-56.124,-73.4866;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;100;134.0142,79.5585;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;412.921,-4.975601;Float;False;True;-1;2;ASEMaterialInspector;0;0;CustomLighting;Symphonie/Legacy/Slime;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;1;False;;3;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;121;0;107;0
WireConnection;121;1;118;0
WireConnection;108;0;107;0
WireConnection;108;1;105;0
WireConnection;109;0;108;0
WireConnection;153;0;118;0
WireConnection;153;1;105;0
WireConnection;124;0;121;0
WireConnection;106;0;124;0
WireConnection;106;1;109;0
WireConnection;154;0;135;0
WireConnection;154;1;153;0
WireConnection;125;0;106;0
WireConnection;155;0;154;0
WireConnection;129;0;108;0
WireConnection;142;0;133;0
WireConnection;142;1;155;0
WireConnection;127;0;125;0
WireConnection;128;0;127;0
WireConnection;128;1;129;0
WireConnection;139;0;133;0
WireConnection;139;1;142;0
WireConnection;140;0;142;0
WireConnection;136;0;128;0
WireConnection;136;1;139;0
WireConnection;141;0;136;0
WireConnection;141;1;140;0
WireConnection;50;0;43;0
WireConnection;50;1;51;0
WireConnection;110;0;141;0
WireConnection;203;0;201;0
WireConnection;203;1;202;0
WireConnection;52;0;50;0
WireConnection;156;0;110;0
WireConnection;159;0;153;0
WireConnection;159;1;157;0
WireConnection;196;0;194;0
WireConnection;196;1;203;0
WireConnection;26;0;24;0
WireConnection;2;0;43;0
WireConnection;67;0;52;0
WireConnection;158;0;161;0
WireConnection;158;1;159;0
WireConnection;144;0;156;0
WireConnection;199;0;196;0
WireConnection;199;2;26;0
WireConnection;199;3;26;0
WireConnection;68;0;2;0
WireConnection;68;1;67;0
WireConnection;160;0;144;0
WireConnection;160;1;158;0
WireConnection;25;0;199;0
WireConnection;25;1;68;0
WireConnection;25;2;69;0
WireConnection;192;0;191;0
WireConnection;192;1;190;0
WireConnection;143;0;160;0
WireConnection;28;0;18;0
WireConnection;151;0;149;0
WireConnection;151;1;25;0
WireConnection;37;3;35;0
WireConnection;167;0;169;0
WireConnection;167;1;192;0
WireConnection;167;2;170;0
WireConnection;167;3;88;0
WireConnection;167;4;171;0
WireConnection;167;5;93;0
WireConnection;167;6;189;0
WireConnection;167;7;172;0
WireConnection;62;4;63;0
WireConnection;152;0;25;0
WireConnection;152;1;151;0
WireConnection;152;2;145;0
WireConnection;34;0;37;0
WireConnection;176;0;99;0
WireConnection;176;1;101;1
WireConnection;176;2;96;0
WireConnection;176;3;167;0
WireConnection;64;0;152;0
WireConnection;64;1;62;0
WireConnection;20;0;7;0
WireConnection;20;4;30;0
WireConnection;166;0;176;0
WireConnection;65;0;20;0
WireConnection;65;1;64;0
WireConnection;65;2;36;0
WireConnection;100;0;65;0
WireConnection;100;1;165;0
WireConnection;0;13;100;0
ASEEND*/
//CHKSM=720BF66C3F731BCB5F378BF65173C65F94E46DC6