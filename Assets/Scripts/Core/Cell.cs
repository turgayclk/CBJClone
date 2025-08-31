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
    public CellType Type; // H�cre tipi
    public Color BlockColor; // Blok rengi
    public Vector2Int GridPos; // H�crenin grid �zerindeki pozisyonu
    public GameObject GateObj; // Gate prefab referans�

    public Cell(CellType type, Color blockColor, Vector2Int gridPos)
    {
        Type = type;
        BlockColor = blockColor;
        GridPos = gridPos;
        GateObj = null;
    }
}
