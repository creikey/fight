using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody != null)
        {
            Debug.Log("Grenade hit!");
            Vector3 towards = (Vector3)collision.attachedRigidbody.position - transform.position;
            if(Vector3.SqrMagnitude(towards) < 0.01f) // the grenade the explosion is coming from?
            {
                return;
            }
            collision.attachedRigidbody.AddForce(Vector3.Normalize(towards) * 100.0f);

            if(MultiplayerConfig.host && collision.gameObject.tag == "Player")
            {
                Player player = collision.gameObject.GetComponent<Player>();

                if(player.isPlayerLeft)
                {
                    player.gs.playerRight.score += 1;
                } else
                {
                    player.gs.playerLeft.score += 1;
                }

            }
        }
    }
}
