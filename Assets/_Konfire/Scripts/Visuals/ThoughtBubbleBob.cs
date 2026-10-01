using UnityEngine;

public class ThoughtBubbleBob : MonoBehaviour
{
    private Vector3 initialLocalPos;
    public float bobSpeed = 4f;
    public float bobAmount = 0.08f;

    void Awake()
    {
        initialLocalPos = transform.localPosition;
    }

    void OnEnable()
    {
        transform.localPosition = initialLocalPos;
    }

    void Update()
    {
        float y = initialLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = new Vector3(initialLocalPos.x, y, initialLocalPos.z);
    }
}
