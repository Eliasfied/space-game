Shader "Asterion/Ground Telegraph"
{
 Properties { _BaseColor("Color", Color) = (0.1,0.8,1,0.2) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+20" }
  Pass {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest LEqual
   Cull Off
   Offset -1, -1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
   struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
   CBUFFER_END
   Varyings Vert(Attributes input) { Varyings o; o.world=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=input.uv;return o; }
   half4 Frag(Varyings i) : SV_Target {
    float r=length(i.uv*2-1);
    float border=smoothstep(.82,.97,r);
    float hatch=step(.65,frac((i.world.x+i.world.z)*2));
    half alpha=_BaseColor.a*(.5+border*1.6+hatch*.35);
    return half4(_BaseColor.rgb,alpha);
   }
   ENDHLSL
  }
 }
}
