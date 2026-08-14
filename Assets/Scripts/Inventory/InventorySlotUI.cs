using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Put on each of the 4 slot UI GameObjects. Handles showing the icon/quantity
// and lets the player click to select a slot (used by PlayerInteractor for drop).
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
            if (s.selectionHighlight == null) continue;
            bool isSelected = (s.slotIndex == selected);
            Color c = s.selectionHighlight.color;
            c.a = isSelected ? 1f : 0f;
            s.selectionHighlight.color = c;
        }
    }
}