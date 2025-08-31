using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float baseX = 2f;       // Baþlangýç X
    [SerializeField] private float baseY = 2.5f;     // Baþlangýç Y
    [SerializeField] private float baseZ = -12f;     // Baþlangýç Z
    [SerializeField] private float xPerWidth = 0.5f; // Her ekstra sütun için X artýþý
    [SerializeField] private float zPerCell = 2f;   // Her ekstra hücre için Z artýþý

    private void Start()
    {
        SetStartCenterCamera();
    }

    private void OnEnable()
    {
        GameManager.AddCameraCenterPos += SetCenterCamera;
        GameManager.ResetCameraPos += SetStartCenterCamera;
    }

    private void OnDisable()
    {
        GameManager.AddCameraCenterPos -= SetCenterCamera;
        GameManager.ResetCameraPos -= SetStartCenterCamera;
    }

    private void SetStartCenterCamera()
    {
        // Start kamera pozisyonu
        float posX = 2f;
        float posY = 2.5f; 
        float posZ = -12f;

        transform.position = new Vector3(posX, posY, posZ);
    }

    private void SetCenterCamera()
    {
        // X = baseX + (gridWidth - 1) * xPerWidth
        float posX = baseX + xPerWidth;
        float posY = baseY; // Sabit
        float posZ = baseZ + -zPerCell;

        transform.position = new Vector3(posX, posY, posZ);
    }
}
