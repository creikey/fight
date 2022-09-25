using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions;

public class Player : MonoBehaviour
{
    // do not change gs after enabled, disable it first to remove input callback
    public GameSession gs;

    public Rigidbody2D body;
    public LineRenderer line;
    public float speed = 10.0f;
    float lineLength;

    void OnInput(Input input)
    {
        Debug.Log("Input: " + input);
    }


    void Start()
    {
        lineLength = Vector3.Magnitude(line.GetPosition(line.positionCount - 1));
    }

    void Update()
    {
        float target = gs.state switch
        {
            GameSession.State.WaitingForInput => 1.0f,
            GameSession.State.Processing => 0.0f,
            _ => float.NaN,
        };
        Assert.IsTrue(target != float.NaN);

        Color newColor = line.startColor;
        newColor.a = Mathf.Lerp(newColor.a, target, Time.unscaledDeltaTime * 14.0f);
        line.startColor = newColor;
        line.endColor = newColor;

        //line.colorGradient.alphaKeys[0].alpha = Mathf.Lerp(line.colorGradient.alphaKeys[0].alpha, target, Time.unscaledDeltaTime * 100.0f);
        //line.colorGradient.alphaKeys[1].alpha = line.colorGradient.alphaKeys[0].alpha;
        //line.colorGradient.SetKeys(line.colorGradient.colorKeys, line.colorGradient.alphaKeys);
        //Debug.Log(Mathf.Lerp(line.colorGradient.alphaKeys[0].alpha, target, Time.unscaledDeltaTime * 100.0f));

        switch (gs.state)
        {
            case GameSession.State.WaitingForInput:
                Vector3 dir = new Vector3(); // in world space
                // calculate dir, to mouse
                {
                    Vector3 world3DMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    world3DMouse.z = 0.0f;
                    dir = Vector3.Normalize(world3DMouse - transform.position);
                }
                Vector3 from = transform.position;
                line.SetPosition(1, from + dir * lineLength);
                line.SetPosition(0, from);


                if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    GameInput newInput = new GameInput();
                    newInput.direction = dir;
                    newInput.use = Input.GetMouseButtonDown(1);
                    gs.SupplyInput(newInput);
                }
                break;
            case GameSession.State.Processing:
                break;
        }
    }

    void OnEnable()
    {
        gs.OnInput += OnInput;
    }
    void OnDisable()
    {
        gs.OnInput -= OnInput;
    }
    void OnInput(GameInput input)
    {
        body.velocity = input.direction * speed;
    }
}