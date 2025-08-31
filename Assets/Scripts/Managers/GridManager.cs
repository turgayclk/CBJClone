using System;
using System.Collections.Generic;
using UnityEngine;
public class GridManager : MonoBehaviour
{
    [Header("Level Data")]
    [SerializeField] private LevelData levelData;
    public LevelData LevelData => levelData; // Public getter for level data

    [Header("Prefabs")]
    [SerializeField] private GameObject tilePrefab; // Prefab for the tile
    [SerializeField] private GameObject blockPrefab; // Prefab for the block
    [SerializeField] private GameObject gatePrefab; // Prefab for the gate
    [SerializeField] private GameObject wallPrefab; // Prefab for the wall
    [SerializeField] private GameObject cornerWallPrefab; // Prefab for the corner wall

    [Header("Grid Settings")]
    [SerializeField] private float cellSize; // Size of each cell

    public float CellSize => cellSize;

    private Cell[,] grid;

    Vector2Int blockSize;

    private const float gateYOffset = -0.3f; // Offset for gate Y position
    private const float gateZPos = 0.409f; // Offset for gate Z position

    public void LoadLevel(LevelData data)
    {
        // 1) Önce eskisini tamamen temizle
        ClearLevelScene();

        // 2) Sonra yenisini kur
        this.levelData = data;
        grid = new Cell[data.width, data.height];

        // Grid hücrelerini ve Tile’larý oluþtur
        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                grid[x, y] = new Cell(CellType.Empty, Color.white, new Vector2Int(x, y));

                Vector3 pos = new Vector3(x * cellSize, y * cellSize, 0.5f);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}";
                tile.GetComponent<Renderer>().material.color = Color.white;
            }
        }

        // Ardýndan sýrayla yerleþtir
        PlaceBlocks(data);
        PlaceGates(data);
        PlaceWalls(data);

        Debug.Log("Level Loaded: " + data.name);

        for (int y = data.height - 1; y >= 0; y--) // üstten alta
        {
            string row = "";
            for (int x = 0; x < data.width; x++)
            {
                row += grid[x, y].Type.ToString().PadRight(6) + " ";
            }
            Debug.Log(row);
        }

    }

    private void ClearLevelScene()
    {
        // GridManager altýnda oluþturduðun her þey zaten parent=transform ile instantiate ediliyor.
        // Hepsini güvenli þekilde sil.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            #if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
            #else
            Destroy(child);
             #endif
        }

        grid = null;

    }


    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Grid'in world pozisyonunu çýkar
        Vector3 localPos = worldPos - transform.position;

        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int y = Mathf.RoundToInt(localPos.y / cellSize);

        // Sýnýrlarý clamp et
        x = Mathf.Clamp(x, 0, levelData.width - 1);
        y = Mathf.Clamp(y, 0, levelData.height - 1);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 worldPos = transform.position + new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0);
        return worldPos;
    }

    // GridManager.cs içine ekleyin
    public Vector2Int AnchorTopLeftFromCenterWorld(Vector3 centerWorld, Vector2Int size)
    {
        // world -> local
        Vector3 local = centerWorld - transform.position;

        float gx = local.x / cellSize; // merkez grid-x (float)
        float gy = local.y / cellSize; // merkez grid-y (float)

        float halfW = (size.x - 1) / 2f;
        float halfH = (size.y - 1) / 2f;

        // Merkezden sol-üst anchor hücresini bul
        int ax = Mathf.RoundToInt(gx - halfW);
        int ay = Mathf.RoundToInt(gy + halfH);

        // Taþmayý önle (sol-üst anchor ayarlanýyor)
        ax = Mathf.Clamp(ax, 0, levelData.width - size.x);
        ay = Mathf.Clamp(ay, size.y - 1, levelData.height - 1);

        return new Vector2Int(ax, ay);
    }

    public Vector3 CenterWorldFromAnchorTopLeft(Vector2Int anchorTopLeft, Vector2Int size)
    {
        float halfW = (size.x - 1) / 2f;
        float halfH = (size.y - 1) / 2f;

        float cx = anchorTopLeft.x + halfW;   // merkez grid-x
        float cy = anchorTopLeft.y - halfH;   // merkez grid-y (sol-üstten aþaðýya doðru)

        return transform.position + new Vector3(cx * cellSize, cy * cellSize, 0f);
    }

    public bool IsCellFree(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= levelData.width || pos.y < 0 || pos.y >= levelData.height)
            return false; // Grid dýþý
        return grid[pos.x, pos.y].Type == CellType.Empty || grid[pos.x, pos.y].Type == CellType.Gate;
    }

    public Cell GetCell(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= levelData.width || pos.y < 0 || pos.y >= levelData.height)
            return null;
        return grid[pos.x, pos.y];
    }

    public void SetBlock(Vector2Int pos, Color color)
    {
        if (pos.x < 0 || pos.x >= levelData.width || pos.y < 0 || pos.y >= levelData.height)
            return;

        grid[pos.x, pos.y].Type = CellType.Block;
        grid[pos.x, pos.y].BlockColor = color;
    }

    public void ClearCell(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= levelData.width || pos.y < 0 || pos.y >= levelData.height)
            return;

        grid[pos.x, pos.y].Type = CellType.Empty;
        grid[pos.x, pos.y].BlockColor = Color.white;
    }

    public GameObject GetGateAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= levelData.width || pos.y < 0 || pos.y >= levelData.height)
            return null;

        if (grid[pos.x, pos.y].Type == CellType.Gate)
            return grid[pos.x, pos.y].GateObj;

        return null;
    }

    public void ClearBlock(Vector2Int startPos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                grid[startPos.x + x, startPos.y + y].Type = CellType.Empty;
            }
        }
    }

    public void SetBlockArea(Vector2Int startPos, Vector2Int size, Color color)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                grid[startPos.x + x, startPos.y + y].Type = CellType.Block;
                grid[startPos.x + x, startPos.y + y].BlockColor = color;
            }
        }
    }

    // GridManager.cs içine ekle
    public int GetRemainingBlockCount()
    {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");
        return blocks.Length;
    }

    private void PlaceBlocks(LevelData data)
    {
        foreach (var block in data.blocks)
        {
            // 1?? Önce sol-üst anchor hesapla
            Vector2Int anchorTopLeft = block.position; // block.position = sol-üst hücre gibi düþün
            Vector3 centerPos = CenterWorldFromAnchorTopLeft(anchorTopLeft, block.size);

            // 2?? Blok objesini oluþtur
            GameObject obj = Instantiate(block.prefab, centerPos, Quaternion.identity, transform);
            obj.GetComponent<Renderer>().material.color = block.color;

            // 3?? Görsel boyutu ayarla
            obj.transform.localScale = new Vector3(block.size.x * cellSize, block.size.y * cellSize, 1);

            blockSize = block.size;

            // 4?? Grid hücrelerini doldur
            for (int x = 0; x < block.size.x; x++)
            {
                for (int y = 0; y < block.size.y; y++)
                {
                    Vector2Int cellPos = anchorTopLeft + new Vector2Int(x, -y); // sol-üstten aþaðýya doðru
                    grid[cellPos.x, cellPos.y].Type = CellType.Block;
                    grid[cellPos.x, cellPos.y].BlockColor = block.color;
                }
            }

            // opsiyonel: debug log
            Debug.Log($"Block placed: pos={block.position}, size={block.size}, center={centerPos}");
        }
    }

    private void PlaceGates(LevelData data)
    {
        foreach (var gate in data.gates)
        {
            bool isBottom = gate.position.y == 0;
            bool isTop = gate.position.y + gate.size.y - 1 == data.height - 1;
            bool isLeft = gate.position.x == 0;
            bool isRight = gate.position.x + gate.size.x - 1 == data.width - 1;

            bool isHorizontal = isBottom || isTop;
            bool isVertical = isLeft || isRight;

            if (!isHorizontal && !isVertical)
            {
                Debug.LogWarning($"Geçersiz Gate pozisyonu: {gate.position}. Gate sadece kenarlara yerleþtirilebilir!");
                continue;
            }

            // Kapsadýðý hücre aralýðý
            int startX = gate.position.x;
            int startY = gate.position.y;
            int endX = startX + gate.size.x - 1;
            int endY = startY + gate.size.y - 1;

            // Hücreleri iþaretle
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    grid[x, y].Type = CellType.Gate;
                    grid[x, y].BlockColor = gate.color;
                }
            }

            // Ortasýný hesapla
            float cx = (startX + endX) / 2f;
            float cy = (startY + endY) / 2f;
            Vector3 worldPos = new Vector3(cx * cellSize, cy * cellSize, gateZPos);

            // Offset gerekirse buraya eklersin
            if (isLeft) worldPos.x -= cellSize * 0f;
            if (isRight) worldPos.x += cellSize * 0f;
            if (isBottom) worldPos.y -= cellSize * 0f;
            if (isTop) worldPos.y += cellSize * 0f;

            // Gate objesi oluþtur
            GameObject obj = Instantiate(gatePrefab, worldPos, Quaternion.identity, transform);

            float gateScaleX = gate.size.x * cellSize;
            float gateScaleY = gate.size.y * cellSize;

            if (isHorizontal)
                obj.transform.localScale = new Vector3(gateScaleX, 1f, 0.12672f);
            else if (isVertical)
                obj.transform.localScale = new Vector3(1f, gateScaleY, 0.12672f);

            obj.GetComponent<Renderer>().material.color = gate.color;

            // ?? Particle System Velocity over Lifetime ayarý
            ParticleSystem ps = obj.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;

                // Default sýfýr
                vel.x = new ParticleSystem.MinMaxCurve(0f);
                vel.y = new ParticleSystem.MinMaxCurve(0f);
                vel.z = new ParticleSystem.MinMaxCurve(0f);

                if (isLeft) vel.x = new ParticleSystem.MinMaxCurve(-3f);
                if (isRight) vel.x = new ParticleSystem.MinMaxCurve(3f);
                if (isBottom) vel.y = new ParticleSystem.MinMaxCurve(-3f);
                if (isTop) vel.y = new ParticleSystem.MinMaxCurve(3f);
            }

            // Hücrelere referans ata
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    grid[x, y].GateObj = obj;
                }
            }

            Debug.Log($"Gate oluþturuldu -> pos: {gate.position}, size: {gate.size}, worldPos: {worldPos}");
        }
    }

    private void PlaceWalls(LevelData data)
    {
        int w = data.width;
        int h = data.height;

        // --- Köþeler ---
        Vector3 bottomLeft = new Vector3(0, 0, gateZPos);
        Vector3 bottomRight = new Vector3((w - 1) * cellSize, 0, gateZPos);
        Vector3 topLeft = new Vector3(0, (h - 1) * cellSize, gateZPos);
        Vector3 topRight = new Vector3((w - 1) * cellSize, (h - 1) * cellSize, gateZPos);

        Instantiate(cornerWallPrefab, bottomLeft, Quaternion.identity, transform);
        grid[0, 0].Type = CellType.Wall;

        Instantiate(cornerWallPrefab, bottomRight, Quaternion.identity, transform);
        grid[w - 1, 0].Type = CellType.Wall;

        Instantiate(cornerWallPrefab, topLeft, Quaternion.identity, transform);
        grid[0, h - 1].Type = CellType.Wall;

        Instantiate(cornerWallPrefab, topRight, Quaternion.identity, transform);
        grid[w - 1, h - 1].Type = CellType.Wall;

        // --- Alt ve üst kenar ---
        for (int x = 1; x < w - 1; x++)
        {
            // alt kenar (y=0)
            if (grid[x, 0].Type != CellType.Gate)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, gateZPos);
                Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                grid[x, 0].Type = CellType.Wall;
            }

            // üst kenar (y=h-1)
            if (grid[x, h - 1].Type != CellType.Gate)
            {
                Vector3 pos = new Vector3(x * cellSize, (h - 1) * cellSize, gateZPos);
                Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                grid[x, h - 1].Type = CellType.Wall;
            }
        }

        // --- Sol ve sað kenar ---
        for (int y = 1; y < h - 1; y++)
        {
            // sol kenar (x=0)
            if (grid[0, y].Type != CellType.Gate)
            {
                Vector3 pos = new Vector3(0, y * cellSize, gateZPos);
                Instantiate(wallPrefab, pos, Quaternion.Euler(0, 0, 90), transform);
                grid[0, y].Type = CellType.Wall;
            }

            // sað kenar (x=w-1)
            if (grid[w - 1, y].Type != CellType.Gate)
            {
                Vector3 pos = new Vector3((w - 1) * cellSize, y * cellSize, gateZPos);
                Instantiate(wallPrefab, pos, Quaternion.Euler(0, 0, 90), transform);
                grid[w - 1, y].Type = CellType.Wall;
            }
        }
    }
}
