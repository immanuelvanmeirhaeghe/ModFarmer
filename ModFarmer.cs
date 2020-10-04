using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModFarmer
{
    /// <summary>
    /// ModFarmer is a mod for Green Hell
    /// that allows a player to get farming and some special food items.
    /// Enable the mod UI by pressing Home.
    /// </summary>
    public class ModFarmer : MonoBehaviour
    {
        private static ModFarmer s_Instance;

        private static readonly string ModName = nameof(ModFarmer);

        private bool ShowUI = false;

        public static Rect ModFarmerScreen = new Rect(Screen.width / 2.5f, Screen.height / 2.5f, 450f, 150f);

        private static ItemsManager itemsManager;
        private static HUDManager hUDManager;
        private static Player player;

        private static string m_CountSpecial = "1";
        private static string m_CountSeeds = "1";
        private static string m_CountFlowers = "1";
        private static string m_CountNuts = "1";
        private static string m_CountDroppings = "1";
        private static string m_CountShrooms = "1";

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static string HUDBigInfoMessage(string message) => $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.red)}>System</color>\n{message}";

        public static string AddedToInventoryMessage(int count, ItemInfo itemInfo) => $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>Added {count} x {itemInfo.GetNameToDisplayLocalized()}</color> to inventory.";

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>Permission to use mods for multiplayer was granted!</color>")
                            : HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.yellow)}>Permission to use mods for multiplayer was revoked!</color>")),
                           $"{ModName} Info",
                           HUDInfoLogTextureType.Count.ToString());
        }

        public List<ItemInfo> FarmingItemInfos = new List<ItemInfo>();
        public bool FarmingUnlocked { get; set; } = false;

        public ModFarmer()
        {
            useGUILayout = true;
            s_Instance = this;
        }

        public static ModFarmer Get()
        {
            return s_Instance;
        }

        public void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDBigInfo hudBigInfo = (HUDBigInfo)hUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData hudBigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            hudBigInfo.AddInfo(hudBigInfoData);
            hudBigInfo.Show(true);
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                player.BlockMoves();
                player.BlockRotation();
                player.BlockInspection();
            }
            else
            {
                player.UnblockMoves();
                player.UnblockRotation();
                player.UnblockInspection();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUi();
                if (!ShowUI)
                {
                    EnableCursor(false);
                }
            }
        }

        private void ToggleShowUi()
        {
            ShowUI = !ShowUI;
        }

        private void OnGUI()
        {
            if (ShowUI)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModFarmerScreen = GUILayout.Window(wid, ModFarmerScreen, InitModFarmerScreen, $"{ModName}", GUI.skin.window);
        }

        private void InitData()
        {
            itemsManager = ItemsManager.Get();
            player = Player.Get();
            hUDManager = HUDManager.Get();
            UnlockFarming();
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void InitModFarmerScreen(int windowID)
        {
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUI.Button(new Rect(430f, 0f, 20f, 20f), "X", GUI.skin.button))
                {
                    CloseWindow();
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("How many coca - and ficus leaves?: ", GUI.skin.label);
                    m_CountSpecial = GUILayout.TextField(m_CountSpecial, GUI.skin.textField, GUILayout.MaxWidth(50f));
                    if (GUILayout.Button("Get specials", GUI.skin.button, GUILayout.MinWidth(100f), GUILayout.MaxWidth(200f)))
                    {
                        OnClickGetSpecialsButton();
                        CloseWindow();
                    }
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("How many seeds?: ", GUI.skin.label);
                    m_CountSeeds = GUILayout.TextField(m_CountSeeds, GUI.skin.textField, GUILayout.MaxWidth(50f));
                    if (GUILayout.Button("Get seeds", GUI.skin.button, GUILayout.MinWidth(100f), GUILayout.MaxWidth(200f)))
                    {
                        OnClickGetSeedsButton();
                        CloseWindow();
                    }
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("How many flowers?: ", GUI.skin.label);
                    m_CountFlowers = GUILayout.TextField(m_CountFlowers, GUI.skin.textField, GUILayout.MaxWidth(50f));
                    if (GUILayout.Button("Get flowers", GUI.skin.button, GUILayout.MinWidth(100f), GUILayout.MaxWidth(200f)))
                    {
                        OnClickGetFlowersButton();
                        CloseWindow();
                    }
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("How many nuts?: ", GUI.skin.label);
                    m_CountNuts = GUILayout.TextField(m_CountNuts, GUI.skin.textField, GUILayout.MaxWidth(50f));
                    if (GUILayout.Button("Get nuts", GUI.skin.button, GUILayout.MinWidth(100f), GUILayout.MaxWidth(200f)))
                    {
                        OnClickGetNutsButton();
                        CloseWindow();
                    }
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("How many droppings?: ", GUI.skin.label);
                    m_CountDroppings = GUILayout.TextField(m_CountDroppings, GUI.skin.textField, GUILayout.MaxWidth(50f));
                    if (GUILayout.Button("Get droppings", GUI.skin.button, GUILayout.MinWidth(100f), GUILayout.MaxWidth(200f)))
                    {
                        OnClickGetDroppingsButton();
                        CloseWindow();
                    }
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("How many shrooms?: ", GUI.skin.label);
                    m_CountShrooms = GUILayout.TextField(m_CountShrooms, GUI.skin.textField, GUILayout.MaxWidth(50f));
                    if (GUILayout.Button("Get shrooms", GUI.skin.button, GUILayout.MinWidth(100f), GUILayout.MaxWidth(200f)))
                    {
                        OnClickGetShroomsButton();
                        CloseWindow();
                    }
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void OnClickGetSpecialsButton()
        {
            try
            {
                AddCocaineAndFicusToInventory(Int32.Parse(m_CountSpecial));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickGetSpecialsButton)}] throws exception: {exc.Message}");
            }
        }

        private void OnClickGetShroomsButton()
        {
            try
            {
                AddShroomsToInventory(Int32.Parse(m_CountShrooms));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickGetShroomsButton)}] throws exception: {exc.Message}");
            }
        }

        private void OnClickGetSeedsButton()
        {
            try
            {
                AddSeedsToInventory(Int32.Parse(m_CountSeeds));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickGetSeedsButton)}] throws exception: {exc.Message}");
            }
        }

        private void OnClickGetFlowersButton()
        {
            try
            {
                AddFlowersToInventory(Int32.Parse(m_CountFlowers));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickGetFlowersButton)}] throws exception: {exc.Message}");
            }
        }

        private void OnClickGetNutsButton()
        {
            try
            {
                AddNutsToInventory(Int32.Parse(m_CountNuts));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickGetNutsButton)}] throws exception: {exc.Message}");
            }
        }

        private void OnClickGetDroppingsButton()
        {
            try
            {
                AddDroppingsToInventory(Int32.Parse(m_CountDroppings));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickGetDroppingsButton)}] throws exception: {exc.Message}");
            }
        }

        public void UnlockFarming()
        {
            try
            {
                if (!FarmingUnlocked)
                {
                    UnlockNuts();
                    UnlockFlowers();
                    UnlockDroppings();
                    UnlockSeeds();
                    UnlockShrooms();
                    UnlockConstructions();
                    UnlockSnowman();

                    foreach (ItemInfo farmingItemInfo in FarmingItemInfos)
                    {
                        itemsManager.UnlockItemInNotepad(farmingItemInfo.m_ID);
                        itemsManager.UnlockItemInfo(farmingItemInfo.m_ID.ToString());
                    }

                    FarmingUnlocked = true;
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(UnlockFarming)}] throws exception: {exc.Message}");
            }
        }

        private void UnlockSnowman()
        {
            foreach (ItemInfo itemInfo in GetExtrasInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
            }
        }

        private void UnlockConstructions()
        {
            foreach (ItemInfo itemInfo in GetConstructionInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
            }
        }

        private void UnlockShrooms()
        {
            foreach (ItemInfo itemInfo in GetShroomsInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
            }
        }

        private void UnlockSeeds()
        {
            foreach (ItemInfo itemInfo in GetSeedInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
            }
        }

        private void UnlockDroppings()
        {
            foreach (ItemInfo itemInfo in GetDroppingsInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
            }
        }

        private void UnlockFlowers()
        {
            foreach (ItemInfo itemInfo in GetFlowerInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
            }
        }

        private void UnlockNuts()
        {
            foreach (ItemInfo itemInfo in GetNutsInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
            }
        }

        private List<ItemInfo> GetConstructionInfos() => new List<ItemInfo>
            {
                itemsManager.GetInfo(ItemID.Acre),
                itemsManager.GetInfo(ItemID.AcreLiquidContainer),
                itemsManager.GetInfo(ItemID.Acre_Small),
                itemsManager.GetInfo(ItemID.AcreLiquidContainerSmall),
                itemsManager.GetInfo(ItemID.mud_mixer),
                itemsManager.GetInfo(ItemID.mud_water_collector),
                itemsManager.GetInfo(ItemID.Water_Collector),
                itemsManager.GetInfo(ItemID.Water_Filter),
                itemsManager.GetInfo(ItemID.Bamboo_Water_Collector),
                itemsManager.GetInfo(ItemID.Bamboo_Water_Filter)
            };

        private List<ItemInfo> GetNutsInfos() => new List<ItemInfo>
            {
                itemsManager.GetInfo(ItemID.Brazil_nut),
                itemsManager.GetInfo(ItemID.Brazil_nut_whole),
                itemsManager.GetInfo(ItemID.Raffia_nut),
                itemsManager.GetInfo(ItemID.Coconut)
            };

        private List<ItemInfo> GetDroppingsInfos() => new List<ItemInfo>
            {
                itemsManager.GetInfo(ItemID.animal_droppings_item)
            };

        private List<ItemInfo> GetShroomsInfos() => new List<ItemInfo>
            {
                itemsManager.GetInfo(ItemID.marasmius_haematocephalus),
                itemsManager.GetInfo(ItemID.marasmius_haematocephalus_Dryed),
                itemsManager.GetInfo(ItemID.indigo_blue_leptonia),
                itemsManager.GetInfo(ItemID.indigo_blue_leptonia_dryed),
                itemsManager.GetInfo(ItemID.Phallus_indusiatus),
                itemsManager.GetInfo(ItemID.Phallus_indusiatus_Dryed),
                itemsManager.GetInfo(ItemID.copa_hongo),
                itemsManager.GetInfo(ItemID.copa_hongo_dryed),
                itemsManager.GetInfo(ItemID.geoglossum_viride),
                itemsManager.GetInfo(ItemID.geoglossum_viride_Dryed),
                itemsManager.GetInfo(ItemID.hura_crepitans),
                itemsManager.GetInfo(ItemID.Gerronema_retiarium),
                itemsManager.GetInfo(ItemID.Gerronema_retiarium_dryed),
                itemsManager.GetInfo(ItemID.Gerronema_viridilucens),
                itemsManager.GetInfo(ItemID.Gerronema_viridilucens_dryed)
            };

        private List<ItemInfo> GetExtrasInfos() => new List<ItemInfo>
            {
                itemsManager.GetInfo(ItemID.coca_leafs),
                itemsManager.GetInfo(ItemID.Ficus_leaf),
            };

        private List<ItemInfo> GetSeedInfos()
        {
            List<ItemInfo> isSeedInfos = itemsManager.GetAllInfos().Values.Where(info => info.IsSeed()).ToList();
            List<ItemInfo> moreSeedInfos = itemsManager.GetAllInfos().Values.Where(info => info.ToString().ToLower().EndsWith("_seeds")).ToList();
            foreach (ItemInfo moreSeedInfo in moreSeedInfos)
            {
                if (!isSeedInfos.Contains(moreSeedInfo))
                {
                    isSeedInfos.Add(moreSeedInfo);
                }
            }
            return isSeedInfos;
        }

        private List<ItemInfo> GetFlowerInfos() => new List<ItemInfo>
            {
                    itemsManager.GetInfo(ItemID.Albahaca_flower_Dryed),
                    itemsManager.GetInfo(ItemID.tobacco_flowers_Dryed),
                    itemsManager.GetInfo(ItemID.monstera_deliciosa_flower_Dryed),
                    itemsManager.GetInfo(ItemID.molineria_flowers_Dryed),
                    itemsManager.GetInfo(ItemID.plantain_lilly_flowers_Dryed),
                    itemsManager.GetInfo(ItemID.Quassia_Amara_flowers_Dryed)
            };

        public void AddCocaineAndFicusToInventory(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetExtrasInfos();
                foreach (ItemInfo extra in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        // Item item = itemsManager.CreateItem(extra.m_ID, true, player.transform.position + player.transform.forward * (i + 2f), player.transform.rotation);
                        player.AddItemToInventory(extra.m_ID.ToString());
                    }
                    ShowHUDBigInfo($"Added {count} x {extra.GetNameToDisplayLocalized()} to inventory.", $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(AddCocaineAndFicusToInventory)}] throws exception: {exc.Message}");
            }
        }

        public void AddShroomsToInventory(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetShroomsInfos();
                foreach (ItemInfo shroom in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        player.AddItemToInventory(shroom.m_ID.ToString());
                    }
                    ShowHUDBigInfo(
                        HUDBigInfoMessage(AddedToInventoryMessage(count, shroom)),
                        $"{ModName} Info",
                        HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(AddShroomsToInventory)}] throws exception: {exc.Message}");
            }
        }

        public void AddDroppingsToInventory(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetDroppingsInfos();
                foreach (ItemInfo dropping in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        player.AddItemToInventory(dropping.m_ID.ToString());
                    }
                    ShowHUDBigInfo(
                        HUDBigInfoMessage(AddedToInventoryMessage(count, dropping)),
                        $"{ModName} Info",
                        HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(AddDroppingsToInventory)}] throws exception: {exc.Message}");
            }
        }

        public void AddNutsToInventory(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetNutsInfos();
                foreach (ItemInfo nut in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        player.AddItemToInventory(nut.m_ID.ToString());
                    }
                    ShowHUDBigInfo(
                        HUDBigInfoMessage(AddedToInventoryMessage(count, nut)),
                        $"{ModName} Info",
                        HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(AddNutsToInventory)}] throws exception: {exc.Message}");
            }
        }

        public void AddFlowersToInventory(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetFlowerInfos();
                foreach (ItemInfo flower in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        player.AddItemToInventory(flower.m_ID.ToString());
                    }
                    ShowHUDBigInfo(
                       HUDBigInfoMessage(AddedToInventoryMessage(count, flower)),
                       $"{ModName} Info",
                       HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(AddFlowersToInventory)}] throws exception: {exc.Message}");
            }
        }

        public void AddSeedsToInventory(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetSeedInfos();
                foreach (ItemInfo seed in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        player.AddItemToInventory(seed.m_ID.ToString());
                    }
                    ShowHUDBigInfo(
                       HUDBigInfoMessage(AddedToInventoryMessage(count, seed)),
                       $"{ModName} Info",
                       HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(AddSeedsToInventory)}] throws exception: {exc.Message}");
            }
        }
    }
}
