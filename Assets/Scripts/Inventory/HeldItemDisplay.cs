using UnityEngine;
using UnityEngine.UI;

// Shows the item in the currently SELECTED slot as an icon on screen,
// simulating "this is what you're holding". Put this on its own UI GameObject
// (e.g. under Canvas, positioned bottom-center or wherever you want the
// held item to appear) with an Image component already on it, or let this
// script find/add one.
//
// Setup:
// 1. Create empty GameObject under Canvas, name it "HeldItemDisplay".
// 2. Add Component -> Image (leave sprite empty).
// 3. Add Component -> HeldItemDisplay (this script).
// 4. Drag your PlayerInteractor into the field.
// 5. Position/size the RectTransform wherever you want the held item shown.
[RequireComponent(typeof(Image))]
public class HeldItemDisplay : MonoBehaviour
{
    [Header("References")]
    public PlayerInteractor playerInteractor;

    private Image displayImage;

    void Awake()
    {
        displayImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        if (playerInteractor != null)
            playerInteractor.OnSelectedSlotChanged += OnSelectedSlotChanged;

        Refresh();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
        if (playerInteractor != null)
            playerInteractor.OnSelectedSlotChanged -= OnSelectedSlotChanged;
    }

    private void OnSelectedSlotChanged(int newIndex) => Refresh();

    private void Refresh()
    {
        if (InventoryManager.Instance == null || playerInteractor == null)
        {
            displayImage.enabled = false;
            return;
        }

        int index = playerInteractor.SelectedSlotIndex;
        if (index < 0 || InventoryManager.Instance.IsSlotEmpty(index))
        {
            displayImage.enabled = false;
            displayImage.sprite = null;
            return;
        }

        var slot = InventoryManager.Instance.slots[index];
        displayImage.enabled = true;
        displayImage.sprite = slot.item.icon;
        displayImage.preserveAspect = true;
    }
}
