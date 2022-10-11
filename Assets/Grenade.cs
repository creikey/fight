using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float progress = 0.0f; // in seconds

    const float blinkTimeInterval = 0.05f;
    const float blinkingTime = blinkTimeInterval * 8.0f;
    const float explodeExpandTime = 0.15f;
    const float explosionFadeTime = 0.3f;

    public Rigidbody2D grenadeBody;
    public SpriteRenderer grenadeSprite;
    public GameObject explosion;
    public SpriteRenderer explosionSprite;
    public Collider2D explosionCollider;

    private float explosionMaxRadius;

    void Start()
    {
        explosionMaxRadius = explosion.transform.localScale.x;
        SetExplosionRadius(0.0f);
        grenadeSprite.enabled = false;
    }

    private void SetExplosionRadius(float radiusFraction)
    {
        float actualRadius = radiusFraction * explosionMaxRadius;
        explosion.transform.localScale = new Vector3(actualRadius, actualRadius, actualRadius);
    }

    private void SetExplosionAlpha(float alpha)
    {
        Color newColor = explosionSprite.color;
        newColor.a = alpha;
        explosionSprite.color = newColor;
    }

    // Update is called once per frame
    void Update()
    {
        progress += Time.deltaTime;

        if(progress < blinkingTime)
        {
            // blinky blinky
            grenadeBody.bodyType = RigidbodyType2D.Dynamic;
            explosionCollider.enabled = false;
            grenadeSprite.enabled = true;
            SetExplosionRadius(0.0f);
            if (Mathf.FloorToInt(progress / 0.05f) % 2 == 0)
            {
                Color col = grenadeSprite.color;
                col.a = 1.0f;
                grenadeSprite.color = col;
            }
            else
            {
                Color col = grenadeSprite.color;
                col.a = 0.5f;
                grenadeSprite.color = col;
            }
        }
        else if(progress < blinkingTime + explodeExpandTime)
        {
            grenadeBody.bodyType = RigidbodyType2D.Static;
            // expand the explosion, explosion is active
            explosionCollider.enabled = true;
            grenadeSprite.enabled = false;
            float explosionProgress = (progress - blinkingTime) / explodeExpandTime;
            SetExplosionAlpha(1.0f);
            SetExplosionRadius(explosionProgress);
        }
        else if(progress < blinkingTime + explodeExpandTime + explosionFadeTime)
        {
            SetExplosionRadius(1.0f);
            grenadeBody.bodyType = RigidbodyType2D.Static;
            // fade away. explosion no longer active
            explosionCollider.enabled = false;
            float fadedProgress = (progress - (blinkingTime + explodeExpandTime)) / explosionFadeTime;
            SetExplosionAlpha(1.0f - (fadedProgress));
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
