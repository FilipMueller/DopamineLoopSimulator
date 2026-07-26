using System.Collections;
using UnityEngine;

public class ButtonPressAnimation : MonoBehaviour
{
    [Header("Button Positions")]
    [SerializeField] private Vector3 defaultLocalPosition = new Vector3(0f, 0.002f, -0.002f);
    [SerializeField] private Vector3 pressedLocalPosition = new Vector3(0f, 0f, 0f);

    [Header("Timing")]
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float holdDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.12f;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        transform.localPosition = defaultLocalPosition;
    }

    public void PlayPressAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(PressRoutine());
    }

    private IEnumerator PressRoutine()
    {
        yield return MoveToPosition(transform.localPosition, pressedLocalPosition, pressDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return MoveToPosition(pressedLocalPosition, defaultLocalPosition, releaseDuration);

        animationCoroutine = null;
    }

    private IEnumerator MoveToPosition(Vector3 from, Vector3 to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            transform.localPosition = Vector3.Lerp(from, to, t);

            yield return null;
        }

        transform.localPosition = to;
    }
}