using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float baseX = 2f;       // Ba�lang�� X
    [SerializeField] private float baseY = 2.5f;     // Ba�lang�� Y
    [SerializeField] private float baseZ = -12f;     // Ba�lang�� Z
    [SerializeField] private float xPerWidth = 0.5f; // Her ekstra s�tun i�in X art���
    [SerializeField] private float zPerCell = 2f;   // Her ekstra h�cre i�in Z art���

    private void Start()
    {
        SetStartCenterCamera();
    }

    private void OnEnable()
    {
        GameManager.AddCameraCenterPos += SetCenterCamera;
    }

    private void OnDisable()
    {
        GameManager.AddCameraCenterPos -= SetCenterCamera;
    }

    private void SetStartCenterCamera()
    {
        // Start kamera pozisyonu
        float posX = baseX;
        float posY = baseY; 
        float posZ = baseZ;

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
