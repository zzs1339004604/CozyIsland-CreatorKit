Shader "ResearchCamp/VertexTint" { Properties { _Color ("Color", Color) = (1,1,1,1) } SubShader { Tags { "RenderType"="Opaque" } Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
float4 _Color;
struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
struct v2f { float4 pos:SV_POSITION; float light:TEXCOORD0; };
v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.light=.65+.35*saturate(dot(UnityObjectToWorldNormal(v.normal),normalize(float3(.4,.8,.3))));return o;}
fixed4 frag(v2f i):SV_Target{return fixed4(_Color.rgb*i.light,1);}
ENDCG } } }
