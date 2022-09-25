using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class GameInput
{
    public Vector2 direction;
    public bool use;
}

public class GameSession : MonoBehaviour
{
    public enum State
    {
        WaitingForInput,
        Processing,
    }


    public AnimationCurve deltaCurve;
    private float processProgress = 0.0f;
    public float processSpeed { get; private set; } = 1.5f;
    public State state { get; private set; } = State.WaitingForInput;


    public delegate void OnNewInput(GameInput input);
    public event OnNewInput OnInput;

    public void SupplyInput(GameInput input)
    {
        if (OnInput != null)
        {
            OnInput(input);
        }
        processProgress = 0.0f;
        state = State.Processing;
    }

    private void PauseTime()
    {
        Time.timeScale = 0.0f;
        Time.fixedDeltaTime = 0.0f;
    }

    private void Start()
    {
        PauseTime();
    }

    private void Update()
    {
        switch (state)
        {
            case State.WaitingForInput:
                PauseTime();
                break;
            case State.Processing:
                processProgress += Time.unscaledDeltaTime * processSpeed;
                if (processProgress > 1.0f)
                {
                    state = State.WaitingForInput;
                }
                float newScale = deltaCurve.Evaluate(processProgress);
                newScale = Mathf.Max(newScale, 0.01f);
                Time.timeScale = newScale;
                Time.fixedDeltaTime = Time.timeScale * 0.02f;

                break;
        }

    }
}