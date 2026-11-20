using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Bifrost.Cooked;
using Bifrost.ConstEnum;

[assembly: MelonInfo(typeof(MimesisMenu.Plugin), "RenUI", "1.8.1", "Pattern")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace MimesisMenu
{
    public class ItemEntry
    {
        public int MasterId;
        public string RawName;
        public string DisplayName;
        public ItemType ItemType;
        public int Weight;
        public int PriceMin;
        public int PriceMax;
        public int MeanPrice;

        public string GetPriceDisplay()
        {
            if (PriceMin <= 0 && PriceMax <= 0 && MeanPrice <= 0) return "";
            if (PriceMin > 0 && PriceMax > 0)
            {
                if (PriceMin == PriceMax) return $"${PriceMin}";
                return $"${PriceMin}-${PriceMax}";
            }
            if (PriceMin > 0) return $"${PriceMin}";
            if (PriceMax > 0) return $"${PriceMax}";
            if (MeanPrice > 0) return $"${MeanPrice}";
            return "";
        }
    }

    public class TeamMemberEntry
    {
        public int ActorID;
        public string Name;
        public string DisplayText;
        public bool IsDead;
        public bool IsLocal;
        public long HP;
    }

    public class KeybindEntry
    {
        public string Id;
        public string DisplayName;
        public MelonPreferences_Entry<string> PrefEntry;
        public MelonPreferences_Entry<bool> TogglePref;
        public Key DefaultKey;
        public Key CurrentKey;

        public KeybindEntry(string id, string displayName, MelonPreferences_Entry<string> prefEntry, MelonPreferences_Entry<bool> togglePref, Key defaultKey)
        {
            Id = id;
            DisplayName = displayName;
            PrefEntry = prefEntry;
            TogglePref = togglePref;
            DefaultKey = defaultKey;
            CurrentKey = Plugin.ParseKey(prefEntry?.Value, defaultKey);
        }
    }

    public class ItemSpawnBind
    {
        public string Id;
        public int MasterId;
        public string ItemName;
        public bool ToInventory; // true = в руки / инвентарь, false = на землю
        public Key BoundKey;
    }

    public class Plugin : MelonMod
    {
        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<bool> Pref_GodMode;
        public static MelonPreferences_Entry<bool> Pref_InfiniteStamina;
        public static MelonPreferences_Entry<bool> Pref_NoWeightSlowdown;
        public static MelonPreferences_Entry<bool> Pref_SilentSteps;
        public static MelonPreferences_Entry<bool> Pref_InfiniteBattery;
        public static MelonPreferences_Entry<bool> Pref_AntiContamination;
        public static MelonPreferences_Entry<bool> Pref_FullBrightEnabled;
        public static MelonPreferences_Entry<float> Pref_FullBrightIntensity;
        public static MelonPreferences_Entry<bool> Pref_ShowTeamStatus;
        public static MelonPreferences_Entry<bool> Pref_ItemEspEnabled;
        public static MelonPreferences_Entry<float> Pref_ItemEspDistance;
        public static MelonPreferences_Entry<float> Pref_ItemEspColorR;
        public static MelonPreferences_Entry<float> Pref_ItemEspColorG;
        public static MelonPreferences_Entry<float> Pref_ItemEspColorB;
        public static MelonPreferences_Entry<bool> Pref_ActorEspEnabled;
        public static MelonPreferences_Entry<bool> Pref_ActorEspShowAllies;
        public static MelonPreferences_Entry<bool> Pref_ActorEspShowEnemies;
        public static MelonPreferences_Entry<bool> Pref_ActorEspShowName;
        public static MelonPreferences_Entry<bool> Pref_ActorEspShowDist;
        public static MelonPreferences_Entry<bool> Pref_ActorEspShowHp;
        public static MelonPreferences_Entry<bool> Pref_ActorEspShowRadiation;
        public static MelonPreferences_Entry<float> Pref_ActorEspDistance;
        public static MelonPreferences_Entry<float> Pref_ActorEspFontSize;
        public static MelonPreferences_Entry<float> Pref_ItemEspFontSize;
        public static MelonPreferences_Entry<float> Pref_WindowX;
        public static MelonPreferences_Entry<float> Pref_WindowY;

        // Keybind preferences
        public static MelonPreferences_Entry<string> Pref_KeyMenu;
        public static MelonPreferences_Entry<string> Pref_KeyGodMode;
        public static MelonPreferences_Entry<string> Pref_KeyStamina;
        public static MelonPreferences_Entry<string> Pref_KeyNoWeight;
        public static MelonPreferences_Entry<string> Pref_KeySilentSteps;
        public static MelonPreferences_Entry<string> Pref_KeyBattery;
        public static MelonPreferences_Entry<string> Pref_KeyAntiConta;
        public static MelonPreferences_Entry<string> Pref_KeyFullBright;
        public static MelonPreferences_Entry<string> Pref_KeyTeamHud;
        public static MelonPreferences_Entry<string> Pref_KeyItemEsp;
        public static MelonPreferences_Entry<string> Pref_KeyActorEsp;
        public static MelonPreferences_Entry<string> Pref_ItemBinds;

        public static bool ShowMenu = false;

        // Movable window state (Variant 1: Cyber-Modern Sidebar 960x640)
        private static Rect windowRect = new Rect(60, 60, 960, 640);
        private static bool isDragging = false;
        private static Vector2 dragOffset;

        // Tabs: 0 = Cheats & Tweaks, 1 = Item Spawner, 2 = Keybinds
        private static int currentTab = 0;
        private static Vector2 scrollPosCheats;
        private static Vector2 scrollPosSpawner;
        private static Vector2 scrollPosKeybinds;

        // Keybinds state
        public static List<KeybindEntry> keybindEntries = new List<KeybindEntry>();
        private static string waitingForKeyAction = null;
        private static Key[] allInputKeys = null;

        // Item quick-spawn binds state
        public static List<ItemSpawnBind> itemSpawnBinds = new List<ItemSpawnBind>();
        private static int waitingForItemBindMasterId = 0;
        private static bool pendingBindToInventory = true;

        // FullBright state
        private static GameObject fullBrightLightGo = null;
        private static Light fullBrightLight = null;
        private static bool originalAmbientSaved = false;
        private static Color savedAmbientLight;
        private static float savedAmbientIntensity;
        private static UnityEngine.Rendering.AmbientMode savedAmbientMode;

        // Team status HUD state
        private static List<TeamMemberEntry> cachedTeamMembers = new List<TeamMemberEntry>();
        private static float lastTeamScanTime = 0f;
        private static UIPrefab_Inventory cachedInventoryUI = null;
        private static float lastInvenScanTime = 0f;
        private static float cachedSlotBottomImgui = -1f;
        private GUIStyle teamTextStyle;

        // Item ESP state
        private static List<LootingLevelObject> cachedLootObjects = new List<LootingLevelObject>();
        private static float lastLootScanTime = 0f;
        private static Camera cachedMainCamera = null;
        private static float lastCameraCheck = 0f;
        private static Dictionary<int, string> itemNamesById = new Dictionary<int, string>();
        private static FieldInfo fi_isAttached;
        private GUIStyle espTextStyle;
        private GUIStyle espBoxStyle;
        private GUIStyle espPresetBtnStyle;
        private GUIStyle paletteBtnStyle;
        private GUIStyle paletteBtnActiveStyle;
        private GUIStyle espPreviewStyle;

        // Actor ESP (Allies & Enemies) state
        private static List<Mimic.Actors.ProtoActor> cachedActorsList = new List<Mimic.Actors.ProtoActor>();
        private static float lastActorScanTime = 0f;
        private static Dictionary<int, string> monsterNamesById = new Dictionary<int, string>();
        private GUIStyle actorEspTextStyle;
        private static float lastAdminSyncTime = 0f;

        // Item spawner state
        private static List<ItemEntry> allItems = null;
        private static List<ItemEntry> filteredItems = new List<ItemEntry>();
        private static string searchQuery = "";
        private static string lastSearchQuery = null;
        private static int selectedCategory = 0; // 0: All, 1: Equipment, 2: Consumable, 3: Miscellany
        private static int lastCategory = -1;
        private static Vector2 scrollPos;
        private static ItemEntry selectedItem = null;
        private static string statusMessage = "";
        private static float statusTimer = 0f;

        // Cached internal getters for Hub
        private static MethodInfo mi_get_dataman;
        private static MethodInfo mi_get_tableman;
        private static MethodInfo mi_get_netman2;
        private static MethodInfo mi_get_vworld;
        private static FieldInfo fi_lcman;
        private static FieldInfo fi_proto_inventory;
        private static FieldInfo fi_vrooms;
        private static MethodInfo mi_set_dead;
        private static MethodInfo mi_set_deadTime;

        // Cached looting object table
        private static MMLootingObjectTable lootingObjectTable;

        // Styles & Textures
        private static Texture2D whiteTex;
        private GUIStyle headerTitleStyle;
        private GUIStyle headerHintStyle;
        private GUIStyle badgeTextStyle;
        private GUIStyle navBtnActiveStyle;
        private GUIStyle navBtnInactiveStyle;
        private GUIStyle profileDotStyle;
        private GUIStyle profileNameStyle;
        private GUIStyle profileSubStyle;
        private GUIStyle modernSectionHeaderStyle;
        private GUIStyle modernCardStyle;
        private GUIStyle modernCardTitleStyle;
        private GUIStyle modernCardDescStyle;
        private GUIStyle modernCardKeyStyle;
        private GUIStyle switchActiveStyle;
        private GUIStyle switchInactiveStyle;
        private GUIStyle filterAllyActiveStyle;
        private GUIStyle filterEnemyActiveStyle;
        private GUIStyle btnGreenStyle;
        private GUIStyle btnRedStyle;
        private GUIStyle btnDarkStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle rowBtnStyleOn;
        private GUIStyle rowBtnStyleOff;
        private GUIStyle actionBtnStyle;
        private GUIStyle tabBtnActive;
        private GUIStyle tabBtnInactive;
        private GUIStyle filterBtnActive;
        private GUIStyle filterBtnInactive;
        private GUIStyle itemRowStyle;
        private GUIStyle itemRowSelectedStyle;
        private GUIStyle itemLabelStyle;
        private GUIStyle itemLabelSelectedStyle;
        private GUIStyle itemSubLabelStyle;
        private GUIStyle selectBtnStyle;
        private GUIStyle checkmarkSelectedStyle;
        private GUIStyle checkmarkUnselectedStyle;
        private GUIStyle spawnBtnPrimary;
        private GUIStyle spawnBtnSecondary;
        private GUIStyle cardBoxStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardSubStyle;
        private GUIStyle cardKeyStyle;
        private GUIStyle searchFieldStyle;
        private GUIStyle statusStyle;
        private bool stylesInitialized = false;

        public override void OnInitializeMelon()
        {
            Category = MelonPreferences.CreateCategory("MimesisMenu");
            Pref_GodMode = Category.CreateEntry("GodMode", false, "God Mode (Invulnerability)");
            Pref_InfiniteStamina = Category.CreateEntry("InfiniteStamina", false, "Infinite Stamina");
            Pref_NoWeightSlowdown = Category.CreateEntry("NoWeightSlowdown", false, "No Weight Slowdown");
            Pref_SilentSteps = Category.CreateEntry("SilentSteps", false, "Silent Steps");
            Pref_InfiniteBattery = Category.CreateEntry("InfiniteBattery", false, "Infinite Battery & Durability");
            Pref_AntiContamination = Category.CreateEntry("AntiContamination", false, "Anti-Contamination");
            Pref_FullBrightEnabled = Category.CreateEntry("FullBrightEnabled", false, "FullBright (Night Vision)");
            Pref_FullBrightIntensity = Category.CreateEntry("FullBrightIntensity", 1.2f, "FullBright Intensity");
            Pref_ShowTeamStatus = Category.CreateEntry("ShowTeamStatus", false, "Show Team Status HUD (Alive / Dead)");
            Pref_ItemEspEnabled = Category.CreateEntry("ItemEspEnabled", false, "Item ESP Through Walls");
            Pref_ItemEspDistance = Category.CreateEntry("ItemEspDistance", 40f, "Item ESP Max Distance");
            Pref_ItemEspColorR = Category.CreateEntry("ItemEspColorR", 1.0f, "Item ESP Color R");
            Pref_ItemEspColorG = Category.CreateEntry("ItemEspColorG", 0.85f, "Item ESP Color G");
            Pref_ItemEspColorB = Category.CreateEntry("ItemEspColorB", 0.20f, "Item ESP Color B");
            Pref_ActorEspEnabled = Category.CreateEntry("ActorEspEnabled", false, "Player & Monster ESP");
            Pref_ActorEspShowAllies = Category.CreateEntry("ActorEspShowAllies", true, "Show Allies in ESP");
            Pref_ActorEspShowEnemies = Category.CreateEntry("ActorEspShowEnemies", true, "Show Enemies in ESP");
            Pref_ActorEspShowName = Category.CreateEntry("ActorEspShowName", true, "Show Name in Actor ESP");
            Pref_ActorEspShowDist = Category.CreateEntry("ActorEspShowDist", true, "Show Distance in Actor ESP");
            Pref_ActorEspShowHp = Category.CreateEntry("ActorEspShowHp", true, "Show HP in Actor ESP");
            Pref_ActorEspShowRadiation = Category.CreateEntry("ActorEspShowRadiation", true, "Show Radiation in Actor ESP");
            Pref_ActorEspDistance = Category.CreateEntry("ActorEspDistance", 100f, "Actor ESP Max Distance");
            Pref_ActorEspFontSize = Category.CreateEntry("ActorEspFontSize", 12f, "Actor ESP Font Size");
            Pref_ItemEspFontSize = Category.CreateEntry("ItemEspFontSize", 12f, "Item ESP Font Size");
            Pref_WindowX = Category.CreateEntry("WindowX", 60f, "Window X Position");
            Pref_WindowY = Category.CreateEntry("WindowY", 60f, "Window Y Position");

            // Keybind preferences
            Pref_KeyMenu = Category.CreateEntry("Key_Menu", "F7", "Keybind: Toggle Menu");
            Pref_KeyGodMode = Category.CreateEntry("Key_GodMode", "None", "Keybind: God Mode");
            Pref_KeyStamina = Category.CreateEntry("Key_Stamina", "None", "Keybind: Infinite Stamina");
            Pref_KeyNoWeight = Category.CreateEntry("Key_NoWeight", "None", "Keybind: No Weight");
            Pref_KeySilentSteps = Category.CreateEntry("Key_SilentSteps", "None", "Keybind: Silent Steps");
            Pref_KeyBattery = Category.CreateEntry("Key_Battery", "None", "Keybind: Infinite Battery");
            Pref_KeyAntiConta = Category.CreateEntry("Key_AntiConta", "None", "Keybind: Anti-Contamination");
            Pref_KeyFullBright = Category.CreateEntry("Key_FullBright", "None", "Keybind: FullBright");
            Pref_KeyTeamHud = Category.CreateEntry("Key_TeamHud", "None", "Keybind: Team Status HUD");
            Pref_KeyItemEsp = Category.CreateEntry("Key_ItemEsp", "None", "Keybind: Item ESP");
            Pref_KeyActorEsp = Category.CreateEntry("Key_ActorEsp", "None", "Keybind: Actor ESP");
            Pref_ItemBinds = Category.CreateEntry("ItemBinds", "", "Saved Item Spawn Binds");

            windowRect = new Rect(Pref_WindowX.Value, Pref_WindowY.Value, 960f, 640f);

            try
            {
                allInputKeys = (Key[])Enum.GetValues(typeof(Key));
            }
            catch
            {
                allInputKeys = new Key[0];
            }

            keybindEntries = new List<KeybindEntry>
            {
                new KeybindEntry("menu", "Открыть / Закрыть меню", Pref_KeyMenu, null, Key.F7),
                new KeybindEntry("godmode", "Режим Бога (God Mode)", Pref_KeyGodMode, Pref_GodMode, Key.None),
                new KeybindEntry("stamina", "Бесконечный бег (Infinite Stamina)", Pref_KeyStamina, Pref_InfiniteStamina, Key.None),
                new KeybindEntry("noweight", "Без штрафа веса (No Weight)", Pref_KeyNoWeight, Pref_NoWeightSlowdown, Key.None),
                new KeybindEntry("silentsteps", "Бесшумные шаги (Silent Steps)", Pref_KeySilentSteps, Pref_SilentSteps, Key.None),
                new KeybindEntry("battery", "Вечный фонарик (Infinite Battery)", Pref_KeyBattery, Pref_InfiniteBattery, Key.None),
                new KeybindEntry("anticonta", "Защита от дождя (Anti-Contamination)", Pref_KeyAntiConta, Pref_AntiContamination, Key.None),
                new KeybindEntry("fullbright", "Полный свет (FullBright)", Pref_KeyFullBright, Pref_FullBrightEnabled, Key.None),
                new KeybindEntry("teamhud", "Статус команды (Team Status HUD)", Pref_KeyTeamHud, Pref_ShowTeamStatus, Key.None),
                new KeybindEntry("itemesp", "Подсветка предметов (Item ESP)", Pref_KeyItemEsp, Pref_ItemEspEnabled, Key.None),
                new KeybindEntry("actoresp", "Подсветка игроков и врагов (Actor ESP)", Pref_KeyActorEsp, Pref_ActorEspEnabled, Key.None),
            };

            LoadItemBinds();

            try
            {
                mi_get_dataman = AccessTools.PropertyGetter(typeof(Hub), "dataman");
                mi_get_tableman = AccessTools.PropertyGetter(typeof(Hub), "tableman");
                mi_get_netman2 = AccessTools.PropertyGetter(typeof(Hub), "netman2");
                mi_get_vworld = AccessTools.PropertyGetter(typeof(Hub), "vworld");
                fi_lcman = AccessTools.Field(typeof(Hub), "lcman");
                fi_proto_inventory = AccessTools.Field(typeof(Mimic.Actors.ProtoActor), "inventory");
                fi_vrooms = AccessTools.Field(typeof(VRoomManager), "_vrooms");
                fi_isAttached = AccessTools.Field(typeof(LootingLevelObject), "_isAttached");
                mi_set_dead = AccessTools.PropertySetter(typeof(Mimic.Actors.ProtoActor), "dead");
                mi_set_deadTime = AccessTools.PropertySetter(typeof(Mimic.Actors.ProtoActor), "deadTime");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error resolving accessors: {ex}");
            }

            LoggerInstance.Msg("CustomMenu 1.7.0 by Pattern loaded! GodMode, FullBright, Actor ESP and Keybinds ready.");
        }

        public static Key ParseKey(string keyStr, Key fallback = Key.None)
        {
            if (string.IsNullOrEmpty(keyStr)) return fallback;
            if (Enum.TryParse<Key>(keyStr, true, out var k)) return k;
            return fallback;
        }

        public static string GetKeyDisplayName(Key key)
        {
            if (key == Key.None) return "—";
            string s = key.ToString();
            if (s.StartsWith("Digit")) return s.Substring(5);
            if (s.StartsWith("Numpad")) return "Num " + s.Substring(6);
            return s;
        }

        public static void ApplyKeybind(string actionId, Key newKey)
        {
            for (int i = 0; i < keybindEntries.Count; i++)
            {
                if (keybindEntries[i].Id == actionId)
                {
                    keybindEntries[i].CurrentKey = newKey;
                    if (keybindEntries[i].PrefEntry != null)
                    {
                        keybindEntries[i].PrefEntry.Value = newKey.ToString();
                    }
                    SavePrefs();
                    break;
                }
            }
        }

        public static void ResetAllKeybinds()
        {
            for (int i = 0; i < keybindEntries.Count; i++)
            {
                keybindEntries[i].CurrentKey = keybindEntries[i].DefaultKey;
                if (keybindEntries[i].PrefEntry != null)
                {
                    keybindEntries[i].PrefEntry.Value = keybindEntries[i].DefaultKey.ToString();
                }
            }
            SavePrefs();
        }

        public static Key GetBoundKey(string actionId, Key fallback = Key.None)
        {
            for (int i = 0; i < keybindEntries.Count; i++)
            {
                if (keybindEntries[i].Id == actionId)
                {
                    return keybindEntries[i].CurrentKey;
                }
            }
            return fallback;
        }

        public static string GetBoundKeyDisplay(string actionId)
        {
            Key k = GetBoundKey(actionId, Key.None);
            return (k == Key.None) ? null : GetKeyDisplayName(k);
        }

        public static void LoadItemBinds()
        {
            itemSpawnBinds.Clear();
            if (Pref_ItemBinds == null || string.IsNullOrEmpty(Pref_ItemBinds.Value)) return;

            string[] tokens = Pref_ItemBinds.Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                string[] parts = tokens[i].Split(':');
                if (parts.Length >= 5)
                {
                    // New 5-part format: id:masterId:toInven:boundKey:itemName
                    string bId = parts[0];
                    if (int.TryParse(parts[1], out int id))
                    {
                        bool toInven = parts[2].ToLower() == "true" || parts[2] == "1";
                        Key k = ParseKey(parts[3], Key.None);
                        if (k != Key.None)
                        {
                            string name = parts[4];
                            itemSpawnBinds.Add(new ItemSpawnBind
                            {
                                Id = !string.IsNullOrEmpty(bId) ? bId : Guid.NewGuid().ToString("N").Substring(0, 8),
                                MasterId = id,
                                ItemName = !string.IsNullOrEmpty(name) ? name : GetItemDisplayName(id),
                                ToInventory = toInven,
                                BoundKey = k
                            });
                        }
                    }
                }
                else if (parts.Length >= 3)
                {
                    // Legacy 3/4-part format: masterId:toInven:boundKey[:itemName]
                    if (int.TryParse(parts[0], out int id))
                    {
                        bool toInven = parts[1].ToLower() == "true" || parts[1] == "1";
                        Key k = ParseKey(parts[2], Key.None);
                        if (k != Key.None)
                        {
                            string name = parts.Length >= 4 ? parts[3] : GetItemDisplayName(id);
                            itemSpawnBinds.Add(new ItemSpawnBind
                            {
                                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                                MasterId = id,
                                ItemName = name,
                                ToInventory = toInven,
                                BoundKey = k
                            });
                        }
                    }
                }
            }
            itemSpawnBinds.RemoveAll(b => b.BoundKey == Key.None);
            SaveItemBinds();
        }

        public static void SaveItemBinds()
        {
            if (Pref_ItemBinds == null) return;
            List<string> parts = new List<string>();
            for (int i = 0; i < itemSpawnBinds.Count; i++)
            {
                var b = itemSpawnBinds[i];
                if (b.BoundKey != Key.None)
                {
                    if (string.IsNullOrEmpty(b.Id)) b.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                    parts.Add($"{b.Id}:{b.MasterId}:{(b.ToInventory ? "1" : "0")}:{b.BoundKey}:{b.ItemName}");
                }
            }
            Pref_ItemBinds.Value = string.Join(";", parts);
            SavePrefs();
        }

        public static List<ItemSpawnBind> GetBindsForItem(int masterId)
        {
            var list = new List<ItemSpawnBind>();
            for (int i = 0; i < itemSpawnBinds.Count; i++)
            {
                if (itemSpawnBinds[i].MasterId == masterId && itemSpawnBinds[i].BoundKey != Key.None)
                {
                    list.Add(itemSpawnBinds[i]);
                }
            }
            return list;
        }

        public static ItemSpawnBind GetItemBind(int masterId)
        {
            for (int i = 0; i < itemSpawnBinds.Count; i++)
            {
                if (itemSpawnBinds[i].MasterId == masterId && itemSpawnBinds[i].BoundKey != Key.None)
                {
                    return itemSpawnBinds[i];
                }
            }
            return null;
        }

        public static void SetItemBind(int masterId, string itemName, bool toInventory, Key key)
        {
            if (key == Key.None) return;
            // Remove previous bind for this item (replace existing)
            RemoveItemBind(masterId);
            // Remove any other item bound to the same key (prevent conflicts)
            itemSpawnBinds.RemoveAll(b => b.BoundKey == key);

            itemSpawnBinds.Add(new ItemSpawnBind
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                MasterId = masterId,
                ItemName = itemName,
                ToInventory = toInventory,
                BoundKey = key
            });
            SaveItemBinds();
        }

        public static void RemoveItemBindById(string bindId)
        {
            itemSpawnBinds.RemoveAll(b => b.Id == bindId);
            SaveItemBinds();
        }

        public static void RemoveItemBind(int masterId)
        {
            itemSpawnBinds.RemoveAll(b => b.MasterId == masterId);
            SaveItemBinds();
        }

        public static ItemEntry FindItemByMasterId(int masterId)
        {
            if (allItems != null)
            {
                for (int i = 0; i < allItems.Count; i++)
                {
                    if (allItems[i].MasterId == masterId) return allItems[i];
                }
            }
            return new ItemEntry { MasterId = masterId, DisplayName = GetItemDisplayName(masterId), ItemType = ItemType.Equipment, Weight = 1 };
        }

        public static void RestoreGameCursor()
        {
            try
            {
                var localPlayer = GetLocalPlayer();
                var vworld = GetVWorld();

                // If outside active gameplay session (Main menu, Lobby, Setup):
                if (localPlayer == null || vworld == null || !localPlayer.gameObject.activeInHierarchy)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    return;
                }

                // Check if any in-game UI/menu is currently open (Esc menu, F1 tram settings, dialogs, etc.)
                bool isGameMenuOpen = false;
                try
                {
                    var uiman = UnityEngine.Object.FindAnyObjectByType<UIManager>();
                    if (uiman != null)
                    {
                        if (uiman.isGameMenuOpen) isGameMenuOpen = true;
                        if (uiman.ui_escapeStack != null && uiman.ui_escapeStack.Count > 0) isGameMenuOpen = true;
                        if (uiman.inGameMenu != null && uiman.inGameMenu.gameObject.activeInHierarchy) isGameMenuOpen = true;
                        if (uiman.settingsUI != null && uiman.settingsUI.gameObject.activeInHierarchy) isGameMenuOpen = true;
                        if (uiman.tramPasswordPopupUI != null && uiman.tramPasswordPopupUI.gameObject.activeInHierarchy) isGameMenuOpen = true;
                    }
                    if (UIPrefab_TramSettingPopup.Current != null && UIPrefab_TramSettingPopup.Current.gameObject.activeInHierarchy) isGameMenuOpen = true;
                }
                catch { }

                if (isGameMenuOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    // In Unity, CursorLockMode.Locked natively centers and hides the cursor in first-person gameplay.
                    // Leaving Cursor.visible = true ensures that whenever the game unlocks the cursor (Esc menu, F1 tram settings, etc.),
                    // the cursor is immediately visible and clickable!
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = true;
                }
            }
            catch
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public static void ToggleMenu()
        {
            ShowMenu = !ShowMenu;
            if (ShowMenu)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                if (allItems == null)
                {
                    LoadItems();
                }
            }
            else
            {
                RestoreGameCursor();
                isDragging = false;
                waitingForKeyAction = null;
                waitingForItemBindMasterId = 0;
                Pref_WindowX.Value = windowRect.x;
                Pref_WindowY.Value = windowRect.y;
                SavePrefs();
            }
        }

        public static DataManager GetDataManager()
        {
            if (Hub.s == null || mi_get_dataman == null) return null;
            return mi_get_dataman.Invoke(Hub.s, null) as DataManager;
        }

        public static void EnsureTablesLoaded()
        {
            if (lootingObjectTable != null) return;
            try
            {
                if (Hub.s == null || mi_get_tableman == null) return;
                object tm = mi_get_tableman.Invoke(Hub.s, null);
                if (tm != null)
                {
                    Type tmType = tm.GetType();
                    lootingObjectTable = AccessTools.Field(tmType, "lootingObject")?.GetValue(tm) as MMLootingObjectTable;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading table references: {ex}");
            }
        }

        public static NetworkManagerV2 GetNetworkManager()
        {
            if (Hub.s == null || mi_get_netman2 == null) return null;
            return mi_get_netman2.Invoke(Hub.s, null) as NetworkManagerV2;
        }

        public static VWorld GetVWorld()
        {
            if (Hub.s == null || mi_get_vworld == null) return null;
            return mi_get_vworld.Invoke(Hub.s, null) as VWorld;
        }

        public static L10NManager GetL10NManager()
        {
            if (Hub.s == null || fi_lcman == null) return null;
            return fi_lcman.GetValue(Hub.s) as L10NManager;
        }

        public static Mimic.Actors.ProtoActor GetLocalPlayer()
        {
            try
            {
                var actors = UnityEngine.Object.FindObjectsByType<Mimic.Actors.ProtoActor>(FindObjectsSortMode.None);
                for (int i = 0; i < actors.Length; i++)
                {
                    if (actors[i] != null && actors[i].AmIAvatar())
                    {
                        return actors[i];
                    }
                }
            }
            catch { }
            return null;
        }

        public override void OnUpdate()
        {
            if (Keyboard.current != null)
            {
                // 1. Interactive key listening for rebinding
                if (waitingForKeyAction != null)
                {
                    if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        waitingForKeyAction = null;
                    }
                    else if (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
                    {
                        ApplyKeybind(waitingForKeyAction, Key.None);
                        waitingForKeyAction = null;
                    }
                    else if (allInputKeys != null)
                    {
                        for (int i = 0; i < allInputKeys.Length; i++)
                        {
                            Key k = allInputKeys[i];
                            if (k == Key.None || k == Key.Escape || k == Key.Delete || k == Key.Backspace) continue;
                            try
                            {
                                var ctrl = Keyboard.current[k];
                                if (ctrl != null && ctrl.wasPressedThisFrame)
                                {
                                    ApplyKeybind(waitingForKeyAction, k);
                                    waitingForKeyAction = null;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
                else if (waitingForItemBindMasterId != 0)
                {
                    if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        waitingForItemBindMasterId = 0;
                    }
                    else if (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
                    {
                        RemoveItemBind(waitingForItemBindMasterId);
                        waitingForItemBindMasterId = 0;
                    }
                    else if (allInputKeys != null)
                    {
                        for (int i = 0; i < allInputKeys.Length; i++)
                        {
                            Key k = allInputKeys[i];
                            if (k == Key.None || k == Key.Escape || k == Key.Delete || k == Key.Backspace) continue;
                            try
                            {
                                var ctrl = Keyboard.current[k];
                                if (ctrl != null && ctrl.wasPressedThisFrame)
                                {
                                    string itName = (selectedItem != null && selectedItem.MasterId == waitingForItemBindMasterId) ? selectedItem.DisplayName : GetItemDisplayName(waitingForItemBindMasterId);
                                    SetItemBind(waitingForItemBindMasterId, itName, pendingBindToInventory, k);
                                    statusMessage = $"✓ [{itName}] привязан к [{GetKeyDisplayName(k)}] ({(pendingBindToInventory ? "в руки" : "на землю")})";
                                    statusTimer = 3.5f;
                                    waitingForItemBindMasterId = 0;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    // 2. Open / Close Menu hotkey
                    Key menuKey = GetBoundKey("menu", Key.F7);
                    if (menuKey != Key.None)
                    {
                        try
                        {
                            var ctrl = Keyboard.current[menuKey];
                            if (ctrl != null && ctrl.wasPressedThisFrame)
                            {
                                ToggleMenu();
                            }
                        }
                        catch { }
                    }

                    // 3. Hotkeys for cheat features & item spawns (only when not typing in search field)
                    if (GUI.GetNameOfFocusedControl() != "SearchFieldInput")
                    {
                        for (int i = 0; i < keybindEntries.Count; i++)
                        {
                            var entry = keybindEntries[i];
                            if (entry.Id == "menu" || entry.TogglePref == null) continue;
                            if (entry.CurrentKey == Key.None) continue;

                            try
                            {
                                var ctrl = Keyboard.current[entry.CurrentKey];
                                if (ctrl != null && ctrl.wasPressedThisFrame)
                                {
                                    entry.TogglePref.Value = !entry.TogglePref.Value;
                                    if (entry.TogglePref == Pref_ShowTeamStatus && entry.TogglePref.Value)
                                    {
                                        lastTeamScanTime = 0f;
                                        lastInvenScanTime = 0f;
                                    }
                                    if (entry.TogglePref == Pref_ItemEspEnabled && entry.TogglePref.Value)
                                    {
                                        lastLootScanTime = 0f;
                                    }
                                    if (entry.TogglePref == Pref_ActorEspEnabled && entry.TogglePref.Value)
                                    {
                                        lastActorScanTime = 0f;
                                    }
                                    SavePrefs();
                                }
                            }
                            catch { }
                        }

                        // 4. Hotkeys for quick item spawning
                        for (int i = 0; i < itemSpawnBinds.Count; i++)
                        {
                            var bind = itemSpawnBinds[i];
                            if (bind.BoundKey == Key.None) continue;

                            try
                            {
                                var ctrl = Keyboard.current[bind.BoundKey];
                                if (ctrl != null && ctrl.wasPressedThisFrame)
                                {
                                    ItemEntry it = FindItemByMasterId(bind.MasterId);
                                    if (it != null)
                                    {
                                        TriggerSpawn(it, bind.ToInventory);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            if (ShowMenu)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }

                if (statusTimer > 0f)
                {
                    statusTimer -= Time.unscaledDeltaTime;
                    if (statusTimer <= 0f)
                    {
                        statusMessage = "";
                    }
                }
            }
            else
            {
                // When RenUI is closed: whenever the game or any UI unlocks the cursor (Esc menu, F1 tram settings, etc.),
                // ensure the cursor is instantly visible!
                if (Cursor.lockState == CursorLockMode.None && !Cursor.visible)
                {
                    Cursor.visible = true;
                }
            }

            // FullBright live update
            UpdateFullBright();

            // GodMode live update
            if (Pref_GodMode != null && Pref_GodMode.Value)
            {
                ApplyGodModeFrame();
            }

            // Cheats live enforcement (Infinite Stamina, Anti-Contamination, No Weight Slowdown)
            try
            {
                var local = GetLocalPlayer();
                if (local != null && local.gameObject.activeInHierarchy && local.netSyncActorData != null)
                {
                    if (Pref_InfiniteStamina != null && Pref_InfiniteStamina.Value)
                    {
                        long maxStamina = local.netSyncActorData.maxStamina > 0 ? local.netSyncActorData.maxStamina : 10000L;
                        local.netSyncActorData.stamina = maxStamina;
                    }

                    if (Pref_AntiContamination != null && Pref_AntiContamination.Value)
                    {
                        local.netSyncActorData.conta = 0L;
                    }

                    if (Pref_NoWeightSlowdown != null && Pref_NoWeightSlowdown.Value)
                    {
                        if (local.netSyncActorData.MoveSpeedWalk < 400L) local.netSyncActorData.MoveSpeedWalk = 400L;
                        if (local.netSyncActorData.MoveSpeedRun < 600L) local.netSyncActorData.MoveSpeedRun = 600L;
                    }
                }
            }
            catch { }

            // Multiplayer periodic server synchronization (keeps host in sync for guest clients)
            if (Time.unscaledTime - lastAdminSyncTime > 2.0f)
            {
                lastAdminSyncTime = Time.unscaledTime;
                try
                {
                    var netman = GetNetworkManager();
                    if (netman != null)
                    {
                        if (Pref_GodMode != null && Pref_GodMode.Value)
                        {
                            netman.SendNoCallback(new AdminCommandReq { command = "undying", args = "flag=true" });
                        }
                        if (Pref_AntiContamination != null && Pref_AntiContamination.Value)
                        {
                            netman.SendNoCallback(new AdminCommandReq { command = "removeconta", args = "" });
                        }
                    }
                }
                catch { }
            }

            // Team status HUD scan
            if (Pref_ShowTeamStatus != null && Pref_ShowTeamStatus.Value)
            {
                if (Time.unscaledTime - lastTeamScanTime > 0.25f)
                {
                    lastTeamScanTime = Time.unscaledTime;
                    UpdateTeamData();
                }
                if (Time.unscaledTime - lastInvenScanTime > 0.5f)
                {
                    lastInvenScanTime = Time.unscaledTime;
                    UpdateSlotPosition();
                }
            }

            // Item ESP scan
            if (Pref_ItemEspEnabled != null && Pref_ItemEspEnabled.Value)
            {
                if (Time.unscaledTime - lastLootScanTime > 0.7f)
                {
                    lastLootScanTime = Time.unscaledTime;
                    UpdateLootObjects();
                }
            }

            // Actor ESP scan
            if (Pref_ActorEspEnabled != null && Pref_ActorEspEnabled.Value)
            {
                if (Time.unscaledTime - lastActorScanTime > 0.4f)
                {
                    lastActorScanTime = Time.unscaledTime;
                    UpdateCachedActors();
                }
            }
        }

        private static void ApplyGodModeFrame()
        {
            try
            {
                var local = GetLocalPlayer();
                if (local != null && local.gameObject.activeInHierarchy)
                {
                    if (local.netSyncActorData != null)
                    {
                        long maxHp = local.netSyncActorData.maxHP > 0 ? local.netSyncActorData.maxHP : 100L;
                        local.netSyncActorData.hp = maxHp;
                    }
                    mi_set_dead?.Invoke(local, new object[] { false });
                    mi_set_deadTime?.Invoke(local, new object[] { 0f });
                    var charController = local.GetComponent("CharacterController") as Behaviour;
                    if (charController != null && !charController.enabled)
                    {
                        charController.enabled = true;
                    }
                }
            }
            catch { }
        }

        private static void UpdateFullBright()
        {
            try
            {
                if (Pref_FullBrightEnabled != null && Pref_FullBrightEnabled.Value)
                {
                    Camera cam = GetMainCamera();
                    if (cam != null)
                    {
                        if (fullBrightLightGo == null)
                        {
                            fullBrightLightGo = new GameObject("CustomMenu_FullBrightLight");
                            UnityEngine.Object.DontDestroyOnLoad(fullBrightLightGo);
                            fullBrightLight = fullBrightLightGo.AddComponent<Light>();
                            fullBrightLight.type = LightType.Point;
                            fullBrightLight.range = 300f;
                            fullBrightLight.shadows = LightShadows.None;
                            fullBrightLight.color = Color.white;
                            fullBrightLight.cullingMask = ~0;
                        }

                        fullBrightLightGo.transform.position = cam.transform.position;
                        fullBrightLight.intensity = Pref_FullBrightIntensity != null ? Pref_FullBrightIntensity.Value : 1.2f;
                        if (!fullBrightLight.enabled) fullBrightLight.enabled = true;
                    }

                    if (!originalAmbientSaved)
                    {
                        savedAmbientLight = RenderSettings.ambientLight;
                        savedAmbientIntensity = RenderSettings.ambientIntensity;
                        savedAmbientMode = RenderSettings.ambientMode;
                        originalAmbientSaved = true;
                    }

                    float intens = Pref_FullBrightIntensity != null ? Pref_FullBrightIntensity.Value : 1.2f;
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                    RenderSettings.ambientLight = Color.white * Mathf.Clamp(intens * 0.45f, 0.2f, 1.0f);
                    RenderSettings.ambientIntensity = Mathf.Clamp(intens, 0.5f, 4.0f);
                }
                else
                {
                    if (fullBrightLight != null && fullBrightLight.enabled)
                    {
                        fullBrightLight.enabled = false;
                    }
                    if (originalAmbientSaved)
                    {
                        RenderSettings.ambientLight = savedAmbientLight;
                        RenderSettings.ambientIntensity = savedAmbientIntensity;
                        RenderSettings.ambientMode = savedAmbientMode;
                        originalAmbientSaved = false;
                    }
                }
            }
            catch { }
        }

        public static void LoadItems()
        {
            try
            {
                var dataman = GetDataManager();
                if (dataman?.ExcelDataManager?.ItemInfoDict == null)
                {
                    return;
                }

                EnsureTablesLoaded();
                var lcman = GetL10NManager();
                var dict = dataman.ExcelDataManager.ItemInfoDict;

                var list = new List<ItemEntry>();
                foreach (var kvp in dict)
                {
                    var info = kvp.Value;
                    if (info == null) continue;

                    string disp = null;
                    if (lcman != null && !string.IsNullOrEmpty(info.Name))
                    {
                        try
                        {
                            disp = lcman.GetText(info.Name);
                            if (disp != null && disp.StartsWith("|L10N")) disp = null;
                        }
                        catch { }
                    }

                    if (string.IsNullOrEmpty(disp))
                    {
                        disp = !string.IsNullOrEmpty(info.Name) ? info.Name : $"Item #{info.MasterID}";
                    }

                    if (itemNamesById == null) itemNamesById = new Dictionary<int, string>();
                    itemNamesById[info.MasterID] = disp;

                    int pMin = info.PriceForSellMin;
                    int pMax = info.PriceForSellMax;
                    int pMean = 0;
                    try { pMean = info.GetMeanPrice(); } catch { }

                    list.Add(new ItemEntry
                    {
                        MasterId = info.MasterID,
                        RawName = info.Name ?? "",
                        DisplayName = disp,
                        ItemType = info.ItemType,
                        Weight = info.Weight,
                        PriceMin = pMin,
                        PriceMax = pMax,
                        MeanPrice = pMean
                    });
                }

                list.Sort((a, b) =>
                {
                    int c = a.ItemType.CompareTo(b.ItemType);
                    if (c != 0) return c;
                    return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                });

                allItems = list;
                lastSearchQuery = null;
                UpdateFilteredItems();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Spawner] Error loading items: {ex}");
            }
        }

        private static void UpdateFilteredItems()
        {
            if (allItems == null)
            {
                filteredItems.Clear();
                return;
            }

            string q = searchQuery.Trim().ToLowerInvariant();
            List<ItemEntry> res = new List<ItemEntry>();

            for (int i = 0; i < allItems.Count; i++)
            {
                var it = allItems[i];

                if (selectedCategory == 1 && it.ItemType != ItemType.Equipment) continue;
                if (selectedCategory == 2 && it.ItemType != ItemType.Consumable) continue;
                if (selectedCategory == 3 && it.ItemType != ItemType.Miscellany) continue;

                if (!string.IsNullOrEmpty(q))
                {
                    bool match = (it.DisplayName != null && it.DisplayName.ToLowerInvariant().Contains(q)) ||
                                 (it.RawName != null && it.RawName.ToLowerInvariant().Contains(q)) ||
                                 it.MasterId.ToString().Contains(q);
                    if (!match) continue;
                }

                res.Add(it);
            }

            filteredItems = res;
            lastSearchQuery = searchQuery;
            lastCategory = selectedCategory;

            if (selectedItem == null && filteredItems.Count > 0)
            {
                selectedItem = filteredItems[0];
            }
        }

        public static void TriggerSpawn(ItemEntry item, bool toInventory)
        {
            if (item == null) return;
            try
            {
                var localPlayer = GetLocalPlayer();
                var vworld = GetVWorld();

                // Determine item price for spawning
                int targetPrice = 0;
                if (item.PriceMin > 0 && item.PriceMax >= item.PriceMin)
                {
                    targetPrice = (item.PriceMin == item.PriceMax) ? item.PriceMin : UnityEngine.Random.Range(item.PriceMin, item.PriceMax + 1);
                }
                else if (item.MeanPrice > 0)
                {
                    targetPrice = item.MeanPrice;
                }

                bool success = false;
                string details = "";

                if (toInventory)
                {
                    bool given = false;

                    // 1. Host authoritative give (solo / host of lobby)
                    try
                    {
                        if (vworld != null && vworld.VRoomManager != null && fi_vrooms != null)
                        {
                            var vroomsDict = fi_vrooms.GetValue(vworld.VRoomManager) as Dictionary<long, IVroom>;
                            IVroom room = null;
                            if (vroomsDict != null)
                            {
                                foreach (var r in vroomsDict.Values)
                                {
                                    if (r != null) { room = r; break; }
                                }
                            }

                            if (room != null)
                            {
                                VPlayer targetVPlayer = null;
                                room.IterateAllActor(a =>
                                {
                                    if (a is VPlayer vp)
                                    {
                                        if (localPlayer != null && (vp.ObjectID == localPlayer.ActorID || vp.SteamID == localPlayer.steamID))
                                        {
                                            targetVPlayer = vp;
                                        }
                                        else if (targetVPlayer == null)
                                        {
                                            targetVPlayer = vp;
                                        }
                                    }
                                });

                                if (targetVPlayer != null && targetVPlayer.InventoryControlUnit != null)
                                {
                                    var elem = room.GetNewItemElement(item.MasterId, false, 1, 10000, 10000, targetPrice);
                                    if (elem != null)
                                    {
                                        var err = targetVPlayer.InventoryControlUnit.HandleAddItem(elem, out int addedSlot, true, false);
                                        if (err == ReluProtocol.Enum.MsgErrorCode.Success)
                                        {
                                            targetVPlayer.InventoryControlUnit.HandleChangeActiveInvenSlot(addedSlot, true, 0);
                                            given = true;
                                            success = true;
                                            details = $"в руки (хост, слот #{addedSlot})";
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[Spawner] Host inven give notice: {ex.Message}");
                    }

                    // 2. Client in multiplayer lobby: send AdminCommandReq to host
                    if (!given)
                    {
                        try
                        {
                            var netman = GetNetworkManager();
                            if (netman != null)
                            {
                                var req = new AdminCommandReq
                                {
                                    command = "giveitem",
                                    args = $"masterid={item.MasterId}^price={targetPrice}^hand=true"
                                };
                                netman.SendNoCallback(req);
                                given = true;
                                success = true;
                                details = "в руки (сеть)";
                            }
                        }
                        catch { }
                    }

                    // 3. Fallback: Direct local client injection if not host and network unavailable
                    if (!given && localPlayer != null && fi_proto_inventory != null)
                    {
                        try
                        {
                            object inven = fi_proto_inventory.GetValue(localPlayer);
                            if (inven != null)
                            {
                                int targetSlot = localPlayer.GetSelectedInventorySlotIndex();
                                var slotItems = localPlayer.GetInventoryItems();

                                if (slotItems != null)
                                {
                                    if (targetSlot < 0 || targetSlot >= slotItems.Count || slotItems[targetSlot] != null)
                                    {
                                        for (int s = 0; s < slotItems.Count; s++)
                                        {
                                            if (slotItems[s] == null)
                                            {
                                                targetSlot = s;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (targetSlot < 0) targetSlot = 0;

                                if (slotItems != null && targetSlot >= slotItems.Count)
                                {
                                    MethodInfo mi_ResizeSlots = inven.GetType().GetMethod("ResizeSlots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                    mi_ResizeSlots?.Invoke(inven, new object[] { targetSlot + 1 });
                                }

                                long newItemId = (long)UnityEngine.Random.Range(1000000, 9999999);
                                var info = new ReluProtocol.ItemInfo
                                {
                                    itemMasterID = item.MasterId,
                                    itemID = newItemId,
                                    itemType = item.ItemType,
                                    stackCount = 1,
                                    durability = 10000,
                                    remainGauge = 10000,
                                    isTurnOn = false,
                                    isFake = false,
                                    price = targetPrice,
                                    customIntValue = 0
                                };

                                MethodInfo mi_resolve = inven.GetType().GetMethod("ResolveInventoryItemInfo", new Type[] { typeof(ReluProtocol.ItemInfo), typeof(int) });
                                if (mi_resolve != null)
                                {
                                    mi_resolve.Invoke(inven, new object[] { info, targetSlot });
                                    success = true;
                                    details = $"слот #{targetSlot + 1}";
                                }
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    // Spawn on ground
                    Vector3 spawnPos = Vector3.zero;
                    float rotY = 0f;
                    if (localPlayer != null)
                    {
                        spawnPos = localPlayer.transform.position + localPlayer.transform.forward * 1.5f + Vector3.up * 0.35f;
                        rotY = localPlayer.transform.eulerAngles.y;
                    }
                    else
                    {
                        var cam = GetMainCamera();
                        if (cam != null)
                        {
                            spawnPos = cam.transform.position + cam.transform.forward * 2.0f;
                            rotY = cam.transform.eulerAngles.y;
                        }
                    }

                    bool spawned = false;

                    // 1. Host authoritative world spawn (creates exactly 1 item in room)
                    try
                    {
                        if (vworld != null && vworld.VRoomManager != null && fi_vrooms != null)
                        {
                            var vroomsDict = fi_vrooms.GetValue(vworld.VRoomManager) as Dictionary<long, IVroom>;
                            IVroom room = null;
                            if (vroomsDict != null)
                            {
                                foreach (var r in vroomsDict.Values)
                                {
                                    if (r != null) { room = r; break; }
                                }
                            }

                            if (room != null)
                            {
                                var elem = room.GetNewItemElement(item.MasterId, false, 1, 10000, 10000, targetPrice);
                                if (elem != null)
                                {
                                    ReluProtocol.PosWithRot pwr = new ReluProtocol.PosWithRot
                                    {
                                        pos = spawnPos,
                                        rot = new Vector3(0, rotY, 0)
                                    };
                                    int objId = room.SpawnLootingObject(elem, pwr, false, ReluProtocol.Enum.ReasonOfSpawn.Admin, 0, 0, 0L, false, false);
                                    if (objId > 0)
                                    {
                                        spawned = true;
                                        success = true;
                                        details = "на землю (хост)";
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[Spawner] Host ground spawn exception: {ex.Message}");
                    }

                    // 2. Client in multiplayer lobby: send AdminCommandReq to host (only if not already spawned by host)
                    if (!spawned)
                    {
                        try
                        {
                            var netman = GetNetworkManager();
                            if (netman != null)
                            {
                                var req = new AdminCommandReq
                                {
                                    command = "spawnitem",
                                    args = $"masterid={item.MasterId}^price={targetPrice}^fake=false"
                                };
                                netman.SendNoCallback(req);
                                spawned = true;
                                success = true;
                                details = "на землю (сеть)";
                            }
                        }
                        catch { }
                    }
                }

                if (success)
                {
                    string where = toInventory ? $"в руки" : "на землю";
                    string priceTag = targetPrice > 0 ? $" [${targetPrice}]" : "";
                    statusMessage = $"✓ [{item.DisplayName}]{priceTag} создан ({where})";
                    statusTimer = 3.5f;
                    MelonLogger.Msg($"[Spawner] Created item '{item.DisplayName}' (#{item.MasterId}){priceTag} ({where})");
                }
                else
                {
                    statusMessage = "⚠ Не удалось заспавнить предмет";
                    statusTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                statusMessage = $"❌ Ошибка спавна: {ex.Message}";
                statusTimer = 3.5f;
                MelonLogger.Error($"[Spawner] Exception spawning {item.MasterId}: {ex}");
            }
        }

        public override void OnGUI()
        {
            if (Pref_ShowTeamStatus != null && Pref_ShowTeamStatus.Value)
            {
                DrawTeamStatusHUD();
            }

            if (Pref_ItemEspEnabled != null && Pref_ItemEspEnabled.Value)
            {
                DrawItemESP();
            }

            if (Pref_ActorEspEnabled != null && Pref_ActorEspEnabled.Value)
            {
                DrawActorESP();
            }

            if (!ShowMenu) return;

            InitStyles();

            // Handle window dragging via header bar
            Rect headerRect = new Rect(windowRect.x, windowRect.y, windowRect.width, 40);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && headerRect.Contains(e.mousePosition))
            {
                isDragging = true;
                dragOffset = e.mousePosition - new Vector2(windowRect.x, windowRect.y);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (isDragging)
                {
                    Pref_WindowX.Value = windowRect.x;
                    Pref_WindowY.Value = windowRect.y;
                    Category.SaveToFile();
                    MelonPreferences.Save();
                }
                isDragging = false;
            }
            else if (isDragging && e.type == EventType.MouseDrag)
            {
                windowRect.x = e.mousePosition.x - dragOffset.x;
                windowRect.y = e.mousePosition.y - dragOffset.y;
                windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, Screen.width - windowRect.width));
                windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, Screen.height - windowRect.height));
                e.Use();
            }

            // Window Dimensions: 960 x 640
            windowRect.width = 960f;
            windowRect.height = 640f;
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, Screen.height - windowRect.height));

            // Window Background & Border
            DrawRect(windowRect, new Color(0.08f, 0.10f, 0.14f, 0.98f));
            DrawBorder(windowRect, 2, new Color(0.18f, 0.85f, 0.45f, 1f));

            // Header Bar
            DrawRect(headerRect, new Color(0.06f, 0.07f, 0.10f, 1f));
            DrawRect(new Rect(windowRect.x, windowRect.y + 39, windowRect.width, 1), new Color(0.14f, 0.17f, 0.23f, 1f));

            GUI.Label(new Rect(windowRect.x + 14, windowRect.y + 10, 80, 22), "REN UI", headerTitleStyle);

            // Version Badge (Amethyst Violet)
            Rect badgeRect = new Rect(windowRect.x + 86, windowRect.y + 12, 54, 18);
            DrawRect(badgeRect, new Color(0.55f, 0.20f, 0.88f, 1f));
            GUI.Label(badgeRect, "v1.8.0", badgeTextStyle);

            GUI.Label(new Rect(windowRect.x + windowRect.width - 260, windowRect.y + 11, 246, 20), "Зажмите шапку для перемещения ✥", headerHintStyle);

            // Left Sidebar
            float sidebarWidth = 200f;
            Rect sidebarRect = new Rect(windowRect.x, windowRect.y + 40, sidebarWidth, windowRect.height - 40);
            DrawRect(sidebarRect, new Color(0.06f, 0.07f, 0.10f, 1f));
            DrawRect(new Rect(sidebarRect.xMax - 1, sidebarRect.y, 1, sidebarRect.height), new Color(0.14f, 0.17f, 0.23f, 1f));

            // Sidebar Navigation buttons
            float navY = sidebarRect.y + 14;
            float navBtnH = 38f;
            float navGap = 6f;

            string[] navNames = new string[] { "⚡  ТВИКИ И ЧИТЫ", "📦  СПАВНЕР ПРЕДМЕТОВ", "⌨  ГОРЯЧИЕ КЛАВИШИ" };
            for (int i = 0; i < navNames.Length; i++)
            {
                bool isActive = (currentTab == i);
                Rect bRect = new Rect(sidebarRect.x + 10, navY, sidebarWidth - 20, navBtnH);
                if (GUI.Button(bRect, navNames[i], isActive ? navBtnActiveStyle : navBtnInactiveStyle))
                {
                    currentTab = i;
                    if (i == 1 && allItems == null) LoadItems();
                }
                navY += navBtnH + navGap;
            }

            // User Profile Card at bottom of sidebar
            float profileH = 54f;
            Rect profileRect = new Rect(sidebarRect.x + 10, windowRect.y + windowRect.height - profileH - 12, sidebarWidth - 20, profileH);
            DrawRect(profileRect, new Color(0.08f, 0.10f, 0.14f, 1f));
            DrawBorder(profileRect, 1, new Color(0.14f, 0.17f, 0.23f, 1f));

            var localP = GetLocalPlayer();
            string pName = "Pattern";
            string hpInfo = "Вне сессии";
            if (localP != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(localP.nickName)) pName = localP.nickName;
                    else if (localP.netSyncActorData != null && !string.IsNullOrEmpty(localP.netSyncActorData.actorName)) pName = localP.netSyncActorData.actorName;
                    else if (!string.IsNullOrEmpty(localP.puppetName)) pName = localP.puppetName;

                    long curHp = (localP.netSyncActorData != null) ? localP.netSyncActorData.hp : 100L;
                    long curConta = (localP.netSyncActorData != null) ? localP.netSyncActorData.conta : 0L;
                    hpInfo = $"{curHp} HP  •  {curConta}% Рад";
                }
                catch { }
            }
            string roleTag = " (Вы)";

            GUI.Label(new Rect(profileRect.x + 8, profileRect.y + 9, 16, 18), "●", profileDotStyle);
            GUI.Label(new Rect(profileRect.x + 24, profileRect.y + 9, profileRect.width - 28, 18), pName + roleTag, profileNameStyle);
            GUI.Label(new Rect(profileRect.x + 24, profileRect.y + 27, profileRect.width - 28, 18), hpInfo, profileSubStyle);

            // Right Main Content Area
            Rect contentArea = new Rect(sidebarRect.xMax + 14, windowRect.y + 48, windowRect.width - sidebarWidth - 28, windowRect.height - 58);
            GUILayout.BeginArea(contentArea);

            if (currentTab == 0)
            {
                DrawTabCheats();
            }
            else if (currentTab == 1)
            {
                DrawTabSpawner();
            }
            else
            {
                DrawTabKeybinds();
            }

            GUILayout.EndArea();
        }

        private void SetAllCheats(bool enable)
        {
            Pref_GodMode.Value = enable;
            Pref_InfiniteStamina.Value = enable;
            Pref_NoWeightSlowdown.Value = enable;
            Pref_SilentSteps.Value = enable;
            Pref_InfiniteBattery.Value = enable;
            Pref_AntiContamination.Value = enable;
            Pref_FullBrightEnabled.Value = enable;
            Pref_ShowTeamStatus.Value = enable;
            Pref_ItemEspEnabled.Value = enable;
            Pref_ActorEspEnabled.Value = enable;
            lastTeamScanTime = 0f;
            lastInvenScanTime = 0f;
            lastLootScanTime = 0f;
            lastActorScanTime = 0f;
            Category.SaveToFile();
            MelonPreferences.Save();
        }

        private void DrawTabCheats()
        {
            scrollPosCheats = GUILayout.BeginScrollView(scrollPosCheats, GUILayout.ExpandHeight(true));

            GUILayout.BeginHorizontal();

            // Left Column: Способности персонажа (Width: ~330)
            GUILayout.BeginVertical(GUILayout.Width(330));
            GUILayout.Label("СПОСОБНОСТИ ПЕРСОНАЖА", modernSectionHeaderStyle);
            GUILayout.Space(6);

            DrawModernCard("Режим Бога (GodMode)", "Бессмертие, защита от всех атак", Pref_GodMode, GetBoundKeyDisplay("godmode"));
            DrawModernCard("Бесконечный бег", "Выносливость не тратится", Pref_InfiniteStamina, GetBoundKeyDisplay("stamina"));
            DrawModernCard("Без штрафа веса", "Максимальная скорость бега", Pref_NoWeightSlowdown, GetBoundKeyDisplay("noweight"));
            DrawModernCard("Бесшумные шаги", "Монстры не слышат шагов", Pref_SilentSteps, GetBoundKeyDisplay("silent"));
            DrawModernCard("Вечный фонарик / Батарея", "100% заряд и прочность предметов", Pref_InfiniteBattery, GetBoundKeyDisplay("battery"));
            DrawModernCard("Анти-радиация", "Защита от дождя и заражения", Pref_AntiContamination, GetBoundKeyDisplay("anticonta"));
            DrawModernCard("Статус команды HUD", "Живые и мертвые под слотами", Pref_ShowTeamStatus, GetBoundKeyDisplay("teamhud"));

            GUILayout.EndVertical();

            GUILayout.Space(12);

            // Right Column: Подсветка и визуалы (Width: ~330)
            GUILayout.BeginVertical(GUILayout.Width(330));
            GUILayout.Label("ПОДСВЕТКА И ВИЗУАЛЫ", modernSectionHeaderStyle);
            GUILayout.Space(6);

            // Card 1: Item ESP
            DrawModernEspCard();

            // Card 2: Actor ESP
            DrawModernActorEspCard();

            // Card 3: FullBright
            DrawModernFullBrightCard();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            // Bottom Action Bar
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("✔ Включить всё", btnGreenStyle, GUILayout.Height(32)))
            {
                SetAllCheats(true);
            }
            GUILayout.Space(8);
            if (GUILayout.Button("✕ Выключить всё", btnRedStyle, GUILayout.Height(32)))
            {
                SetAllCheats(false);
            }
            GUILayout.Space(8);
            string closeBtnText = $"Закрыть меню [{GetKeyDisplayName(GetBoundKey("menu", Key.F7))}]";
            if (GUILayout.Button(closeBtnText, btnDarkStyle, GUILayout.Height(32)))
            {
                ToggleMenu();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawModernCard(string title, string desc, MelonPreferences_Entry<bool> pref, string hotkey)
        {
            bool isOn = pref != null && pref.Value;
            GUILayout.BeginVertical(modernCardStyle, GUILayout.Height(50));
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(title, modernCardTitleStyle);
            GUILayout.Label(desc, modernCardDescStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(hotkey) && hotkey != "None")
            {
                GUILayout.Label($"[{hotkey}]", modernCardKeyStyle, GUILayout.Height(26));
                GUILayout.Space(4);
            }

            string btnText = isOn ? "● ВКЛ" : "○ ВЫКЛ";
            GUIStyle swStyle = isOn ? switchActiveStyle : switchInactiveStyle;
            if (GUILayout.Button(btnText, swStyle, GUILayout.Width(64), GUILayout.Height(26)))
            {
                pref.Value = !pref.Value;
                if (pref == Pref_ShowTeamStatus && pref.Value)
                {
                    lastTeamScanTime = 0f;
                    lastInvenScanTime = 0f;
                }
                SavePrefs();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawColorBtn(float r, float g, float b, string name)
        {
            bool isCur = Mathf.Approximately(Pref_ItemEspColorR.Value, r) &&
                         Mathf.Approximately(Pref_ItemEspColorG.Value, g) &&
                         Mathf.Approximately(Pref_ItemEspColorB.Value, b);

            Color prev = GUI.color;
            GUI.color = new Color(r, g, b, 1f);
            if (GUILayout.Button(isCur ? "●" : "■", isCur ? paletteBtnActiveStyle : paletteBtnStyle, GUILayout.Width(28), GUILayout.Height(22)))
            {
                Pref_ItemEspColorR.Value = r;
                Pref_ItemEspColorG.Value = g;
                Pref_ItemEspColorB.Value = b;
                SavePrefs();
            }
            GUI.color = prev;
        }

        private void DrawModernEspCard()
        {
            bool isOn = Pref_ItemEspEnabled != null && Pref_ItemEspEnabled.Value;
            GUILayout.BeginVertical(modernCardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Подсветка предметов (Item ESP)", modernCardTitleStyle);
            GUILayout.FlexibleSpace();
            string hotkey = GetBoundKeyDisplay("itemesp");
            if (!string.IsNullOrEmpty(hotkey) && hotkey != "None")
            {
                GUILayout.Label($"[{hotkey}]", modernCardKeyStyle, GUILayout.Height(26));
                GUILayout.Space(4);
            }
            string btnText = isOn ? "● ВКЛ" : "○ ВЫКЛ";
            if (GUILayout.Button(btnText, isOn ? switchActiveStyle : switchInactiveStyle, GUILayout.Width(64), GUILayout.Height(26)))
            {
                Pref_ItemEspEnabled.Value = !Pref_ItemEspEnabled.Value;
                if (Pref_ItemEspEnabled.Value) lastLootScanTime = 0f;
                SavePrefs();
            }
            GUILayout.EndHorizontal();

            if (isOn)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Дистанция: {Mathf.RoundToInt(Pref_ItemEspDistance.Value)}м", modernCardDescStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("25м", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_ItemEspDistance.Value = 25f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("50м", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_ItemEspDistance.Value = 50f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("75м", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_ItemEspDistance.Value = 75f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("100м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(20))) { Pref_ItemEspDistance.Value = 100f; SavePrefs(); }
                GUILayout.EndHorizontal();

                float newDist = GUILayout.HorizontalSlider(Pref_ItemEspDistance.Value, 10f, 150f);
                if (Mathf.Abs(newDist - Pref_ItemEspDistance.Value) > 0.5f) { Pref_ItemEspDistance.Value = newDist; SavePrefs(); }

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Размер шрифта: {Mathf.RoundToInt(Pref_ItemEspFontSize.Value)}px", modernCardDescStyle, GUILayout.Width(130));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("10", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ItemEspFontSize.Value = 10f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("12", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ItemEspFontSize.Value = 12f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("14", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ItemEspFontSize.Value = 14f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("16", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ItemEspFontSize.Value = 16f; SavePrefs(); }
                GUILayout.EndHorizontal();

                float newFontSz = GUILayout.HorizontalSlider(Pref_ItemEspFontSize.Value, 8f, 22f);
                if (Mathf.Abs(newFontSz - Pref_ItemEspFontSize.Value) > 0.5f) { Pref_ItemEspFontSize.Value = newFontSz; SavePrefs(); }

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Цвет:", modernCardDescStyle, GUILayout.Width(36), GUILayout.Height(22));
                DrawColorBtn(1f, 0.85f, 0.20f, "Желтый");
                DrawColorBtn(0.20f, 0.95f, 0.40f, "Зеленый");
                DrawColorBtn(0.20f, 0.85f, 1.0f, "Голубой");
                DrawColorBtn(0.85f, 0.40f, 1.0f, "Фиолет");
                DrawColorBtn(1.0f, 0.55f, 0.15f, "Оранж");
                DrawColorBtn(1.0f, 1.0f, 1.0f, "Белый");
                DrawColorBtn(1.0f, 0.35f, 0.45f, "Красный");
                GUILayout.EndHorizontal();

                GUILayout.Space(2);
                Color curCol = new Color(Pref_ItemEspColorR.Value, Pref_ItemEspColorG.Value, Pref_ItemEspColorB.Value);
                espPreviewStyle.normal.textColor = curCol;
                GUILayout.Label("● Золотой слиток [$120] (14м)", espPreviewStyle);
            }

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawModernActorEspCard()
        {
            bool isOn = Pref_ActorEspEnabled != null && Pref_ActorEspEnabled.Value;
            GUILayout.BeginVertical(modernCardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("ESP игроков и врагов (Actor ESP)", modernCardTitleStyle);
            GUILayout.FlexibleSpace();
            string hotkey = GetBoundKeyDisplay("actoresp");
            if (!string.IsNullOrEmpty(hotkey) && hotkey != "None")
            {
                GUILayout.Label($"[{hotkey}]", modernCardKeyStyle, GUILayout.Height(26));
                GUILayout.Space(4);
            }
            string btnText = isOn ? "● ВКЛ" : "○ ВЫКЛ";
            if (GUILayout.Button(btnText, isOn ? switchActiveStyle : switchInactiveStyle, GUILayout.Width(64), GUILayout.Height(26)))
            {
                Pref_ActorEspEnabled.Value = !Pref_ActorEspEnabled.Value;
                if (Pref_ActorEspEnabled.Value) lastActorScanTime = 0f;
                SavePrefs();
            }
            GUILayout.EndHorizontal();

            if (isOn)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Дистанция: {Mathf.RoundToInt(Pref_ActorEspDistance.Value)}м", modernCardDescStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("50м", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_ActorEspDistance.Value = 50f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("100м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(20))) { Pref_ActorEspDistance.Value = 100f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("150м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(20))) { Pref_ActorEspDistance.Value = 150f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("200м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(20))) { Pref_ActorEspDistance.Value = 200f; SavePrefs(); }
                GUILayout.EndHorizontal();

                float newDist = GUILayout.HorizontalSlider(Pref_ActorEspDistance.Value, 20f, 250f);
                if (Mathf.Abs(newDist - Pref_ActorEspDistance.Value) > 0.5f) { Pref_ActorEspDistance.Value = newDist; SavePrefs(); }

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                bool showAllies = Pref_ActorEspShowAllies != null && Pref_ActorEspShowAllies.Value;
                bool showEnemies = Pref_ActorEspShowEnemies != null && Pref_ActorEspShowEnemies.Value;

                if (GUILayout.Button(showAllies ? "✔ Союзники" : "✖ Союзники", showAllies ? filterAllyActiveStyle : switchInactiveStyle, GUILayout.Height(22)))
                {
                    Pref_ActorEspShowAllies.Value = !Pref_ActorEspShowAllies.Value;
                    SavePrefs();
                }
                GUILayout.Space(4);
                if (GUILayout.Button(showEnemies ? "✔ Враги" : "✖ Враги", showEnemies ? filterEnemyActiveStyle : switchInactiveStyle, GUILayout.Height(22)))
                {
                    Pref_ActorEspShowEnemies.Value = !Pref_ActorEspShowEnemies.Value;
                    SavePrefs();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUILayout.Label("Отображать в ESP:", modernCardDescStyle);
                GUILayout.Space(1);
                GUILayout.BeginHorizontal();
                bool sName = Pref_ActorEspShowName != null && Pref_ActorEspShowName.Value;
                bool sHp = Pref_ActorEspShowHp != null && Pref_ActorEspShowHp.Value;
                bool sDist = Pref_ActorEspShowDist != null && Pref_ActorEspShowDist.Value;
                bool sRad = Pref_ActorEspShowRadiation != null && Pref_ActorEspShowRadiation.Value;

                if (GUILayout.Button(sName ? "✔ Ник" : "✖ Ник", sName ? filterAllyActiveStyle : switchInactiveStyle, GUILayout.Height(22)))
                {
                    Pref_ActorEspShowName.Value = !Pref_ActorEspShowName.Value;
                    SavePrefs();
                }
                GUILayout.Space(3);
                if (GUILayout.Button(sHp ? "✔ HP" : "✖ HP", sHp ? filterAllyActiveStyle : switchInactiveStyle, GUILayout.Height(22)))
                {
                    Pref_ActorEspShowHp.Value = !Pref_ActorEspShowHp.Value;
                    SavePrefs();
                }
                GUILayout.Space(3);
                if (GUILayout.Button(sDist ? "✔ Дистанция" : "✖ Дистанция", sDist ? filterAllyActiveStyle : switchInactiveStyle, GUILayout.Height(22)))
                {
                    Pref_ActorEspShowDist.Value = !Pref_ActorEspShowDist.Value;
                    SavePrefs();
                }
                GUILayout.Space(3);
                if (GUILayout.Button(sRad ? "✔ Радиация" : "✖ Радиация", sRad ? filterAllyActiveStyle : switchInactiveStyle, GUILayout.Height(22)))
                {
                    Pref_ActorEspShowRadiation.Value = !Pref_ActorEspShowRadiation.Value;
                    SavePrefs();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Размер шрифта: {Mathf.RoundToInt(Pref_ActorEspFontSize.Value)}px", modernCardDescStyle, GUILayout.Width(130));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("10", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ActorEspFontSize.Value = 10f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("12", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ActorEspFontSize.Value = 12f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("14", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ActorEspFontSize.Value = 14f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("16", espPresetBtnStyle, GUILayout.Width(32), GUILayout.Height(20))) { Pref_ActorEspFontSize.Value = 16f; SavePrefs(); }
                GUILayout.EndHorizontal();

                float newActorFont = GUILayout.HorizontalSlider(Pref_ActorEspFontSize.Value, 8f, 22f);
                if (Mathf.Abs(newActorFont - Pref_ActorEspFontSize.Value) > 0.5f) { Pref_ActorEspFontSize.Value = newActorFont; SavePrefs(); }
            }

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawModernFullBrightCard()
        {
            bool isOn = Pref_FullBrightEnabled != null && Pref_FullBrightEnabled.Value;
            GUILayout.BeginVertical(modernCardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("FullBright (Ночное зрение)", modernCardTitleStyle);
            GUILayout.FlexibleSpace();
            string hotkey = GetBoundKeyDisplay("fullbright");
            if (!string.IsNullOrEmpty(hotkey) && hotkey != "None")
            {
                GUILayout.Label($"[{hotkey}]", modernCardKeyStyle, GUILayout.Height(26));
                GUILayout.Space(4);
            }
            string btnText = isOn ? "● ВКЛ" : "○ ВЫКЛ";
            if (GUILayout.Button(btnText, isOn ? switchActiveStyle : switchInactiveStyle, GUILayout.Width(64), GUILayout.Height(26)))
            {
                Pref_FullBrightEnabled.Value = !Pref_FullBrightEnabled.Value;
                SavePrefs();
            }
            GUILayout.EndHorizontal();

            if (isOn)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Яркость: {Pref_FullBrightIntensity.Value:0.0}x", modernCardDescStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("0.8x", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_FullBrightIntensity.Value = 0.8f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("1.2x", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_FullBrightIntensity.Value = 1.2f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("2.0x", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_FullBrightIntensity.Value = 2.0f; SavePrefs(); }
                GUILayout.Space(2);
                if (GUILayout.Button("3.0x", espPresetBtnStyle, GUILayout.Width(38), GUILayout.Height(20))) { Pref_FullBrightIntensity.Value = 3.0f; SavePrefs(); }
                GUILayout.EndHorizontal();

                float newInt = GUILayout.HorizontalSlider(Pref_FullBrightIntensity.Value, 0.4f, 3.5f);
                if (Mathf.Abs(newInt - Pref_FullBrightIntensity.Value) > 0.05f) { Pref_FullBrightIntensity.Value = newInt; SavePrefs(); }
            }

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawTabSpawner()
        {
            if (allItems == null)
            {
                GUILayout.Space(20);
                GUILayout.Label("Загрузка данных игры... Если список пуст, войдите в сессию.", subHeaderStyle);
                GUILayout.Space(10);
                if (GUILayout.Button("Обновить список предметов", btnGreenStyle, GUILayout.Height(34)))
                {
                    LoadItems();
                }
                return;
            }

            GUILayout.BeginHorizontal();

            // Left Column: Items List & Filters (Width: ~370)
            GUILayout.BeginVertical(GUILayout.Width(370));

            // Search Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Поиск:", modernCardDescStyle, GUILayout.Width(44), GUILayout.Height(26));
            string newSearch = GUILayout.TextField(searchQuery, searchFieldStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));
            if (newSearch != searchQuery) searchQuery = newSearch;
            if (!string.IsNullOrEmpty(searchQuery) && GUILayout.Button("✕", espPresetBtnStyle, GUILayout.Width(24), GUILayout.Height(26))) searchQuery = "";
            if (GUILayout.Button("↻", espPresetBtnStyle, GUILayout.Width(24), GUILayout.Height(26))) LoadItems();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Category Filter Pills
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Все", selectedCategory == 0 ? filterBtnActive : filterBtnInactive, GUILayout.Height(24), GUILayout.ExpandWidth(true))) selectedCategory = 0;
            if (GUILayout.Button("Оборудование", selectedCategory == 1 ? filterBtnActive : filterBtnInactive, GUILayout.Height(24), GUILayout.ExpandWidth(true))) selectedCategory = 1;
            if (GUILayout.Button("Расходники", selectedCategory == 2 ? filterBtnActive : filterBtnInactive, GUILayout.Height(24), GUILayout.ExpandWidth(true))) selectedCategory = 2;
            if (GUILayout.Button("Хлам / Лут", selectedCategory == 3 ? filterBtnActive : filterBtnInactive, GUILayout.Height(24), GUILayout.ExpandWidth(true))) selectedCategory = 3;
            GUILayout.EndHorizontal();

            if (searchQuery != lastSearchQuery || selectedCategory != lastCategory)
            {
                UpdateFilteredItems();
            }

            GUILayout.Space(3);
            GUILayout.Label($"Найдено предметов: {filteredItems.Count}", modernCardDescStyle);
            GUILayout.Space(3);

            // Scrollable List of Items (Height: ~435)
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(435));
            for (int i = 0; i < filteredItems.Count; i++)
            {
                var it = filteredItems[i];
                bool isSel = (selectedItem != null && selectedItem.MasterId == it.MasterId);

                GUILayout.BeginHorizontal(isSel ? itemRowSelectedStyle : itemRowStyle, GUILayout.Height(36));

                GUILayout.BeginVertical(GUILayout.Width(290));
                GUILayout.Label(it.DisplayName, isSel ? itemLabelSelectedStyle : itemLabelStyle);
                string pDisp = it.GetPriceDisplay();
                string pTag = !string.IsNullOrEmpty(pDisp) ? $" • <color=#00E676>{pDisp}</color>" : "";

                // Purple keybind badge if item has a bind:
                var itBind = GetItemBind(it.MasterId);
                string bindTag = "";
                if (itBind != null && itBind.BoundKey != Key.None)
                {
                    bindTag = $" • <color=#C77DFF>[{GetKeyDisplayName(itBind.BoundKey)}]</color>";
                }

                GUILayout.Label($"ID: {it.MasterId} • {GetTypeBadge(it.ItemType)}{pTag} • {it.Weight} кг{bindTag}", itemSubLabelStyle);
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                if (isSel)
                {
                    if (GUILayout.Button("✔", checkmarkSelectedStyle, GUILayout.Width(34), GUILayout.Height(28)))
                    {
                        selectedItem = it;
                    }
                }
                else
                {
                    if (GUILayout.Button("+", checkmarkUnselectedStyle, GUILayout.Width(34), GUILayout.Height(28)))
                    {
                        selectedItem = it;
                    }
                }

                GUILayout.EndHorizontal();

                // Clicking anywhere on the row also selects the item
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    selectedItem = it;
                    Event.current.Use();
                }

                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.Space(14);

            // Right Column: Selected Item Card & Quick Binds (Width: ~324)
            GUILayout.BeginVertical(modernCardStyle, GUILayout.Width(324), GUILayout.ExpandHeight(true));
            if (selectedItem != null)
            {
                GUILayout.Label(selectedItem.DisplayName, cardTitleStyle);
                GUILayout.Space(2);
                string priceDisplay = selectedItem.GetPriceDisplay();
                string subMeta = $"ID: {selectedItem.MasterId} • {GetTypeBadge(selectedItem.ItemType)} • {selectedItem.Weight} кг";
                if (!string.IsNullOrEmpty(priceDisplay))
                {
                    subMeta += $" • <color=#00E676>{priceDisplay}</color>";
                }

                var curBind = GetItemBind(selectedItem.MasterId);
                if (curBind != null && curBind.BoundKey != Key.None)
                {
                    subMeta += $" • <color=#C77DFF>[{GetKeyDisplayName(curBind.BoundKey)}]</color>";
                }

                GUILayout.Label(subMeta, cardSubStyle);
                if (!string.IsNullOrEmpty(selectedItem.RawName))
                {
                    GUILayout.Label($"Ключ: {selectedItem.RawName}", cardKeyStyle);
                }

                GUILayout.Space(8);
                if (GUILayout.Button("⚡ Выдать в инвентарь", spawnBtnPrimary, GUILayout.Height(32)))
                {
                    TriggerSpawn(selectedItem, toInventory: true);
                }
                GUILayout.Space(6);
                if (GUILayout.Button("📦 Заспавнить на землю", spawnBtnSecondary, GUILayout.Height(32)))
                {
                    TriggerSpawn(selectedItem, toInventory: false);
                }

                GUILayout.Space(8);
                DrawRect(GUILayoutUtility.GetRect(1f, 1f), new Color(0.18f, 0.22f, 0.30f, 0.8f));
                GUILayout.Space(6);

                GUILayout.Label("⚡ Быстрый спавн по кнопке:", cardSubStyle);
                GUILayout.Space(4);

                // Mode toggle
                bool isToInven = (curBind != null) ? curBind.ToInventory : pendingBindToInventory;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(isToInven ? "● В руки" : "○ В руки", isToInven ? filterAllyActiveStyle : espPresetBtnStyle, GUILayout.Width(144), GUILayout.Height(24)))
                {
                    pendingBindToInventory = true;
                    if (curBind != null)
                    {
                        curBind.ToInventory = true;
                        SaveItemBinds();
                    }
                }
                GUILayout.Space(6);
                if (GUILayout.Button(!isToInven ? "● На землю" : "○ На землю", !isToInven ? filterAllyActiveStyle : espPresetBtnStyle, GUILayout.Width(144), GUILayout.Height(24)))
                {
                    pendingBindToInventory = false;
                    if (curBind != null)
                    {
                        curBind.ToInventory = false;
                        SaveItemBinds();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                // Key binding controls
                bool isWaitingThis = (waitingForItemBindMasterId == selectedItem.MasterId);

                if (isWaitingThis)
                {
                    if (GUILayout.Button(">> Нажмите клавишу... [Esc: отмена] <<", spawnBtnPrimary, GUILayout.Height(28)))
                    {
                        waitingForItemBindMasterId = 0;
                    }
                }
                else if (curBind != null && curBind.BoundKey != Key.None)
                {
                    GUILayout.BeginHorizontal();
                    string changeKeyText = $"Клавиша: [ {GetKeyDisplayName(curBind.BoundKey)} ]  (сменить)";
                    if (GUILayout.Button(changeKeyText, espPresetBtnStyle, GUILayout.Width(248), GUILayout.Height(28)))
                    {
                        waitingForItemBindMasterId = selectedItem.MasterId;
                        pendingBindToInventory = curBind.ToInventory;
                    }
                    GUILayout.Space(6);
                    if (GUILayout.Button("✕", btnRedStyle, GUILayout.Width(38), GUILayout.Height(28)))
                    {
                        RemoveItemBind(selectedItem.MasterId);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button("+ Назначить клавишу для спавна", espPresetBtnStyle, GUILayout.Height(28)))
                    {
                        waitingForItemBindMasterId = selectedItem.MasterId;
                    }
                }
            }
            else
            {
                GUILayout.Label("Выберите предмет", cardTitleStyle);
                GUILayout.Space(4);
                GUILayout.Label("Кликните на любой предмет из списка слева, чтобы заспавнить его или привязать к горячим клавишам.", cardSubStyle);
            }

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUILayout.Label(statusMessage, statusStyle, GUILayout.Width(300));
                GUILayout.Space(6);
            }

            string closeSpawnerText = $"Закрыть меню [{GetKeyDisplayName(GetBoundKey("menu", Key.F7))}]";
            if (GUILayout.Button(closeSpawnerText, btnDarkStyle, GUILayout.Height(30)))
            {
                ToggleMenu();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawTabKeybinds()
        {
            GUILayout.BeginHorizontal();

            // Left Column: Functions Keybinds (Width: ~335)
            GUILayout.BeginVertical(GUILayout.Width(335));
            GUILayout.Label("КЛАВИШИ ФУНКЦИЙ МОДА", modernSectionHeaderStyle);
            GUILayout.Label("Кликните на кнопку с клавишей для переназначения:", cardKeyStyle);
            GUILayout.Space(6);

            scrollPosKeybinds = GUILayout.BeginScrollView(scrollPosKeybinds, GUILayout.Height(430));
            for (int i = 0; i < keybindEntries.Count; i++)
            {
                var entry = keybindEntries[i];
                GUILayout.BeginHorizontal(cardBoxStyle);
                GUILayout.Label(entry.DisplayName, modernCardDescStyle, GUILayout.Width(170), GUILayout.Height(24));
                GUILayout.FlexibleSpace();

                bool isListening = (waitingForKeyAction == entry.Id);
                string keyText = isListening ? ">> ... <<" : $"[ {GetKeyDisplayName(entry.CurrentKey)} ]";

                if (GUILayout.Button(keyText, isListening ? spawnBtnPrimary : espPresetBtnStyle, GUILayout.Width(90), GUILayout.Height(24)))
                {
                    waitingForKeyAction = isListening ? null : entry.Id;
                }

                GUILayout.Space(2);
                if (GUILayout.Button("✕", espPresetBtnStyle, GUILayout.Width(24), GUILayout.Height(24)))
                {
                    ApplyKeybind(entry.Id, Key.None);
                    if (waitingForKeyAction == entry.Id) waitingForKeyAction = null;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            if (GUILayout.Button("Сбросить клавиши функций по умолчанию", espPresetBtnStyle, GUILayout.Height(28)))
            {
                ResetAllKeybinds();
            }

            GUILayout.EndVertical();

            GUILayout.Space(14);

            // Right Column: Item Quick-Spawn Binds (Width: ~340)
            GUILayout.BeginVertical(GUILayout.Width(340));
            GUILayout.Label("БЫСТРЫЙ СПАВН ПРЕДМЕТОВ", modernSectionHeaderStyle);
            GUILayout.Label("Создание предметов по клавише прямо в игре:", cardKeyStyle);
            GUILayout.Space(6);

            scrollPosSpawner = GUILayout.BeginScrollView(scrollPosSpawner, GUILayout.Height(430));
            if (itemSpawnBinds.Count == 0)
            {
                GUILayout.Label("Пока нет привязанных предметов.\n\nПерейдите в 'СПАВНЕР ПРЕДМЕТОВ', выберите предмет и нажмите '+ Назначить клавишу для спавна'.", cardKeyStyle);
            }
            else
            {
                for (int b = 0; b < itemSpawnBinds.Count; b++)
                {
                    var bind = itemSpawnBinds[b];
                    if (bind.BoundKey == Key.None) continue;

                    GUILayout.BeginHorizontal(cardBoxStyle);
                    GUILayout.BeginVertical(GUILayout.Width(130));
                    GUILayout.Label(bind.ItemName, itemLabelStyle);
                    string modeStr = bind.ToInventory ? "● В руки" : "● На землю";
                    if (GUILayout.Button(modeStr, filterBtnActive, GUILayout.Width(84), GUILayout.Height(18)))
                    {
                        bind.ToInventory = !bind.ToInventory;
                        SaveItemBinds();
                    }
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    bool isWait = (waitingForItemBindMasterId == bind.MasterId);
                    string kText = isWait ? ">> ... <<" : $"[ {GetKeyDisplayName(bind.BoundKey)} ]";
                    if (GUILayout.Button(kText, isWait ? spawnBtnPrimary : espPresetBtnStyle, GUILayout.Width(80), GUILayout.Height(24)))
                    {
                        waitingForItemBindMasterId = isWait ? 0 : bind.MasterId;
                        pendingBindToInventory = bind.ToInventory;
                    }

                    GUILayout.Space(4);
                    if (GUILayout.Button("✕", btnRedStyle, GUILayout.Width(28), GUILayout.Height(24)))
                    {
                        RemoveItemBind(bind.MasterId);
                        if (waitingForItemBindMasterId == bind.MasterId) waitingForItemBindMasterId = 0;
                        break;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("✕ Очистить бинды предметов", espPresetBtnStyle, GUILayout.Height(28)))
            {
                itemSpawnBinds.Clear();
                SaveItemBinds();
            }
            GUILayout.Space(6);
            string closeKeybindText = $"Закрыть [{GetKeyDisplayName(GetBoundKey("menu", Key.F7))}]";
            if (GUILayout.Button(closeKeybindText, btnDarkStyle, GUILayout.Height(28)))
            {
                ToggleMenu();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private static string GetTypeBadge(ItemType type)
        {
            switch (type)
            {
                case ItemType.Equipment: return "Оборудование";
                case ItemType.Consumable: return "Расходник";
                case ItemType.Miscellany: return "Хлам / Лут";
                default: return "Предмет";
            }
        }

        private void DrawToggleRow(string title, MelonPreferences_Entry<bool> pref)
        {
            bool active = pref.Value;
            string statusTag = active ? "[ ВКЛ ]  " : "[ ВЫКЛ ] ";
            GUIStyle style = active ? rowBtnStyleOn : rowBtnStyleOff;

            if (GUILayout.Button(statusTag + title, style, GUILayout.Height(34)))
            {
                pref.Value = !pref.Value;
                if (pref == Pref_ShowTeamStatus && pref.Value)
                {
                    lastTeamScanTime = 0f;
                    lastInvenScanTime = 0f;
                }
                if (pref == Pref_ItemEspEnabled && pref.Value)
                {
                    lastLootScanTime = 0f;
                }
                Category.SaveToFile();
                MelonPreferences.Save();
            }
            GUILayout.Space(3);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerTitleStyle = new GUIStyle();
            headerTitleStyle.alignment = TextAnchor.MiddleLeft;
            headerTitleStyle.fontSize = 15;
            headerTitleStyle.fontStyle = FontStyle.Bold;
            headerTitleStyle.normal.textColor = new Color(0.78f, 0.45f, 1.0f); // Dragon Amethyst Violet #C77DFF

            headerHintStyle = new GUIStyle();
            headerHintStyle.alignment = TextAnchor.MiddleRight;
            headerHintStyle.fontSize = 11;
            headerHintStyle.normal.textColor = new Color(0.55f, 0.62f, 0.72f);

            badgeTextStyle = new GUIStyle();
            badgeTextStyle.alignment = TextAnchor.MiddleCenter;
            badgeTextStyle.fontSize = 10;
            badgeTextStyle.fontStyle = FontStyle.Bold;
            badgeTextStyle.normal.textColor = Color.white;

            subHeaderStyle = new GUIStyle();
            subHeaderStyle.fontSize = 12;
            subHeaderStyle.normal.textColor = new Color(0.70f, 0.76f, 0.85f);
            subHeaderStyle.alignment = TextAnchor.MiddleLeft;

            Texture2D navActiveTex = MakeSolidTex(2, 2, new Color(0.08f, 0.22f, 0.16f, 1f));
            Texture2D navInactiveTex = MakeSolidTex(2, 2, new Color(0.08f, 0.10f, 0.14f, 1f));

            navBtnActiveStyle = new GUIStyle();
            navBtnActiveStyle.normal.background = navActiveTex;
            navBtnActiveStyle.hover.background = MakeSolidTex(2, 2, new Color(0.11f, 0.28f, 0.20f, 1f));
            navBtnActiveStyle.active.background = navActiveTex;
            navBtnActiveStyle.normal.textColor = new Color(0.18f, 0.85f, 0.45f);
            navBtnActiveStyle.hover.textColor = new Color(0.25f, 0.95f, 0.55f);
            navBtnActiveStyle.fontSize = 12;
            navBtnActiveStyle.fontStyle = FontStyle.Bold;
            navBtnActiveStyle.alignment = TextAnchor.MiddleLeft;
            navBtnActiveStyle.padding = new RectOffset(14, 10, 0, 0);
            navBtnActiveStyle.margin = new RectOffset(0, 0, 0, 0);
            navBtnActiveStyle.overflow = new RectOffset(0, 0, 0, 0);

            navBtnInactiveStyle = new GUIStyle();
            navBtnInactiveStyle.normal.background = navInactiveTex;
            navBtnInactiveStyle.hover.background = MakeSolidTex(2, 2, new Color(0.12f, 0.15f, 0.20f, 1f));
            navBtnInactiveStyle.active.background = navInactiveTex;
            navBtnInactiveStyle.normal.textColor = new Color(0.55f, 0.60f, 0.68f);
            navBtnInactiveStyle.hover.textColor = Color.white;
            navBtnInactiveStyle.fontSize = 12;
            navBtnInactiveStyle.fontStyle = FontStyle.Normal;
            navBtnInactiveStyle.alignment = TextAnchor.MiddleLeft;
            navBtnInactiveStyle.padding = new RectOffset(14, 10, 0, 0);
            navBtnInactiveStyle.margin = new RectOffset(0, 0, 0, 0);
            navBtnInactiveStyle.overflow = new RectOffset(0, 0, 0, 0);

            profileDotStyle = new GUIStyle();
            profileDotStyle.alignment = TextAnchor.MiddleCenter;
            profileDotStyle.fontSize = 14;
            profileDotStyle.fontStyle = FontStyle.Bold;
            profileDotStyle.normal.textColor = new Color(0.18f, 0.85f, 0.45f);

            profileNameStyle = new GUIStyle();
            profileNameStyle.alignment = TextAnchor.MiddleLeft;
            profileNameStyle.fontSize = 11;
            profileNameStyle.fontStyle = FontStyle.Bold;
            profileNameStyle.normal.textColor = Color.white;

            profileSubStyle = new GUIStyle();
            profileSubStyle.alignment = TextAnchor.MiddleLeft;
            profileSubStyle.fontSize = 10;
            profileSubStyle.normal.textColor = new Color(0.55f, 0.60f, 0.68f);

            modernSectionHeaderStyle = new GUIStyle();
            modernSectionHeaderStyle.alignment = TextAnchor.MiddleLeft;
            modernSectionHeaderStyle.fontSize = 12;
            modernSectionHeaderStyle.fontStyle = FontStyle.Bold;
            modernSectionHeaderStyle.normal.textColor = new Color(0.18f, 0.85f, 0.45f);

            Texture2D modernCardTex = MakeSolidTex(2, 2, new Color(0.10f, 0.13f, 0.17f, 1f));
            modernCardStyle = new GUIStyle();
            modernCardStyle.normal.background = modernCardTex;
            modernCardStyle.padding = new RectOffset(8, 8, 6, 6);

            modernCardTitleStyle = new GUIStyle();
            modernCardTitleStyle.alignment = TextAnchor.MiddleLeft;
            modernCardTitleStyle.fontSize = 12;
            modernCardTitleStyle.fontStyle = FontStyle.Bold;
            modernCardTitleStyle.normal.textColor = Color.white;

            modernCardDescStyle = new GUIStyle();
            modernCardDescStyle.alignment = TextAnchor.MiddleLeft;
            modernCardDescStyle.fontSize = 10;
            modernCardDescStyle.normal.textColor = new Color(0.55f, 0.60f, 0.68f);

            modernCardKeyStyle = new GUIStyle();
            modernCardKeyStyle.alignment = TextAnchor.MiddleRight;
            modernCardKeyStyle.fontSize = 11;
            modernCardKeyStyle.fontStyle = FontStyle.Bold;
            modernCardKeyStyle.normal.textColor = new Color(0.40f, 0.70f, 1.0f);

            Texture2D swOnTex = MakeSolidTex(2, 2, new Color(0.09f, 0.25f, 0.18f, 1f));
            Texture2D swOffTex = MakeSolidTex(2, 2, new Color(0.13f, 0.15f, 0.19f, 1f));

            switchActiveStyle = new GUIStyle(GUI.skin.button);
            switchActiveStyle.normal.background = swOnTex;
            switchActiveStyle.normal.textColor = new Color(0.18f, 0.85f, 0.45f);
            switchActiveStyle.hover.textColor = new Color(0.25f, 0.95f, 0.55f);
            switchActiveStyle.active.textColor = Color.white;
            switchActiveStyle.fontSize = 11;
            switchActiveStyle.fontStyle = FontStyle.Bold;
            switchActiveStyle.alignment = TextAnchor.MiddleCenter;
            switchActiveStyle.margin = new RectOffset(0, 0, 0, 0);
            switchActiveStyle.overflow = new RectOffset(0, 0, 0, 0);

            switchInactiveStyle = new GUIStyle(GUI.skin.button);
            switchInactiveStyle.normal.background = swOffTex;
            switchInactiveStyle.normal.textColor = new Color(0.55f, 0.60f, 0.68f);
            switchInactiveStyle.hover.textColor = Color.white;
            switchInactiveStyle.active.textColor = Color.white;
            switchInactiveStyle.fontSize = 11;
            switchInactiveStyle.fontStyle = FontStyle.Normal;
            switchInactiveStyle.alignment = TextAnchor.MiddleCenter;
            switchInactiveStyle.margin = new RectOffset(0, 0, 0, 0);
            switchInactiveStyle.overflow = new RectOffset(0, 0, 0, 0);

            Texture2D allyOnTex = MakeSolidTex(2, 2, new Color(0.09f, 0.25f, 0.18f, 1f));
            Texture2D enemyOnTex = MakeSolidTex(2, 2, new Color(0.28f, 0.12f, 0.14f, 1f));

            filterAllyActiveStyle = new GUIStyle(GUI.skin.button);
            filterAllyActiveStyle.normal.background = allyOnTex;
            filterAllyActiveStyle.normal.textColor = new Color(0.18f, 0.85f, 0.45f);
            filterAllyActiveStyle.hover.textColor = new Color(0.25f, 0.95f, 0.55f);
            filterAllyActiveStyle.active.textColor = Color.white;
            filterAllyActiveStyle.fontSize = 11;
            filterAllyActiveStyle.fontStyle = FontStyle.Bold;
            filterAllyActiveStyle.alignment = TextAnchor.MiddleCenter;
            filterAllyActiveStyle.margin = new RectOffset(0, 0, 0, 0);
            filterAllyActiveStyle.overflow = new RectOffset(0, 0, 0, 0);

            filterEnemyActiveStyle = new GUIStyle(GUI.skin.button);
            filterEnemyActiveStyle.normal.background = enemyOnTex;
            filterEnemyActiveStyle.normal.textColor = new Color(1.0f, 0.35f, 0.40f);
            filterEnemyActiveStyle.hover.textColor = new Color(1.0f, 0.5f, 0.55f);
            filterEnemyActiveStyle.active.textColor = Color.white;
            filterEnemyActiveStyle.fontSize = 11;
            filterEnemyActiveStyle.fontStyle = FontStyle.Bold;
            filterEnemyActiveStyle.alignment = TextAnchor.MiddleCenter;
            filterEnemyActiveStyle.margin = new RectOffset(0, 0, 0, 0);
            filterEnemyActiveStyle.overflow = new RectOffset(0, 0, 0, 0);

            Texture2D btnGreenTex = MakeSolidTex(2, 2, new Color(0.14f, 0.52f, 0.21f, 1f));
            Texture2D btnRedTex = MakeSolidTex(2, 2, new Color(0.65f, 0.18f, 0.18f, 1f));
            Texture2D btnDarkTex = MakeSolidTex(2, 2, new Color(0.18f, 0.22f, 0.28f, 1f));

            btnGreenStyle = new GUIStyle(GUI.skin.button);
            btnGreenStyle.normal.background = btnGreenTex;
            btnGreenStyle.normal.textColor = Color.white;
            btnGreenStyle.hover.textColor = Color.white;
            btnGreenStyle.active.textColor = new Color(0.6f, 1f, 0.6f);
            btnGreenStyle.fontSize = 12;
            btnGreenStyle.fontStyle = FontStyle.Bold;
            btnGreenStyle.alignment = TextAnchor.MiddleCenter;
            btnGreenStyle.margin = new RectOffset(0, 0, 0, 0);
            btnGreenStyle.overflow = new RectOffset(0, 0, 0, 0);

            btnRedStyle = new GUIStyle(GUI.skin.button);
            btnRedStyle.normal.background = btnRedTex;
            btnRedStyle.normal.textColor = Color.white;
            btnRedStyle.hover.textColor = Color.white;
            btnRedStyle.active.textColor = new Color(1f, 0.6f, 0.6f);
            btnRedStyle.fontSize = 12;
            btnRedStyle.fontStyle = FontStyle.Bold;
            btnRedStyle.alignment = TextAnchor.MiddleCenter;
            btnRedStyle.margin = new RectOffset(0, 0, 0, 0);
            btnRedStyle.overflow = new RectOffset(0, 0, 0, 0);

            btnDarkStyle = new GUIStyle(GUI.skin.button);
            btnDarkStyle.normal.background = btnDarkTex;
            btnDarkStyle.normal.textColor = Color.white;
            btnDarkStyle.hover.textColor = Color.white;
            btnDarkStyle.active.textColor = new Color(0.7f, 0.85f, 1.0f);
            btnDarkStyle.fontSize = 12;
            btnDarkStyle.fontStyle = FontStyle.Bold;
            btnDarkStyle.alignment = TextAnchor.MiddleCenter;
            btnDarkStyle.margin = new RectOffset(0, 0, 0, 0);
            btnDarkStyle.overflow = new RectOffset(0, 0, 0, 0);

            Texture2D btnOnTex = MakeSolidTex(2, 2, new Color(0.12f, 0.28f, 0.18f, 1f));
            Texture2D btnOffTex = MakeSolidTex(2, 2, new Color(0.14f, 0.16f, 0.20f, 1f));
            Texture2D btnActionTex = MakeSolidTex(2, 2, new Color(0.18f, 0.22f, 0.30f, 1f));
            Texture2D btnActiveTabTex = MakeSolidTex(2, 2, new Color(0.15f, 0.35f, 0.22f, 1f));
            Texture2D btnInactiveTabTex = MakeSolidTex(2, 2, new Color(0.12f, 0.14f, 0.18f, 1f));
            Texture2D rowSelTex = MakeSolidTex(2, 2, new Color(0.15f, 0.30f, 0.22f, 1f));
            Texture2D rowNormTex = MakeSolidTex(2, 2, new Color(0.11f, 0.13f, 0.17f, 1f));
            Texture2D cardBgTex = MakeSolidTex(2, 2, new Color(0.11f, 0.14f, 0.19f, 1f));
            Texture2D spawnPrimaryTex = MakeSolidTex(2, 2, new Color(0.16f, 0.44f, 0.26f, 1f));
            Texture2D spawnPrimaryHoverTex = MakeSolidTex(2, 2, new Color(0.22f, 0.58f, 0.34f, 1f));
            Texture2D spawnPrimaryActiveTex = MakeSolidTex(2, 2, new Color(0.12f, 0.36f, 0.20f, 1f));
            Texture2D spawnSecondaryTex = MakeSolidTex(2, 2, new Color(0.14f, 0.30f, 0.38f, 1f));
            Texture2D spawnSecondaryHoverTex = MakeSolidTex(2, 2, new Color(0.18f, 0.38f, 0.48f, 1f));
            Texture2D spawnSecondaryActiveTex = MakeSolidTex(2, 2, new Color(0.10f, 0.24f, 0.32f, 1f));

            rowBtnStyleOn = new GUIStyle();
            rowBtnStyleOn.normal.background = btnOnTex;
            rowBtnStyleOn.hover.background = MakeSolidTex(2, 2, new Color(0.16f, 0.36f, 0.24f, 1f));
            rowBtnStyleOn.active.background = btnOnTex;
            rowBtnStyleOn.normal.textColor = new Color(0.35f, 0.95f, 0.55f);
            rowBtnStyleOn.hover.textColor = new Color(0.45f, 1.0f, 0.65f);
            rowBtnStyleOn.fontSize = 13;
            rowBtnStyleOn.fontStyle = FontStyle.Bold;
            rowBtnStyleOn.alignment = TextAnchor.MiddleLeft;
            rowBtnStyleOn.padding = new RectOffset(12, 10, 0, 0);
            rowBtnStyleOn.margin = new RectOffset(0, 0, 0, 0);
            rowBtnStyleOn.overflow = new RectOffset(0, 0, 0, 0);

            rowBtnStyleOff = new GUIStyle();
            rowBtnStyleOff.normal.background = btnOffTex;
            rowBtnStyleOff.hover.background = MakeSolidTex(2, 2, new Color(0.18f, 0.21f, 0.26f, 1f));
            rowBtnStyleOff.active.background = btnOffTex;
            rowBtnStyleOff.normal.textColor = new Color(0.60f, 0.65f, 0.70f);
            rowBtnStyleOff.hover.textColor = Color.white;
            rowBtnStyleOff.fontSize = 13;
            rowBtnStyleOff.fontStyle = FontStyle.Normal;
            rowBtnStyleOff.alignment = TextAnchor.MiddleLeft;
            rowBtnStyleOff.padding = new RectOffset(12, 10, 0, 0);
            rowBtnStyleOff.margin = new RectOffset(0, 0, 0, 0);
            rowBtnStyleOff.overflow = new RectOffset(0, 0, 0, 0);

            actionBtnStyle = new GUIStyle();
            actionBtnStyle.normal.background = btnActionTex;
            actionBtnStyle.hover.background = MakeSolidTex(2, 2, new Color(0.24f, 0.30f, 0.40f, 1f));
            actionBtnStyle.active.background = MakeSolidTex(2, 2, new Color(0.12f, 0.16f, 0.22f, 1f));
            actionBtnStyle.normal.textColor = Color.white;
            actionBtnStyle.fontSize = 12;
            actionBtnStyle.fontStyle = FontStyle.Bold;
            actionBtnStyle.alignment = TextAnchor.MiddleCenter;
            actionBtnStyle.margin = new RectOffset(0, 0, 0, 0);
            actionBtnStyle.overflow = new RectOffset(0, 0, 0, 0);

            tabBtnActive = new GUIStyle();
            tabBtnActive.normal.background = btnActiveTabTex;
            tabBtnActive.normal.textColor = new Color(0.35f, 0.95f, 0.55f);
            tabBtnActive.fontSize = 13;
            tabBtnActive.fontStyle = FontStyle.Bold;
            tabBtnActive.alignment = TextAnchor.MiddleCenter;
            tabBtnActive.margin = new RectOffset(0, 0, 0, 0);
            tabBtnActive.overflow = new RectOffset(0, 0, 0, 0);

            tabBtnInactive = new GUIStyle();
            tabBtnInactive.normal.background = btnInactiveTabTex;
            tabBtnInactive.normal.textColor = new Color(0.60f, 0.65f, 0.72f);
            tabBtnInactive.fontSize = 13;
            tabBtnInactive.fontStyle = FontStyle.Normal;
            tabBtnInactive.alignment = TextAnchor.MiddleCenter;
            tabBtnInactive.margin = new RectOffset(0, 0, 0, 0);
            tabBtnInactive.overflow = new RectOffset(0, 0, 0, 0);

            filterBtnActive = new GUIStyle(GUI.skin.button);
            filterBtnActive.normal.background = btnActiveTabTex;
            filterBtnActive.normal.textColor = new Color(0.35f, 0.95f, 0.55f);
            filterBtnActive.hover.textColor = new Color(0.45f, 1.0f, 0.65f);
            filterBtnActive.active.textColor = Color.white;
            filterBtnActive.fontSize = 11;
            filterBtnActive.fontStyle = FontStyle.Bold;
            filterBtnActive.alignment = TextAnchor.MiddleCenter;
            filterBtnActive.margin = new RectOffset(0, 0, 0, 0);
            filterBtnActive.overflow = new RectOffset(0, 0, 0, 0);

            filterBtnInactive = new GUIStyle(GUI.skin.button);
            filterBtnInactive.normal.background = btnInactiveTabTex;
            filterBtnInactive.normal.textColor = new Color(0.55f, 0.60f, 0.68f);
            filterBtnInactive.hover.textColor = Color.white;
            filterBtnInactive.active.textColor = Color.white;
            filterBtnInactive.fontSize = 11;
            filterBtnInactive.fontStyle = FontStyle.Normal;
            filterBtnInactive.alignment = TextAnchor.MiddleCenter;
            filterBtnInactive.margin = new RectOffset(0, 0, 0, 0);
            filterBtnInactive.overflow = new RectOffset(0, 0, 0, 0);

            itemRowStyle = new GUIStyle();
            itemRowStyle.normal.background = rowNormTex;
            itemRowStyle.padding = new RectOffset(8, 8, 3, 3);

            itemRowSelectedStyle = new GUIStyle();
            itemRowSelectedStyle.normal.background = rowSelTex;
            itemRowSelectedStyle.padding = new RectOffset(8, 8, 3, 3);

            itemLabelStyle = new GUIStyle();
            itemLabelStyle.fontSize = 12;
            itemLabelStyle.fontStyle = FontStyle.Bold;
            itemLabelStyle.normal.textColor = Color.white;
            itemLabelStyle.alignment = TextAnchor.MiddleLeft;

            itemLabelSelectedStyle = new GUIStyle();
            itemLabelSelectedStyle.fontSize = 12;
            itemLabelSelectedStyle.fontStyle = FontStyle.Bold;
            itemLabelSelectedStyle.normal.textColor = new Color(0.35f, 0.95f, 0.55f);
            itemLabelSelectedStyle.alignment = TextAnchor.MiddleLeft;

            itemSubLabelStyle = new GUIStyle();
            itemSubLabelStyle.fontSize = 11;
            itemSubLabelStyle.normal.textColor = new Color(0.65f, 0.72f, 0.82f);
            itemSubLabelStyle.alignment = TextAnchor.MiddleLeft;
            itemSubLabelStyle.richText = true;

            selectBtnStyle = new GUIStyle(GUI.skin.button);
            selectBtnStyle.normal.textColor = new Color(0.85f, 0.90f, 0.95f);
            selectBtnStyle.hover.textColor = Color.white;
            selectBtnStyle.active.textColor = Color.white;
            selectBtnStyle.fontSize = 11;
            selectBtnStyle.alignment = TextAnchor.MiddleCenter;
            selectBtnStyle.margin = new RectOffset(0, 0, 0, 0);
            selectBtnStyle.overflow = new RectOffset(0, 0, 0, 0);

            checkmarkSelectedStyle = new GUIStyle(GUI.skin.button);
            checkmarkSelectedStyle.normal.background = MakeSolidTex(2, 2, new Color(0.12f, 0.65f, 0.32f, 1f));
            checkmarkSelectedStyle.normal.textColor = Color.white;
            checkmarkSelectedStyle.hover.textColor = Color.white;
            checkmarkSelectedStyle.active.textColor = Color.black;
            checkmarkSelectedStyle.fontSize = 14;
            checkmarkSelectedStyle.fontStyle = FontStyle.Bold;
            checkmarkSelectedStyle.alignment = TextAnchor.MiddleCenter;
            checkmarkSelectedStyle.padding = new RectOffset(0, 0, 0, 0);
            checkmarkSelectedStyle.margin = new RectOffset(0, 0, 0, 0);
            checkmarkSelectedStyle.overflow = new RectOffset(0, 0, 0, 0);

            checkmarkUnselectedStyle = new GUIStyle(GUI.skin.button);
            checkmarkUnselectedStyle.normal.background = MakeSolidTex(2, 2, new Color(0.14f, 0.18f, 0.25f, 0.9f));
            checkmarkUnselectedStyle.normal.textColor = new Color(0.60f, 0.70f, 0.82f);
            checkmarkUnselectedStyle.hover.textColor = Color.white;
            checkmarkUnselectedStyle.active.textColor = Color.white;
            checkmarkUnselectedStyle.fontSize = 14;
            checkmarkUnselectedStyle.fontStyle = FontStyle.Bold;
            checkmarkUnselectedStyle.alignment = TextAnchor.MiddleCenter;
            checkmarkUnselectedStyle.padding = new RectOffset(0, 0, 0, 0);
            checkmarkUnselectedStyle.margin = new RectOffset(0, 0, 0, 0);
            checkmarkUnselectedStyle.overflow = new RectOffset(0, 0, 0, 0);

            spawnBtnPrimary = new GUIStyle(GUI.skin.button);
            spawnBtnPrimary.normal.textColor = Color.white;
            spawnBtnPrimary.hover.textColor = Color.white;
            spawnBtnPrimary.active.textColor = new Color(0.35f, 0.95f, 0.55f);
            spawnBtnPrimary.fontSize = 12;
            spawnBtnPrimary.fontStyle = FontStyle.Bold;
            spawnBtnPrimary.alignment = TextAnchor.MiddleCenter;
            spawnBtnPrimary.margin = new RectOffset(0, 0, 0, 0);
            spawnBtnPrimary.overflow = new RectOffset(0, 0, 0, 0);

            spawnBtnSecondary = new GUIStyle(GUI.skin.button);
            spawnBtnSecondary.normal.textColor = Color.white;
            spawnBtnSecondary.hover.textColor = Color.white;
            spawnBtnSecondary.active.textColor = new Color(0.40f, 0.85f, 1.0f);
            spawnBtnSecondary.fontSize = 12;
            spawnBtnSecondary.fontStyle = FontStyle.Bold;
            spawnBtnSecondary.alignment = TextAnchor.MiddleCenter;
            spawnBtnSecondary.margin = new RectOffset(0, 0, 0, 0);
            spawnBtnSecondary.overflow = new RectOffset(0, 0, 0, 0);

            cardBoxStyle = new GUIStyle();
            cardBoxStyle.normal.background = cardBgTex;
            cardBoxStyle.padding = new RectOffset(10, 10, 8, 8);

            cardTitleStyle = new GUIStyle();
            cardTitleStyle.fontSize = 13;
            cardTitleStyle.fontStyle = FontStyle.Bold;
            cardTitleStyle.normal.textColor = new Color(0.35f, 0.95f, 0.55f);

            cardSubStyle = new GUIStyle();
            cardSubStyle.fontSize = 11;
            cardSubStyle.normal.textColor = new Color(0.75f, 0.80f, 0.88f);
            cardSubStyle.richText = true;

            cardKeyStyle = new GUIStyle();
            cardKeyStyle.fontSize = 10;
            cardKeyStyle.normal.textColor = new Color(0.55f, 0.60f, 0.70f);

            searchFieldStyle = new GUIStyle(GUI.skin.textField);
            searchFieldStyle.normal.background = MakeSolidTex(2, 2, new Color(0.12f, 0.15f, 0.20f, 1f));
            searchFieldStyle.focused.background = MakeSolidTex(2, 2, new Color(0.15f, 0.19f, 0.26f, 1f));
            searchFieldStyle.normal.textColor = Color.white;
            searchFieldStyle.focused.textColor = Color.white;
            searchFieldStyle.fontSize = 12;
            searchFieldStyle.padding = new RectOffset(6, 6, 4, 4);
            searchFieldStyle.margin = new RectOffset(0, 0, 0, 0);

            statusStyle = new GUIStyle();
            statusStyle.fontSize = 12;
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.normal.textColor = new Color(0.35f, 0.95f, 0.55f);
            statusStyle.alignment = TextAnchor.MiddleCenter;
            statusStyle.wordWrap = true;

            teamTextStyle = new GUIStyle(GUI.skin.label);
            teamTextStyle.fontSize = 13;
            teamTextStyle.fontStyle = FontStyle.Bold;
            teamTextStyle.alignment = TextAnchor.MiddleCenter;
            teamTextStyle.wordWrap = false;
            teamTextStyle.clipping = TextClipping.Overflow;
            teamTextStyle.padding = new RectOffset(0, 0, 0, 0);
            teamTextStyle.margin = new RectOffset(0, 0, 0, 0);

            espTextStyle = new GUIStyle(GUI.skin.label);
            espTextStyle.fontSize = 12;
            espTextStyle.fontStyle = FontStyle.Bold;
            espTextStyle.alignment = TextAnchor.MiddleCenter;
            espTextStyle.wordWrap = false;
            espTextStyle.clipping = TextClipping.Overflow;
            espTextStyle.padding = new RectOffset(0, 0, 0, 0);
            espTextStyle.margin = new RectOffset(0, 0, 0, 0);

            Texture2D espBoxTex = MakeSolidTex(2, 2, new Color(0.10f, 0.13f, 0.18f, 1f));
            espBoxStyle = new GUIStyle();
            espBoxStyle.normal.background = espBoxTex;
            espBoxStyle.padding = new RectOffset(10, 10, 8, 8);

            espPresetBtnStyle = new GUIStyle(GUI.skin.button);
            espPresetBtnStyle.normal.textColor = new Color(0.85f, 0.90f, 0.95f);
            espPresetBtnStyle.hover.textColor = Color.white;
            espPresetBtnStyle.active.textColor = new Color(0.35f, 0.95f, 0.55f);
            espPresetBtnStyle.fontSize = 11;
            espPresetBtnStyle.alignment = TextAnchor.MiddleCenter;
            espPresetBtnStyle.margin = new RectOffset(0, 0, 0, 0);
            espPresetBtnStyle.overflow = new RectOffset(0, 0, 0, 0);

            paletteBtnStyle = new GUIStyle();
            paletteBtnStyle.fontSize = 14;
            paletteBtnStyle.alignment = TextAnchor.MiddleCenter;
            paletteBtnStyle.margin = new RectOffset(0, 0, 0, 0);
            paletteBtnStyle.overflow = new RectOffset(0, 0, 0, 0);

            paletteBtnActiveStyle = new GUIStyle();
            paletteBtnActiveStyle.fontSize = 14;
            paletteBtnActiveStyle.fontStyle = FontStyle.Bold;
            paletteBtnActiveStyle.alignment = TextAnchor.MiddleCenter;
            paletteBtnActiveStyle.margin = new RectOffset(0, 0, 0, 0);
            paletteBtnActiveStyle.overflow = new RectOffset(0, 0, 0, 0);

            espPreviewStyle = new GUIStyle(GUI.skin.label);
            espPreviewStyle.fontSize = 12;
            espPreviewStyle.fontStyle = FontStyle.Bold;
            espPreviewStyle.alignment = TextAnchor.MiddleLeft;

            actorEspTextStyle = new GUIStyle(GUI.skin.label);
            actorEspTextStyle.fontSize = 11;
            actorEspTextStyle.fontStyle = FontStyle.Bold;
            actorEspTextStyle.alignment = TextAnchor.MiddleCenter;
            actorEspTextStyle.wordWrap = false;
            actorEspTextStyle.clipping = TextClipping.Overflow;
            actorEspTextStyle.padding = new RectOffset(0, 0, 0, 0);
            actorEspTextStyle.margin = new RectOffset(0, 0, 0, 0);

            stylesInitialized = true;
        }

        public static void DrawRect(Rect rect, Color color)
        {
            if (whiteTex == null)
            {
                whiteTex = new Texture2D(1, 1);
                whiteTex.SetPixel(0, 0, Color.white);
                whiteTex.Apply();
            }
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = prev;
        }

        public static void DrawBorder(Rect rect, int thickness, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
        }

        private Texture2D MakeSolidTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        #region Team Status HUD
        private static void UpdateTeamData()
        {
            try
            {
                var actors = UnityEngine.Object.FindObjectsByType<Mimic.Actors.ProtoActor>(FindObjectsSortMode.None);
                if (actors == null || actors.Length == 0)
                {
                    cachedTeamMembers.Clear();
                    return;
                }

                List<TeamMemberEntry> list = new List<TeamMemberEntry>();
                HashSet<int> seenIds = new HashSet<int>();

                for (int i = 0; i < actors.Length; i++)
                {
                    var a = actors[i];
                    if (a == null) continue;

                    try
                    {
                        if (!a.IsPlayer()) continue;
                    }
                    catch
                    {
                        continue;
                    }

                    int id = a.ActorID;
                    if (id != 0 && seenIds.Contains(id)) continue;
                    if (id != 0) seenIds.Add(id);

                    bool isLocal = false;
                    try { isLocal = a.AmIAvatar(); } catch { }

                    string pName = null;
                    try { pName = a.nickName; } catch { }
                    if (string.IsNullOrWhiteSpace(pName))
                    {
                        try { pName = a.netSyncActorData.actorName; } catch { }
                    }
                    if (string.IsNullOrWhiteSpace(pName))
                    {
                        try { pName = a.puppetName; } catch { }
                    }
                    if (string.IsNullOrWhiteSpace(pName))
                    {
                        pName = $"Игрок #{id}";
                    }

                    bool dead = false;
                    long hp = 0;
                    try
                    {
                        dead = a.dead || a.netSyncActorData.hp <= 0;
                        hp = a.netSyncActorData.hp;
                    }
                    catch { }

                    string displayName = pName;
                    if (isLocal)
                    {
                        displayName += " (Вы)";
                    }

                    string formattedText;
                    if (dead)
                    {
                        formattedText = $"{displayName} [МЁРТВ]";
                    }
                    else
                    {
                        formattedText = hp > 0 ? $"{displayName} [{hp} HP]" : displayName;
                    }

                    list.Add(new TeamMemberEntry
                    {
                        ActorID = id,
                        Name = pName,
                        DisplayText = formattedText,
                        IsDead = dead,
                        IsLocal = isLocal,
                        HP = hp
                    });
                }

                list.Sort((x, y) =>
                {
                    if (x.IsLocal && !y.IsLocal) return -1;
                    if (!x.IsLocal && y.IsLocal) return 1;
                    if (!x.IsDead && y.IsDead) return -1;
                    if (x.IsDead && !y.IsDead) return 1;
                    return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                });

                cachedTeamMembers = list;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[TeamHUD] Update error: {ex.Message}");
            }
        }

        private static void UpdateSlotPosition()
        {
            try
            {
                if (cachedInventoryUI == null || !cachedInventoryUI.gameObject.activeInHierarchy)
                {
                    cachedInventoryUI = UnityEngine.Object.FindAnyObjectByType<UIPrefab_Inventory>();
                }

                if (cachedInventoryUI != null && cachedInventoryUI.gameObject.activeInHierarchy)
                {
                    float maxBottom = -1f;

                    var rects = cachedInventoryUI.GetComponentsInChildren<RectTransform>(false);
                    if (rects != null)
                    {
                        for (int i = 0; i < rects.Length; i++)
                        {
                            var rt = rects[i];
                            if (rt == null || !rt.gameObject.activeInHierarchy) continue;
                            if (rt == cachedInventoryUI.transform) continue;

                            Vector3[] corners = new Vector3[4];
                            rt.GetWorldCorners(corners);
                            float h = corners[1].y - corners[0].y;
                            if (h < 10f || h > 200f) continue;

                            float bottomImgui = Screen.height - corners[0].y;
                            if (bottomImgui > maxBottom && bottomImgui < Screen.height - 8f)
                            {
                                maxBottom = bottomImgui;
                            }
                        }
                    }

                    cachedSlotBottomImgui = maxBottom;
                }
                else
                {
                    cachedSlotBottomImgui = -1f;
                }
            }
            catch
            {
                cachedSlotBottomImgui = -1f;
            }
        }

        private void DrawTeamStatusHUD()
        {
            if (Event.current.type != EventType.Repaint) return;

            InitStyles();

            if (cachedTeamMembers == null || cachedTeamMembers.Count == 0)
            {
                if (Time.unscaledTime - lastTeamScanTime > 0.25f)
                {
                    lastTeamScanTime = Time.unscaledTime;
                    UpdateTeamData();
                }
                if (cachedTeamMembers == null || cachedTeamMembers.Count == 0) return;
            }

            float hudHeight = 24f;
            float hudY;
            if (cachedSlotBottomImgui > 0 && cachedSlotBottomImgui + hudHeight + 2f <= Screen.height)
            {
                hudY = cachedSlotBottomImgui + 2f;
            }
            else
            {
                hudY = Screen.height - hudHeight - 4f;
            }

            float spacing = 10f;
            float dividerWidth = 10f;
            float totalContentWidth = 0f;

            List<float> textWidths = new List<float>(cachedTeamMembers.Count);
            List<string> fullStrings = new List<string>(cachedTeamMembers.Count);

            for (int i = 0; i < cachedTeamMembers.Count; i++)
            {
                var m = cachedTeamMembers[i];
                string prefix = m.IsDead ? "✕ " : "● ";
                string fullStr = prefix + m.DisplayText;
                fullStrings.Add(fullStr);

                Vector2 sz = teamTextStyle.CalcSize(new GUIContent(fullStr));
                float w = Mathf.Ceil(sz.x) + 12f;
                textWidths.Add(w);
                totalContentWidth += w;

                if (i < cachedTeamMembers.Count - 1)
                {
                    totalContentWidth += spacing + dividerWidth + spacing;
                }
            }

            float plateX = Mathf.Round((Screen.width - totalContentWidth) * 0.5f);

            // Render each team member badge directly with clean outline, no dark background
            float curX = plateX;
            for (int i = 0; i < cachedTeamMembers.Count; i++)
            {
                var m = cachedTeamMembers[i];
                float w = textWidths[i];
                Rect textRect = new Rect(curX, hudY, w, hudHeight);

                Color textColor = m.IsDead
                    ? new Color(1.0f, 0.28f, 0.28f, 1f)   // High-contrast Red
                    : new Color(0.25f, 0.98f, 0.38f, 1f);  // High-contrast Green

                DrawOutlinedLabel(textRect, fullStrings[i], teamTextStyle, textColor, Color.black, 1);

                curX += w;

                if (i < cachedTeamMembers.Count - 1)
                {
                    curX += spacing;
                    Rect divRect = new Rect(curX, hudY, dividerWidth, hudHeight);
                    DrawOutlinedLabel(divRect, "|", teamTextStyle, new Color(0.75f, 0.80f, 0.90f, 0.85f), Color.black, 1);
                    curX += dividerWidth + spacing;
                }
            }
        }

        public static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, int outlineDist = 1)
        {
            Color prev = style.normal.textColor;

            style.normal.textColor = outlineColor;
            GUI.Label(new Rect(rect.x - outlineDist, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + outlineDist, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - outlineDist, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + outlineDist, rect.width, rect.height), text, style);

            style.normal.textColor = textColor;
            GUI.Label(rect, text, style);

            style.normal.textColor = prev;
        }
        #endregion

        #region Item ESP (Wallhack Through Walls)
        private static void UpdateLootObjects()
        {
            try
            {
                var found = UnityEngine.Object.FindObjectsByType<LootingLevelObject>(FindObjectsSortMode.None);
                if (found == null || found.Length == 0)
                {
                    cachedLootObjects.Clear();
                    return;
                }

                cachedLootObjects.Clear();
                for (int i = 0; i < found.Length; i++)
                {
                    var obj = found[i];
                    if (obj != null && obj.gameObject.activeInHierarchy)
                    {
                        cachedLootObjects.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[ESP] Update error: {ex.Message}");
            }
        }

        public static Camera GetMainCamera()
        {
            if (cachedMainCamera != null && cachedMainCamera.gameObject.activeInHierarchy)
            {
                return cachedMainCamera;
            }

            if (Time.unscaledTime - lastCameraCheck < 0.5f)
            {
                return cachedMainCamera;
            }
            lastCameraCheck = Time.unscaledTime;

            cachedMainCamera = Camera.main;
            if (cachedMainCamera != null && cachedMainCamera.gameObject.activeInHierarchy)
            {
                return cachedMainCamera;
            }

            var cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                var c = cameras[i];
                if (c != null && c.gameObject.activeInHierarchy && c.enabled)
                {
                    if (c.cullingMask != 1 << 5)
                    {
                        cachedMainCamera = c;
                        return cachedMainCamera;
                    }
                }
            }

            return null;
        }

        public static string GetItemDisplayName(int masterId)
        {
            if (itemNamesById != null && itemNamesById.TryGetValue(masterId, out var cachedName))
            {
                return cachedName;
            }

            try
            {
                var dataman = GetDataManager();
                if (dataman?.ExcelDataManager?.ItemInfoDict != null &&
                    dataman.ExcelDataManager.ItemInfoDict.TryGetValue(masterId, out var info) && info != null)
                {
                    var lcman = GetL10NManager();
                    string disp = null;
                    if (lcman != null && !string.IsNullOrEmpty(info.Name))
                    {
                        try
                        {
                            disp = lcman.GetText(info.Name);
                            if (disp != null && disp.StartsWith("|L10N")) disp = null;
                        }
                        catch { }
                    }
                    if (string.IsNullOrEmpty(disp))
                    {
                        disp = !string.IsNullOrEmpty(info.Name) ? info.Name : $"Item #{masterId}";
                    }
                    if (itemNamesById == null) itemNamesById = new Dictionary<int, string>();
                    itemNamesById[masterId] = disp;
                    return disp;
                }
            }
            catch { }

            return $"Предмет #{masterId}";
        }

        private void DrawItemESP()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (cachedLootObjects == null || cachedLootObjects.Count == 0) return;

            InitStyles();
            espTextStyle.fontSize = Pref_ItemEspFontSize != null ? Mathf.RoundToInt(Pref_ItemEspFontSize.Value) : 12;

            Camera cam = GetMainCamera();
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;

            float maxDist = Pref_ItemEspDistance != null ? Pref_ItemEspDistance.Value : 40f;
            float maxDistSq = maxDist * maxDist;

            Color userColor = new Color(
                Pref_ItemEspColorR != null ? Pref_ItemEspColorR.Value : 1.0f,
                Pref_ItemEspColorG != null ? Pref_ItemEspColorG.Value : 0.85f,
                Pref_ItemEspColorB != null ? Pref_ItemEspColorB.Value : 0.20f,
                1.0f
            );

            int count = cachedLootObjects.Count;
            for (int i = 0; i < count; i++)
            {
                var obj = cachedLootObjects[i];
                if (obj == null || !obj.gameObject.activeInHierarchy || obj.AreVisualsHidden) continue;

                if (fi_isAttached != null)
                {
                    try
                    {
                        if ((bool)fi_isAttached.GetValue(obj)) continue;
                    }
                    catch { }
                }

                Vector3 pos = obj.transform.position;
                Vector3 toItem = pos - camPos;

                float sqrDist = toItem.sqrMagnitude;
                if (sqrDist > maxDistSq || sqrDist < 0.2f) continue;

                if (Vector3.Dot(camFwd, toItem) <= 0.15f) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(pos);
                if (screenPos.z <= 0.2f) continue;

                float guiX = screenPos.x;
                float guiY = Screen.height - screenPos.y;

                if (guiX < -80f || guiX > Screen.width + 80f || guiY < -40f || guiY > Screen.height + 40f)
                {
                    continue;
                }

                float dist = Mathf.Sqrt(sqrDist);
                string itemName = GetItemDisplayName(obj.itemMasterID);
                int price = obj.marketPrice;
                bool isFake = obj.isFake;

                string displayLabel;
                Color labelColor = userColor;

                if (isFake)
                {
                    displayLabel = $"⚠ {itemName} [МИМИК!] ({dist:0}м)";
                    labelColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);
                }
                else if (price > 0)
                {
                    displayLabel = $"{itemName} [${price}] ({dist:0}м)";
                }
                else
                {
                    displayLabel = $"{itemName} ({dist:0}м)";
                }

                Vector2 sz = espTextStyle.CalcSize(new GUIContent(displayLabel));
                float labelW = sz.x + 8f;
                float labelH = sz.y + 2f;
                Rect labelRect = new Rect(guiX - labelW * 0.5f, guiY - labelH, labelW, labelH);

                espTextStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
                GUI.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height), displayLabel, espTextStyle);

                espTextStyle.normal.textColor = labelColor;
                GUI.Label(labelRect, displayLabel, espTextStyle);
            }
        }

        private void DrawEspSettings()
        {
            GUILayout.BeginVertical(espBoxStyle);

            // 1. Distance header and instant preset buttons
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Дистанция поиска: {Mathf.RoundToInt(Pref_ItemEspDistance.Value)} м", cardSubStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("25м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_ItemEspDistance.Value = 25f;
                SavePrefs();
            }
            if (GUILayout.Button("50м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_ItemEspDistance.Value = 50f;
                SavePrefs();
            }
            if (GUILayout.Button("75м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_ItemEspDistance.Value = 75f;
                SavePrefs();
            }
            if (GUILayout.Button("100м", espPresetBtnStyle, GUILayout.Width(48), GUILayout.Height(22)))
            {
                Pref_ItemEspDistance.Value = 100f;
                SavePrefs();
            }
            GUILayout.EndHorizontal();

            // Distance slider
            float newDist = GUILayout.HorizontalSlider(Pref_ItemEspDistance.Value, 5f, 150f, GUILayout.Height(18));
            if (Mathf.Abs(newDist - Pref_ItemEspDistance.Value) > 0.5f)
            {
                Pref_ItemEspDistance.Value = Mathf.Round(newDist);
                SavePrefs();
            }

            GUILayout.Space(6);

            // 2. Color Palette Header
            GUILayout.Label("Цвет надписей предметов (Палитра):", cardSubStyle);
            GUILayout.Space(2);

            Color[] palettePresets = new Color[]
            {
                new Color(1.0f, 0.85f, 0.20f), // Золотой
                new Color(0.20f, 0.95f, 0.40f), // Неоново-зеленый
                new Color(0.20f, 0.85f, 1.00f), // Циан
                new Color(0.85f, 0.40f, 1.00f), // Фиолетовый
                new Color(1.00f, 0.55f, 0.15f), // Оранжевый
                new Color(1.00f, 1.00f, 1.00f), // Белый
                new Color(1.00f, 0.35f, 0.45f)  // Коралловый
            };

            GUILayout.BeginHorizontal();
            for (int p = 0; p < palettePresets.Length; p++)
            {
                Color pCol = palettePresets[p];
                bool isCurrent = Mathf.Approximately(Pref_ItemEspColorR.Value, pCol.r) &&
                                 Mathf.Approximately(Pref_ItemEspColorG.Value, pCol.g) &&
                                 Mathf.Approximately(Pref_ItemEspColorB.Value, pCol.b);

                GUI.color = pCol;
                if (GUILayout.Button(isCurrent ? "●" : "■", isCurrent ? paletteBtnActiveStyle : paletteBtnStyle, GUILayout.Height(26)))
                {
                    Pref_ItemEspColorR.Value = pCol.r;
                    Pref_ItemEspColorG.Value = pCol.g;
                    Pref_ItemEspColorB.Value = pCol.b;
                    SavePrefs();
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // RGB Fine-Tuning Sliders
            GUILayout.BeginHorizontal();
            GUILayout.Label($"R: {Mathf.RoundToInt(Pref_ItemEspColorR.Value * 255)}", cardKeyStyle, GUILayout.Width(45));
            float r = GUILayout.HorizontalSlider(Pref_ItemEspColorR.Value, 0f, 1f, GUILayout.Height(16));
            if (Mathf.Abs(r - Pref_ItemEspColorR.Value) > 0.01f) { Pref_ItemEspColorR.Value = r; SavePrefs(); }

            GUILayout.Label($"G: {Mathf.RoundToInt(Pref_ItemEspColorG.Value * 255)}", cardKeyStyle, GUILayout.Width(45));
            float g = GUILayout.HorizontalSlider(Pref_ItemEspColorG.Value, 0f, 1f, GUILayout.Height(16));
            if (Mathf.Abs(g - Pref_ItemEspColorG.Value) > 0.01f) { Pref_ItemEspColorG.Value = g; SavePrefs(); }

            GUILayout.Label($"B: {Mathf.RoundToInt(Pref_ItemEspColorB.Value * 255)}", cardKeyStyle, GUILayout.Width(45));
            float b = GUILayout.HorizontalSlider(Pref_ItemEspColorB.Value, 0f, 1f, GUILayout.Height(16));
            if (Mathf.Abs(b - Pref_ItemEspColorB.Value) > 0.01f) { Pref_ItemEspColorB.Value = b; SavePrefs(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Live Preview Box
            Color previewCol = new Color(Pref_ItemEspColorR.Value, Pref_ItemEspColorG.Value, Pref_ItemEspColorB.Value, 1f);
            Color prevCol = espPreviewStyle.normal.textColor;
            espPreviewStyle.normal.textColor = previewCol;
            GUILayout.Label("Превью в игре: ● Золотой слиток [$120] (14м)", espPreviewStyle);
            espPreviewStyle.normal.textColor = prevCol;

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void DrawFullBrightSettings()
        {
            GUILayout.BeginVertical(espBoxStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Яркость освещения: {Pref_FullBrightIntensity.Value:0.0}x", cardSubStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("0.8x", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_FullBrightIntensity.Value = 0.8f;
                SavePrefs();
            }
            if (GUILayout.Button("1.2x", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_FullBrightIntensity.Value = 1.2f;
                SavePrefs();
            }
            if (GUILayout.Button("2.0x", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_FullBrightIntensity.Value = 2.0f;
                SavePrefs();
            }
            if (GUILayout.Button("3.0x", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_FullBrightIntensity.Value = 3.0f;
                SavePrefs();
            }
            GUILayout.EndHorizontal();

            float newIntensity = GUILayout.HorizontalSlider(Pref_FullBrightIntensity.Value, 0.2f, 3.5f, GUILayout.Height(18));
            if (Mathf.Abs(newIntensity - Pref_FullBrightIntensity.Value) > 0.05f)
            {
                Pref_FullBrightIntensity.Value = (float)Math.Round(newIntensity, 1);
                SavePrefs();
            }

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        #region Actor ESP (Allies & Enemies)
        private static void UpdateCachedActors()
        {
            try
            {
                var actors = UnityEngine.Object.FindObjectsByType<Mimic.Actors.ProtoActor>(FindObjectsSortMode.None);
                if (actors == null || actors.Length == 0)
                {
                    cachedActorsList.Clear();
                    return;
                }

                cachedActorsList.Clear();
                for (int i = 0; i < actors.Length; i++)
                {
                    var a = actors[i];
                    if (a != null && a.gameObject.activeInHierarchy)
                    {
                        cachedActorsList.Add(a);
                    }
                }
            }
            catch { }
        }

        public static string GetMonsterDisplayName(int monsterMasterId, string fallbackPuppet)
        {
            if (monsterMasterId > 0 && monsterNamesById != null && monsterNamesById.TryGetValue(monsterMasterId, out var cachedName))
            {
                return cachedName;
            }

            try
            {
                var dataman = GetDataManager();
                if (dataman?.ExcelDataManager?.MonsterInfoDict != null &&
                    dataman.ExcelDataManager.MonsterInfoDict.TryGetValue(monsterMasterId, out var info) && info != null)
                {
                    var lcman = GetL10NManager();
                    string disp = null;
                    if (lcman != null && !string.IsNullOrEmpty(info.Name))
                    {
                        try
                        {
                            disp = lcman.GetText(info.Name);
                            if (disp != null && disp.StartsWith("|L10N")) disp = null;
                        }
                        catch { }
                    }
                    if (string.IsNullOrEmpty(disp))
                    {
                        disp = !string.IsNullOrEmpty(info.PuppetName) ? info.PuppetName : info.Name;
                    }
                    if (!string.IsNullOrEmpty(disp))
                    {
                        if (monsterNamesById == null) monsterNamesById = new Dictionary<int, string>();
                        monsterNamesById[monsterMasterId] = disp;
                        return disp;
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(fallbackPuppet)) return fallbackPuppet;
            return monsterMasterId > 0 ? $"Монстр #{monsterMasterId}" : "Враг";
        }

        private void DrawActorESP()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (cachedActorsList == null || cachedActorsList.Count == 0) return;

            InitStyles();

            Camera cam = GetMainCamera();
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;

            float maxDist = Pref_ActorEspDistance != null ? Pref_ActorEspDistance.Value : 100f;
            float maxDistSq = maxDist * maxDist;

            bool showAllies = Pref_ActorEspShowAllies != null ? Pref_ActorEspShowAllies.Value : true;
            bool showEnemies = Pref_ActorEspShowEnemies != null ? Pref_ActorEspShowEnemies.Value : true;
            bool showName = Pref_ActorEspShowName != null ? Pref_ActorEspShowName.Value : true;
            bool showDist = Pref_ActorEspShowDist != null ? Pref_ActorEspShowDist.Value : true;
            bool showHp = Pref_ActorEspShowHp != null ? Pref_ActorEspShowHp.Value : true;
            bool showRad = Pref_ActorEspShowRadiation != null ? Pref_ActorEspShowRadiation.Value : true;

            int actorFontSz = Pref_ActorEspFontSize != null ? Mathf.RoundToInt(Pref_ActorEspFontSize.Value) : 12;
            actorEspTextStyle.fontSize = actorFontSz;

            Color allyColor = new Color(0.20f, 0.98f, 0.40f, 1.0f);
            Color enemyColor = new Color(1.00f, 0.28f, 0.28f, 1.0f);

            int count = cachedActorsList.Count;
            for (int i = 0; i < count; i++)
            {
                var a = cachedActorsList[i];
                if (a == null || !a.gameObject.activeInHierarchy) continue;

                bool isLocal = false;
                try { isLocal = a.AmIAvatar(); } catch { }
                if (isLocal) continue;

                Vector3 pos = a.transform.position;
                Vector3 headPos = pos + Vector3.up * 1.8f;
                Vector3 toActor = headPos - camPos;

                float sqrDist = toActor.sqrMagnitude;
                if (sqrDist > maxDistSq || sqrDist < 0.2f) continue;

                if (Vector3.Dot(camFwd, toActor) <= 0.15f) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(headPos);
                if (screenPos.z <= 0.2f) continue;

                float guiX = screenPos.x;
                float guiY = Screen.height - screenPos.y;

                if (guiX < -100f || guiX > Screen.width + 100f || guiY < -100f || guiY > Screen.height + 100f)
                    continue;

                float dist = Mathf.Sqrt(sqrDist);

                bool isPlayer = false;
                try { isPlayer = a.IsPlayer(); } catch { }

                if (isPlayer)
                {
                    if (!showAllies) continue;

                    string pName = a.nickName;
                    if (string.IsNullOrWhiteSpace(pName) && a.netSyncActorData != null) pName = a.netSyncActorData.actorName;
                    if (string.IsNullOrWhiteSpace(pName)) pName = a.puppetName;
                    if (string.IsNullOrWhiteSpace(pName)) pName = $"Игрок #{a.ActorID}";

                    long hp = 0;
                    long maxHp = 100;
                    long conta = 0;
                    long maxConta = 100;
                    if (a.netSyncActorData != null)
                    {
                        hp = a.netSyncActorData.hp;
                        maxHp = a.netSyncActorData.maxHP > 0 ? a.netSyncActorData.maxHP : 100;
                        conta = a.netSyncActorData.conta;
                        maxConta = a.netSyncActorData.maxConta > 0 ? a.netSyncActorData.maxConta : 100;
                    }

                    int contaPct = maxConta > 0 ? Mathf.RoundToInt((float)conta / maxConta * 100f) : Mathf.RoundToInt(a.ContaAppearancePercent * 100f);
                    if (contaPct < 0) contaPct = 0;
                    if (contaPct > 100) contaPct = 100;

                    string hpText = a.dead ? "[МЁРТВ]" : $"{hp} HP";

                    // Custom element selection: Nickname, Distance, HP, Radiation
                    List<string> lines = new List<string>(4);
                    if (showName) lines.Add(pName);
                    if (showDist) lines.Add($"{dist:0}м");
                    if (showHp) lines.Add(hpText);
                    if (showRad) lines.Add($"Рад: {contaPct}%");
                    if (lines.Count == 0) lines.Add(pName);
                    string display = string.Join("\n", lines);

                    DrawActorEspText(guiX, guiY, display, allyColor);
                }
                else
                {
                    if (!showEnemies) continue;
                    if (a.dead) continue;

                    string enemyName = GetMonsterDisplayName(a.monsterMasterID, a.puppetName);
                    if (string.IsNullOrWhiteSpace(enemyName)) enemyName = "Враг";

                    long hp = a.netSyncActorData != null ? a.netSyncActorData.hp : 0;
                    string hpText = hp > 0 ? $"{hp} HP" : "";

                    // Custom element selection: Monster name, HP, Distance
                    List<string> lines = new List<string>(3);
                    if (showName) lines.Add(enemyName);
                    if (showHp && !string.IsNullOrEmpty(hpText)) lines.Add(hpText);
                    if (showDist) lines.Add($"{dist:0}м");
                    if (lines.Count == 0) lines.Add(enemyName);
                    string display = string.Join("\n", lines);

                    DrawActorEspText(guiX, guiY, display, enemyColor);
                }
            }
        }

        private void DrawActorEspText(float guiX, float guiY, string text, Color color)
        {
            Vector2 sz = actorEspTextStyle.CalcSize(new GUIContent(text));
            float w = sz.x + 8f;
            float h = sz.y + 4f;
            Rect r = new Rect(guiX - w * 0.5f, guiY - h, w, h);

            actorEspTextStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, actorEspTextStyle);

            actorEspTextStyle.normal.textColor = color;
            GUI.Label(r, text, actorEspTextStyle);
        }

        private void DrawActorEspSettings()
        {
            GUILayout.BeginVertical(espBoxStyle);

            // Distance header and presets
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Дистанция поиска: {Mathf.RoundToInt(Pref_ActorEspDistance.Value)} м", cardSubStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("50м", espPresetBtnStyle, GUILayout.Width(44), GUILayout.Height(22)))
            {
                Pref_ActorEspDistance.Value = 50f;
                SavePrefs();
            }
            if (GUILayout.Button("100м", espPresetBtnStyle, GUILayout.Width(48), GUILayout.Height(22)))
            {
                Pref_ActorEspDistance.Value = 100f;
                SavePrefs();
            }
            if (GUILayout.Button("150м", espPresetBtnStyle, GUILayout.Width(48), GUILayout.Height(22)))
            {
                Pref_ActorEspDistance.Value = 150f;
                SavePrefs();
            }
            if (GUILayout.Button("200м", espPresetBtnStyle, GUILayout.Width(48), GUILayout.Height(22)))
            {
                Pref_ActorEspDistance.Value = 200f;
                SavePrefs();
            }
            GUILayout.EndHorizontal();

            // Distance slider
            float newDist = GUILayout.HorizontalSlider(Pref_ActorEspDistance.Value, 10f, 300f, GUILayout.Height(18));
            if (Mathf.Abs(newDist - Pref_ActorEspDistance.Value) > 0.5f)
            {
                Pref_ActorEspDistance.Value = Mathf.Round(newDist);
                SavePrefs();
            }

            GUILayout.Space(6);

            // Toggles for Allies and Enemies
            GUILayout.BeginHorizontal();

            bool curAllies = Pref_ActorEspShowAllies.Value;
            bool newAllies = GUILayout.Toggle(curAllies, "  Союзники (Зелёный)", espPreviewStyle, GUILayout.Height(22));
            if (newAllies != curAllies)
            {
                Pref_ActorEspShowAllies.Value = newAllies;
                SavePrefs();
            }

            GUILayout.Space(14);

            bool curEnemies = Pref_ActorEspShowEnemies.Value;
            bool newEnemies = GUILayout.Toggle(curEnemies, "  Враги (Красный)", espPreviewStyle, GUILayout.Height(22));
            if (newEnemies != curEnemies)
            {
                Pref_ActorEspShowEnemies.Value = newEnemies;
                SavePrefs();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }
        #endregion

        private static void SavePrefs()
        {
            Category.SaveToFile();
            MelonPreferences.Save();
        }
        #endregion
    }

    #region Input Block While Menu Open
    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "RotateByInput")]
    public static class Patch_ProtoActor_RotateByInput
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.ShowMenu) return false;
            return true;
        }
    }
    #endregion

    #region 1. Infinite Stamina (Client + Host)
    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "UpdateStamina")]
    public static class Patch_ProtoActor_UpdateStamina
    {
        [HarmonyPrefix]
        public static void Prefix(Mimic.Actors.ProtoActor __instance, ref long newStamina)
        {
            if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value)
            {
                try
                {
                    if (__instance != null && __instance.AmIAvatar())
                    {
                        newStamina = 10000L;
                    }
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "ProcessSprintKey")]
    public static class Patch_ProtoActor_ProcessSprintKey
    {
        [HarmonyPrefix]
        public static void Prefix(Mimic.Actors.ProtoActor __instance)
        {
            if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value)
            {
                try
                {
                    if (__instance != null && __instance.AmIAvatar() && __instance.netSyncActorData != null)
                    {
                        if (__instance.netSyncActorData.stamina < 10000L)
                        {
                            __instance.netSyncActorData.stamina = 10000L;
                        }
                    }
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "Update")]
    public static class Patch_ProtoActor_Update
    {
        [HarmonyPostfix]
        public static void Postfix(Mimic.Actors.ProtoActor __instance)
        {
            try
            {
                if (__instance != null && __instance.AmIAvatar() && __instance.netSyncActorData != null)
                {
                    if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value)
                    {
                        __instance.netSyncActorData.stamina = 10000L;
                    }
                    if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value)
                    {
                        __instance.netSyncActorData.conta = 0L;
                    }
                    if (Plugin.Pref_NoWeightSlowdown != null && Plugin.Pref_NoWeightSlowdown.Value)
                    {
                        if (__instance.netSyncActorData.MoveSpeedWalk < 400L) __instance.netSyncActorData.MoveSpeedWalk = 400L;
                        if (__instance.netSyncActorData.MoveSpeedRun < 600L) __instance.netSyncActorData.MoveSpeedRun = 600L;
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.ConsumeStamina))]
    public static class Patch_StatManager_ConsumeStamina
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(StatController), nameof(StatController.ConsumeStamina))]
    public static class Patch_StatController_ConsumeStamina
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.GetCurrentStamina))]
    public static class Patch_StatManager_GetCurrentStamina
    {
        [HarmonyPrefix]
        public static bool Prefix(ref long __result)
        {
            if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value)
            {
                __result = 10000L;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatController), nameof(StatController.GetCurrentStamina))]
    public static class Patch_StatController_GetCurrentStamina
    {
        [HarmonyPrefix]
        public static bool Prefix(ref long __result)
        {
            if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value)
            {
                __result = 10000L;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.IsStaminaFull))]
    public static class Patch_StatManager_IsStaminaFull
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (Plugin.Pref_InfiniteStamina != null && Plugin.Pref_InfiniteStamina.Value)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
    #endregion

    #region 2. No Weight Slowdown (Client + Host)
    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "get_walkSpeed")]
    public static class Patch_ProtoActor_WalkSpeed
    {
        [HarmonyPostfix]
        public static void Postfix(Mimic.Actors.ProtoActor __instance, ref float __result)
        {
            if (Plugin.Pref_NoWeightSlowdown != null && Plugin.Pref_NoWeightSlowdown.Value)
            {
                if (__result < 4.0f) __result = 4.0f;
            }
        }
    }

    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "get_runSpeed")]
    public static class Patch_ProtoActor_RunSpeed
    {
        [HarmonyPostfix]
        public static void Postfix(Mimic.Actors.ProtoActor __instance, ref float __result)
        {
            if (Plugin.Pref_NoWeightSlowdown != null && Plugin.Pref_NoWeightSlowdown.Value)
            {
                if (__result < 6.0f) __result = 6.0f;
            }
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.SetMoveSpeedDecreaseRateByWeight))]
    public static class Patch_StatManager_SetMoveSpeedDecreaseRateByWeight
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int rate)
        {
            if (Plugin.Pref_NoWeightSlowdown != null && Plugin.Pref_NoWeightSlowdown.Value)
            {
                rate = 0;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatController), nameof(StatController.SetMoveSpeedDecreaseRateByWeight))]
    public static class Patch_StatController_SetMoveSpeedDecreaseRateByWeight
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int rate)
        {
            if (Plugin.Pref_NoWeightSlowdown != null && Plugin.Pref_NoWeightSlowdown.Value)
            {
                rate = 0;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), "CalcImmutableStats")]
    public static class Patch_StatManager_CalcImmutableStats
    {
        [HarmonyPostfix]
        public static void Postfix(StatManager __instance)
        {
            if (Plugin.Pref_NoWeightSlowdown != null && Plugin.Pref_NoWeightSlowdown.Value)
            {
                try
                {
                    __instance.SetMoveSpeedDecreaseRateByWeight(0);
                }
                catch { }
            }
        }
    }
    #endregion

    #region 3. Silent Steps (Host / AI Aggro)
    [HarmonyPatch(typeof(MovementController), nameof(MovementController.BroadcastAggro))]
    public static class Patch_MovementController_BroadcastAggro
    {
        [HarmonyPrefix]
        public static bool Prefix(MovementController __instance)
        {
            if (Plugin.Pref_SilentSteps.Value)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(AIController), nameof(AIController.AddMovementAggroPoint))]
    public static class Patch_AIController_AddMovementAggroPoint
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_SilentSteps.Value)
            {
                return false;
            }
            return true;
        }
    }
    #endregion

    #region 4. Infinite Battery (Client + Host)
    [HarmonyPatch(typeof(Mimic.InventoryItem), "get_RemainGauge")]
    public static class Patch_InventoryItem_get_RemainGauge
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                __result = 10000;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Mimic.InventoryItem), "set_RemainGauge")]
    public static class Patch_InventoryItem_set_RemainGauge
    {
        [HarmonyPrefix]
        public static void Prefix(ref int value)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                value = 10000;
            }
        }
    }

    [HarmonyPatch(typeof(Mimic.InventoryItem), "get_Durability")]
    public static class Patch_InventoryItem_get_Durability
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                __result = 10000;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Mimic.InventoryItem), "set_Durability")]
    public static class Patch_InventoryItem_set_Durability
    {
        [HarmonyPrefix]
        public static void Prefix(ref int value)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                value = 10000;
            }
        }
    }

    [HarmonyPatch(typeof(Mimic.Actors.Flashlight), "OnGaugeChanged")]
    public static class Patch_Flashlight_OnGaugeChanged
    {
        [HarmonyPrefix]
        public static void Prefix(ref int remainGauge)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                remainGauge = 10000;
            }
        }
    }

    [HarmonyPatch(typeof(EquipmentItemElement), nameof(EquipmentItemElement.SetAmount))]
    public static class Patch_EquipmentItemElement_SetAmount
    {
        [HarmonyPrefix]
        public static void Prefix(ref int amount)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                amount = 10000;
            }
        }
    }

    [HarmonyPatch(typeof(EquipmentItemElement), nameof(EquipmentItemElement.SetDurability))]
    public static class Patch_EquipmentItemElement_SetDurability
    {
        [HarmonyPrefix]
        public static void Prefix(ref int durability)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                durability = 10000;
            }
        }
    }

    [HarmonyPatch(typeof(EquipmentItemElement), nameof(EquipmentItemElement.CheckElapsed))]
    public static class Patch_EquipmentItemElement_CheckElapsed
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryController), "CanDestroyItemByGauge")]
    public static class Patch_InventoryController_CanDestroyItemByGauge
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryController), "CanDestroyItemByDurability")]
    public static class Patch_InventoryController_CanDestroyItemByDurability
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (Plugin.Pref_InfiniteBattery != null && Plugin.Pref_InfiniteBattery.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
    #endregion

    #region 5. Anti-Contamination (Client + Host)
    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "UpdateConta")]
    public static class Patch_ProtoActor_UpdateConta
    {
        [HarmonyPrefix]
        public static void Prefix(Mimic.Actors.ProtoActor __instance, ref long conta)
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value)
            {
                try
                {
                    if (__instance != null && __instance.AmIAvatar())
                    {
                        conta = 0L;
                    }
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.IncreaseConta), new Type[] { typeof(long) })]
    public static class Patch_StatManager_IncreaseConta_Int64
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.IncreaseConta), new Type[] { typeof(int) })]
    public static class Patch_StatManager_IncreaseConta_Int32
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.UpdateConta))]
    public static class Patch_StatManager_UpdateConta
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(StatController), nameof(StatController.IncreaseConta), new Type[] { typeof(long) })]
    public static class Patch_StatController_IncreaseConta
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(StatController), nameof(StatController.AdjustConta))]
    public static class Patch_StatController_AdjustConta
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.GetCurrentConta))]
    public static class Patch_StatManager_GetCurrentConta
    {
        [HarmonyPrefix]
        public static bool Prefix(ref long __result)
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value)
            {
                __result = 0L;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatController), nameof(StatController.GetCurrentConta))]
    public static class Patch_StatController_GetCurrentConta
    {
        [HarmonyPrefix]
        public static bool Prefix(ref long __result)
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value)
            {
                __result = 0L;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), nameof(StatManager.IsContaExceeded))]
    public static class Patch_StatManager_IsContaExceeded
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (Plugin.Pref_AntiContamination != null && Plugin.Pref_AntiContamination.Value)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
    #endregion

    #region 6. God Mode (Client + Host)
    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "UpdateHp")]
    public static class Patch_ProtoActor_UpdateHp
    {
        [HarmonyPrefix]
        public static void Prefix(Mimic.Actors.ProtoActor __instance, ref long hp, ref long maxHP)
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                try
                {
                    if (__instance != null && __instance.AmIAvatar())
                    {
                        if (maxHP <= 0) maxHP = 100L;
                        hp = maxHP;
                    }
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "OnActorDeath")]
    public static class Patch_ProtoActor_OnActorDeath
    {
        [HarmonyPrefix]
        public static bool Prefix(Mimic.Actors.ProtoActor __instance)
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                try
                {
                    if (__instance != null && __instance.AmIAvatar())
                    {
                        return false; // Suppress death completely
                    }
                }
                catch { }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "ResolvePacket_ActorDyingSig")]
    public static class Patch_ProtoActor_ResolvePacket_ActorDyingSig
    {
        [HarmonyPrefix]
        public static bool Prefix(Mimic.Actors.ProtoActor __instance)
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                try
                {
                    if (__instance != null && __instance.AmIAvatar())
                    {
                        return false; // Ignore server dying packet
                    }
                }
                catch { }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Mimic.Actors.ProtoActor), "set_dead")]
    public static class Patch_ProtoActor_set_dead
    {
        [HarmonyPrefix]
        public static bool Prefix(Mimic.Actors.ProtoActor __instance, ref bool value)
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value && value)
            {
                try
                {
                    if (__instance != null && __instance.AmIAvatar())
                    {
                        value = false;
                        return false;
                    }
                }
                catch { }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MovementController), "ApplyFallDamage")]
    public static class Patch_MovementController_ApplyFallDamage
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MovementController), "CheckFallDamage")]
    public static class Patch_MovementController_CheckFallDamage
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatController), "ApplyDamage")]
    public static class Patch_StatController_ApplyDamage
    {
        [HarmonyPrefix]
        public static bool Prefix(StatController __instance, ApplyDamageArgs args)
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                try
                {
                    if (args != null && args.Victim != null)
                    {
                        var local = Plugin.GetLocalPlayer();
                        if (local != null && args.Victim.ObjectID == local.ActorID)
                        {
                            return false; // Ignore all damage calculations
                        }
                    }
                }
                catch { }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), "ApplyDamage")]
    public static class Patch_StatManager_ApplyDamage
    {
        [HarmonyPrefix]
        public static bool Prefix(ApplyDamageArgs args)
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                try
                {
                    var local = Plugin.GetLocalPlayer();
                    if (local != null && args != null && args.Victim != null)
                    {
                        if (args.Victim.ObjectID == local.ActorID)
                        {
                            return false;
                        }
                    }
                }
                catch { }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StatManager), "OnDamaged")]
    public static class Patch_StatManager_OnDamaged
    {
        [HarmonyPrefix]
        public static bool Prefix(StatManager __instance, ApplyDamageArgs args)
        {
            if (Plugin.Pref_GodMode != null && Plugin.Pref_GodMode.Value)
            {
                try
                {
                    var local = Plugin.GetLocalPlayer();
                    if (local != null)
                    {
                        if (args != null && args.Victim != null && args.Victim.ObjectID == local.ActorID)
                        {
                            return false;
                        }
                    }
                }
                catch { }
            }
            return true;
        }
    }
    #endregion
}
