using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "LevelData", order = 1)]
public class LevelData : ScriptableObject
{
    public int width = 5;
    public int height = 5;

    public List<BlockData> blocks = new List<BlockData>();
    public List<GateData> gates = new List<GateData>();
}

[System.Serializable]
public class BlockData
{
    public Vector2Int position; // Baþlangýç hücresi (sol-alt hücre gibi düþünülebilir)
    public Vector2Int size = Vector2Int.one; // Blok kaç hücre kaplýyor (ör: 2x2, 1x3)
    public Color color;
    public GameObject prefab;
}

[System.Serializable]
public class GateData
{
    public Vector2Int position; // sol-alt
    public Vector2Int size = Vector2Int.one;
    public Color color;
}

