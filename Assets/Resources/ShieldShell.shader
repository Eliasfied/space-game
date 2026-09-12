Shader "Asterion/ShieldShell"
{
 Properties
 {
  [HDR] _BaseColor("Shield glow", Color) = (0.08,1.5,2.8,1)
  _Opacity("Opacity", Range(0,1)) = 1
  _Hit("Hit flash", Range(0,1)) = 0
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Back
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half _Opacity;
    half _Hit;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; };
   Varyings vert(Attributes v)
   {
    Varyings o;
    o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(v.normalOS);
    o.uv=v.uv;
    return o;
   }
   half4 frag(Varyings i):SV_Target
   {
    half rim=pow(1-saturate(dot(normalize(i.normalWS),GetWorldSpaceNormalizeViewDir(i.positionWS))),2.6);
    float2 grid=abs(frac(i.uv*float2(24,12))-.5);
    half panels=smoothstep(.46,.5,max(grid.x,grid.y));
    half scan=pow(saturate(.5+.5*sin(i.uv.y*25-_Time.y*5)),18);
    half alpha=saturate((.025+rim*.6+panels*.065+scan*.09+_Hit*.23)*_Opacity);
    half3 glow=lerp(_BaseColor.rgb,half3(3,3.8,4),_Hit)*(.65+rim*.7+scan*.35);
    return half4(glow,alpha);
   }
   ENDHLSL
  }
 }
}
