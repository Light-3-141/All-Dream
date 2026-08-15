using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

// Put on each of the 4 slot UI GameObjects. Handles showing the icon/quantity
// and lets the player click to select a slot (used by PlayerInteractor for drop).
//
// The "active slot" look is drawn by tinting this slot's own border Image gold
// when selected, instead of covering the whole slot with a solid yellow quad.
[RequireComponent(typeof(Image))]
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    public int slotIndex;               // which InventoryManager.slots[] index this represents
    public Image iconImage;             // child Image that shows the item icon
    public Text quantityText;           // child Text showing stack count (use TMP_Text if you use TextMeshPro)
    public Image selectionHighlight;    // ring Image whose alpha is toggled when selected

    [Header("References")]
    public PlayerInteractor playerInteractor; // to set SelectedSlotIndex on click

    [Header("Selection Look")]
    [Tooltip("Border color used while this slot is the active selection.")]
    public Color selectedBorderColor = new Color(1f, 0.84f, 0f, 1f); // gold frame

    // This script sits on the border GameObject, so its own Image IS the border.
    private Image borderImage;
    private Color defaultBorderColor;

    // Animation state (pop-in + border-color tween).
    private ItemData _showingItem;
    private Coroutine _popRoutine;
    private Coroutine _colorRoutine;

    void Awake()
    {
        // Capture the border color before any selection logic can change it.
        borderImage = GetComponent<Image>();
        if (borderImage != null) defaultBorderColor = borderImage.color;
    }

    void Start()
    {
        Refresh();
        RefreshAllHighlights(); // make sure the correct slot shows as selected on scene start
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        if (playerInteractor != null)
            playerInteractor.OnSelectedSlotChanged += OnSelectedSlotChanged;
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
        if (playerInteractor != null)
            playerInteractor.OnSelectedSlotChanged -= OnSelectedSlotChanged;

        if (_popRoutine != null) { StopCoroutine(_popRoutine); _popRoutine = null; }
        if (_colorRoutine != null) { StopCoroutine(_colorRoutine); _colorRoutine = null; }
    }

    // Fired by PlayerInteractor whenever selection changes (click OR number key).
    private void OnSelectedSlotChanged(int newIndex)
    {
        RefreshAllHighlights();
    }

    public void Refresh()
    {
        var slot = InventoryManager.Instance.slots[slotIndex];

        if (slot.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
            if (quantityText != null) quantityText.text = "";

            _showingItem = null;
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = slot.item.icon;
            }
            if (quantityText != null)
                quantityText.text = (slot.item.isStackable && slot.quantity > 1) ? slot.quantity.ToString() : "";

            // Little "pop" when a NEW item shows up in this slot.
            if (_showingItem != slot.item)
            {
                _showingItem = slot.item;
                PlayPopAnimation();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playerInteractor != null)
        {
            playerInteractor.SelectSlot(slotIndex); // fires OnSelectedSlotChanged -> highlights refresh
        }
    }

    // Called by every slot's Start(), and after any click, so all slots agree
    // on which one is highlighted based on PlayerInteractor.SelectedSlotIndex.
    public void RefreshAllHighlights()
    {
        var allSlots = transform.parent.GetComponentsInChildren<InventorySlotUI>();
        int selected = playerInteractor != null ? playerInteractor.SelectedSlotIndex : -1;

        foreach (var s in allSlots)
        {
            bool isSelected = (s.slotIndex == selected);

            // Legacy overlay (full-slot yellow quad created by the old builder):
            // keep it permanently invisible so it never paints the slot yellow.
            if (s.selectionHighlight != null)
            {
                Color c = s.selectionHighlight.color;
                c.a = 0f;
                s.selectionHighlight.color = c;
            }

            // New look: tint the slot's border frame gold when selected (tweened).
            if (s.borderImage != null)
                s.TweenBorderColor(isSelected ? s.selectedBorderColor : s.defaultBorderColor);
        }
    }

    // ---------- Small animations ----------

    // Bounces the whole slot when a new item lands in it.
    private void PlayPopAnimation()
    {
        if (!isActiveAndEnabled || transform == null) return;
        if (_popRoutine != null) StopCoroutine(_popRoutine);
        _popRoutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        Vector3 startScale = transform.localScale;
        float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float factor = 1f + 0.14f * Mathf.Sin(p * Mathf.PI); // 1 -> 1.14 -> 1
            transform.localScale = startScale * factor;
            yield return null;
        }
        transform.localScale = startScale;
        _popRoutine = null;
    }

    // Smoothly lerps the border color instead of snapping it.
    public void TweenBorderColor(Color target)
    {
        if (borderImage == null || !isActiveAndEnabled) return;
        if (_colorRoutine != null) StopCoroutine(_colorRoutine);
        _colorRoutine = StartCoroutine(TweenBorderColorRoutine(target));
    }

    private IEnumerator TweenBorderColorRoutine(Color target)
    {
        Color start = borderImage.color;
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            borderImage.color = Color.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        borderImage.color = target;
        _colorRoutine = null;
    }
}