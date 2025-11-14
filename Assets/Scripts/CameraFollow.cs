using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [Header("Offset & Smoothness")]
    [SerializeField] private Vector3 offset = new Vector3(40f, 65f, -40f);
    [SerializeField] private float smoothSpeed = 15f;

    [SerializeField] private Vector3 fixedRotation = new Vector3 (40f, -45f, 0f);

    private bool hasWarnedOnce = false; // Chỉ warn 1 lần để tránh spam console

    void Start()
    {
        // Tự động tìm player nếu chưa assign
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // Giữ nguyên góc nghiêng camera cố định
        transform.rotation = Quaternion.Euler(fixedRotation);
    }

    void LateUpdate()
    {
        // Tự động tìm player nếu bị mất (vd: sau khi restart hoặc đang load async)
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                hasWarnedOnce = false; // Reset warning khi tìm thấy
            }
            else
            {
                // Chỉ warn 1 lần để tránh spam console mỗi frame
                if (!hasWarnedOnce)
                {
                    Debug.LogWarning("[CameraFollow] Player not found in scene. Waiting for spawn...");
                    hasWarnedOnce = true;
                }
                return; // Không có player thì không follow
            }
        }

        // Di chuyển camera theo player, duy trì offset
        Vector3 targetPosition = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
