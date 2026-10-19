Shader "Custom/TreeLeaf_Simple"
{
     Properties

    {

        _Color("Main Color", Color) = (1,1,1,1)

        _clipAlpha("clip Alpha", Range(0 , 1) ) = 0.5

        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.333

        _Strength("摇摆幅度", Float) = 1

        _Speed("摇摆速度", Float) = 3

        _AoColor("漫反射堆叠色", Color) = (1,1,1)

        _Specular("高光色", Color) = (1,1,1)

        _Gloss("光泽度", Float) = 1

    }


 

    SubShader

    {

        Tags

        {

            "Queue" = "AlphaTest"

            "IgnoreProjector" = "True"

            "RenderType" = "TransparentCutout"

        }

        LOD 250

        Cull Off

        Pass

        {

            CGPROGRAM

            #include "../CGIncludes/UnityCG.cginc"

            #include "../CGIncludes/UnityLightingCommon.cginc"

            // #pragma alphatest:_Cutoff noforwardadd   // #pragma surface surf Lambert

            ///*nodirlightmap nodynlightmap noforwardadd*/

            #pragma target 2.0

            #pragma vertex vert

            #pragma fragment frag alphatest:_Cutoff noforwardadd


 

            fixed4 _Color;

            float _clipAlpha;

            sampler2D _MainTex;

            float4 _MainTex_ST;

            fixed3 _AoColor;

            float _Speed;

            float _Strength;

           

            fixed3 _Specular;

            float _Gloss;

            struct appdata

            {

                float4 vertex : POSITION;

                float2 uv : TEXCOORD0;

                float4 color : COLOR0;

                float3 normal: NORMAL;

            };

            struct v2f

            {

                float2 uv : TEXCOORD0;

                float4 pos : SV_POSITION;

                float4 color: TEXCOORD1;

                float3 worldNormal: TEXCOORD2;

                float3 worldPos : TEXCOORD3;

            };

            v2f vert (appdata v)

            {

                v2f o;

                // float3 worldPos = UnityObjectToWorldDir(v.vertex);   // 将顶点从 模型空间 转换到 世界空间

                float stage1 = dot(v.vertex, float3(0, 1, 0)) * _Strength;

                float stage2 = sin(dot(v.vertex, float3(1, 0, 0)) * _Strength + _Time.y * _Speed);

                float3 stage3 = stage1 * stage2 * float3(0.001, 0, 0.001) * v.color.a;

               

                o.pos = UnityObjectToClipPos(v.vertex + stage3);        // 对象空间转换到裁剪空间

                // 下面这两个函数等价

                // o.uv = TRANSFORM_TEX( v.texcoord , _MainTex );

                // o.uv = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.color = v.color;

                // 归一化向量

                o.worldNormal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;

            }

           

            fixed4 frag (v2f i) : SV_Target

            {

                // sample the texture

                fixed4 col = tex2D( _MainTex, i.uv );

                // 丢弃掉小于 0 的像素

                clip ( col.a - _clipAlpha );

                // 世界空间下的法线

                fixed3 worldNormal = i.worldNormal;

                // 世界空间下光源方向

                fixed3 worldLightDir = normalize( _WorldSpaceLightPos0.xyz );

                // 漫反射

                fixed3 diffuse = _LightColor0.rgb * col.rgb * _AoColor * (dot(worldNormal, worldLightDir) * 0.5 + 0.5 );

                // 环境光

                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * col.rgb;

                // 世界空间下的视角方向

                fixed3 viewDir = normalize( _WorldSpaceCameraPos.xyz - i.worldPos.xyz );

                // 世界空间下的half方向

                fixed3 halfDir = normalize( worldLightDir + viewDir );

                // 高光

                fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow( saturate( dot(halfDir, worldNormal) ) , _Gloss );

                // return fixed4( diffuse, col.a );

                return fixed4( diffuse + ambient + specular, col.a );

            };

            // void surf(Input IN, inout SurfaceOutput OUT)

            // {

            //  fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            //  OUT.Albedo = c.rgb;

            //  OUT.Alpha = c.a;

            // }            

            ENDCG

        } // end Pass

    } // end SubShader


 

    FallBack "XW/Mobile/Cutout/VertexLit"
}
