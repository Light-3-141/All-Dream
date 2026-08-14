using UnityEngine;
using UnityEngine.UI;

// Editor-time helper: right-click this component in the Inspector and choose
// "Build Inventory UI" (Context Menu) to auto-generate the 4-slot bar matching
// the reference image: dark near-black slots with a thick gray border, in a row.
//
// Usage:
// 1. Create an empty GameObject under your Canvas, name it "InventoryUI".
// 2. Add this component to it.
// 3. Right-click the component header -> "Build Inventory UI".
// 4. It creates 4 slots wired to InventoryManager (assumes InventoryManager exists in scene).
public class InventoryUIBuilder : MonoBehaviour
{
    [Header("Layout")]
    public int slotCountToBuild = 4;
    public Vector2 slotSize = new Vector2(90, 90);
    public float spacing = 6f;
    public float borderThickness = 6f;

    [Header("Colors (matches reference image)")]
    public Color borderColor = new Color(0.55f, 0.55f, 0.55f); // gray border
    public Color slotColor = new Color(0.07f, 0.07f, 0.07f);   // near-black fill

    [Header("Player Wiring")]
    public PlayerInteractor playerInteractor;

    [ContextMenu("Build Inventory UI")]
    public void BuildUI()
    {
        // Clear existing children first.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

        var rowLayout = gameObject.GetComponent<HorizontalLayoutGroup>();
        if (rowLayout == null) rowLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = spacing;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;

        var fitter = gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < slotCountToBuild; i++)
        {
            CreateSlot(i);
        }
    }

    private void CreateSlot(int index)
    {
        // --- Border (outer) ---
        GameObject borderGO = new GameObject($"Slot_{index}_Border", typeof(RectTransform), typeof(Image));
        borderGO.transform.SetParent(transform, false);
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.sizeDelta = slotSize;
        Image borderImg = borderGO.GetComponent<Image>();
        borderImg.color = borderColor;

        var le = borderGO.AddComponent<LayoutElement>();
        le.preferredWidth = slotSize.x;
        le.preferredHeight = slotSize.y;

        // --- Fill (inner dark slot) ---
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(borderGO.transform, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(borderThickness, borderThickness);
        fillRT.offsetMax = new Vector2(-borderThickness, -borderThickness);
        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.color = slotColor;

        // --- Icon ---
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(fillGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.enabled = false;
        iconImg.preserveAspect = true;

        // --- Quantity Text ---
        GameObject textGO = new GameObject("Quantity", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(fillGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.55f, 0.05f);
        textRT.anchorMax = new Vector2(0.95f, 0.35f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        Text qtyText = textGO.GetComponent<Text>();
        qtyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        qtyText.alignment = TextAnchor.LowerRight;
        qtyText.color = Color.white;
        qtyText.fontSize = 20;
        qtyText.text = "";

        // --- Selection highlight: a ring drawn on top of the border, toggled
        // on/off via alpha. This is what visually shows "this is the active slot".
        GameObject highlightGO = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
        highlightGO.transform.SetParent(borderGO.transform, false);
        RectTransform highlightRT = highlightGO.GetComponent<RectTransform>();
        highlightRT.anchorMin = Vector2.zero;
        highlightRT.anchorMax = Vector2.one;
        highlightRT.offsetMin = new Vector2(-4, -4); // slightly larger than the border = visible ring
        highlightRT.offsetMax = new Vector2(4, 4);
        highlightGO.transform.SetAsLastSibling(); // draw on top of fill/icon
        Image highlightImg = highlightGO.GetComponent<Image>();
        highlightImg.color = new Color(1f, 0.85f, 0f, 0f); // transparent yellow = "off" by default
        highlightImg.raycastTarget = false; // never block clicks meant for the slot

        // --- Wire up the InventorySlotUI script on the border object ---
        InventorySlotUI slotUI = borderGO.AddComponent<InventorySlotUI>();
        slotUI.slotIndex = index;
        slotUI.iconImage = iconImg;
        slotUI.quantityText = qtyText;
        slotUI.playerInteractor = playerInteractor;
        slotUI.selectionHighlight = highlightImg;

        // Make border clickable (needs raycast target true, which Image defaults to).
        borderImg.raycastTarget = true;
    }
}