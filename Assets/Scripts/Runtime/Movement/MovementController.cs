using System.Collections;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public void Move(Vector3 endPosition, float speed)
    {
        Vector3 startPosition = transform.position;
        StartCoroutine(MovementCoroutine(startPosition, endPosition, speed));
    }

    private IEnumerator MovementCoroutine(Vector3 startPosition, Vector3 endPosition, float speed)
    {
        Vector3 direction = (endPosition - startPosition).normalized;
        if (direction != Vector3.zero)
        {
            transform.LookAt(endPosition);
        }

        float duration = Vector3.Distance(startPosition, endPosition) / speed;
        float elapsed = 0f;
                
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = movementCurve.Evaluate(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        transform.position = endPosition;
    }
}