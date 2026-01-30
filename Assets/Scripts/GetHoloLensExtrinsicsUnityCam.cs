using UnityEngine;
using Microsoft.MixedReality.Toolkit;

public class GetHoloLensExtrinsicsUnityCam : MonoBehaviour
{
    void Update()
    {
        if (Camera.main == null)
            return;

        // 1. Camera extrinsics (world pose)
        Matrix4x4 cameraToWorld = Camera.main.cameraToWorldMatrix;

        // 2. Projection matrix (contains fx, fy, cx, cy)
        Matrix4x4 projection = Camera.main.projectionMatrix;

        // 3. Extract intrinsics K from projection matrix
        Matrix4x4 K = ExtractIntrinsics(projection);

        // 4. Log results
        Debug.Log("=== HoloLens Camera Extrinsics (Approach 2) ===");
        Debug.Log("Camera To World Matrix:\n" + cameraToWorld);

        Vector3 position = cameraToWorld.GetColumn(3);
        Quaternion rotation = Quaternion.LookRotation(
            cameraToWorld.GetColumn(2),
            cameraToWorld.GetColumn(1)
        );

        Debug.Log($"Position: {position}");
        Debug.Log($"Rotation: {rotation}");

        Debug.Log("Projection Matrix:\n" + projection);
        Debug.Log("Intrinsics (K):\n" + K);
    }

    // --- Extract fx, fy, cx, cy from Unity projection matrix ---
    Matrix4x4 ExtractIntrinsics(Matrix4x4 P)
    {
        float fx = P[0, 0];
        float fy = P[1, 1];
        float cx = P[0, 2];
        float cy = P[1, 2];

        Matrix4x4 K = Matrix4x4.zero;
        K[0, 0] = fx;
        K[1, 1] = fy;
        K[0, 2] = cx;
        K[1, 2] = cy;
        K[2, 2] = 1;

        return K;
    }
}
