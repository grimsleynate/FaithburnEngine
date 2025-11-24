using Xunit;
using FaithburnEngine.Core.Inventory;
using System;

public class InventoryTests
{
    [Fact]
    public void AddItem_MergesAndCreatesSlots()
    {
        var inv = new Inventory(4);
        int leftover = inv.AddItem("wood", 50, id => 20);
    }
}