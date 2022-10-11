using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using LOR_DiceSystem;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using UnityEngine;
using Opening;
using StoryScene;
using System.Linq;
using System.IO;
using Sound;

namespace LibraryOfRuinaQolMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public partial class Plugin : BaseUnityPlugin
    {
        static readonly string toggleSection = "Toggles";
        static readonly string numberSection = "Numbers";
        static readonly string keyboardShortcutSection = "Keyboard Shortcuts";

        public static ConfigEntry<bool> AllowReceptionPassiveAttribution;
        public static ConfigEntry<bool> DisableBookLoss;
        public static ConfigEntry<bool> DropEverything;
        public static ConfigEntry<bool> EnhanceSearchSetting;
        public static ConfigEntry<bool> FastDeckImport;
        public static ConfigEntry<bool> FastStartup;
        public static ConfigEntry<bool> FastText;
        public static ConfigEntry<bool> InfiniteResources;
        public static ConfigEntry<bool> LogStats;
        public static ConfigEntry<bool> ShowTrueDiceValues;
        public static ConfigEntry<bool> ShowTrueDiceValuesExtreme;
        public static ConfigEntry<bool> UseCardHotkeys;
        public static ConfigEntry<int> BookDropMultiplier;
        public static ConfigEntry<int> CombatSpeedMultiplier;
        public static ConfigEntry<int> DiceRollSimulationIterations;
        public static ConfigEntry<int> WhiteNightStart;
        public static ConfigEntry<int> WhiteNightEnd;
        public static ConfigEntry<KeyboardShortcut> ShowOriginalDiceValuesShortcut;

        public static ManualLogSource logger;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            logger = Logger;

            AllowReceptionPassiveAttribution = Config.Bind(toggleSection, "AllowReceptionPassiveAttribution", true, "Enable passive attribution in reception menu");
            DisableBookLoss = Config.Bind(toggleSection, "DisableBookLoss", false, "Stop books from decreasing");
            DropEverything = Config.Bind(toggleSection, "DropEverything", false, "Burning a book drops 10x as much");
            EnhanceSearchSetting = Config.Bind(toggleSection, "EnhanceSearchSetting", true, "Enable searching ranged cards by typing \"ranged\"");
            FastDeckImport = Config.Bind(toggleSection, "FastDeckImport", false, "Enable fast deck import. Middle clicking on a saved deck immediately imports it.");
            FastStartup = Config.Bind(toggleSection, "FastStartup", true, "Disables startup logos and video");
            FastText = Config.Bind(toggleSection, "FastText", true, "Make dialogue text display 4x faster");
            InfiniteResources = Config.Bind(toggleSection, "InfiniteResources", false, "Fixes the amount of cards to 50");
            LogStats = Config.Bind(toggleSection, "LogStats", false, "Shows historical stats in battle");
            ShowTrueDiceValues = Config.Bind(toggleSection, "ShowTrueDiceValues", false, "Try to predict the range of dice rolls on cards in speed dice, accounting for buffs and debuffs");
            ShowTrueDiceValuesExtreme = Config.Bind(toggleSection, "ShowTrueDiceValuesExtreme", false, "Try even harder to predict dice values. May have side effects.");
            UseCardHotkeys = Config.Bind(toggleSection, "UseCardHotkeys", false, "Add hotkeys (numbers 1-9) to select cards in hand.");

            BookDropMultiplier = Config.Bind(numberSection, "BookDropMultiplier", 1, new ConfigDescription("Multiply the number of books dropped by this number", new AcceptableValueRange<int>(1, 10)));
            CombatSpeedMultiplier = Config.Bind(numberSection, "CombatSpeedMultiplier", 1, new ConfigDescription("Multiply the speed of combat by this number", new AcceptableValueRange<int>(1, 10)));
            DiceRollSimulationIterations = Config.Bind(numberSection, "DiceRollSimulationIterations", 10, new ConfigDescription("How many times to simulate dice rolls to find max and min values (higher is slower but more accurate)", new AcceptableValueRange<int>(1, 1000)));

            ShowOriginalDiceValuesShortcut = Config.Bind(keyboardShortcutSection, "ShowOriginalDiceValuesShortcut", new KeyboardShortcut(KeyCode.LeftShift), "While this key is down, show the original values of dice rolls");

            WhiteNightStart = Config.Bind(numberSection, "WhiteNightStart", 0, new ConfigDescription("WhiteNight start", new AcceptableValueRange<int>(0, 3)));
            WhiteNightEnd = Config.Bind(numberSection, "WhiteNightEnd", 0, new ConfigDescription("WhiteNightEnd", new AcceptableValueRange<int>(0, 3)));

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched method {method.DeclaringType.Name}.{method.Name}");
            }
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is done patching!");

            //DumpAssets();
        }

        Texture2D duplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        void DumpAssets()
        {
            string outputDir = @"D:\my-files\my-documents\extracted\library-of-ruina-assets\RawImages";
            string cardArtworkOutputDir = $"{outputDir}/card-artwork";
            var cardAssets = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "AssetBundles", "card_art.pres"));
            var cardArt = cardAssets.LoadAllAssets<Sprite>();
            var egoCardArt = Resources.LoadAll<Sprite>("Sprites/CreatureArtworks/");

            logger.LogDebug($"{cardArt.Length} card images and {egoCardArt.Length} EGO card images");
            foreach (var art in cardArt)
            {
                var fileName = $"{cardArtworkOutputDir}/{art.name}.png";
                logger.LogDebug($"Writing to file {fileName}");

                var texture = duplicateTexture(art.texture);
                byte[] image = texture.EncodeToPNG();
                File.WriteAllBytes(fileName, image);
            }

            foreach (var art in egoCardArt)
            {
                var fileName = $"{cardArtworkOutputDir}/{art.name}.png";
                logger.LogDebug($"Writing to file {fileName}");

                var texture = duplicateTexture(art.texture);
                byte[] image = texture.EncodeToPNG();
                File.WriteAllBytes(fileName, image);
            }
        }

        class Patches_AllowReceptionPassiveAttribution
        {
            [HarmonyPatch(typeof(UIBattleSettingPanel), "OnOpen")]
            class Patch_UIBattleSettingPanel_OnOpen
            {
                static void Postfix(UIBattleSettingLibrarianInfoPanel ___infoRightPanel)
                {
                    logger.LogDebug($"UIBattleSettingPanel.OnOpen() Passive slots panel enabled: {___infoRightPanel.passiveSlotsPanel.selectable} {___infoRightPanel.passiveSlotsPanel.enabled} {___infoRightPanel.passiveSlotsPanel.isActiveAndEnabled}");
                }
            }

            [HarmonyPatch(typeof(UIBattleSettingLibrarianInfoPanel), "SetData")]
            class Patch_UIBattleSettingLibrarianInfoPanel_SetData
            {
                static readonly EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
                static readonly EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
                static readonly EventTrigger.Entry pointerClickEvent = new EventTrigger.Entry();
                static UnitDataModel currentCharacter;

                static void Postfix(UIBattleSettingLibrarianInfoPanel __instance, UnitDataModel data)
                {
                    if (!AllowReceptionPassiveAttribution.Value)
                    {
                        return;
                    }
                    logger.LogDebug($"UIBattleSettingLibrarianInfoPanel.SetData() instance: {__instance.GetInstanceID()}, unit: {data.name}, owner sephirah: {data.OwnerSephirah}");
                    // there must be a better way to check if character is an enemy
                    if (data.OwnerSephirah == SephirahType.None)
                    {
                        return;
                    }
                    currentCharacter = data;

                    var passiveSlotsSelectable = __instance.passiveSlotsPanel.selectable;
                    var trigger = Traverse.Create(passiveSlotsSelectable).Field("trigger").GetValue() as EventTrigger;
                    if (!trigger.triggers.Contains(pointerEnterEvent))
                    {
                        logger.LogDebug($"UISetInfoSlotListSc added PointerEnter event listener");
                        pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
                        pointerEnterEvent.callback.AddListener(delegate (BaseEventData eventData)
                        {
                            logger.LogDebug($"UISetInfoSlotListSc passive list hovered! unit: {currentCharacter.name}");
                        });

                        trigger.triggers.Add(pointerEnterEvent);
                    }

                    if (!trigger.triggers.Contains(pointerExitEvent))
                    {
                        logger.LogDebug($"UISetInfoSlotListSc added PointerExit event listener");
                        pointerExitEvent.eventID = EventTriggerType.PointerExit;
                        pointerExitEvent.callback.AddListener(delegate (BaseEventData eventData)
                        {
                        });
                        trigger.triggers.Add(pointerExitEvent);
                    }

                    if (!trigger.triggers.Contains(pointerClickEvent))
                    {
                        logger.LogDebug($"UISetInfoSlotListSc added PointerClick event listener");
                        pointerClickEvent.eventID = EventTriggerType.PointerClick;
                        pointerClickEvent.callback.AddListener(delegate (BaseEventData eventData)
                        {
                            logger.LogDebug($"UISetInfoSlotListSc passive list clicked! unit: {currentCharacter.name} {currentCharacter}, unit is sephirah: {currentCharacter.isSephirah}, unit class info id: {currentCharacter.defaultBook.ClassInfo.id}, current ui phase: {UI.UIController.Instance.CurrentUIPhase}");
                            if (UIControlManager.GetInpuTypeOf(eventData) == InputType.RightClick)
                            {
                                return;
                            }
                            if (!LibraryModel.Instance.CanPassiveSuccession())
                            {
                                return;
                            }
                            if (currentCharacter == null)
                            {
                                return;
                            }
                            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
                            if (UI.UIController.Instance.CurrentUIPhase == UIPhase.Main_ItemList)
                            {
                                (UI.UIController.Instance.GetUIPanel(UIPanelType.ItemList) as UIEquipPageInventoryPanel).ReleaseSelectedSlot();
                            }
                            UIPassiveSuccessionPopup.Instance.SetData(currentCharacter, delegate
                            {
                                UIControlManager.Instance.SelectSelectableForcely(passiveSlotsSelectable, false);
                            });
                        });
                        trigger.triggers.Add(pointerClickEvent);
                    }
                    logger.LogDebug($"UIBattleSettingLibrarianInfoPanel has {trigger.triggers.Count} triggers, parent selectable: {passiveSlotsSelectable.parentSelectable}");
                }
            }

            // somehow I can patch delegates (event listeners)
            [HarmonyPatch(typeof(UIPassiveSuccessionPopup), "<OnClickApplyButton>b__42_0")]
            class Patch_UIPassiveSuccessionPopup_OnClickApplyButton
            {
                static void Postfix(bool b)
                {
                    UIPhase currentUIPhase = UI.UIController.Instance.CurrentUIPhase;
                    logger.LogDebug($"UIPassiveSuccessionPopup.OnClickApplyButton() yes: {b}, current ui phase: {currentUIPhase}");
                    if (b)
                    {
                        if (currentUIPhase == UIPhase.BattleSetting)
                        {
                            UIBattleSettingPanel uibattleSettingPanel = UIPanel.Controller.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel;
                            uibattleSettingPanel.SetLibrarianProfileData(UI.UIController.Instance.CurrentUnit);
                            uibattleSettingPanel.UpdateEditPanel();
                        }
                    }
                }
            }
        }

        class Patches_DisableBookLoss
        {
            [HarmonyPatch(typeof(DropBookInventoryModel), "RemoveBook")]
            class Patch_DropBookInventoryModel_RemoveBook
            {
                static bool Prefix()
                {
                    return !DisableBookLoss.Value;
                }
            }
        }

        class Patches_DropEverything
        {
            [HarmonyPatch(typeof(LibraryFloorModel), "FeedBook")]
            class Patch_LibraryFloorModel_FeedBook
            {
                static void Prefix(ref DropBookXmlInfo book)
                {
                    if (DropEverything.Value)
                    {
                        book.DropNum *= 10;
                    }
                }
            }
        }

        class Patches_EnhanceSearchSetting
        {
            [HarmonyPatch(typeof(UIInvenCardListScroll), "GetCardBySearchFilterUI")]
            class Patch_UIInvenCardListScroll_GetCardBySearchFilterUI
            {
                static string originalSearchKey;

                static void Prefix(UIInvenCardListScroll __instance)
                {
                    if (!EnhanceSearchSetting.Value)
                    {
                        return;
                    }
                    //logger.LogDebug($"UIInvenCardListScroll.GetCardBySearchFilterUI() prefix");
                    string searchKey = __instance.CardFilter.CheckSearchKey();
                    originalSearchKey = searchKey;
                    RegexOptions options = RegexOptions.IgnoreCase;
                    searchKey = Regex.Replace(searchKey, @"\branged\b", "", options);
                    searchKey = Regex.Replace(searchKey, @"\bsummation\b", "", options);
                    searchKey = Regex.Replace(searchKey, @"\bindividual\b", "", options);
                    Traverse.Create(__instance.CardFilter).Field("SearchFilterPanel").Field("SearchKey").SetValue(searchKey);
                    //logger.LogDebug($"UIInvenCardListScroll.GetCardBySearchFilterUI() prefix search key after filtering: {searchKey} = {__instance.CardFilter.CheckSearchKey()}");
                }

                static void Postfix(UIInvenCardListScroll __instance, ref List<DiceCardItemModel> __result)
                {
                    if (!EnhanceSearchSetting.Value)
                    {
                        return;
                    }
                    //logger.LogDebug($"UIInvenCardListScroll.GetCardBySearchFilterUI() postfix. Original search key: {originalSearchKey}, new search key: {__instance.CardFilter.CheckSearchKey()}");
                    var newResult = new List<DiceCardItemModel>();
                    RegexOptions options = RegexOptions.IgnoreCase;
                    string searchKey = originalSearchKey;
                    if (Regex.IsMatch(searchKey, @"\branged\b", options))
                    {
                        newResult.AddRange(__result.FindAll(delegate (DiceCardItemModel card)
                        {
                            return card.GetSpec().Ranged == CardRange.Far;
                        }));
                    }
                    if (Regex.IsMatch(searchKey, @"\bsummation\b", options))
                    {
                        newResult.AddRange(__result.FindAll(delegate (DiceCardItemModel card)
                        {
                            return card.GetSpec().Ranged == CardRange.FarArea;
                        }));
                    }
                    if (Regex.IsMatch(searchKey, @"\bindividual\b", options))
                    {
                        newResult.AddRange(__result.FindAll(delegate (DiceCardItemModel card)
                        {
                            return card.GetSpec().Ranged == CardRange.FarAreaEach;
                        }));
                    }
                    if (newResult.Count > 0)
                    {
                        __result = newResult;
                    }
                    Traverse.Create(__instance.CardFilter).Field("SearchFilterPanel").Field("SearchKey").SetValue(originalSearchKey);
                }
            }
        }

        class Patches_FastDeckImport
        {
            [HarmonyPatch(typeof(UIDeckSlot), "OnPointerClick")]
            class Patch_UIDeckSlot_OnPointerClick
            {
                private static UILibrarianEquipDeckPanel GetCardDeckPanel()
                {
                    if (UI.UIController.Instance.CurrentUIPhase != UIPhase.BattleSetting)
                    {
                        return (UI.UIController.Instance.GetUIPanel(UIPanelType.Page) as UICardPanel).EquipInfoDeckPanel;
                    }
                    return (UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel).EditPanel.BattleCardPanel.EquipInfoDeckPanel;
                }

                static bool Prefix(UIDeckSlot __instance, BaseEventData bData, bool ___editable)
                {
                    if (FastDeckImport.Value)
                    {
                        if (___editable && UIControlManager.GetInpuTypeOf(bData) == InputType.WheelClick)
                        {
                            GetCardDeckPanel().Unitdata.EmptyDeckToInventory();
                            var curDeckSlot = __instance.deckSlotModel;
                            for (int i = 0; i < curDeckSlot.Deck.GetAllCardList().Count; i++)
                            {
                                if (GetCardDeckPanel().Unitdata.AddCardFromInventory(curDeckSlot.Deck.GetAllCardList()[i].id) != CardEquipState.Equippable)
                                {
                                    logger.LogError("Cannot Load Card" + curDeckSlot.Deck.GetAllCardList()[i].id);
                                }
                            }
                            GetCardDeckPanel().EquipDeckPanel.changed = true;
                            GetCardDeckPanel().SetData();
                            GetCardDeckPanel().RefreshAll();
                            UIControlManager.Instance.SelectSelectableForcely(GetCardDeckPanel().GetOpenDeckListSelectable(), false);
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        class Patches_FastStartup
        {
            [HarmonyPatch(typeof(LogoPlayer), "Update")]
            class Patch_LogoPlayer_PlayLogo
            {
                static void Postfix(LogoPlayer __instance)
                {
                    logger.LogDebug($"LogoPlayer.Update() postfix");
                    if (FastStartup.Value)
                    {
                        Traverse.Create(__instance).Method("EndLogo").GetValue();
                    }
                }
            }

            [HarmonyPatch(typeof(GameOpeningController), "PlayOpening")]
            class Patch_GameOpeningController_PlayOpening
            {
                static void Postfix(GameOpeningController __instance)
                {
                    logger.LogDebug($"GameOpeningController.PlayOpening() postfix");
                    if (FastStartup.Value)
                    {
                        __instance.StopOpening();
                    }
                }
            }
        }

        class Patches_FastText
        {
            [HarmonyPatch(typeof(StoryManager), "ChangeFontByLanguage")]
            class Patch_StoryManager_DisplayDialog
            {
                static void Postfix(ref float ___dialogWaitTime)
                {
                    logger.LogDebug($"StoryManager.DisplayDialog() postfix");
                    if (FastText.Value)
                    {
                        ___dialogWaitTime /= 4;
                    }
                }
            }
        }

        class Patches_InfiniteResources
        {
            [HarmonyPatch(typeof(InventoryModel), "RemoveCard")]
            class Patch_InventoryModel_RemoveCard
            {
                static void Postfix(LorId cardId, List<DiceCardItemModel> ____cardList, bool __result)
                {
                    if (__result && InfiniteResources.Value)
                    {
                        DiceCardItemModel diceCardItemModel = ____cardList.Find((DiceCardItemModel x) => x.GetID() == cardId);
                        diceCardItemModel.num = 50;
                    }
                }
            }
        }

        class Patches_LogStats
        {
            static readonly Dictionary<string, int> damageDealt = new Dictionary<string, int>();
            static readonly Dictionary<string, int> damageTaken = new Dictionary<string, int>();

            [HarmonyPatch(typeof(MapManager), "InitializeMap")]
            class Patch_MapManager_InitializeMap
            {
                static void Postfix()
                {
                    damageDealt.Clear();
                    damageTaken.Clear();
                }
            }

            [HarmonyPatch(typeof(BattleUnitModel), "TakeDamage")]
            class Patch_BattleUnitModel_TakeDamage
            {
                static void Postfix(BattleUnitModel __instance, int v, DamageType type, BattleUnitModel attacker, KeywordBuf keyword, int __result)
                {
                    if (!LogStats.Value || __instance.IsDead())
                    {
                        return;
                    }
                    int damage = __result;

                    logger.LogDebug($"{__instance.UnitData.unitData.name} took {damage} ({v}) {type} damage from {attacker?.UnitData.unitData.name ?? "nobody"} with keyword {keyword}");

                    string damageTypeName = type.ToString();
                    if (keyword != KeywordBuf.None)
                    {
                        damageTypeName = keyword.ToString();
                    }

                    if (__instance.faction == Faction.Player)
                    {
                        if (!damageTaken.ContainsKey(damageTypeName))
                        {
                            damageTaken[damageTypeName] = damage;
                        }
                        else
                        {
                            damageTaken[damageTypeName] += damage;
                        }
                    }
                    else
                    {
                        if (!damageDealt.ContainsKey(damageTypeName))
                        {
                            damageDealt[damageTypeName] = damage;
                        }
                        else
                        {
                            damageDealt[damageTypeName] += damage;
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(BattleManagerUI), "Init")]
            class Patch_BattleManagerUI_Init
            {
                static void Postfix(BattleManagerUI __instance)
                {
                    logger.LogDebug($"BattleManagerUI.Init()");
                    if (__instance.GetComponent<GlobalMonoBehaviour>() == null)
                    {
                        logger.LogDebug($"Adding LogText component to BattleManagerUI");
                        __instance.gameObject.AddComponent<GlobalMonoBehaviour>();
                    }
                }
            }

            class GlobalMonoBehaviour : MonoBehaviour
            {
                void Awake()
                {
                    logger.LogDebug($"LogText is awake");
                }

                void Start()
                {
                    logger.LogDebug($"LogText is enabled");
                }

                void OnGUI()
                {
                    if (!LogStats.Value)
                    {
                        return;
                    }

                    string damageDealtText = "Damage dealt this battle by damage type:\n";
                    foreach (var entry in damageDealt)
                    {
                        damageDealtText += $"{entry.Key}: {entry.Value}\n";
                    }
                    string damageTakenText = "Damage taken this battle by damage type:\n";
                    foreach (var entry in damageTaken)
                    {
                        damageTakenText += $"{entry.Key}: {entry.Value}\n";
                    }
                    GUI.Label(new Rect(Screen.width / 2 - 150, 100, 100, 100), damageDealtText);
                    GUI.Label(new Rect(Screen.width / 2 + 60, 100, 100, 100), damageTakenText);
                }
            }
        }

        partial class Patches_ShowTrueDiceValues
        {
            static readonly Dictionary<string, List<string>> cachedDiceRolls = new Dictionary<string, List<string>>();

            static int GetFinalDiceValue(BattleDiceBehavior dice, int value)
            {
                //logger.LogDebug($"GetFinalDiceValue dice: {dice}, value: {value}");

                if (dice.abilityList.Exists((DiceCardAbilityBase x) => x.Invalidity))
                {
                    return 0;
                }
                var statBonus = Traverse.Create(dice).Field("_statBonus").GetValue() as DiceStatBonus;
                if (statBonus.ignorePower)
                {
                    return value;
                }
                if (dice.card != null)
                {
                    if (dice.card.ignorePower)
                    {
                        return Mathf.Max(1, value);
                    }
                    if (dice.owner != null && dice.owner.IsNullifyPower())
                    {
                        return Mathf.Max(1, value);
                    }
                }
                int power = statBonus.power;
                if (dice.abilityList.Find((DiceCardAbilityBase x) => x.IsDoublePower()) != null)
                {
                    power += statBonus.power;
                }
                if (dice.card != null && dice.owner != null && dice.owner.IsHalfPower())
                {
                    power /= 2;
                }
                value += power;
                //logger.LogDebug($"Stat bonus: {statBonus.power} => {power} = {value}");
                return Mathf.Max(1, value);
            }

            static List<string> GetUpdatedDiceDescriptions(BattleDiceCardModel cardModel)
            {
                logger.LogDebug($"GetUpdatedDiceDescriptions card: {cardModel.GetName()}");

                var unitData = cardModel.owner;
                var battleCard = unitData?.cardSlotDetail?.cardAry?.Find((x) => x?.card != null && x.card == cardModel);

                bool canCalculateValues = battleCard != null && !ShowOriginalDiceValuesShortcut.Value.IsPressed();

                if (canCalculateValues)
                {
                    var key = GenerateKey(battleCard);

                    if (cachedDiceRolls.ContainsKey(key))
                    {
                        logger.LogDebug($"GetUpdatedDiceDescriptions returning cached value for key {key}");
                        return new List<string>(cachedDiceRolls[key]);
                    }

                    var cardCopy = Utils.Clone(battleCard);
                    if (ShowTrueDiceValuesExtreme.Value)
                    {
                        cardCopy.OnUseCard_before();
                        cardCopy.OnUseCard();
                        cardCopy.card.OnUseCard(cardCopy.owner, cardCopy);
                        if (!cardCopy.isKeepedCard)
                        {
                            cardCopy.owner.passiveDetail.OnUseCard(cardCopy);
                            cardCopy.owner.emotionDetail.OnUseCard(cardCopy);
                            cardCopy.owner.bufListDetail.OnUseCard(cardCopy);
                            cardCopy.owner.allyCardDetail.OnUseCard(cardCopy);
                            logger.LogDebug($"{cardCopy.target.UnitData.unitData.name} {cardCopy.target.speedDiceResult.Count}, {cardCopy.targetSlotOrder} {cardCopy.target.speedDiceResult[cardCopy.targetSlotOrder].value} < {cardCopy.speedDiceResultValue}");
                        }
                        cardCopy.owner.OnStartCardAction(cardCopy);
                        logger.LogDebug($"OnUseCard simulated");
                    }

                    var diceDescriptions = new List<string>();
                    var behaviourList = cardModel.GetBehaviourList();

                    var targetCard = Utils.Clone(battleCard.target.cardSlotDetail.cardAry[battleCard.targetSlotOrder]);

                    var oldBufList = cardCopy.owner.bufListDetail;
                    cardCopy.owner.bufListDetail = unitData.bufListDetail;
                    int i = 0;
                    cardCopy.NextDice();
                    while (cardCopy.currentBehavior != null)
                    {
                        var behaviour = behaviourList[i];
                        string desc = $"{behaviour.GetMinText()}-{behaviour.GetMaxText()}";

                        var diceBehaviour = cardCopy.currentBehavior;
                        diceBehaviour.card = cardCopy;
                        diceBehaviour.BeforeRollDice(targetCard?.currentBehavior);
                        int adjustedMin = GetFinalDiceValue(diceBehaviour, diceBehaviour.GetDiceMin());
                        int adjustedMax = GetFinalDiceValue(diceBehaviour, diceBehaviour.GetDiceMax());

                        if (adjustedMin.ToString() != behaviour.GetMinText() || adjustedMax.ToString() != behaviour.GetMaxText())
                        {
                            desc = $"({adjustedMin}-{adjustedMax})";
                        }
                        diceDescriptions.Add(desc);

                        if (targetCard?.currentBehavior != null)
                        {
                            targetCard.NextDice();
                        }
                        cardCopy.NextDice();
                        ++i;
                    }
                    cardCopy.owner.bufListDetail = oldBufList;

                    cachedDiceRolls[key] = new List<string>(diceDescriptions);

                    return new List<string>(diceDescriptions);
                }

                return null;
            }

            static string GenerateKey(BattlePlayingCardDataInUnitModel card)
            {
                return $"{card.owner.id}-{card.GetHashCode()}-{card.target.id}-{card.GetHashCode()}";
            }

            [HarmonyPatch(typeof(StageController), "ArrangeCardsPhase")]
            class Patch_StageController_ArrangeCardsPhase
            {
                static void Postfix()
                {
                    cachedDiceRolls.Clear();
                }
            }

            [HarmonyPatch(typeof(BattleDiceCardUI), "SetCard")]
            class Patch_BattleDiceCardUI_SetCard
            {
                static void Postfix(BattleDiceCardUI __instance, BattleDiceCardModel cardModel)
                {
                    if (!ShowTrueDiceValues.Value)
                    {
                        return;
                    }
                    //logger.LogDebug($"BattleDiceCardUI.SetCard() postfix");
                    var diceDescriptions = GetUpdatedDiceDescriptions(cardModel);
                    if (diceDescriptions == null)
                    {
                        return;
                    }
                    for (int i = 0; i < diceDescriptions.Count; i++)
                    {
                        var behaviourDesc = __instance.ui_behaviourDescList[i];
                        if (behaviourDesc.txt_range != null && behaviourDesc.txt_range.text.Length > 0)
                        {
                            behaviourDesc.txt_range.text = diceDescriptions[i];
                        }
                    }
                }
            }
        }

        class Patches_UseCardHotkeys
        {
            [HarmonyPatch(typeof(BattleUnitTargetArrowManagerUI), "Update")]
            class Patch_BattleUnitTargetArrowManagerUI_Update
            {
                static bool Prefix()
                {
                    bool selectedCard = false;
                    if (UseCardHotkeys.Value && Singleton<StageController>.Instance.Phase == StageController.StagePhase.ApplyLibrarianCardPhase)
                    {
                        // these are numbers from 1-9
                        for (int i = 0; i < 10; i++)
                        {
                            if (Input.GetKeyUp((KeyCode)(49 + i)))
                            {
                                logger.LogDebug($"BattleUnitTargetArrowManagerUI.Update() prefix");
                                var cardsInHand = SingletonBehavior<BattleManagerUI>.Instance.ui_unitCardsInHand;
                                var unitData = cardsInHand.SelectedModel;
                                if (unitData == null)
                                {
                                    break;
                                }
                                var hand = cardsInHand.GetCardUIList();
                                if (i >= hand.Count)
                                {
                                    break;
                                }
                                var card = hand[i];
                                if (card == null || !card.IsActivatedObject())
                                {
                                    break;
                                }

                                card.ShowDetail();
                                card.OnPdSubmit(null);
                                if (card.GetClickableState() != BattleDiceCardUI.ClickableState.CanClick)
                                {
                                    card.HideDetail();
                                }
                                selectedCard = true;
                                break;
                            }
                        }
                    }
                    return !selectedCard;
                }
            }
        }

        class Patches_BookDropMultiplier
        {
            [HarmonyPatch(typeof(BattleUnitModel), "OnDie")]
            class Patch_BattleUnitModel_OnDie
            {
                static void Postfix(BattleUnitModel __instance, bool callEvent)
                {
                    logger.LogDebug($"BattleUnitModel.OnDie() postfix");

                    if (!callEvent)
                    {
                        return;
                    }
                    if (__instance.faction == Faction.Enemy)
                    {
                        int emotionLevel = __instance.emotionDetail.EmotionLevel;

                        // 1 because the base method already ran this logic
                        for (int repeats = 1; repeats < BookDropMultiplier.Value; ++repeats)
                        {
                            for (int i = emotionLevel; i >= 0; i--)
                            {
                                DropTable dropTable;
                                if (__instance.UnitData.unitData.DropTable.TryGetValue(i, out dropTable))
                                {
                                    using (List<DropBookDataForAddedReward>.Enumerator enumerator2 = dropTable.DropRemakeCompare(emotionLevel).GetEnumerator())
                                    {
                                        while (enumerator2.MoveNext())
                                        {
                                            DropBookDataForAddedReward dropBookDataForAddedReward = enumerator2.Current;
                                            Singleton<StageController>.Instance.OnEnemyDropBookForAdded(dropBookDataForAddedReward);
                                            __instance.view.OnEnemyDropBook(dropBookDataForAddedReward.GetLorId());
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        class Patches_CombatSpeedMultiplier
        {
            [HarmonyPatch(typeof(StageController), "ArrangeCardsPhase")]
            class Patch_StageController_ArrangeCardsPhase
            {
                static void Postfix()
                {
                    var timeManagerInfo = Traverse.Create(TimeManager.Instance);
                    var goalTimeScale = timeManagerInfo.Field("_goalTimeScale");
                    goalTimeScale.SetValue(CombatSpeedMultiplier.Value);
                    logger.LogDebug($"StageController.ArrangeCardsPhase() postfix");
                }
            }
        }

        //class Patches_WhiteNightMapManager
        //{
        //    static int tries = 0;
        //    static void DisableGameObject(MonoBehaviour mb)
        //    {
        //        // logger.LogDebug($"MonoBehaviour: {mb}");
        //        if (mb != null && mb.gameObject != null && mb.gameObject.activeSelf)
        //        {
        //            mb.gameObject.SetActive(false);
        //        }
        //    }

        //    [HarmonyPatch(typeof(WhiteNightMapManager), "InitializeMap")]
        //    class Patch_WhiteNightMapManager_InitializeMap
        //    {
        //        static bool Prefix(WhiteNightMapManager __instance, Color[] ____wingColors, ref float ____elapsedWingColorChange, ref int ____start, ref int ____end, SpriteRenderer ____whitenightWingSprite, GameObject ____whiteNightWingObj, WhiteNightObjectController ____whiteNightObj, Animator ____colorChangeAnimator, SpriteRenderer[] ____sideFloors, GameObject ____oneSinObject, ref int ____deadApostleCount, ref float ____elapsedEscortFilterOn, GameObject ____lightEffect, GameObject ____hokmaAuraPrefab, GameObject ____whiteNightEffectPrefab, GameObject ____lightEffectConfess1, GameObject ____lightEffectConfess2, GameObject ____mapFeatherEffect, GameObject ____hokmaAuraObj, GameObject ____confessDivergencePrefab, GameObject ____crossExplosionEffectPrefab, GameObject ____featherExplosionEffectPrefab, SpriteRenderer ____createEscortApostleFilter, AnimationCurve ____escortFilterCurve)
        //        {
        //            ____whiteNightWingObj.transform.position = ____whiteNightWingObj.transform.position + new Vector3(0, -20, 0);
        //            ____whiteNightObj.transform.position = ____whiteNightObj.transform.position + new Vector3(0, -20, 0);
        //            SingletonBehavior<BattleCamManager>.Instance.BlurBackgroundCam(enable: false);
        //            ____oneSinObject.SetActive(value: false);
        //            ____whiteNightWingObj.SetActive(value: true);
        //            ____whiteNightObj.gameObject.SetActive(value: false);
        //            ____elapsedWingColorChange = 0f;
        //            ____start = 0;
        //            ____end = 0;
        //            ____deadApostleCount = 0;
        //            ____elapsedEscortFilterOn = 0f;
        //            ____createEscortApostleFilter.gameObject.SetActive(false);
        //            ____createEscortApostleFilter.enabled = false;
        //            //____lightEffect.SetActive(false);
        //            //____hokmaAuraPrefab.SetActive(false);
        //            //____whiteNightEffectPrefab.SetActive(false);
        //            //____lightEffectConfess1.SetActive(false);
        //            //____lightEffectConfess2.SetActive(false);
        //            //____confessDivergencePrefab.SetActive(false);
        //            //____crossExplosionEffectPrefab.SetActive(false);
        //            //____featherExplosionEffectPrefab.SetActive(false);
        //            //____mapFeatherEffect.SetActive(false);
        //            //____hokmaAuraObj.SetActive(false);
        //            //____colorChangeAnimator.SetTrigger("ToWhite");
        //            //____whiteNightObj.gameObject.SetActive(false);
        //            //____whiteNightWingObj.gameObject.SetActive(false);
        //            //____whitenightWingSprite.enabled = false;

        //            tries = 0;

        //            return false;
        //        }
        //    }

        //    [HarmonyPatch(typeof(WhiteNightMapManager), "Update")]
        //    class Patch_WhiteNightMapManager_Update
        //    {
        //        static bool Prefix(WhiteNightMapManager __instance, Color[] ____wingColors, ref float ____elapsedWingColorChange, ref int ____start, ref int ____end, SpriteRenderer ____whitenightWingSprite, GameObject ____whiteNightWingObj, WhiteNightObjectController ____whiteNightObj, Animator ____colorChangeAnimator, SpriteRenderer[] ____sideFloors, GameObject ____oneSinObject, ref int ____deadApostleCount, ref float ____elapsedEscortFilterOn, GameObject ____lightEffect, GameObject ____hokmaAuraPrefab, GameObject ____whiteNightEffectPrefab, GameObject ____lightEffectConfess1, GameObject ____lightEffectConfess2, GameObject ____mapFeatherEffect, GameObject ____hokmaAuraObj, GameObject ____confessDivergencePrefab, GameObject ____crossExplosionEffectPrefab, GameObject ____featherExplosionEffectPrefab, SpriteRenderer ____createEscortApostleFilter, AnimationCurve ____escortFilterCurve)
        //        {
        //            __instance.ChangeWingColor(WhiteNightStart.Value, WhiteNightEnd.Value);
        //            if (tries > 10)
        //            {
        //                return true;
        //            }
        //            tries++;

        //            ____whitenightWingSprite.color = Color.white;
        //            ____colorChangeAnimator.SetTrigger("ToWhite");
        //            for (int i = 0; i < ____sideFloors.Length; i++)
        //            {
        //                ____sideFloors[i].color = Color.white;
        //            }

        //            DisableGameObject(SingletonBehavior<BattleManagerUI>.Instance);
        //            DisableGameObject(SingletonBehavior<BattleEmotionCoinUI>.Instance);
        //            DisableGameObject(SingletonBehavior<BattleEmotionBarPortraitUI>.Instance);
        //            DisableGameObject(SingletonBehavior<BattleEmotionBarTeamSlotUI>.Instance);
        //            DisableGameObject(SingletonBehavior<BattleCharacterProfileEmotionUI>.Instance);
        //            DisableGameObject(SingletonBehavior<BattleUnitDiceActionUI>.Instance);
        //            DisableGameObject(SingletonBehavior<BattleDiceCardUI>.Instance);
        //            if (SingletonBehavior<UICharacterRenderer>.Instance != null)
        //            {
        //                SingletonBehavior<UICharacterRenderer>.Instance.DestroyCharacters();
        //            }
        //            DisableGameObject(SingletonBehavior<UICharacterRenderer>.Instance);

        //            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetList())
        //            {
        //                unit.view.StartDeadEffect();
        //                DisableGameObject(unit.view.costUI);
        //                unit.view.speedDiceSetterUI.SetDisable();
        //            }

        //            return true;

        //            logger.LogDebug($"WhiteNight.Update(). start: {____start}, end: {____end}, wingColors: {____wingColors}, elapsedWingColorChange: {____elapsedWingColorChange}");
        //            if (true || ____start != ____end)
        //            {
        //                // ____end = (____end + 1) % 3;
        //                Color a = ____wingColors[____start];
        //                Color b = ____wingColors[____end];
        //                ____elapsedWingColorChange += Time.deltaTime;
        //                ____whitenightWingSprite.color = Color.Lerp(a, b, ____elapsedWingColorChange);
        //                if (____elapsedWingColorChange >= 1f)
        //                {
        //                    ____elapsedWingColorChange = 0f;
        //                    ____start = ____end;
        //                    if (____end == 3)
        //                    {
        //                        ____whiteNightWingObj.SetActive(value: false);
        //                        ____whiteNightObj.gameObject.SetActive(value: true);
        //                        ____colorChangeAnimator.SetTrigger("ToWhite");
        //                        SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Creature/WhiteNight____Appear");
        //                        for (int i = 0; i < ____sideFloors.Length; i++)
        //                        {
        //                            ____sideFloors[i].color = Color.white;
        //                        }
        //                    }
        //                    else if (____end == 2)
        //                    {
        //                        ____colorChangeAnimator.SetTrigger("ToGrey");
        //                        for (int j = 0; j < ____sideFloors.Length; j++)
        //                        {
        //                            ____sideFloors[j].color = Color.grey;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        class Patches_EnemyTeamStageManager_HokmaFinal
        {
            [HarmonyPatch(typeof(EnemyTeamStageManager_HokmaFinal), "OnWaveStart")]
            class Patch_EnemyTeamStageManager_HokmaFinal_OnWaveStart
            {
                static bool Prefix(ref EnemyTeamStageManager_HokmaFinal.Phase ____curPhase)
                {
                    return true;
                    ____curPhase = EnemyTeamStageManager_HokmaFinal.Phase.THREE;
                    logger.LogDebug($"Current phase: {____curPhase}");
                    return false;
                }
            }
        }

        class Patches_Blacklist
        {
            //[HarmonyPatch(typeof(UIInvenCardSlot), "OnPointerClick")]
            //class Patch_UIInvenCardListScroll_Initialized
            //{
            //    static bool Prefix(UIInvenCardSlot __instance, BaseEventData eventData)
            //    {
            //        logger.LogDebug($"UIInvenCardListScroll.OnPointerClick() postfix. slot: {__instance} {__instance.name} {__instance.CardModel.GetName()}");
            //        if (UIControlManager.GetInpuTypeOf(eventData) != InputType.WheelClick)
            //        {
            //            return true;
            //        }
            //        var card = __instance.CardModel;
            //        var cardId = card.GetID().id;
            //        if (!blacklistedCards.Contains(cardId))
            //        {
            //            logger.LogDebug($"Blacklisted {cardId} {card.GetID()} {card.GetName()}");
            //            blacklistedCards.Add(cardId);
            //        }
            //        return false;
            //    }
            //}

            //[HarmonyPatch(typeof(UICostFilterPanel), "Init")]
            //class Patch_UICostFilterPanel_Init
            //{
            //    static void Postfix(UICostFilterPanel __instance, List<UICostFilterSlot> ___costSlots, UICustomSelectable ___parentSelectablePanel)
            //    {
            //        logger.LogDebug($"UICostFilterPanel.Init() postfix. # of cost slots: {___costSlots.Count}");

            //        int xOffset = 10;

            //        UICostFilterSlot originalCostSlot = null;
            //        foreach (var costSlot in ___costSlots)
            //        {
            //            var rect = Traverse.Create(costSlot).Field("rect").GetValue() as RectTransform;
            //            originalCostSlot = costSlot;
            //            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + xOffset, rect.anchoredPosition.y);
            //            rect.localPosition = new Vector3(rect.localPosition.x + xOffset, rect.localPosition.y, rect.localPosition.z);
            //            rect.position = new Vector3(rect.position.x + xOffset, rect.position.y, rect.position.z);
            //            Traverse.Create(costSlot).Field("rect").SetValue(rect);
            //            logger.LogDebug($"Cost slot cost: {costSlot.cost}, enabled: {costSlot.IsOn}, rect: {rect} {rect.GetInstanceID()}, parent: {rect.parent} {rect.parent.position}, position: {rect.position}, localPosition: {rect.localPosition}, anchoredPosition: {rect.anchoredPosition}, scale: {rect.localScale}");
            //        }

            //        var newCostSlot = Instantiate(originalCostSlot);
            //        var costSlotInfo = Traverse.Create(newCostSlot);
            //        var newRect = costSlotInfo.Field("rect").GetValue() as RectTransform;
            //        var newSelectableToggle = costSlotInfo.Field("SelectableToggle").GetValue() as UICustomSelectableToggle;
            //        logger.LogDebug($"New cost slot cost: {newCostSlot.cost}, on: {newCostSlot.IsOn}, rect: {newRect}, selectable toggle: {newSelectableToggle} {newSelectableToggle.GetInstanceID()}");
            //        newCostSlot.Init();
            //        newCostSlot.SetParentSelectable(___parentSelectablePanel);
            //        newRect = costSlotInfo.Field("rect").GetValue() as RectTransform;
            //        logger.LogDebug($"New cost slot cost: {newCostSlot.cost}, on: {newCostSlot.IsOn}, rect: {newRect} {newRect.GetInstanceID()} {newRect.position} {newRect.localPosition} {newRect.anchoredPosition} {newRect.localScale}, selectable toggle: {newSelectableToggle} {newSelectableToggle.GetInstanceID()}");
            //        newCostSlot.gameObject.SetActive(true);
            //    }
            //}

            //[HarmonyPatch(typeof(UICostFilterPanel), "CheckCostFilter")]
            //class Patch_UICostFilterPanel_CheckCostFilter
            //{
            //    static void Postfix(UICostFilterPanel __instance, List<UICostFilterSlot> ___costSlots)
            //    {
            //        logger.LogDebug($"UICostFilterPanel.CheckCostFilter() postfix. # of cost slots: {___costSlots.Count}");
            //        foreach (var costSlot in ___costSlots)
            //        {
            //            var rect = Traverse.Create(costSlot).Field("rect").GetValue() as RectTransform;
            //            logger.LogDebug($"Cost slot cost: {costSlot.cost}, enabled: {costSlot.IsOn}, rect: {rect}, position: {rect.position}, scale: {rect.localScale}");
            //        }
            //    }
            //}
        }
    }
}
