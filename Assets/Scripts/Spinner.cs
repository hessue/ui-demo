using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField] private float m_Speed = 70f;

    private float _speed;

    private void Awake()
    {
        _speed = m_Speed;
    }

    private void Update()
    {
        transform.Rotate(0f, _speed * Time.deltaTime, 0f, Space.Self);
    }

    public void SetSpeed(float rpm)
    {
        _speed = rpm;
    }
}

