using MessageTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GrenadeManager : MonoBehaviour
{
    public GameSession gs;
    public GameObject grenade;

    // for collision ignoring
    public Collider2D playerLeft;
    public Collider2D playerRight;

    private List<GameObject> activeNades = new List<GameObject>();

    public void SpawnGrenade(Vector3 from, Vector3 direction)
    {
        GameObject newNade = Instantiate(grenade);
        newNade.transform.position = from;
        newNade.GetComponent<Rigidbody2D>().velocity = direction * 8.0f;
        Physics2D.IgnoreCollision(newNade.GetComponent<Collider2D>(), playerLeft);
        Physics2D.IgnoreCollision(newNade.GetComponent<Collider2D>(), playerRight);
        activeNades.Add(newNade);
    }

    public void OnFromGameState(GameState gs)
    {
        foreach (GameObject activeNade in activeNades)
        {
            Destroy(activeNade);
        }

        foreach(GameState.Grenade newNade in gs.nades)
        {
            GameObject activeNade = Instantiate(grenade);
            activeNade.transform.position = U.from(newNade.position);
            activeNade.GetComponent<Rigidbody2D>().velocity = U.from(newNade.velocity);
            activeNade.GetComponent<Grenade>().progress = newNade.progress;
        }
    }

    public void OnUpdateGameState(GameState gs)
    {
        foreach(GameObject activeNade in activeNades)
        {
            if(activeNade == null)
            {
                continue;
            }
            gs.nades.Add(new GameState.Grenade
            {
                position = U.from(activeNade.transform.position),
                velocity = U.from(activeNade.GetComponent<Rigidbody2D>().velocity),
                progress = activeNade.GetComponent<Grenade>().progress,
            });
        }
    }

    private void OnEnable()
    {
        gs.OnFromGameState += OnFromGameState;
        gs.OnUpdateGameState += OnUpdateGameState;
    }

    private void OnDisable()
    {
        gs.OnFromGameState -= OnFromGameState;
        gs.OnUpdateGameState -= OnUpdateGameState;
    }
}
