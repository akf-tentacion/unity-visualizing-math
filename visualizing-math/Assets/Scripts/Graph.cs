using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] Transform pointPrefab;
    [SerializeField, Range(10, 500)] int resolution = 10;
    [SerializeField] FunctionLibrary.FunctionName function;
    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;
    Transform[] points;
    float duration;

    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;

    private void Awake()
    {
        CreateGraph();
    }

    void CreateGraph()
    {
        float step = 2f / resolution;

        Vector3 scale = Vector3.one * step;

        points = new Transform[resolution * resolution];    //x,z平面に点を並べるから２乗
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = Instantiate(pointPrefab);
            point.localScale = scale;
            point.SetParent(transform, false);
            points[i] = point;
        }
    }

    private void Update()
    {
        duration += Time.deltaTime;
        if (transitioning) {
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
            function = FunctionLibrary.GetNextFunctionName(function);
        }

        if (transitioning)
        {
            UpdateFunctionTransition();
            return;
        }

        UpdateFunction();
    }

    void UpdateFunction()
    {
        FunctionLibrary.FunctionDelegate f = FunctionLibrary.GetFunction(function);

        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = f(u, v, Time.time);
        }
    }

    void UpdateFunctionTransition()
    {
        FunctionLibrary.FunctionDelegate
            from = FunctionLibrary.GetFunction(transitionFunction),
            to = FunctionLibrary.GetFunction(function);
        float progress = duration / transitionDuration;
        float time = Time.time;
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = FunctionLibrary.Morph(
                u, v, time, from, to, progress
            );
        }
    }
}
