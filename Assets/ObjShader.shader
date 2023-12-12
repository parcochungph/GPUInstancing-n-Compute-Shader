Shader "Custom/ObjShader"
{
    Properties
    {
        [Header(Test)]
        _InteractionRadius ("Interaction Radius",float) = 30
        
        [Header(Colour Collection 1)]
        _ColourCollection1a("Collection 1a", Color) = (1, .51, .76, 1)
        _ColourCollection1b("Collection 1b", Color) = (1, .27, .49, 1)
        _ColourCollection1c("Collection 1c", Color) = (1, .66, .66, 1)
        
        [Header(Colour Collection 2)]
        _ColourCollection2a("Collection 2a", Color) = (1, .7, .0, 1)
        _ColourCollection2b("Collection 2b", Color) = (1, .43, .2, 1)
        _ColourCollection2c("Collection 2c", Color) = (1, .97, .0, 1)
        
        [Header(Colour Collection 3)]
        _ColourCollection3a("Collection 3a", Color) = (.05, 1, .21, 1)
        _ColourCollection3b("Collection 3b", Color) = (.09, .69, .17, 1)
        _ColourCollection3c("Collection 3c", Color) = (.21, .96, .46, 1)
        
        [Header(Colour Collection 4)]
        _ColourCollection4a("Collection 4a", Color) = (.41, 1, .81, 1)
        _ColourCollection4b("Collection 4b", Color) = (0, .28, .93, 1)
        _ColourCollection4c("Collection 4c", Color) = (.5, .75, .88, 1)
        
        [Header(Colour Default)]
        _ColourDefaulta("Colour Default a", Color) = (1, 1, 1, 1)
        _ColourDefaultb("Colour Default b", Color) = (.81, .81, .81, 1)
        _ColourDefaultc("Colour Default c", Color) = (1, .87, .84, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="False"
            "RenderType"="Opaque"
        }
        Cull Off
        Lighting Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float _InteractionRadius;

            //float4 _ColourSpring[3] = [_ColourSpring1, _ColourSpring2, _ColourSpring3];

            float4 _ColourCollection1a;
            float4 _ColourCollection1b;
            float4 _ColourCollection1c;

            float4 _ColourCollection2a;
            float4 _ColourCollection2b;
            float4 _ColourCollection2c;

            float4 _ColourCollection3a;
            float4 _ColourCollection3b;
            float4 _ColourCollection3c;

            float4 _ColourCollection4a;
            float4 _ColourCollection4b;
            float4 _ColourCollection4c;

            float4 _ColourDefaulta;
            float4 _ColourDefaultb;
            float4 _ColourDefaultc;

            struct ObjData
            {
                float3 pos;
                float3 movingPos;
                float3 adjustPos;
                float3 showPos;
                float colourDelta;
                float vertDelta;
                int colourGrp;
                float effectStartTime;
                int reactEffectMode;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            StructuredBuffer<ObjData> _ObjDataBuffer;
            float3 _MeshScale;
            int _Phase;
            float _colourChangeFactor;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Pos
                float4x4 matrix_ = (float4x4)0;
                matrix_._11_22_33_44 = float4(_MeshScale.xyz, 1.0);
                matrix_._14_24_34 += _ObjDataBuffer[instanceID].showPos;
                v.vertex = mul(matrix_, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Colour
                float4 colour1 = _ColourDefaulta;
                float4 colour2 = _ColourDefaulta;

                if (_ObjDataBuffer[instanceID].colourDelta > 0)
                {
                    switch (_ObjDataBuffer[instanceID].colourGrp)
                    {
                        case 0:
                            colour1 = _ColourDefaulta;
                            break;
                        case 1:
                            colour1 = _ColourDefaultb;
                            break;
                        case 2:
                            colour1 = _ColourDefaultc;
                            break;
                    }
                    switch (_Phase)
                    {
                        case 0:
                            switch (_ObjDataBuffer[instanceID].colourGrp)
                            {
                                case 0:
                                    colour2 = lerp(_ColourCollection1a, _ColourCollection2a, _colourChangeFactor);
                                    break;
                                case 1:
                                    colour2 =lerp(_ColourCollection1b, _ColourCollection2b, _colourChangeFactor);
                                    break;
                                case 2:
                                    colour2 =lerp(_ColourCollection1c, _ColourCollection2c, _colourChangeFactor);
                                    break;
                            }
                            break;
                        case 1:
                            switch (_ObjDataBuffer[instanceID].colourGrp)
                            {
                                case 0:
                                    colour2 = lerp(_ColourCollection2a, _ColourCollection3a, _colourChangeFactor);
                                    break;
                                case 1:
                                    colour2 = lerp(_ColourCollection2b, _ColourCollection3b, _colourChangeFactor);
                                    break;
                                case 2:
                                    colour2 = lerp(_ColourCollection2c, _ColourCollection3c, _colourChangeFactor);
                                    break;
                            }
                            break;
                        case 2:
                            switch (_ObjDataBuffer[instanceID].colourGrp)
                            {
                                case 0:
                                    colour2 = lerp(_ColourCollection3a, _ColourCollection4a, _colourChangeFactor);
                                    break;
                                case 1:
                                    colour2 = lerp(_ColourCollection3b, _ColourCollection4b, _colourChangeFactor);
                                    break;
                                case 2:
                                    colour2 = lerp(_ColourCollection3c, _ColourCollection4c, _colourChangeFactor);
                                    break;
                            }
                            break;
                        case 3:
                            switch (_ObjDataBuffer[instanceID].colourGrp)
                            {
                                case 0:
                                    colour2 = lerp(_ColourCollection4a, _ColourCollection1a, _colourChangeFactor);
                                    break;
                                case 1:
                                    colour2 = lerp(_ColourCollection4b, _ColourCollection1b, _colourChangeFactor);
                                    break;
                                case 2:
                                    colour2 = lerp(_ColourCollection4c, _ColourCollection1c, _colourChangeFactor);
                                    break;
                            }
                            break;
                    }
                    o.color = lerp(colour1, colour2, _ObjDataBuffer[instanceID].colourDelta);
                }
                else
                {
                    switch (_ObjDataBuffer[instanceID].colourGrp)
                    {
                        case 0:
                            o.color = _ColourDefaulta;
                            break;
                        case 1:
                            o.color = _ColourDefaultb;
                            break;
                        case 2:
                            o.color = _ColourDefaultc;
                            break;
                    }
                }

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);
                // // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                // return col;
                return i.color;
            }
            ENDCG
        }
    }
}
