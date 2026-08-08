using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testStereo : MonoBehaviour
{
    public float radius = 5f;       // 环绕半径
    public float speed = 90f;       // 每秒旋转角度

    private Transform player;       // Player 的 Transform
    private float angle;            // 当前角度

    // Start is called before the first frame update
    void Start()
    {
        // 找到 Player（或者就是自己挂载的这个物体）
        player = Camera.main != null ? Camera.main.transform : this.transform;

        // 初始位置：从 Player 前方 radius 距离开始
        angle = 0f;
        UpdatePosition();

        AudioManager.PlayAudio3D("test", this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        angle += speed * Time.deltaTime;
        if (angle >= 360f) angle -= 360f;
        UpdatePosition();
    }

    void UpdatePosition()
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;
        transform.position = player.position + offset;
    }
}
