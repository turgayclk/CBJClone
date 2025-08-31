using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlockController : MonoBehaviour
{
    private GridManager gridManager;

    private Vector3 startMousePos;
    private Vector3 startBlockPos;
    private Vector2Int startGridPos;
    private bool isDragging = false;

    private float fixedZ;

    private Cell currentCell;
    private Cell targetCell;

    public Cell TargetCell => targetCell;
    public static event System.Action<BlockController, GateSystem> GateAction;

    private Vector2Int currentAnchorTopLeft;  // aktif sol-üst anchor
    private Vector2Int size;

    const float snapDuration = 0.5f; // Tween süresi
    const float moveLerpDuration = 35f; // Tween süresi

    private Vector3[] cornerOffsets; // Ýç içe girmeme kontrolü için köþe offset'leri

    [SerializeField] private GameObject outlineQuad; // block prefab’ýnda child quad

    private Vector2Int lastCheckedGridPos;
    private Renderer rend;


    private void Start()
    {
        gridManager = GameObject.FindFirstObjectByType<GridManager>();
        outlineQuad = transform.Find("OutlineQuad")?.gameObject;
        rend = GetComponent<Renderer>(); // <-- eklendi

        fixedZ = transform.position.z;

        if (outlineQuad != null)
            outlineQuad.SetActive(false);

        size = new Vector2Int(
            Mathf.RoundToInt(transform.localScale.x),
            Mathf.RoundToInt(transform.localScale.y)
        );

        if (size.x > 1 || size.y > 1)
        {
            currentAnchorTopLeft = gridManager.AnchorTopLeftFromCenterWorld(transform.position, size);
            Vector3 snappedCenter = gridManager.CenterWorldFromAnchorTopLeft(currentAnchorTopLeft, size);
            snappedCenter.z = fixedZ;
            transform.position = snappedCenter;

            startBlockPos = snappedCenter;
            startGridPos = gridManager.WorldToGrid(snappedCenter);
        }

        Vector3 halfSize = GetComponent<Renderer>().bounds.extents;
        float padding = 0.05f;

        cornerOffsets = new Vector3[]
        {
        new Vector3(-halfSize.x + padding, -halfSize.y + padding, 0),
        new Vector3( halfSize.x - padding, -halfSize.y + padding, 0),
        new Vector3(-halfSize.x + padding,  halfSize.y - padding, 0),
        new Vector3( halfSize.x - padding,  halfSize.y - padding, 0)
        };
    }

    private void OnMouseDown()
    {
        currentCell = gridManager.GetCell(gridManager.WorldToGrid(transform.position));

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        startMousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        startBlockPos = transform.position;
        startGridPos = gridManager.WorldToGrid(transform.position);

        isDragging = true;

        if (outlineQuad != null)
            outlineQuad.SetActive(true); // týklanýnca aç
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        // Multi-hücre kontrolü
        if (transform.localScale.x > 1 || transform.localScale.y > 1)
        {
            HandleMultiCellDragSmooth();
            return;
        }

        // Tek hücreli blok
        SingleCellDragSmooth();
    }

    private void OnMouseUp()
    {
        isDragging = false;

        if (outlineQuad != null)
            outlineQuad.SetActive(false); // býrakýnca kapan

        if (size.x > 1 || size.y > 1) // Multi-cell blok
        {
            // Eski alaný temizle
            Vector2Int oldBottomLeft = new Vector2Int(currentAnchorTopLeft.x, currentAnchorTopLeft.y - size.y + 1);
            gridManager.ClearBlock(oldBottomLeft, size);

            // Snap to nearest anchor
            Vector2Int snapAnchor = gridManager.AnchorTopLeftFromCenterWorld(transform.position, size);
            Vector3 snappedCenter = gridManager.CenterWorldFromAnchorTopLeft(snapAnchor, size);
            snappedCenter.z = fixedZ;

            transform.DOMove(snappedCenter, snapDuration).SetEase(Ease.OutQuad);

            // Yeni alaný doldur
            Vector2Int newBottomLeft = new Vector2Int(snapAnchor.x, snapAnchor.y - size.y + 1);
            gridManager.SetBlockArea(newBottomLeft, size, GetComponent<Renderer>().material.color);

            currentAnchorTopLeft = snapAnchor;

            Debug.Log("[SNAP] Multi-cell block snapped to " + snapAnchor);
        }
        else // Single-cell
        {
            if (currentCell != null && targetCell != null && targetCell.Type != CellType.Gate)
                gridManager.ClearCell(currentCell.GridPos);

            Vector2Int nearestGridPos = gridManager.WorldToGrid(transform.position);
            nearestGridPos.x = Mathf.Clamp(nearestGridPos.x, 0, gridManager.LevelData.width - 1);
            nearestGridPos.y = Mathf.Clamp(nearestGridPos.y, 0, gridManager.LevelData.height - 1);

            Vector3 snappedPos = gridManager.GridToWorld(nearestGridPos);
            snappedPos.z = fixedZ;

            transform.DOMove(snappedPos, snapDuration).SetEase(Ease.OutQuad);

            gridManager.SetBlock(nearestGridPos, GetComponent<Renderer>().material.color);
            currentCell = gridManager.GetCell(nearestGridPos);

            Debug.Log("[SNAP] Single-cell block snapped to " + nearestGridPos);
        }
    }

    private bool IsPosBlocked(Vector3 worldPos)
    {
        foreach (var offset in cornerOffsets)
        {
            Vector2Int cell = gridManager.WorldToGrid(worldPos + offset);
            Cell c = gridManager.GetCell(cell);

            if (c == null)
            {
                Debug.Log($"GATE LOG {cell} is null ? blocked");
                return true; // grid dýþý
            }

            // Kendi baþlangýç hücresini atla
            if (cell == startGridPos)
            {
                Debug.Log($"GATE LOG {cell} skipped: startGridPos");
                continue;
            }

            // Ayný renkli Gate ? geçiþ serbest (cornerOffsets kontrolünde engel sayma)
            if (c.Type == CellType.Gate && ColorsAreEqual(c.BlockColor, rend.material.color))
            {
                Debug.Log($"GATE LOG {cell} is Gate with same color ? free");
                continue; // engel sayma
            }

            // Wall veya Block engel
            if (c.Type == CellType.Wall || c.Type == CellType.Block)
            {
                Debug.Log($"GATE LOG {cell} is {c.Type} ? blocked");
                return true;
            }

            // Farklý renkli Gate ? engel
            if (c.Type == CellType.Gate)
            {
                Debug.Log($"GATE LOG {cell} is Gate with different color ? blocked");
                return true;
            }

            Debug.Log($"GATE LOG {cell} is {c.Type} ? free");
        }

        return false; // tüm köþeler serbest, hareket mümkün
    }
    private void SingleCellDragSmooth()
    {
        // Mouse ? hedef dünya pozisyonu
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector3 currentMousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3 intendedPos = startBlockPos + (currentMousePos - startMousePos);
        intendedPos.z = fixedZ;

        // Eksene göre ayrý dene: önce X, sonra Y (engel varsa o ekseni iptal)
        Vector3 pos = transform.position;

        // --- X hareketini dene ---
        Vector3 candX = new Vector3(intendedPos.x, pos.y, fixedZ);
        if (!IsPosBlocked(candX))
        {
            pos.x = Mathf.Lerp(pos.x, intendedPos.x, Time.deltaTime * moveLerpDuration);
        }

        // --- Y hareketini dene ---
        Vector3 candY = new Vector3(pos.x, intendedPos.y, fixedZ); // X sonucu üzerinden Y dene
        if (!IsPosBlocked(candY))
        {
            pos.y = Mathf.Lerp(pos.y, intendedPos.y, Time.deltaTime * moveLerpDuration);
        }

        transform.position = pos;

        // Grid hücresi deðiþtiyse gate/target kontrolü
        Vector2Int curGrid = gridManager.WorldToGrid(transform.position);
        if (curGrid != lastCheckedGridPos)
        {
            lastCheckedGridPos = curGrid;

            Cell c = gridManager.GetCell(curGrid);
            targetCell = c;

            // Ayný renk gate -> yok ol
            if (c != null && c.Type == CellType.Gate &&
                ColorsAreEqual(c.BlockColor, rend.material.color))
            {
                var gateObj = gridManager.GetGateAt(c.GridPos);
                var gateSystem = gateObj?.GetComponent<GateSystem>();
                GateAction?.Invoke(this, gateSystem);

                // Baþlangýç hücresini de temizle (tek hücre ghost fix)
                if (currentCell != null) gridManager.ClearCell(currentCell.GridPos);

                // Gate hücresini temizle (opsiyonel: gate de boþalsýn)
                gridManager.ClearCell(c.GridPos);

                isDragging = false;
                transform.DOScale(Vector3.zero, 0.5f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(gameObject));
            }
        }
    }

    // ---- Multi-cell sistemi ----
    private void HandleMultiCellDragSmooth()
    {
        // Mouse world hareketi
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector3 currentMousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector3 worldDelta = currentMousePos - startMousePos;
        Vector3 intendedPos = startBlockPos + worldDelta;
        intendedPos.z = fixedZ;

        // Grid anchor hesapla
        Vector2Int nextAnchor = gridManager.AnchorTopLeftFromCenterWorld(intendedPos, size);

        // Aradaki yol kontrolü
        if (IsMultiCellPathBlocked(currentAnchorTopLeft, nextAnchor, size))
            return;

        // Serbest hareket (smooth)
        transform.position = Vector3.Lerp(transform.position, intendedPos, Time.deltaTime * 20f);

        bool canPlace = true;

        for (int x = 0; x < size.x && canPlace; x++)
        {
            for (int y = 0; y < size.y && canPlace; y++)
            {
                Vector2Int cellPos = new Vector2Int(nextAnchor.x + x, nextAnchor.y - y);
                Cell c = gridManager.GetCell(cellPos);

                if (c == null)
                {
                    canPlace = false;
                    break;
                }

                // ? Kendi eski alanýný sayma
                bool isCurrentBlockCell =
                    cellPos.x >= currentAnchorTopLeft.x && cellPos.x < currentAnchorTopLeft.x + size.x &&
                    cellPos.y <= currentAnchorTopLeft.y && cellPos.y > currentAnchorTopLeft.y - size.y;

                if (isCurrentBlockCell)
                    continue; // kendi hücresini engel sayma

                // Gate kontrol
                if (c.Type == CellType.Gate)
                {
                    if (ColorsAreEqual(c.BlockColor, GetComponent<Renderer>().material.color))
                    {
                        var gateObj = gridManager.GetGateAt(c.GridPos);
                        var gateSystem = gateObj?.GetComponent<GateSystem>();
                        GateAction?.Invoke(this, gateSystem);

                        isDragging = false;
                        transform.DOScale(Vector3.zero, 0.5f)
                            .SetEase(Ease.InBack)
                            .OnComplete(() => Destroy(gameObject));

                        return;
                    }
                    else canPlace = false;
                }

                // Engelleyen blok veya duvar
                if (c.Type == CellType.Block || c.Type == CellType.Wall)
                    canPlace = false;
            }
        }

        if (!canPlace) return;

        // Grid’i güncelle (eskiyi temizle, yeniyi doldur)
        Vector2Int oldBottomLeftNormal = new Vector2Int(currentAnchorTopLeft.x, currentAnchorTopLeft.y - size.y + 1);
        gridManager.ClearBlock(oldBottomLeftNormal, size);

        Vector2Int newBottomLeft = new Vector2Int(nextAnchor.x, nextAnchor.y - size.y + 1);
        gridManager.SetBlockArea(newBottomLeft, size, GetComponent<Renderer>().material.color);

        currentAnchorTopLeft = nextAnchor;
    }

    private void OnDestroy()
    {
        // Bu blok grid’den siliniyorsa, hücreleri boþalt
        Vector2Int bottomLeft = new Vector2Int(currentAnchorTopLeft.x, currentAnchorTopLeft.y - size.y + 1);
        gridManager.ClearBlock(bottomLeft, size);
    }

    // Multi-cell için aradaki yol kontrolü
    private bool IsMultiCellPathBlocked(Vector2Int currentAnchor, Vector2Int targetAnchor, Vector2Int size)
    {
        Vector2Int delta = targetAnchor - currentAnchor;

        // Yalnýzca tek eksen hareket (ya x ya y)
        if (delta.x != 0 && delta.y != 0)
            return true;

        Vector2Int step = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
        int steps = Math.Max(Math.Abs(delta.x), Math.Abs(delta.y));

        Vector2Int currentBottomLeft = new Vector2Int(currentAnchor.x, currentAnchor.y - size.y + 1);

        for (int s = 1; s <= steps; s++)
        {
            Vector2Int checkAnchor = currentAnchor + step * s;
            Vector2Int checkBottomLeft = new Vector2Int(checkAnchor.x, checkAnchor.y - size.y + 1);

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(checkBottomLeft.x + x, checkBottomLeft.y + y);

                    // Kendi alanýmýzdaysa geç
                    if (IsInsideRect(pos, currentBottomLeft, size))
                        continue;

                    Cell c = gridManager.GetCell(pos);
                    if (c != null)
                    {
                        // Block veya Wall engeli
                        if (c.Type == CellType.Block || c.Type == CellType.Wall)
                            return true;

                        // Gate, ama farklý renk ise engel
                        if (c.Type == CellType.Gate && !ColorsAreEqual(c.BlockColor, GetComponent<Renderer>().material.color))
                            return true;
                    }
                }
            }
        }

        return false;
    }

    // Bir nokta dikdörtgenin içinde mi (bottom-left + size)
    private bool IsInsideRect(Vector2Int pos, Vector2Int bottomLeft, Vector2Int size)
    {
        return pos.x >= bottomLeft.x && pos.x < bottomLeft.x + size.x &&
               pos.y >= bottomLeft.y && pos.y < bottomLeft.y + size.y;
    }

    private bool ColorsAreEqual(Color a, Color b, float tolerance = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance &&
               Mathf.Abs(a.a - b.a) < tolerance;
    }
}
