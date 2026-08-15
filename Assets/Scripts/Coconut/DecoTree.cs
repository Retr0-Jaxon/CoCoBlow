using System.Collections;
using UnityEngine;

public class DecoTree : MonoBehaviour
{
    private Coroutine glitchRoutine;

    public void TriggerRotation(float duration, float minInterval, float maxInterval, float maxAngle)
    {
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
        }

        glitchRoutine = StartCoroutine(GlitchRoutine(duration, minInterval, maxInterval, maxAngle));
    }

    private IEnumerator GlitchRoutine(float duration, float minInterval, float maxInterval, float maxAngle)
    {
        Quaternion baseRotation = transform.rotation;
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            transform.rotation = baseRotation * Quaternion.Euler(
                Random.Range(-maxAngle, maxAngle),
                Random.Range(-maxAngle, maxAngle),
                Random.Range(-maxAngle, maxAngle));

            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }

        transform.rotation = baseRotation;
        glitchRoutine = null;
    }
}
