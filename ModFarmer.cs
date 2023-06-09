﻿using Enums;
using ModFarmer.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModFarmer
{
    /// <summary>
    /// ModFarmer is a mod for Green Hell that allows a player
    /// to get farming materials (seeds, flowers, nuts, mushrooms and droppings)
    /// and some special items. The weather can also be changed to raining or dry weather.
    /// Press Keypad5 (default) or the key configurable in ModAPI to open the main mod screen.
    /// </summary>
    public class ModFarmer : MonoBehaviour
    {
        private static readonly string ModName = nameof(ModFarmer);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;

        private Color DefaultGuiColor = GUI.color;

        private static float ModScreenStartPositionX { get; set; } = Screen.width / 3f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 3f;
        private static bool IsMinimized { get; set; } = false;
        private bool ShowUI = false;

        public static Rect ModFarmerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        private static ModFarmer Instance;
        private static ItemsManager LocalItemsManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;
        private static RainManager LocalRainManager;

        public static string CountSpecial { get; set; } = "1";
        public static string CountSeeds { get; set; } = "1";
        public static string CountFlowers { get; set; } = "1";
        public static string CountNuts { get; set; } = "1";
        public static string CountDroppings { get; set; } = "1";
        public static string CountShrooms { get; set; } = "1";

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static List<ItemInfo> FarmingItemInfos = new List<ItemInfo>();
        public bool FarmingUnlocked { get; set; } = false;
        public bool IsRainEnabled { get; private set; } = false;

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
        }
        public static string AlreadyUnlockedFarmer()
            => $"All farmer items were already unlocked!";
        public static string OnlyForSinglePlayerOrHostMessage()
            => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string AddedToInventoryMessage(int count, ItemInfo itemInfo)
            => $"Added {count} x {itemInfo.GetNameToDisplayLocalized()} to inventory.";
        public static string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        public void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo hudBigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 2f;
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

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            var localization = GreenHellGame.Instance.GetLocalization();
            HUDMessages hUDMessages = (HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages));
            hUDMessages.AddMessage(
                $"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}"
                );
        }

        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static KeyCode ModKeybindingId { get; set; } = KeyCode.Keypad5;
        private KeyCode GetConfigurableKey(string buttonId)
        {
            KeyCode configuredKeyCode = default;
            string configuredKeybinding = string.Empty;

            try
            {
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (var xmlReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader["ID"] == ModName)
                            {
                                if (xmlReader.ReadToFollowing(nameof(Button)) && xmlReader["ID"] == buttonId)
                                {
                                    configuredKeybinding = xmlReader.ReadElementContentAsString();
                                }
                            }
                        }
                    }
                }

                configuredKeybinding = configuredKeybinding?.Replace("NumPad", "Keypad").Replace("Oem", "");

                configuredKeyCode = (KeyCode)(!string.IsNullOrEmpty(configuredKeybinding)
                                                            ? Enum.Parse(typeof(KeyCode), configuredKeybinding)
                                                            : GetType()?.GetProperty(buttonId)?.GetValue(this));
                return configuredKeyCode;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetConfigurableKey));
                configuredKeyCode = (KeyCode)(GetType()?.GetProperty(buttonId)?.GetValue(this));
                return configuredKeyCode;
            }
        }

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ModKeybindingId = GetConfigurableKey(nameof(ModKeybindingId));
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, Color.yellow)
                            );
        }


        public ModFarmer()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModFarmer Get()
        {
            return Instance;
        }

        private void InitData()
        {
            LocalItemsManager = ItemsManager.Get();
            LocalPlayer = Player.Get();
            LocalHUDManager = HUDManager.Get();
            LocalRainManager = RainManager.Get();
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
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ModKeybindingId))
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
            ModFarmerScreen = GUILayout.Window(wid, ModFarmerScreen, InitModFarmerScreen, ModName, GUI.skin.window,
                                                                                                        GUILayout.ExpandWidth(true),
                                                                                                        GUILayout.MinWidth(ModScreenMinWidth),
                                                                                                        GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                                        GUILayout.ExpandHeight(true),
                                                                                                        GUILayout.MinHeight(ModScreenMinHeight),
                                                                                                        GUILayout.MaxHeight(ModScreenMaxHeight));
        }

        private void InitModFarmerScreen(int windowID)
        {
            ModScreenStartPositionX = ModFarmerScreen.x;
            ModScreenStartPositionY = ModFarmerScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                {
                    ModOptionsBox();
                    UnlockFarmerItemsBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void UnlockFarmerItemsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var farmeritemsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label($"Farmer tools and other items: ", GUI.skin.label);
                    using (var unlocktoolsScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Click to unlock farmer tools: ", GUI.skin.label);
                        if (GUILayout.Button("Unlock tools", GUI.skin.button))
                        {
                            OnClickUnlockFarmerToolsButton();
                        }
                    }

                    SpecialsBox();
                    SeedsBox();
                    FlowersBox();
                    NutsBox();
                    DroppingsBox();
                    ShroomsBox();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void OnClickUnlockFarmerToolsButton()
        {
            try
            {
                UnlockFarming();
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickUnlockFarmerToolsButton));
            }
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To toggle the mod main UI, press [{ModKeybindingId}]", GUI.skin.label);

                    MultiplayerOptionBox();
                    WeatherOptionBox();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (var multiplayeroptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Multiplayer options: ", GUI.skin.label);
                    string multiplayerOptionMessage = string.Empty;
                    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                    {
                        GUI.color = Color.green;
                        if (IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are the game host";
                        }
                        if (IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host allowed usage";
                        }
                        _ = GUILayout.Toggle(true, PermissionChangedMessage($"granted", multiplayerOptionMessage), GUI.skin.toggle);
                    }
                    else
                    {
                        GUI.color = Color.yellow;
                        if (!IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are not the game host";
                        }
                        if (!IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host did not allow usage";
                        }
                        _ = GUILayout.Toggle(false, PermissionChangedMessage($"revoked", $"{multiplayerOptionMessage}"), GUI.skin.toggle);
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultiplayerOptionBox));
            }
        }

        private void WeatherOptionBox()
        {
            try
            {
                using (var weatheroptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label($"Weather options: ", GUI.skin.label);
                    RainOption();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(WeatherOptionBox));
            }
        }

        private void RainOption()
        {
            try
            {
                bool _tRainEnabled = IsRainEnabled;
                IsRainEnabled = GUILayout.Toggle(IsRainEnabled, $"Switch between raining or dry weather", GUI.skin.toggle);
                if (_tRainEnabled != IsRainEnabled)
                {
                    if (IsRainEnabled)
                    {
                        LocalRainManager.ScenarioStartRain();
                    }
                    else
                    {
                        LocalRainManager.ScenarioStopRain();
                    }
                    MainLevel.Instance.EnableAtmosphereAndCloudsUpdate(IsRainEnabled);
                    string rainOptionMessage = $"Rain { (IsRainEnabled ? "is falling" : "has stopped") }";
                    ShowHUDBigInfo(HUDBigInfoMessage(rainOptionMessage, MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(RainOption));
            }
        }

        private void ShroomsBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("How many shrooms?: ", GUI.skin.label);
                CountShrooms = GUILayout.TextField(CountShrooms, GUI.skin.textField, GUILayout.MaxWidth(50f));
                if (GUILayout.Button("Get shrooms", GUI.skin.button, GUILayout.MaxWidth(200f)))
                {
                    OnClickGetShroomsButton();
                }
            }
        }

        private void DroppingsBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("How many droppings?: ", GUI.skin.label);
                CountDroppings = GUILayout.TextField(CountDroppings, GUI.skin.textField, GUILayout.MaxWidth(50f));
                if (GUILayout.Button("Get droppings", GUI.skin.button, GUILayout.MaxWidth(200f)))
                {
                    OnClickGetDroppingsButton();
                }
            }
        }

        private void NutsBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("How many nuts?: ", GUI.skin.label);
                CountNuts = GUILayout.TextField(CountNuts, GUI.skin.textField, GUILayout.MaxWidth(50f));
                if (GUILayout.Button("Get nuts", GUI.skin.button, GUILayout.MaxWidth(200f)))
                {
                    OnClickGetNutsButton();
                }
            }
        }

        private void FlowersBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("How many flowers?: ", GUI.skin.label);
                CountFlowers = GUILayout.TextField(CountFlowers, GUI.skin.textField, GUILayout.MaxWidth(50f));
                if (GUILayout.Button("Get flowers", GUI.skin.button, GUILayout.MaxWidth(200f)))
                {
                    OnClickGetFlowersButton();
                }
            }
        }

        private void SeedsBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("How many seeds?: ", GUI.skin.label);
                CountSeeds = GUILayout.TextField(CountSeeds, GUI.skin.textField, GUILayout.MaxWidth(50f));
                if (GUILayout.Button("Get seeds", GUI.skin.button, GUILayout.MaxWidth(200f)))
                {
                    OnClickGetSeedsButton();
                }
            }
        }

        private void SpecialsBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("How many coca - and ficus leaves?: ", GUI.skin.label);
                CountSpecial = GUILayout.TextField(CountSpecial, GUI.skin.textField, GUILayout.MaxWidth(50f));
                if (GUILayout.Button("Get specials", GUI.skin.button, GUILayout.MaxWidth(200f)))
                {
                    OnClickGetSpecialsButton();
                }
            }
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModFarmerScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
            {
                CollapseWindow();
            }

            if (GUI.Button(new Rect(ModFarmerScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseWindow()
        {
            if (!IsMinimized)
            {
                ModFarmerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModFarmerScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void OnClickGetSpecialsButton()
        {
            try
            {
                int countSpecial = ValidMinMax(CountSpecial);
                if (countSpecial > 0)
                {
                    AddCocaineAndFicusToInventory(countSpecial);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickGetSpecialsButton));
            }
        }

        private void OnClickGetShroomsButton()
        {
            try
            {
                int validCount = ValidMinMax(CountShrooms);
                if (validCount > 0)
                {
                    AddShroomsToInventory(validCount);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickGetShroomsButton));
            }
        }

        private void OnClickGetSeedsButton()
        {
            try
            {
                int validCount = ValidMinMax(CountSeeds);
                if (validCount > 0)
                {
                    AddSeedsToInventory(validCount);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickGetSeedsButton));
            }
        }

        private void OnClickGetFlowersButton()
        {
            try
            {
                int validCount = ValidMinMax(CountFlowers);
                if (validCount > 0)
                {
                    AddFlowersToInventory(validCount);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickGetFlowersButton));
            }
        }

        private void OnClickGetNutsButton()
        {
            try
            {
                int validCount = ValidMinMax(CountNuts);
                if (validCount > 0)
                {
                    AddNutsToInventory(validCount);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickGetNutsButton));
            }
        }

        private void OnClickGetDroppingsButton()
        {
            try
            {
                int validCount = ValidMinMax(CountDroppings);
                if (validCount > 0)
                {
                    AddDroppingsToInventory(validCount);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickGetDroppingsButton));
            }
        }

        public void UnlockFarming()
        {
            try
            {
                if (!FarmingUnlocked)
                {
                    UnlockTools();
                    UnlockNuts();
                    UnlockFlowers();
                    UnlockDroppings();
                    UnlockSeeds();
                    UnlockShrooms();
                    UnlockConstructions();
                    UnlockSnowman();

                    foreach (ItemInfo farmingItemInfo in FarmingItemInfos)
                    {
                        LocalItemsManager.UnlockItemInNotepad(farmingItemInfo.m_ID);
                        LocalItemsManager.UnlockItemInfo(farmingItemInfo.m_ID.ToString());
                        ShowHUDInfoLog(farmingItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                    }
                    FarmingUnlocked = true;
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(AlreadyUnlockedFarmer(), MessageType.Warning, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(UnlockFarming));
            }
        }

        private void UnlockTools()
        {
            foreach (ItemInfo itemInfo in GetToolInfos())
            {
                if (!FarmingItemInfos.Contains(itemInfo))
                {
                    FarmingItemInfos.Add(itemInfo);
                }
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
                LocalItemsManager.GetInfo(ItemID.Acre),
                LocalItemsManager.GetInfo(ItemID.AcreLiquidContainer),
                LocalItemsManager.GetInfo(ItemID.Acre_Small),
                LocalItemsManager.GetInfo(ItemID.AcreLiquidContainerSmall),
                LocalItemsManager.GetInfo(ItemID.mud_mixer),
                LocalItemsManager.GetInfo(ItemID.mud_water_collector),
                LocalItemsManager.GetInfo(ItemID.Water_Collector),
                LocalItemsManager.GetInfo(ItemID.Water_Filter),
                LocalItemsManager.GetInfo(ItemID.Bamboo_Water_Collector),
                LocalItemsManager.GetInfo(ItemID.Bamboo_Water_Filter)
            };

        private List<ItemInfo> GetNutsInfos() => new List<ItemInfo>
            {
                LocalItemsManager.GetInfo(ItemID.Brazil_nut),
                LocalItemsManager.GetInfo(ItemID.Brazil_nut_whole),
                LocalItemsManager.GetInfo(ItemID.Raffia_nut),
                LocalItemsManager.GetInfo(ItemID.Coconut)
            };

        private List<ItemInfo> GetDroppingsInfos() => new List<ItemInfo>
            {
                LocalItemsManager.GetInfo(ItemID.animal_droppings_item)
            };

        private List<ItemInfo> GetShroomsInfos() => new List<ItemInfo>
            {
                LocalItemsManager.GetInfo(ItemID.marasmius_haematocephalus),
                LocalItemsManager.GetInfo(ItemID.marasmius_haematocephalus_Dryed),
                LocalItemsManager.GetInfo(ItemID.indigo_blue_leptonia),
                LocalItemsManager.GetInfo(ItemID.indigo_blue_leptonia_dryed),
                LocalItemsManager.GetInfo(ItemID.Phallus_indusiatus),
                LocalItemsManager.GetInfo(ItemID.Phallus_indusiatus_Dryed),
                LocalItemsManager.GetInfo(ItemID.copa_hongo),
                LocalItemsManager.GetInfo(ItemID.copa_hongo_dryed),
                LocalItemsManager.GetInfo(ItemID.geoglossum_viride),
                LocalItemsManager.GetInfo(ItemID.geoglossum_viride_Dryed),
                LocalItemsManager.GetInfo(ItemID.hura_crepitans),
                LocalItemsManager.GetInfo(ItemID.Gerronema_retiarium),
                LocalItemsManager.GetInfo(ItemID.Gerronema_retiarium_dryed),
                LocalItemsManager.GetInfo(ItemID.Gerronema_viridilucens),
                LocalItemsManager.GetInfo(ItemID.Gerronema_viridilucens_dryed)
            };

        private List<ItemInfo> GetExtrasInfos() => new List<ItemInfo>
            {
                LocalItemsManager.GetInfo(ItemID.coca_leafs),
                LocalItemsManager.GetInfo(ItemID.Ficus_leaf),
            };

        private List<ItemInfo> GetSeedInfos()
        {
            List<ItemInfo> isSeedInfos = LocalItemsManager.GetAllInfos().Values.Where(info => info.IsSeed()).ToList();
            List<ItemInfo> moreSeedInfos = LocalItemsManager.GetAllInfos().Values.Where(info => info.ToString().ToLower().EndsWith("_seeds")).ToList();
            foreach (ItemInfo moreSeedInfo in moreSeedInfos)
            {
                if (!isSeedInfos.Contains(moreSeedInfo))
                {
                    isSeedInfos.Add(moreSeedInfo);
                }
            }
            return isSeedInfos;
        }

        private List<ItemInfo> GetToolInfos()
        {
            List<ItemInfo> isToolInfos = LocalItemsManager.GetAllInfos().Values.Where(info => info.IsTool()).ToList();

            return isToolInfos;
        }

        private List<ItemInfo> GetFlowerInfos() => new List<ItemInfo>
            {
                    LocalItemsManager.GetInfo(ItemID.Albahaca_flower_Dryed),
                    LocalItemsManager.GetInfo(ItemID.tobacco_flowers_Dryed),
                    LocalItemsManager.GetInfo(ItemID.monstera_deliciosa_flower_Dryed),
                    LocalItemsManager.GetInfo(ItemID.molineria_flowers_Dryed),
                    LocalItemsManager.GetInfo(ItemID.plantain_lilly_flowers_Dryed),
                    LocalItemsManager.GetInfo(ItemID.Quassia_Amara_flowers_Dryed)
            };

        private int ValidMinMax(string countToValidate)
        {
            if (int.TryParse(countToValidate, out int count))
            {
                if (count <= 0)
                {
                    count = 1;
                }
                if (count > 5)
                {
                    count = 5;
                }
                return count;
            }
            else
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {countToValidate}: please input numbers only - min. 1 and max. 5", MessageType.Error, Color.red));
                return -1;
            }
        }

        public void AddCocaineAndFicusToInventory(int count = 1)
        {
            try
            {
                List<ItemInfo> list = GetExtrasInfos();
                foreach (ItemInfo extra in list)
                {
                    for (int i = 0; i < count; i++)
                    {
                        LocalPlayer.AddItemToInventory(extra.m_ID.ToString());
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage(AddedToInventoryMessage(count, extra), MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(AddCocaineAndFicusToInventory));
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
                        LocalPlayer.AddItemToInventory(shroom.m_ID.ToString());
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage(AddedToInventoryMessage(count, shroom), MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(AddShroomsToInventory));
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
                        LocalPlayer.AddItemToInventory(dropping.m_ID.ToString());
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage(AddedToInventoryMessage(count, dropping), MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(AddDroppingsToInventory));
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
                        LocalPlayer.AddItemToInventory(nut.m_ID.ToString());
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage(AddedToInventoryMessage(count, nut), MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(AddNutsToInventory));
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
                        LocalPlayer.AddItemToInventory(flower.m_ID.ToString());
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage(AddedToInventoryMessage(count, flower), MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(AddFlowersToInventory));
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
                        LocalPlayer.AddItemToInventory(seed.m_ID.ToString());
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage(AddedToInventoryMessage(count, seed), MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(AddSeedsToInventory));
            }
        }
    }
}
