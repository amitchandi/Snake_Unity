using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileObject : MonoBehaviour
{
    public Tile tile;
    
    void Awake()
    {
        tile = new Tile
        {
            isNew = true
        };
    }

    public void Shake()
    {
        if (tile.isNew)
        {
            gameObject.transform.position.Set(gameObject.transform.position.x + Mathf.Sin(Time.time * 1f) * 1f, gameObject.transform.position.y + Mathf.Sin(Time.time * 1f) * 1f, gameObject.transform.position.z);
            tile.isNew = false;
            StartCoroutine(ShakeCR());
        }
    }

    IEnumerator ShakeCR()
    {
        for (int i = 0; i < 3; i++)
        {
            gameObject.transform.Rotate(0f, 0f, 10f);
            yield return new WaitForSecondsRealtime(0.1f);
            gameObject.transform.Rotate(0f, 0f, -10f);
            yield return new WaitForSecondsRealtime(0.1f);
            gameObject.transform.Rotate(0f, 0f, -10f);
            yield return new WaitForSecondsRealtime(0.1f);
            gameObject.transform.Rotate(0f, 0f, 10f);
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }
}
