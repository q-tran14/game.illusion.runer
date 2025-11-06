using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Offset & Smoothness")]
    [SerializeField] private Vector3 offset = new Vector3(75f, 85f, -75f);
    [SerializeField] private float smoothSpeed = 15f;

    [SerializeField] private Vector3 fixedRotation = new Vector3 (40f, -45f, 0f);

    void Start()
    {
        // Giữ nguyên góc nghiêng camera cố định
        transform.rotation = Quaternion.Euler(fixedRotation);
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Di chuyển camera theo player, duy trì offset
        Vector3 targetPosition = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
