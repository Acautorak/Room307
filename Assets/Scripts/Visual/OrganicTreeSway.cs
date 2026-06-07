using UnityEngine;
using DG.Tweening;

public class OrganicTreeSway : MonoBehaviour
{
    public float minAngle = 2f;
    public float maxAngle = 6f;

    public float minDuration = 2f;
    public float maxDuration = 5f;

    void Start()
    {
        Invoke(nameof(StartRandomSway), Random.Range(0f, 2f));
    }

    void StartRandomSway()
    {
        float angle = Random.Range(minAngle, maxAngle);

        if (Random.value > 0.5f)
            angle *= -1;

        float duration = Random.Range(minDuration, maxDuration);

        transform.DORotate(
            new Vector3(0, 0, angle),
            duration
        )
        .SetEase(Ease.InOutSine)
        .OnComplete(() =>
        {
            transform.DORotate(
                Vector3.zero,
                duration
            )
            .SetEase(Ease.InOutSine)
            .OnComplete(StartRandomSway);
        });
    }
}