using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using MessageTypes;

public class Player : MonoBehaviour
{
    // do not change gs after enabled, disable it first to remove input callback
    // accessed by explosion to set the score
    public GameSession gs;

    public GrenadeManager grenadeManager;

    public Rigidbody2D body;
    public LineRenderer movePreviewLine;
    public LineRenderer actualMovementLine;
    public LineRenderer bombLine;
    public float speed = 3.0f;
    public int score;
    public bool isPlayerLeft = true;
    float lineLength;

    private Vector2 bombInput = new Vector2(0.0f, 0.0f);
    private bool localInput = false;

    void Start()
    {
        lineLength = Vector3.Magnitude(movePreviewLine.GetPosition(movePreviewLine.positionCount - 1));
    }

    private void LerpLineTo(LineRenderer line, float target)
    {
        Color newColor = line.startColor;
        newColor.a = Mathf.Lerp(newColor.a, target, Time.unscaledDeltaTime * 14.0f);
        line.startColor = newColor;
        line.endColor = newColor;
    }

    void Update()
    {
        movePreviewLine.enabled = localInput;
        actualMovementLine.enabled = localInput;
        float target = gs.State switch
        {
            GameSession.StateType.WaitingForInput => 1.0f,
            GameSession.StateType.Processing => 0.0f,
            _ => float.NaN,
        };
        Assert.IsTrue(target != float.NaN);
        
        LerpLineTo(movePreviewLine, target);

        //line.colorGradient.alphaKeys[0].alpha = Mathf.Lerp(line.colorGradient.alphaKeys[0].alpha, target, Time.unscaledDeltaTime * 100.0f);
        //line.colorGradient.alphaKeys[1].alpha = line.colorGradient.alphaKeys[0].alpha;
        //line.colorGradient.SetKeys(line.colorGradient.colorKeys, line.colorGradient.alphaKeys);
        //Debug.Log(Mathf.Lerp(line.colorGradient.alphaKeys[0].alpha, target, Time.unscaledDeltaTime * 100.0f));

        bombLine.enabled = bombInput.SqrMagnitude() > 0.01f;

        switch (gs.State)
        {
            case GameSession.StateType.WaitingForInput:
                if(localInput)
                {
                    Vector3 dir = new Vector3(); // in world space
                                                 // calculate dir, to mouse
                    {
                        Vector3 world3DMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        world3DMouse.z = 0.0f;
                        dir = Vector3.Normalize(world3DMouse - transform.position);
                    }
                    Vector3 from = transform.position;
                    movePreviewLine.SetPosition(1, from + dir * lineLength);
                    movePreviewLine.SetPosition(0, from);


                    if (Input.GetMouseButtonDown(0))
                    {
                        PlayerInput newInput = new PlayerInput
                        {
                            movementDirection = U.from(dir),
                            nadeThrow = U.from(bombInput),
                        };
                        actualMovementLine.SetPosition(1, movePreviewLine.GetPosition(1));
                        actualMovementLine.SetPosition(0, movePreviewLine.GetPosition(0));
                        gs.SupplyMyInput(newInput);
                    }

                    if (Input.GetMouseButtonDown(1))
                    {
                        bombInput = dir;
                    }
                    bombLine.gameObject.transform.LookAt(bombLine.transform.position + (Vector3)bombInput);
                }
                break;
            case GameSession.StateType.Processing:
                bombInput = new Vector2();
                break;
        }
    }

    private GameState.Player GetMyPlayer(GameState gs)
    {
        if (isPlayerLeft)
        {
            return gs.playerLeft;
        }
        else
        {
            return gs.playerRight;
        }
    }

    void OnFromGameState(GameState gs)
    {
        GameState.Player me = GetMyPlayer(gs);
        score = me.score;
        transform.position = U.from(me.position);
        body.velocity = U.from(me.velocity);
    }

    void OnUpdateGameState(GameState gs)
    {
        GameState.Player me = GetMyPlayer(gs);
        me.score = score;
        me.position = U.from(transform.position);
        me.velocity = U.from(body.velocity);
    }

    private bool IsHostPlayer()
    {
        return isPlayerLeft;
    }

    void OnEnable()
    {
        gs.OnFromGameState += OnFromGameState;
        gs.OnUpdateGameState += OnUpdateGameState;

        if(IsHostPlayer() && MultiplayerConfig.host)
        {
            localInput = true;
        }
        else if(!IsHostPlayer() && !MultiplayerConfig.host)
        {
            localInput = true;
        }
        else
        {
            localInput = false;
        }

        if (localInput)
        {
            gs.OnMyInput += OnInput;
        }
        else
        {
            gs.OnRemoteInput += OnInput;
        }
    }
    void OnDisable()
    {
        gs.OnFromGameState -= OnFromGameState;
        gs.OnUpdateGameState -= OnUpdateGameState;

        // @Robust ensure isLocalPlayer hasn't changed when event was connected to, or just do this better...
        if (localInput)
        {
            gs.OnMyInput -= OnInput;
        }
        else
        {
            gs.OnRemoteInput -= OnInput;
        }

    }
    void OnInput(PlayerInput input)
    {
        Vector2 movementDirection = U.from(input.movementDirection);
        Vector3 dir = movementDirection;

        actualMovementLine.SetPosition(1, transform.position + dir * lineLength);
        actualMovementLine.SetPosition(0, transform.position);

        // @Design I had it actually use force instead of hardsetting velocity, meaning movement from previous
        // round influenced the current movement. This felt bad because when deciding what to do next, what
        // momentum the player had was just not correctly visualized. Maybe some kind of really nice 
        // visualization of the vector addition of new movement would make this good... Something to explore later
        //body.AddForce(movementDirection * speed, ForceMode2D.Impulse);
        body.velocity = movementDirection * speed;

        if (Vector3.SqrMagnitude(U.from(input.nadeThrow)) > 0.01f)
        {
            grenadeManager.SpawnGrenade(transform.position, U.from(input.nadeThrow));
        }
    }
}