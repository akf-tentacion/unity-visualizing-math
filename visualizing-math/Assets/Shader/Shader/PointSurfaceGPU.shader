Shader "Graph/PointSurfaceGPU"
{
    Properties{
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    SubShader{      
        CGPROGRAM
        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural   //頂点ごとに関数を呼び出す
        #pragma editor_sync_compilation //同期コンパイルにより、ダミーシェーダーのクラッシュを回避する。
        #pragma target 4.5  //compute shaderのサポート
       
       #include "PointGPU.hlsl"
       struct Input{
            float3 worldPos;
       };
      
       float _Smoothness;
       
       void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
            surface.Albedo.rg = input.worldPos.xy * 0.5 + 0.5;
            surface.Smoothness = _Smoothness;
       }
       ENDCG
    }

    FallBack "Diffuse"
}
