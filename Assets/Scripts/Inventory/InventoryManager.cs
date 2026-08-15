using UnityEngine;
using System;

// Central inventory brain. Attach to a persistent "Player" or "GameManager" object.
// Slot index 0 = HAND (left-most slot in your UI image). Only ever empty or holding
// nothing picked up passively — reserved as an "active/equipped" slot.
// Slot indices 1,2,3 = the three general storage slots (shown as boxes 2,3,4 in UI).
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("Total slots including the hand. Default 4 to match a hand + 3 storage layout.")]
    public int slotCount = 4;

    [Tooltip("Index of the hand slot. Leave at 0 (left-most).")]
    public int handSlotIndex = 0;

    public InventorySlot[] slots;

    // Fired whenever inventory contents change, so UI can refresh.
    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();
    }

    // ---------- Public API ----------

    /// <summary>
    /// Tries to add an item picked up from the world into storage slots (never the hand slot).
    /// Returns true if it was added successfully.
    /// </summary>
    public bool AddItem(ItemData item, int amount, GameObject sourcePrefab)
    {
        if (item == null || amount <= 0) return false;

        // 1. Try to stack onto an existing matching stack in storage slots.
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (i == handSlotIndex) continue; // never auto-stack into hand
                var slot = slots[i];
                if (!slot.IsEmpty && slot.item == item && slot.quantity < item.maxStackSize)
                {
                    int space = item.maxStackSize - slot.quantity;
                    int toAdd = Mathf.Min(space, amount);
                    slot.quantity += toAdd;
                    amount -= toAdd;

                    if (amount <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        // 2. Put remainder into the first empty storage slot.
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == handSlotIndex) continue; // hand stays reserved
            if (slots[i].IsEmpty)
            {
                slots[i].Set(item, amount, sourcePrefab);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // 3. No room.
        OnInventoryChanged?.Invoke();
        return false;
    }

    /// <summary>
    /// Removes one full stack from a slot (used for dropping). Returns the removed data.
    /// </summary>
    public InventorySlot RemoveSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        var slot = slots[index];
        if (slot.IsEmpty) return null;

        var copy = new InventorySlot();
        copy.Set(slot.item, slot.quantity, slot.sourcePrefab);

        slot.Clear();
        OnInventoryChanged?.Invoke();
        return copy;
    }

    /// <summary>
    /// Removes a specific quantity from a slot. Returns how much was actually removed.
    /// </summary>
    public int RemoveFromSlot(int index, int amount)
    {
        if (index < 0 || index >= slots.Length) return 0;
        var slot = slots[index];
        if (slot.IsEmpty) return 0;

        int removed = Mathf.Min(amount, slot.quantity);
        slot.quantity -= removed;
        if (slot.quantity <= 0) slot.Clear();

        OnInventoryChanged?.Invoke();
        return removed;
    }

    public bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= slots.Length) return true;
        return slots[index].IsEmpty;
    }

    /// <summary>Returns index of the last non-empty storage slot (excluding hand), or -1.</summary>
    public int GetLastFilledStorageSlot()
    {
        for (int i = slots.Length - 1; i >= 0; i--)
        {
            if (i == handSlotIndex) continue;
            if (!slots[i].IsEmpty) return i;
        }
        return -1;
    }

    public bool HasFreeStorageSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == handSlotIndex) continue;
            if (slots[i].IsEmpty) return true;
        }
        return false;
    }

    /// <summary>Returns the storage-slot index holding a stack of the given item, or -1.</summary>
    public int FindSlotContaining(ItemData item)
    {
        if (item == null) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == handSlotIndex) continue;
            if (!slots[i].IsEmpty && slots[i].item == item) return i;
        }
        return -1;
    }

    public void ForceRefreshUI() => OnInventoryChanged?.Invoke();
}