using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameSession : MonoBehaviour
{
    enum State
    {
        WaitingForInput,
        Processing,
    }


    public AnimationCurve deltaCurve;
    private float processProgress = 0.0f;
    private State state = State.WaitingForInput;

    void Update()
    {
        switch (state)
        {
            case State.WaitingForInput:
                break;
            case State.Processing:
                processProgress += Time.unscaledDeltaTime;
                if(processProgress > 1.0f)
                {
                    processProgress = 0.0f;
                    state = State.WaitingForInput;
                }
                Time.timeScale = deltaCurve.Evaluate(processProgress);

                break;
        }
    }
}