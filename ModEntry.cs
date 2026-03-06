using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using StardewValley.Objects;
using Object = StardewValley.Object;
using StardewValley.Tools;
using StardewValley.Menus;
using StardewValley.Characters;
using StardewValley.GameData;
using StardewValley.GameData.Characters;
using StardewValley.Extensions;
using Netcode;

namespace CMX;

internal class CmxTransformDialogueData {
    public string Npcinternalname { get; set; } = "Error NPC internal name";
    public string Dialogue { get; set; } = "Error Dialogue";
}

internal sealed class ModEntry : Mod
{

    public const string CmxTransformDialogueAsset = "boxosoup.cmx/Transform_Dialogue";
    
    public static Dictionary<string, CmxTransformDialogueData> CmxTransformDialogues = new();
    
    public override void Entry(IModHelper helper)
    {
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Display.MenuChanged += OnNewMenu;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed; //for the auto consume
        helper.Events.Content.AssetRequested += OnAssetRequested;
        Harmony harmony = new(ModManifest.UniqueID);
        harmony.Patch(
            original: AccessTools.Method(typeof(Object), nameof(Object.GetObjectDisplayName)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.GetObjectDisplayName_Postfix))
        );
    }
    
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(CmxTransformDialogueAsset))
        {
            e.LoadFrom(
                () => new Dictionary<string, CmxTransformDialogueData>(),
                AssetLoadPriority.Exclusive
            );
        }
    }
    
    private void OnNewMenu(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is DialogueBox)
        {
            SetupMenu(e.NewMenu);
        }
        else
        {
            if (e.OldMenu != null) CheckMenu(e.OldMenu);
        }
    }
        private void SetupMenu(IClickableMenu menu)
        {
            
             this.Monitor.Log(message: $"You are talking to {Game1.currentSpeaker.Name}", LogLevel.Debug);
             if (Game1.currentSpeaker.Name == "boxosoup.cmx_x" || Game1.currentSpeaker.Name == "boxosoup.cmx_zero")
             {
                 NPC speaker = Game1.currentSpeaker;
                 CmxChatter(speaker);
             }

        }
        
        private static void CmxChatter(NPC speaker)
        {
            //function with speaker.name to get asset thing i hate code i hate code i hate code
            speaker.TemporaryDialogue.Push(speaker.Name);
        }

        private void CheckMenu(IClickableMenu menu)
        {
            if (menu is { } dialogueBox)
            {
                this.Monitor.Log(message: $"You have stopped talking.", LogLevel.Debug);
            }

        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            SetupLocation(e.NewLocation);

        }

        private void SetupLocation(GameLocation location)
        {
            NetCollection<NPC> npcCollection = location.characters;
            if (npcCollection.Count == 0)
            {
                this.Monitor.Log($"No NPCs found in {location.Name}.", LogLevel.Debug);
            }
            else
            {
                List<NPC> legibleCharacters = new List<NPC>();
                NPC? cmxAxl = null;
                foreach (NPC npc in location.characters)
                {
                    if (npc == null)
                    {
                        continue;
                    }

                    this.Monitor.Log($"NPC in {location.Name}: {npc.Name}", LogLevel.Debug);
                    if (npc.Name == "boxosoup.cmx_axl")
                    {
                        cmxAxl = npc;
                    }
                    else if ((npc.Sprite != null) && (npc.Sprite.SpriteWidth == 16) && (npc.Sprite.SpriteHeight == 32))
                    {
                        legibleCharacters.Add(npc);
                    }
                }

                if (cmxAxl != null)
                {
                    NPC chosen = Random.Shared.ChooseFrom(legibleCharacters);
                    cmxAxl.Sprite.textureName.Value = chosen.Sprite.textureName.Value;
                    this.Monitor.Log($"Axl has transformed into {{chosen.name}}!", LogLevel.Info);
                }
            }
        }

        public static void GetObjectDisplayName_Postfix(ref string __result, Object.PreserveType? preserveType, string preservedId, string displayNameFormat)
    {
            string? preservedItemId = Object.GetPreservedItemId(preserveType, preservedId);
            ParsedItemData? preservedData = ((preservedItemId != null) ? ItemRegistry.GetDataOrErrorItem(preservedItemId) : null);
            string? preservedName = preservedData?.DisplayName;
            string getInitial(string? name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return "?"; 
                name = name.Trim();
                return name[0].ToString().ToUpper();
            }

                if (!string.IsNullOrEmpty(__result) && __result.Contains('%'))
                {
                    __result = __result.Replace("%boxosoup.cmx_INTIAL_PRESERVED_NAME", getInitial(preservedName));
                }
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
