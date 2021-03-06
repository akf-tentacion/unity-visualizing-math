using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    const int maxResolution = 1000;
    [SerializeField] ComputeShader computeShader = default;
    [SerializeField] Material material = default;
    [SerializeField] Mesh mesh = default;
    [SerializeField, Range(10, maxResolution)] int resolution = 10;
    [SerializeField] FunctionLibrary.FunctionName function = default;
    FunctionLibrary.FunctionName transitionFunction = default;

    public enum TransitionMode { Cycle, Random }
    [SerializeField] TransitionMode transitionMode = TransitionMode.Cycle;

    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;

    ComputeBuffer positionsBuffer;
    bool transitioning;
    float duration;


    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
        transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    //compute bufferの取得コードがホットリロードに耐えられなくなるから、Awakeはやめる。
    private void OnEnable()
    {
        //3つのfloat(4バイト)で構成される位置ベクトルを格納するから、要素サイズは3*4
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);

    }

    void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }

        UpdateFunctionOnGPU();
    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);

        if (transitioning)
        {
            computeShader.SetFloat(
                transitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration)
            );
        }

        int kernelIndex = (int)function + (int)(transitioning ? transitionFunction : function) * FunctionLibrary.FunctionCount;
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);

        //計算シェーダーを呼び出す。各グループが8x8で構成されるので、
        //必要なグループ数は解像度を8で割ったものになる。
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);


        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);
        //boundsの設定。点の形状が立方体であり、サイズがあるので、はみ出ないように設定する。
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material,bounds, resolution * resolution);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;  //グラフが破棄された場合、次の実行時にオブジェクトの再利用を可能にするため、明示的にnullに設定
    }

}
