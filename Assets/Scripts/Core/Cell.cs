using UnityEngine;

public enum CellType
{
    Empty,
    Block,
    Gate,
    Wall,
}

[System.Serializable]
public class Cell
{
    public CellType Type; // Hücre tipi
    public Color BlockColor; // Blok rengi
    public Vector2Int GridPos; // Hücrenin grid üzerindeki pozisyonu
    public GameObject GateObj; // Gate prefab referansý

    public Cell(CellType type, Color blockColor, Vector2Int gridPos)
    {
        Type = type;
        BlockColor = blockColor;
        GridPos = gridPos;
        GateObj = null;
    }
}
