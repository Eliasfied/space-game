Shader "Asterion/Target Selection"
{
 Properties { _BaseColor("Color", Color) = (1,0.06,0.09,1) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+5" }
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
   struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
   CBUFFER_END
   Varyings Vert(Attributes input) {
    Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.uv=input.uv;return output;
   }
   half4 Frag(Varyings input):SV_Target {
    float radius=length(input.uv*2-1);
    float feather=max(fwidth(radius),.012);
    float outer=1-smoothstep(.98-feather,.98+feather,radius);
    float rim=smoothstep(.76,.82,radius)*outer;
    float fill=.28*(1-smoothstep(.76,.84,radius));
    float highlight=smoothstep(.84,.87,radius)*(1-smoothstep(.91,.94,radius));
    // A bright red core and dark outer edge keep the ring readable on both floor tones.
    half3 color=lerp(_BaseColor.rgb,half3(1.4,.42,.34),highlight);
    color=lerp(color,half3(.16,.008,.02),smoothstep(.95,.98,radius));
    return half4(color,_BaseColor.a*max(fill,rim));
   }
   ENDHLSL
  }
 }
}
