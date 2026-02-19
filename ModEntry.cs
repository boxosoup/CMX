using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using StardewValley.TokenizableStrings;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace CMX;
/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.Input.ButtonPressed += this.OnButtonPressed; 
    }
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        // ignore if player hasn't loaded a save yet
        if (!Context.IsWorldReady)
            return;

        // ignore if player doesn't have control
        if (!Context.IsPlayerFree)
        {
            return;
        }
        if (!((Game1.player.Stamina <= 20)||(Game1.player.health <= 20)))
        {
            return;
        }
        ConsumeETank();
    }

    private void ConsumeETank()

    {
        Item? eTank = SearchInventory();
        if ((eTank != null)&&(!Game1.player.isEating))
        {
            this.Monitor.Log(
                $"Stamina: {Game1.player.Stamina}; Health: {Game1.player.health}; consuming Etank");
            EatFood(eTank);
        }
        else
        {
            return;
        }
    }
    private void EatFood(Item food)
    {
        var direction = Game1.player.FacingDirection;
        var toolIndex = Game1.player.CurrentToolIndex;
        if (Game1.player.CurrentTool is FishingRod tool && tool.inUse())
            tool.resetState();
        Game1.player.eatObject((StardewValley.Object)food);
        food.Stack--; 
        if (food.Stack == 0) 
            Game1.player.removeItemFromInventory(food);
        Game1.player.FacingDirection = direction;
        Game1.player.CurrentToolIndex = toolIndex;
    }
    private Item? SearchInventory()
    {
        var consumables = Game1.player.Items.Where(
            curItem => (curItem is StardewValley.Object { Edibility: > 0 } item && item.HasContextTag("Etank"))).ToList();
        if (consumables.Count == 0)
        {
            return null;
        }
        return consumables.OrderBy(curItem => ((StardewValley.Object)curItem).Edibility).First();
    }

}