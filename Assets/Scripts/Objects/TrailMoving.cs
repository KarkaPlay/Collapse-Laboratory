using System.Collections;
using UnityEngine;

public class TrailMoving : MonoBehaviour
{
    [Header("Эффект связи между объектами")]
    public GameObject trailPrefab;

    private float timeToMove;

    public void SetTimeToMove(float newTime)
    {
        timeToMove = newTime;
    }

    public void StartTrail(Transform to)
    {
        GameObject trail = Instantiate(trailPrefab, transform);
        trail.transform.parent = null;

        StartCoroutine(TrailCoroutine(transform, to, trail));
    }

    private IEnumerator TrailCoroutine(Transform from, Transform to, GameObject trail)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = from.position;
        Vector3 endPosition = to.position;

        while (elapsedTime < timeToMove + 1)
        {
            if (trail != null)
            {
                trail.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / timeToMove);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (trail != null)
        {
            trail.transform.position = endPosition;
            Destroy(trail);
        }
    }
}
