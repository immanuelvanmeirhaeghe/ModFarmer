using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModFarmer
{
    public class ModFarmer : MonoBehaviour
    {
        /// <summary>
        /// ModFarmer is a mod for Green Hell
        /// that allows a player to spawn charcoal, stones, obsidian, iron and gold sacks.
        /// The ores will be added to the player inventory.
        /// Enable the mod UI by pressing Home.
        /// </summary>
        private static ModFarmer s_Instance;

        private bool showUI = false;

        public Rect ModFarmerWindow = new Rect(10f, 680f, 450f, 150f);

        private static ItemsManager itemsManager;
        private static HUDManager hUDManager;
        private static Player player;

        private static string m_CountStack = "1";
        private static string m_CountSeeds = "1";
        private static string m_CountFlowers = "1";
        private static string m_CountNuts = "1";
        private static string m_CountDroppings = "1";
        private static string m_CountShrooms = "1";

        private bool _isActiveForMultiplayer;
        public bool IsModActiveForMultiplayer
        {
            get => _isActiveForMultiplayer;
            set => _isActiveForMultiplayer = FindObjectOfType(typeof(ModManager.ModManager)) != null && ModManager.ModManager.AllowModsForMultiplayer;
        }

        private bool _isActiveForSingleplayer;
        public bool IsModActiveForSingleplayer
        {
            get => _isActiveForSingleplayer;
            set => _isActiveForSingleplayer = ReplTools.AmIMaster();
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
                if (!showUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUi();
                if (!showUI)
                {
                    EnableCursor(false);
                }
            }
        }

        private void ToggleShowUi()
        {
            showUI = !showUI;
        }

        private void OnGUI()
        {
            if (showUI)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitWindow()
        {
            ModFarmerWindow = GUI.Window(0, ModFarmerWindow, InitModWindow, $"{nameof(ModFarmer)}", GUI.skin.window);
        }

        private void InitData()
        {
            itemsManager = ItemsManager.Get();
            player = Player.Get();
            hUDManager = HUDManager.Get();

            if (!FarmingUnlocked)
            {
                UnlockFarmingUtils();
            }
        }

        private void CloseWindow()
        {
            showUI = false;
            EnableCursor(false);
        }

        private void InitModWindow(int windowId)
        {
            if (GUI.Button(new Rect(910f, 680f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }

            GUI.Label(new Rect(500f, 700f, 200f, 20f), "All farmer stuff, coca, ficus", GUI.skin.label);
            m_CountStack = GUI.TextField(new Rect(700f, 700f, 20f, 20f), m_CountStack, GUI.skin.textField);
            if (GUI.Button(new Rect(720f, 700f, 150f, 20f), "Get pack", GUI.skin.button))
            {
                OnClickGetPackButton();
                CloseWindow();
            }

            GUI.Label(new Rect(500f, 720f, 200f, 20f), "How many seeds: ", GUI.skin.label);
            m_CountSeeds = GUI.TextField(new Rect(700f, 720f, 20f, 20f), m_CountSeeds, GUI.skin.textField);
            if (GUI.Button(new Rect(720f, 720f, 150f, 20f), "Get seeds", GUI.skin.button))
            {
                OnClickGetSeedsButton();
                CloseWindow();
            }

            GUI.Label(new Rect(500f, 740f, 200f, 20f), "How many flowers: ", GUI.skin.label);
            m_CountFlowers = GUI.TextField(new Rect(700f, 740f, 20f, 20f), m_CountFlowers, GUI.skin.textField);
            if (GUI.Button(new Rect(720f, 740f, 150f, 20f), "Get flowers", GUI.skin.button))
            {
                OnClickGetFlowersButton();
                CloseWindow();
            }

            GUI.Label(new Rect(500f, 760f, 200f, 20f), "How many nuts: ", GUI.skin.label);
            m_CountNuts = GUI.TextField(new Rect(700f, 760f, 20f, 20f), m_CountNuts, GUI.skin.textField);
            if (GUI.Button(new Rect(720f, 760f, 150f, 20f), "Get nuts", GUI.skin.button))
            {
                OnClickGetNutsButton();
                CloseWindow();
            }

            GUI.Label(new Rect(500f, 780f, 200f, 20f), "How many droppings: ", GUI.skin.label);
            m_CountDroppings = GUI.TextField(new Rect(700f, 780f, 20f, 20f), m_CountDroppings, GUI.skin.textField);
            if (GUI.Button(new Rect(720f, 780f, 150f, 20f), "Get droppings", GUI.skin.button))
            {
                OnClickGetDroppingsButton();
                CloseWindow();
            }

            GUI.Label(new Rect(500f, 800f, 200f, 20f), "How many shrooms: ", GUI.skin.label);
            m_CountShrooms = GUI.TextField(new Rect(700f, 800f, 20f, 20f), m_CountShrooms, GUI.skin.textField);
            if (GUI.Button(new Rect(720f, 800f, 150f, 20f), "Get shrooms", GUI.skin.button))
            {
                OnClickGetShroomsButton();
                CloseWindow();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void OnClickGetPackButton()
        {
            try
            {
                AddSeedsToInventory(Int32.Parse(m_CountStack));
                AddFlowersToInventory(Int32.Parse(m_CountStack));
                AddNutsToInventory(Int32.Parse(m_CountStack));
                AddDroppingsToInventory(Int32.Parse(m_CountStack));
                AddShroomsToInventory(Int32.Parse(m_CountStack));
                SpawnCocaineAndFicusBeforePlayer(Int32.Parse(m_CountStack));
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(OnClickGetPackButton)}] throws exception: {exc.Message}");
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
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(OnClickGetShroomsButton)}] throws exception: {exc.Message}");
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
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(OnClickGetSeedsButton)}] throws exception: {exc.Message}");
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
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(OnClickGetFlowersButton)}] throws exception: {exc.Message}");
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
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(OnClickGetNutsButton)}] throws exception: {exc.Message}");
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
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(OnClickGetDroppingsButton)}] throws exception: {exc.Message}");
            }
        }

        public void UnlockFarmingUtils()
        {
            try
            {
                foreach (ItemInfo itemInfo in GetNutsInfos())
                {
                    if (!FarmingItemInfos.Contains(itemInfo))
                    {
                        FarmingItemInfos.Add(itemInfo);
                    }
                }

                foreach (ItemInfo itemInfo in GetFlowerInfos())
                {
                    if (!FarmingItemInfos.Contains(itemInfo))
                    {
                        FarmingItemInfos.Add(itemInfo);
                    }
                }

                foreach (ItemInfo itemInfo in GetDroppingsInfos())
                {
                    if (!FarmingItemInfos.Contains(itemInfo))
                    {
                        FarmingItemInfos.Add(itemInfo);
                    }
                }

                foreach (ItemInfo itemInfo in GetSeedInfos())
                {
                    if (!FarmingItemInfos.Contains(itemInfo))
                    {
                        FarmingItemInfos.Add(itemInfo);
                    }
                }

                foreach (ItemInfo itemInfo in GetShroomsInfos())
                {
                    if (!FarmingItemInfos.Contains(itemInfo))
                    {
                        FarmingItemInfos.Add(itemInfo);
                    }
                }

                foreach (ItemInfo itemInfo in GetConstructionInfos())
                {
                    if (!FarmingItemInfos.Contains(itemInfo))
                    {
                        FarmingItemInfos.Add(itemInfo);
                    }
                }

                foreach (ItemInfo itemInfo in GetExtrasInfos())
                {
                    if (!FarmingItemInfos.Contains(itemInfo))
                    {
                        FarmingItemInfos.Add(itemInfo);
                    }
                }

                foreach (ItemInfo farmingItemInfo in FarmingItemInfos)
                {
                    itemsManager.UnlockItemInNotepad(farmingItemInfo.m_ID);
                    itemsManager.UnlockItemInfo(farmingItemInfo.m_ID.ToString());
                }

                FarmingUnlocked = true;
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(UnlockFarmingUtils)}] throws exception: {exc.Message}");
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

        public void SpawnCocaineAndFicusBeforePlayer(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetExtrasInfos();
                foreach (ItemInfo extra in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Item item = itemsManager.CreateItem(extra.m_ID, true, player.transform.position + player.transform.forward * (i +  2f), player.transform.rotation);
                    }
                    ShowHUDBigInfo($"Spawned {count} x {extra.GetNameToDisplayLocalized()} before player", $"{nameof(ModFarmer)} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(AddShroomsToInventory)}] throws exception: {exc.Message}");
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
                    ShowHUDBigInfo($"Added {count} x {shroom.GetNameToDisplayLocalized()} to inventory", $"{nameof(ModFarmer)} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(AddShroomsToInventory)}] throws exception: {exc.Message}");
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
                    ShowHUDBigInfo($"Added {count} x {dropping.GetNameToDisplayLocalized()} to inventory", $"{nameof(ModFarmer)} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(AddDroppingsToInventory)}] throws exception: {exc.Message}");
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
                    ShowHUDBigInfo($"Added {count} x {nut.GetNameToDisplayLocalized()} to inventory", $"{nameof(ModFarmer)} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(AddNutsToInventory)}] throws exception: {exc.Message}");
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
                    ShowHUDBigInfo($"Added {count} x {flower.GetNameToDisplayLocalized()} to inventory", $"{nameof(ModFarmer)} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(AddFlowersToInventory)}] throws exception: {exc.Message}");
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
                    ShowHUDBigInfo($"Added {count} x {seed.GetNameToDisplayLocalized()} to inventory", $"{nameof(ModFarmer)} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModFarmer)}.{nameof(ModFarmer)}:{nameof(AddSeedsToInventory)}] throws exception: {exc.Message}");
            }
        }
    }
}
