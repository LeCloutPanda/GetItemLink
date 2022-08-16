﻿using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using BaseX;
using System.Reflection;
using System;

namespace GetItemLink
{
    public class GetItemLink : NeosMod
    {
        public override string Name => "GetItemLink";
        public override string Author => "eia485";
        public override string Version => "1.1.0";
        public override string Link => "https://github.com/eia485/NeosGetItemLink/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.eia485.GetItemLink");
            harmony.PatchAll();
        }

        static FieldInfo itemInfo = typeof(InventoryItemUI).GetField("Item", BindingFlags.Instance | BindingFlags.NonPublic);
        static FieldInfo directoryInfo = typeof(InventoryItemUI).GetField("Directory", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(InventoryBrowser))]
        class GetItemLinkPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("OnItemSelected")]
            public static void OnItemSelectedPrefix(ref InventoryBrowser.SpecialItemType __state, Sync<InventoryBrowser.SpecialItemType> ____lastSpecialItemType)
            {
                __state = ____lastSpecialItemType.Value;
            }
            [HarmonyPostfix]
            [HarmonyPatch("OnItemSelected")]
            public static void OnItemSelectedPostfix(InventoryBrowser __instance, BrowserItem currentItem, InventoryBrowser.SpecialItemType __state, SyncRef<Slot> ____buttonsRoot)
            {
                if (__instance.World == Userspace.UserspaceWorld)
                {
                    Slot buttonRoot = ____buttonsRoot.Target[0];
                    if (__state != InventoryBrowser.ClassifyItem(currentItem as InventoryItemUI))
                    {
                        AddButton((IButton button, ButtonEventData eventData) =>
                        {
                            ItemLink(button, __instance.SelectedInventoryItem, false);
                        },
                        color.Purple,
                        NeosAssets.Graphics.Badges.Cheese,
                        buttonRoot);

                        AddButton((IButton button, ButtonEventData eventData) =>
                        {
                            ItemLink(button, __instance.SelectedInventoryItem, true);
                        },
                        color.Brown,
                        NeosAssets.Graphics.Badges.potato,
                        buttonRoot);
                    }
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("OnChanges")]
            public static void OnChangesPrefix(InventoryBrowser __instance, SyncRef<Slot> ____buttonsRoot)
            {
                if (__instance.World == Userspace.UserspaceWorld)
                {
                    Slot buttonRoot = ____buttonsRoot.Target[0];
                    bool enableButtons = __instance.SelectedInventoryItem != null;
                    foreach (var child in buttonRoot.Children)
                    {
                        if (child.Tag == "GetItemLinkButton")
                        {
                            child.GetComponent<Button>().Enabled = enableButtons;
                            child[0].GetComponent<Image>().Tint.Value = color.Black;
                        }
                    }
                }
            }
        }

        public static void AddButton(ButtonEventHandler onPress, color tint, Uri sprite, Slot buttonRoot)
        {
            Slot buttonSlot = buttonRoot.AddSlot("Button");
            buttonSlot.Tag = "GetItemLinkButton";
            buttonSlot.AttachComponent<LayoutElement>().PreferredWidth.Value = 96 * .6f;
            Button userButton = buttonSlot.AttachComponent<Button>();
            Image frontimage = buttonSlot.AttachComponent<Image>(true, null);
            frontimage.Tint.Value = tint;
            userButton.SetupBackgroundColor(frontimage.Tint);
            Slot imageSlot = buttonSlot.AddSlot("Image");
            Image image = imageSlot.AttachComponent<Image>();
            image.Sprite.Target = imageSlot.AttachSprite(sprite);
            image.Tint.Value = color.Black;
            image.RectTransform.AddFixedPadding(2f);
            userButton.LocalPressed += onPress;
        }

        public static void ItemLink(IButton button, InventoryItemUI Item, bool type)
        {
            Record record = (Record)itemInfo.GetValue(Item) ?? ((RecordDirectory)directoryInfo.GetValue(Item)).EntryRecord;
            if (record != null)
            {
                string link = type ? record.URL.ToString() : record.AssetURI;
                if (link != null)
                {
                    Engine.Current.InputInterface.Clipboard.SetText(link);
                    button.Slot[0].GetComponent<Image>().Tint.Value = color.White;
                    return;
                }
            }
            button.Slot[0].GetComponent<Image>().Tint.Value = color.Red;
        }
    }
}