using BattleTech.UI;
using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BattleTech.UI.MSWinMoveWindowUtil;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CustomUnits;
using BattleTech.Save.SaveGameStructure;
using HBS;
using System.Threading;
using TMPro;
using HBS.Collections;
using UnityEngine.Events;
using System.Reflection;
using BattleTech.Data;
using CustAmmoCategories;
using Newtonsoft.Json;
using System.Collections;

namespace StrategicOperations.Framework
{
    public class LanceLoadoutSlotCargoPreview : MonoBehaviour
    {
        public static readonly int MAX_TRANSPORT_SUBSLOTS_COUNT = 5;
        public static readonly float TRANSPORT_SUBSLOTS_SCALE = 0.6f;
        public List<LanceLoadoutSlot> cargoSlots = new List<LanceLoadoutSlot>();
        public List<RectTransform> cargoSlotsRects = new List<RectTransform>();
        public LanceLoadoutSlot parent;
        public RectTransform cargoRectTransform;
        public float activeSize = float.NaN;
        public void SetSlotsCount(int count, bool have_external)
        {
            if (have_external) { ++count; }
            for (int t = 0; t < cargoSlots.Count; ++t)
            {
                cargoSlots[t].SetData(null, null, parent.dataManager, false, false);
                cargoSlots[t].gameObject.SetActive(t < count);
            }
        }
        public void Update()
        {
            float slotsSize = 0f;
            if (cargoRectTransform == null) { return; }
            if (parent == null) { return; }
            ContentSizeFitter fitter = parent.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize) { fitter.verticalFit = ContentSizeFitter.FitMode.MinSize; }
            foreach (var slotrect in cargoSlotsRects)
            {
                if (slotrect.gameObject.activeSelf)
                {
                    slotsSize += slotrect.sizeDelta.y;
                }
            }
            if (activeSize != slotsSize)
            {
                activeSize = slotsSize;
                cargoRectTransform.sizeDelta = new Vector2(cargoRectTransform.sizeDelta.x, slotsSize * TRANSPORT_SUBSLOTS_SCALE);
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel), "SetData")]
    [HarmonyAfter("io.mission.customunits")]
    public static class LancePreviewPanel_SetData
    {
        static void Prefix(LancePreviewPanel __instance, ref int maxUnits)
        {
            try
            {
                for (int t = 0; t < __instance.loadoutSlots.Length; ++t)
                {
                    var srcSlot = __instance.loadoutSlots[t].gameObject;
                    LanceLoadoutSlotCargoPreview cargoInfo = srcSlot.GetComponent<LanceLoadoutSlotCargoPreview>();
                    if (cargoInfo != null) { continue; }
                    List<LanceLoadoutSlot> cargoSlots = new List<LanceLoadoutSlot>();
                    List<RectTransform> cargoSlotsRects = new List<RectTransform>();
                    for (int tt = 0; tt < LanceLoadoutSlotCargoPreview.MAX_TRANSPORT_SUBSLOTS_COUNT; ++tt)
                    {
                        GameObject newSlot = GameObject.Instantiate(srcSlot);
                        newSlot.name = $"sub_lanceSlot1 ({t + 1}-{tt + 1})";
                        cargoSlots.Add(newSlot.GetComponent<LanceLoadoutSlot>());
                        cargoSlotsRects.Add(newSlot.GetComponent<RectTransform>());
                        newSlot.SetActive(false);
                    }
                    //RectTransform rect = srcSlot.GetComponent<RectTransform>();
                    GameObject subsSlotsContainer = GameObject.Instantiate(srcSlot, srcSlot.transform);
                    subsSlotsContainer.name = "subslots";
                    GameObject.DestroyImmediate(subsSlotsContainer.GetComponent<LanceLoadoutSlot>());
                    while (subsSlotsContainer.transform.childCount != 0)
                    {
                        GameObject.DestroyImmediate(subsSlotsContainer.transform.GetChild(0).gameObject);
                    }
                    subsSlotsContainer.transform.localScale = new Vector3(LanceLoadoutSlotCargoPreview.TRANSPORT_SUBSLOTS_SCALE, LanceLoadoutSlotCargoPreview.TRANSPORT_SUBSLOTS_SCALE, 1.0f);
                    RectTransform rect = subsSlotsContainer.GetComponent<RectTransform>();
                    rect.pivot = new Vector2(1f, 1f);
                    //var subsSlotsContainer_background = subsSlotsContainer.AddComponent<Image>();
                    ContentSizeFitter contentSizeFitter = srcSlot.AddComponent<ContentSizeFitter>();
                    contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                    foreach (var subslot in cargoSlots)
                    {
                        subslot.gameObject.transform.SetParent(subsSlotsContainer.transform);
                        subslot.gameObject.transform.localScale = Vector3.one;
                    }
                    cargoInfo = srcSlot.AddComponent<LanceLoadoutSlotCargoPreview>();
                    cargoInfo.parent = __instance.loadoutSlots[t];
                    cargoInfo.cargoSlots.AddRange(cargoSlots);
                    cargoInfo.cargoSlotsRects.AddRange(cargoSlotsRects);
                    cargoInfo.cargoRectTransform = subsSlotsContainer.GetComponent<Image>().rectTransform;
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    public class LanceLoadoutSlotCargo : MonoBehaviour
    {
        public LanceLoadoutSlotCargoConfig parent;
    }
    public class LanceLoadoutSlotCargoConfig : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static readonly int MAX_TRANSPORT_SUBSLOTS_COUNT = 5;
        public static readonly float TRANSPORT_SUBSLOTS_SCALE = 0.6f;
        public List<LanceLoadoutSlot> cargoSlots = new List<LanceLoadoutSlot>();
        public List<RectTransform> cargoSlotsRects = new List<RectTransform>();
        public List<float> cargoTargetHeights = new List<float>();
        public LanceLoadoutSlot parent;
        public float activeSize = float.NaN;
        public bool selfHover = false;
        public float T = 0f;
        public enum State
        {
            StateMovingUp, StateMovigDown, StateUp, StateDown
        }
        public State state = State.StateDown;
        public bool isHover
        {
            get
            {
                if (selfHover) { return true; }
                return false;
            }
        }
        public void ClearCargo()
        {
            foreach (var slot in cargoSlots)
            {
                slot.OnClearSlotsClicked();
                slot.gameObject.SetActive(false);
            }
        }
        public void SetSlotsCount(int count, bool have_external)
        {
            if (have_external) { ++count; }
            for (int t = 0; t < cargoSlots.Count; ++t)
            {
                if (t >= count) { cargoSlots[t].OnClearSlotsClicked(); }
                cargoSlots[t].gameObject.SetActive(t < count);
                //cargoSlots[t].gameObject.GetComponent<LanceLoadoutSlotCargo>().isExternalMount = have_external && (t == (count - 1));
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            for (int t = 0; t < cargoSlotsRects.Count; ++t)
            {
                cargoSlotsRects[t].gameObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
            }
            selfHover = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            selfHover = false;
        }
        public void UpdatePositions()
        {
            for (int t = 0; t < cargoSlotsRects.Count; ++t)
            {
                cargoSlotsRects[t].anchoredPosition = new Vector2(0f, cargoTargetHeights[t] * T);
            }
        }
        public void Update()
        {
            if (isHover)
            {
                if (state == State.StateDown) { state = State.StateMovingUp; }
                if (state == State.StateMovigDown) { state = State.StateMovingUp; }
            }
            else
            {
                if (state == State.StateUp) { state = State.StateMovigDown; }
                if (state == State.StateMovingUp) { state = State.StateMovigDown; }
            }
            if (state == State.StateMovingUp)
            {
                if (T >= 1.0f)
                {
                    T = 1.0f;
                    this.UpdatePositions();
                    state = State.StateUp;
                }
                else
                {
                    this.UpdatePositions();
                    T += (Time.deltaTime * 2.0f);
                }
            }
            if (state == State.StateMovigDown)
            {
                if (T <= 0.0f)
                {
                    T = 0.0f;
                    this.UpdatePositions();
                    state = State.StateDown;
                }
                else
                {
                    this.UpdatePositions();
                    T -= (Time.deltaTime * 2.0f);
                }
            }
        }
    }

    public class LanceLoadoutSlotSourceHolder : MonoBehaviour
    {
        public LanceLoadoutSlot source_LanceLoadoutSlot;
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("SetData")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyBefore("io.mission.customunits")]
    public static class LanceConfiguratorPanel_SetData_Before
    {
        public static void Prefix(LanceConfiguratorPanel __instance, SimGameState sim, ref int maxUnits, Contract contract, List<MechDef> mechs, LanceConfiguration overrideLance)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                LanceLoadoutSlotSourceHolder holder = __instance.gameObject.GetComponent<LanceLoadoutSlotSourceHolder>();
                if (holder != null) { return; }
                holder = __instance.gameObject.AddComponent<LanceLoadoutSlotSourceHolder>();
                GameObject newSlot = GameObject.Instantiate(__instance.loadoutSlots[0].gameObject);
                holder.source_LanceLoadoutSlot = newSlot.GetComponent<LanceLoadoutSlot>();
                newSlot.transform.SetParent(__instance.gameObject.transform);
                newSlot.SetActive(false);
                newSlot.transform.localScale = Vector3.one;
                newSlot.transform.localPosition = Vector3.zero;
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("SetData")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyAfter("io.mission.customunits")]
    public static class LanceConfiguratorPanel_SetData_After
    {
        public static void Prefix(LanceConfiguratorPanel __instance, SimGameState sim, ref int maxUnits, Contract contract, List<MechDef> mechs, LanceConfiguration overrideLance)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                var srcSlot = __instance.loadoutSlots[0].gameObject;
                var holder = __instance.GetComponent<LanceLoadoutSlotSourceHolder>();
                if (holder != null) { srcSlot = holder.source_LanceLoadoutSlot.gameObject; }

                for (int t = 0; t < __instance.loadoutSlots.Length; t++)
                {
                    var parentSlot = __instance.loadoutSlots[t].gameObject;
                    LanceLoadoutSlotCargoConfig cargoInfo = parentSlot.GetComponent<LanceLoadoutSlotCargoConfig>();
                    if (cargoInfo != null)
                    {
                        foreach (var cargoSlot in cargoInfo.cargoSlots)
                        {
                            cargoSlot.SetData(__instance, sim, __instance.dataManager, true, false);
                        }
                        continue;
                    }
                    List<LanceLoadoutSlot> cargoSlots = new List<LanceLoadoutSlot>();
                    List<RectTransform> cargoSlotsRects = new List<RectTransform>();
                    List<float> cargoTargetHeights = new List<float>();
                    float heightCounter = 30f;
                    for (int tt = 0; tt < LanceLoadoutSlotCargoPreview.MAX_TRANSPORT_SUBSLOTS_COUNT; ++tt)
                    {
                        GameObject newSlot = GameObject.Instantiate(srcSlot);
                        newSlot.name = $"sub_lanceSlot1 ({t + 1}-{tt + 1})";
                        cargoSlots.Add(newSlot.GetComponent<LanceLoadoutSlot>());
                        RectTransform slotRect = newSlot.GetComponent<RectTransform>();
                        heightCounter += (slotRect.sizeDelta.y);
                        cargoSlotsRects.Add(slotRect);
                        cargoTargetHeights.Add(heightCounter);
                        newSlot.SetActive(false);
                    }
                    GameObject subsSlotsContainer = new GameObject("subslots");
                    subsSlotsContainer.name = "subslots";
                    subsSlotsContainer.transform.SetParent(parentSlot.transform);
                    subsSlotsContainer.transform.SetSiblingIndex(0);
                    subsSlotsContainer.transform.localPosition = new Vector3(90.0f, 0f, 0f);
                    subsSlotsContainer.transform.localScale = new Vector3(0.6f, 0.6f, 1.0f);
                    foreach (var subslot in cargoSlots)
                    {
                        subslot.gameObject.transform.SetParent(subsSlotsContainer.transform);
                        subslot.gameObject.transform.localScale = Vector3.one;
                        subslot.gameObject.transform.localPosition = Vector3.zero;
                        subslot.SetData(__instance, sim, __instance.dataManager, true, false);
                        Image img = subslot.gameObject.GetComponent<Image>();
                        img.color = new Color(0f, 0f, 0f, 1f);
                    }
                    cargoInfo = parentSlot.AddComponent<LanceLoadoutSlotCargoConfig>();
                    cargoInfo.parent = __instance.loadoutSlots[t];
                    cargoInfo.cargoSlots.AddRange(cargoSlots);
                    cargoInfo.cargoSlots.Reverse();
                    cargoInfo.cargoSlotsRects.AddRange(cargoSlotsRects);
                    cargoInfo.cargoSlotsRects.Reverse();
                    cargoInfo.cargoTargetHeights.AddRange(cargoTargetHeights);
                    for (int tt = 0; tt < cargoInfo.cargoSlots.Count; ++tt)
                    {
                        LanceLoadoutSlotCargo cargo = cargoInfo.cargoSlots[tt].gameObject.AddComponent<LanceLoadoutSlotCargo>();
                        cargo.parent = cargoInfo;
                        //cargo.isExternalMount = (tt == 0);
                    }
                    //cargoInfo.cargoTargetHeights.Reverse();
                    //cargoInfo.cargoRectTransform = subsSlotsContainer.GetComponent<Image>().rectTransform;
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LanceLoadoutSlot))]
    [HarmonyPatch("OnAddItem")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyBefore("io.mission.customunits")]
    [HarmonyPatch(new Type[] { typeof(IMechLabDraggableItem), typeof(bool) })]
    public static class LanceLoadoutSlot_OnAddItem
    {
        public static void Prefix(ref bool __runOriginal, LanceLoadoutSlot __instance, IMechLabDraggableItem item, bool validate, bool __result)
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            if (item.ItemType != MechLabDraggableItemType.Mech) { return; }
            LanceLoadoutSlotCargo cargoSlot = __instance.gameObject.GetComponent<LanceLoadoutSlotCargo>();
            if (cargoSlot == null) { return; }
            LanceLoadoutMechItem lanceLoadoutMechItem = item as LanceLoadoutMechItem;
            var unitCustomInfo = lanceLoadoutMechItem.MechDef.GetCustomInfo();
            if (unitCustomInfo is { SquadInfo.Troopers: <= 1 })
            {
                if (__instance.LC != null) { __instance.LC.ReturnItem(item); }
                __result = false;
                __runOriginal = false;
                GenericPopupBuilder.Create("CAN'T COMPLY", "Only squads allowed").AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
                return;
            }
            else
            {
                LanceLoadoutSlotCargoConfig cargoInfo = cargoSlot.parent;
                if (cargoInfo.parent.SelectedMech == null) {
                    if (__instance.LC != null) { __instance.LC.ReturnItem(item); }
                    __result = false;
                    __runOriginal = false;
                    return;
                }
                int mounted = 0;
                foreach (var cargo in cargoInfo.cargoSlots) { if (cargo.SelectedMech != null) { ++mounted; } };
                int mountCap = cargoInfo.parent.SelectedMech.MechDef.CargoCapacity(__instance.LC != null ? __instance.LC.activeContract: null);
                if (mounted < mountCap) { return; }
                if (mounted > mountCap) {
                    if (__instance.LC != null) { __instance.LC.ReturnItem(item); }
                    __result = false;
                    __runOriginal = false;
                    return;
                }
                if (cargoInfo.parent.SelectedMech.MechDef.CanMountBAExternally(__instance.LC != null ? __instance.LC.activeContract : null) == false)
                {
                    if (__instance.LC != null) { __instance.LC.ReturnItem(item); }
                    __result = false;
                    __runOriginal = false;
                    GenericPopupBuilder.Create("CAN'T COMPLY", $"This unit can be used as carrier").AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
                    return;
                }
                if (lanceLoadoutMechItem.MechDef.isBattleArmorInternalMountsOnly(__instance.LC != null ? __instance.LC.activeContract : null))
                {
                    if (__instance.LC != null) { __instance.LC.ReturnItem(item); }
                    __result = false;
                    __runOriginal = false;
                    GenericPopupBuilder.Create("CAN'T COMPLY", $"This unit can be carried only internally").AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
                    return;
                }
                bool BA_CanMountBADef = lanceLoadoutMechItem.MechDef.CanMountBADef();
                bool Carrier_HasBattleArmorMounts = cargoInfo.parent.SelectedMech.MechDef.HasBattleArmorMounts(__instance.LC != null ? __instance.LC.activeContract : null);
                if ((Carrier_HasBattleArmorMounts == false)&&(BA_CanMountBADef == false))
                {                    
                    if (__instance.LC != null) { __instance.LC.ReturnItem(item); } 
                    __result = false;
                    __runOriginal = false;
                    GenericPopupBuilder.Create("CAN'T COMPLY", $"Carrier does not have battle armor mounts").AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
                    return;
                }
            }
        }
        public static void Postfix(LanceLoadoutSlot __instance, IMechLabDraggableItem item, bool validate, bool __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                if (item.ItemType != MechLabDraggableItemType.Mech) { return; }
                if (__result == false) { return; }
                LanceLoadoutSlotCargoConfig cargoInfo = __instance.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                if (cargoInfo != null)
                {
                    LanceLoadoutMechItem lanceLoadoutMechItem = item as LanceLoadoutMechItem;
                    ModInit.modLog?.Info?.Write($"LanceLoadoutSlot.OnAddItem {lanceLoadoutMechItem.MechDef.ChassisID} cargoCap:{lanceLoadoutMechItem.MechDef.CargoCapacity(__instance.LC != null ? __instance.LC.activeContract : null)} CanMountBAExternally:{lanceLoadoutMechItem.MechDef.CanMountBAExternally(__instance.LC != null ? __instance.LC.activeContract : null)}");
                    cargoInfo.SetSlotsCount(lanceLoadoutMechItem.MechDef.CargoCapacity(__instance.LC != null ? __instance.LC.activeContract : null), lanceLoadoutMechItem.MechDef.CanMountBAExternally(__instance.LC != null ? __instance.LC.activeContract : null));
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
                return;
            }
        }
    }
    [HarmonyPatch(typeof(LanceLoadoutSlot))]
    [HarmonyPatch("OnRemoveItem")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyBefore("io.mission.customunits")]
    [HarmonyPatch(new Type[] { typeof(IMechLabDraggableItem), typeof(bool) })]
    public static class LanceLoadoutSlot_OnRemoveItem
    {
        public static void Postfix(LanceLoadoutSlot __instance, IMechLabDraggableItem item, bool validate, bool __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                if (item.ItemType != MechLabDraggableItemType.Mech) { return; }
                LanceLoadoutSlotCargoConfig cargoInfo = __instance.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                if (cargoInfo != null)
                {
                    cargoInfo.ClearCargo();
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
                return;
            }
        }
    }
    [HarmonyPatch(typeof(LanceLoadoutSlot))]
    [HarmonyPatch("OnClearSlotsClicked")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyBefore("io.mission.customunits")]
    [HarmonyPatch(new Type[] { })]
    public static class LanceLoadoutSlot_OnClearSlotsClicked
    {
        public static void Postfix(LanceLoadoutSlot __instance)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                LanceLoadoutSlotCargoConfig cargoInfo = __instance.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                if (cargoInfo != null)
                {
                    cargoInfo.ClearCargo();
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
                return;
            }
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("ValidateLance")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { })]
    public static class LanceConfiguratorPanel_ValidateLance
    {
        public static void Postfix(LanceConfiguratorPanel __instance, bool __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                if (__result == false) { return; }
                int errorCount = 0;
                List<MechDef> mechs = new List<MechDef>();
                ModInit.modLog?.Info?.Write("LanceConfiguratorPanel.ValidateLance");
                foreach (var slot in __instance.loadoutSlots)
                {
                    if (slot.SelectedMech == null) { continue; }
                    mechs.Add(slot.SelectedMech.MechDef);
                    int cargoSlots = slot.SelectedMech.MechDef.GetTotalBASpaceMechDef();
                    ModInit.modLog?.Info?.Write($" {slot.SelectedMech.MechDef.ChassisID} cargo:{cargoSlots}");
                    LanceLoadoutSlotCargoConfig cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                    ModInit.modLog?.Info?.Write($" {slot.SelectedMech.MechDef.ChassisID} cargo:{cargoSlots} cargoInfoWidget:{(cargoInfo == null ? "null" : "not null")}");
                    if (cargoInfo == null) { continue; }
                    if (cargoSlots == 0) { continue; }
                    for (int t = 0; t < cargoSlots; ++t)
                    {
                        var cargoSlot = cargoInfo.cargoSlots[t];
                        if (cargoSlot.SelectedMech != null)
                        {
                            if (cargoSlot.SelectedPilot == null) { errorCount += 1; }
                            __instance.currentLanceValue += cargoSlot.SelectedMech.MechDef.Description.Cost;
                            mechs.Add(cargoSlot.SelectedMech.MechDef);
                            ModInit.modLog?.Info?.Write($"  cargoSlot:{cargoSlot.SelectedMech.MechDef.ChassisID}");
                        }
                        else
                        {
                            ModInit.modLog?.Info?.Write($"  cargoSlot:empty");
                            if (cargoSlot.SelectedPilot != null) { errorCount += 1; }
                        }
                    }
                }
                if (__instance.maxLanceValue >= 0)
                {
                    if (__instance.currentLanceValue > __instance.maxLanceValue)
                    {
                        __instance.lanceValid = false;
                        __instance.lanceErrorText.Append("Lance budget exceeds limit\n");
                        __result = false;
                    }
                }
                if (errorCount != 0)
                {
                    __instance.lanceValid = false;
                    __instance.lanceErrorText.Append("Lance slots require both a 'Unit and Pilot\n");
                    __result = false;
                }
                __instance.headerWidget.RefreshLanceInfo(__instance.lanceValid, __instance.lanceErrorText, mechs);
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
                return;
            }
        }
    }
    [HarmonyPatch(typeof(LanceHeaderWidget))]
    [HarmonyPatch("RefreshLanceInfo")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { typeof(bool), typeof(Localize.Text), typeof(List<MechDef>) })]
    public static class LanceHeaderWidget_RefreshLanceInfo
    {
        public static void Prefix(LanceHeaderWidget __instance, bool lanceValid, Localize.Text errorText, ref List<MechDef> mechs)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                LanceConfiguratorPanel panel = __instance.gameObject.GetComponentInParent<LanceConfiguratorPanel>();
                if (panel == null) {
                    ModInit.modLog?.Info?.Write("LanceHeaderWidget.RefreshLanceInfo does not have LanceConfiguratorPanel parent");
                    return; 
                }
                ModInit.modLog?.Info?.Write("LanceHeaderWidget.RefreshLanceInfo");
                for (int index = 0; index < panel.loadoutSlots.Length; ++index)
                {
                    LanceLoadoutSlot loadoutSlot = panel.loadoutSlots[index];
                    if (loadoutSlot.SelectedMech == null) { continue; }
                    LanceLoadoutSlotCargoConfig cargoInfo = loadoutSlot.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                    if (cargoInfo == null) { continue; }
                    int mountCounter = 0;
                    int cargoSpace = loadoutSlot.SelectedMech.MechDef.CargoCapacity(__instance.activeContract);
                    for (int t = 0; t < cargoInfo.cargoSlots.Count; ++t)
                    {
                        var cargoSlot = cargoInfo.cargoSlots[t];
                        if (cargoSlot.gameObject.activeSelf == false) { continue; }
                        if (cargoSlot.SelectedMech != null)
                        {
                            ++mountCounter;
                            if (mountCounter > cargoSpace)
                            {
                                if (ModInit.modSettings.ExternalBAAffectsOverallDropTonnage == false) { continue; }
                                ModInit.modLog?.Error?.Write($" external:{cargoSlot.SelectedMech.MechDef.ChassisID}");
                            }
                            else
                            {
                                if (ModInit.modSettings.InternalBAAffectsOverallDropTonnage == false) { continue; }
                                ModInit.modLog?.Error?.Write($" internal:{cargoSlot.SelectedMech.MechDef.ChassisID}");
                            }
                            mechs.Add(cargoSlot.SelectedMech.MechDef);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
                return;
            }
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("ValidateLanceTonnage")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { })]
    public static class LanceConfiguratorPanel_ValidateLanceTonnage
    {
        public static bool TonnageWithinRange(float tonnage, float minTonnage, float maxTonnage)
        {
            if (minTonnage < 0.0f && maxTonnage < 0.0f)
                return true;
            if (minTonnage >= 0.0f && maxTonnage >= 0.0f)
                return tonnage >= minTonnage && tonnage <= maxTonnage;
            if (minTonnage >= 0.0f)
                return tonnage >= minTonnage;
            return maxTonnage < 0.0f || tonnage <= maxTonnage;
        }
        public static void Postfix(LanceConfiguratorPanel __instance, bool __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                if (__result == false) { return; }
                List<MechDef> mechs = new List<MechDef>();
                ModInit.modLog?.Error?.Write("LanceConfiguratorPanel.ValidateLanceTonnage");
                for (int index = 0; index < __instance.loadoutSlots.Length; ++index)
                {
                    LanceLoadoutSlot loadoutSlot = __instance.loadoutSlots[index];
                    if (loadoutSlot.SelectedMech == null) { continue; }
                    mechs.Add(loadoutSlot.SelectedMech.MechDef);
                    //int cargoSlots = loadoutSlot.SelectedMech.MechDef.GetTotalBASpaceMechDef();
                    //if (cargoSlots == 0) { continue; }
                    LanceLoadoutSlotCargoConfig cargoInfo = loadoutSlot.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                    if (cargoInfo == null) { continue; }
                    float slotTonnage = loadoutSlot.SelectedMech.MechDef.Chassis.Tonnage;
                    int mountCounter = 0;
                    int cargoSpace = loadoutSlot.SelectedMech.MechDef.CargoCapacity(__instance.activeContract);
                    ModInit.modLog?.Error?.Write($" {loadoutSlot.SelectedMech.MechDef.ChassisID} curSlotTonnage:{slotTonnage}");
                    for (int t = 0; t < cargoInfo.cargoSlots.Count; ++t)
                    {
                        var cargoSlot = cargoInfo.cargoSlots[t];
                        if (cargoSlot.gameObject.activeSelf == false) { continue; }
                        if (cargoSlot.SelectedMech != null)
                        {
                            ++mountCounter;
                            if (mountCounter > cargoSpace) {
                                if (ModInit.modSettings.ExternalBAAffectsSlotDropTonnage) {
                                    slotTonnage += cargoSlot.SelectedMech.MechDef.Chassis.Tonnage;
                                    ModInit.modLog?.Error?.Write($"  external:{cargoSlot.SelectedMech.MechDef.ChassisID} curSlotTonnage:{slotTonnage}");
                                }
                                if (ModInit.modSettings.ExternalBAAffectsOverallDropTonnage == false) { continue; }
                            }
                            else
                            {
                                if (ModInit.modSettings.InternalBAAffectsSlotDropTonnage)
                                {
                                    slotTonnage += cargoSlot.SelectedMech.MechDef.Chassis.Tonnage;
                                    ModInit.modLog?.Error?.Write($"  internal:{cargoSlot.SelectedMech.MechDef.ChassisID} curSlotTonnage:{slotTonnage}");
                                }
                                if (ModInit.modSettings.InternalBAAffectsOverallDropTonnage == false) { continue; }
                            }
                            __instance.currentLanceValue += cargoSlot.SelectedMech.MechDef.Description.Cost;
                            mechs.Add(cargoSlot.SelectedMech.MechDef);                            
                        }
                    }
                    if(TonnageWithinRange(slotTonnage, loadoutSlot.minTonnage, loadoutSlot.maxTonnage) == false)
                    {
                        __result = false;
                        if (__instance.slotMinTonnages[index] >= 0.0f && __instance.slotMaxTonnages[index] >= 0.0f)
                            __instance.lanceErrorText.Append("Lance slot {0} requires a 'Mech between {1} and {2} Tons\n", index, __instance.slotMinTonnages[index], __instance.slotMaxTonnages[index]);
                        else if (__instance.slotMinTonnages[index] >= 0.0f)
                            __instance.lanceErrorText.Append("Lance slot {0} requires a 'Mech over {1} Tons\n", index, __instance.slotMinTonnages[index]);
                        else if (__instance.slotMaxTonnages[index] >= 0.0f)
                            __instance.lanceErrorText.Append("Lance slot {0} requires a 'Mech under {1} Tons\n", index, __instance.slotMaxTonnages[index]);
                    }
                }
                float lanceWeight = 0f;
                ModInit.modLog?.Error?.Write("overall");
                foreach (var mech in mechs)
                {
                    lanceWeight += mech.Chassis.Tonnage;
                    ModInit.modLog?.Error?.Write($" {mech.ChassisID}:{mech.Chassis.Tonnage}:{lanceWeight}");
                }
                bool lanceOkTonnage = MechValidationRules.LanceTonnageWithinRange(mechs, __instance.lanceMinTonnage, __instance.lanceMaxTonnage);
                if (lanceOkTonnage == false)
                {
                    __result = false;
                    if (__instance.lanceMinTonnage >= 0.0 && __instance.lanceMaxTonnage >= 0.0)
                        __instance.lanceErrorText.Append("Total Lance tonnage must be between {0} and {1} Tons\n", __instance.lanceMinTonnage, __instance.lanceMaxTonnage);
                    else if (__instance.lanceMinTonnage >= 0.0)
                        __instance.lanceErrorText.Append("Total Lance tonnage must be greater than {0} Tons\n", __instance.lanceMinTonnage);
                    else if (__instance.lanceMaxTonnage >= 0.0)
                        __instance.lanceErrorText.Append("Total Lance tonnage must be less than {0} Tons\n", __instance.lanceMaxTonnage);
                }
                ModInit.modLog?.Error?.Write($"cur:{lanceWeight} min:{__instance.lanceMinTonnage} max:{__instance.lanceMaxTonnage} ok:{lanceOkTonnage} result:{__result}");
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
                return;
            }
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel), "CreateLanceDef")]
    [HarmonyBefore("io.mission.customunits")]
    public static class LanceConfiguratorPanel_CreateLanceDef
    {
        static void Prefix(ref bool __runOriginal, LanceConfiguratorPanel __instance, string lanceId, ref LanceDef __result)
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            ModInit.modLog?.Info?.Write($"LanceConfiguratorPanel.CreateLanceDef {lanceId} slots:{__instance.loadoutSlots.Length}");
            if (!__runOriginal) { return; }
            try
            {
                string lanceName = __instance.headerWidget.LanceName;
                DescriptionDef description = new DescriptionDef(lanceId, lanceName, "", "", __instance.currentLanceValue, 0.0f, false, "", "", "");
                TagSet lanceTags = new TagSet(new string[4]
                { "lance_type_custom", "lance_release", "lance_bracket_skirmish", MechValidationRules.GetLanceBracketTag(__instance.currentLanceValue)
                });
                List<LanceDef.Unit> unitList = new List<LanceDef.Unit>();
                for (int index = 0; index < __instance.loadoutSlots.Length; ++index)
                {
                    LanceLoadoutSlot loadoutSlot = __instance.loadoutSlots[index];
                    if ((loadoutSlot.SelectedMech != null) && (loadoutSlot.SelectedPilot != null))
                    {

                        LanceLoadoutSlotCargoConfig cargoInfo = loadoutSlot.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                        List<LanceDef.Unit> mounts = new List<LanceDef.Unit>();
                        if (cargoInfo != null)
                        {
                            for (int t = 0; t < cargoInfo.cargoSlots.Count; ++t)
                            {
                                LanceLoadoutSlot cargoSlot = cargoInfo.cargoSlots[t];
                                if ((cargoSlot.SelectedMech != null) && (cargoSlot.SelectedPilot != null))
                                {
                                    mounts.Add(new LanceDef.Unit()
                                    {
                                        unitType = UnitType.Mech,
                                        unitId = cargoSlot.SelectedMech.MechDef.Description.Id,
                                        pilotId = cargoSlot.SelectedPilot.Pilot.pilotDef.Description.Id,
                                        Mounts = new LanceDef.Unit[] { }
                                    });
                                }
                                else
                                {
                                    mounts.Add(new LanceDef.Unit()
                                    {
                                        unitType = UnitType.Mech,
                                        unitId = string.Empty,
                                        pilotId = string.Empty,
                                        Mounts = new LanceDef.Unit[] { }
                                    });
                                }
                            }
                        }
                        unitList.Add(new LanceDef.Unit()
                        {
                            unitType = UnitType.Mech,
                            unitId = loadoutSlot.SelectedMech.MechDef.Description.Id,
                            pilotId = loadoutSlot.SelectedPilot.Pilot.pilotDef.Description.Id,
                            Mounts = mounts.ToArray()
                        });
                    }
                    else
                    {
                        unitList.Add(new LanceDef.Unit()
                        {
                            unitType = UnitType.Mech,
                            unitId = string.Empty,
                            pilotId = string.Empty,
                            Mounts = new LanceDef.Unit[] { }
                        });
                    }
                }
                __result = new LanceDef(description, 0, lanceTags, unitList.ToArray());
            }
            catch (Exception e)
            {
                ModInit.modLog?.Info?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
            __runOriginal = false;
            return;
        }
        static void Postfix(LanceConfiguratorPanel __instance, string lanceId, ref LanceDef __result)
        {
            //LanceLoadoutSlot[] loadoutSlots = __instance.loadoutSlots;
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            ModInit.modLog?.Info?.Write($"LanceConfiguratorPanel.CreateLanceDef result:");
            ModInit.modLog?.Info?.Write(__result.ToJSON());
        }
    }
    [HarmonyPatch(typeof(LanceDef.Unit))]
    [HarmonyPatch("Copy")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { })]
    public static class LanceDef_Unit_Copy
    {
        public static void Postfix(LanceDef.Unit __instance, ref LanceDef.Unit __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                if (__instance.Mounts != null)
                {
                    __result.Mounts = Utilities.CopyUtils.CopyArray<LanceDef.Unit>(__instance.Mounts);
                }
                else
                {
                    __result.Mounts = new LanceDef.Unit[0];
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("LoadLanceDef")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { typeof(LanceDef) })]
    public static class LanceConfiguratorPanel_LoadLanceDef
    {
        public static void Prefix(ref bool __runOriginal, LanceConfiguratorPanel __instance, LanceDef lance)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                if (!__runOriginal) { return; }
                __instance.mechwarriorLoadNotification.SetActive(true);
                __instance.pilotListWidget.Clear();
                __instance.pilotListWidget.PopulateRosterAsync(__instance.availablePilots, (UnityAction<SGBarracksRosterSlot>)null, (Action)(() =>
                {
                    __instance.mechwarriorLoadNotification.SetActive(false);
                    __instance.headerWidget.LanceName = lance.Description.Name;
                    __instance.oldLanceName = __instance.headerWidget.LanceName;
                    __instance.currentLanceId = lance.Description.Id;
                    for (int i = 0; i < lance.LanceUnits.Length; i++)
                    {
                        if (i < __instance.loadoutSlots.Length)
                        {
                            IMechLabDraggableItem inventoryItem = __instance.mechListWidget.GetInventoryItem(lance.LanceUnits[i].unitId);
                            IMechLabDraggableItem pilot = (IMechLabDraggableItem)__instance.pilotListWidget.GetPilot(lance.LanceUnits[i].pilotId);
                            __instance.loadoutSlots[i].SetLockedData(inventoryItem, pilot, false);
                            __instance.pilotListWidget.RemovePilot(__instance.availablePilots.Find((Predicate<Pilot>)(x => x.Description.Id == lance.LanceUnits[i].pilotId)));
                            LanceLoadoutSlotCargoConfig cargoInfo = __instance.loadoutSlots[i].gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                            if (cargoInfo == null) { continue; }
                            if (lance.LanceUnits[i].Mounts == null) { continue; }
                            for (int ii = 0; ii < lance.LanceUnits[i].Mounts.Length; ++ii)
                            {
                                if (ii >= cargoInfo.cargoSlots.Count) { break; }
                                inventoryItem = __instance.mechListWidget.GetInventoryItem(lance.LanceUnits[i].Mounts[ii].unitId);
                                pilot = (IMechLabDraggableItem)__instance.pilotListWidget.GetPilot(lance.LanceUnits[i].Mounts[ii].pilotId);
                                cargoInfo.cargoSlots[ii].SetLockedData(inventoryItem, pilot, false);
                                __instance.pilotListWidget.RemovePilot(__instance.availablePilots.Find((Predicate<Pilot>)(x => x.Description.Id == lance.LanceUnits[i].Mounts[ii].pilotId)));
                            }
                        }
                    }
                }));
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
            __runOriginal = false;
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("CreateLanceConfiguration")]
    [HarmonyAfter("io.mission.customunits")]
    public static class LanceConfiguratorPanel_CreateLanceConfiguration
    {
        static void Postfix(LanceConfiguratorPanel __instance, ref LanceConfiguration __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                foreach (var slot in __instance.loadoutSlots)
                {
                    LanceLoadoutSlotCargoConfig cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                    if (cargoInfo == null) { continue; }
                    if ((slot.SelectedMech == null) || (slot.SelectedPilot == null)) { continue; }
                    foreach (var cargoSlot in cargoInfo.cargoSlots)
                    {
                        if ((cargoSlot.SelectedMech == null) || (cargoSlot.SelectedPilot == null))
                        {
                            //__result.AddUnit(slot.GetSlotGUID(), string.Empty, string.Empty, UnitType.UNDEFINED);
                            continue;
                        }
                        __result.AddUnit(slot.GetSlotGUID(), cargoSlot.SelectedMech.MechDef, cargoSlot.SelectedPilot.Pilot.pilotDef);
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel))]
    [HarmonyPatch("SetLance")]
    public static class LancePreviewPanel_SetLance
    {
        static void Postfix(LancePreviewPanel __instance, int idx)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                for (int t = 0; t < __instance.curLanceDef.LanceUnits.Length; ++t)
                {
                    LanceDef.Unit lanceUnit = __instance.curLanceDef.LanceUnits[t];
                    if (lanceUnit == null) { continue; }
                    if (t >= __instance.loadoutSlots.Length) { continue; }
                    var slot = __instance.loadoutSlots[t];
                    LanceLoadoutSlotCargoPreview cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoPreview>();
                    if (cargoInfo == null) { continue; }
                    if (slot.SelectedMech == null) { continue; }
                    cargoInfo.SetSlotsCount(slot.SelectedMech.MechDef.CargoCapacity(__instance.LC != null ? __instance.LC.activeContract : null), slot.SelectedMech.MechDef.CanMountBAExternally(__instance.LC != null ? __instance.LC.activeContract : null));
                    if (lanceUnit.Mounts == null) { continue; }
                    for (int tt = 0; tt < lanceUnit.Mounts.Length; ++tt)
                    {
                        LanceDef.Unit cargoUnit = __instance.curLanceDef.LanceUnits[t].Mounts[tt];
                        if (cargoUnit == null) { continue; }
                        if (string.IsNullOrEmpty(cargoUnit.unitId)) { continue; }
                        if (string.IsNullOrEmpty(cargoUnit.pilotId)) { continue; }
                        if (tt >= cargoInfo.cargoSlots.Count) { break; }
                        var cargoSlot = cargoInfo.cargoSlots[tt];
                        MechDef mechDef = __instance.GetMechDef(cargoUnit.unitId, cargoUnit.unitSimGameID);
                        PilotDef pilotDef = string.IsNullOrEmpty(cargoUnit.pilotId) ? __instance.allPilotDefs[(t + 1) % __instance.allPilotDefs.Count] : __instance.GetPilotDef(cargoUnit.pilotId);
                        if (mechDef != null && pilotDef != null)
                        {
                            GameObject mechGO = __instance.dataManager.PooledInstantiate("uixPrfPanl_LC_MechSlot", BattleTechResourceType.UIModulePrefabs);
                            GameObject pilotGO = __instance.dataManager.PooledInstantiate("uixPrfPanl_SIM_mechWarriorSlot-Element", BattleTechResourceType.UIModulePrefabs);
                            if (mechGO != null && pilotGO != null)
                            {
                                LanceLoadoutMechItem mechItem = mechGO.GetComponent<LanceLoadoutMechItem>();
                                SGBarracksRosterSlot pilotItem = pilotGO.GetComponent<SGBarracksRosterSlot>();
                                Pilot p = new Pilot(pilotDef, string.Format("{0}_pilot_{1}_{2}", __instance.curLanceDef.Description.Id, t, tt), true);
                                cargoSlot.SetData((LanceConfiguratorPanel)null, (SimGameState)null, __instance.dataManager, false, false);
                                mechItem.SetData((IMechLabDropTarget)null, __instance.dataManager, mechDef, false, false);
                                mechItem.mechElement.ShowStockIcon(!mechDef.MechTags.Contains("unit_custom"));
                                pilotItem.InitNoDrag(p, null, null, canHighlight: false);
                                cargoSlot.SetLockedData(mechItem, pilotItem, false);
                                mechItem.SetInteractable(true);
                                pilotItem.SetInteractable(true);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel))]
    [HarmonyPatch("ClearLance")]
    public static class LancePreviewPanel_ClearLance
    {
        static void Postfix(LancePreviewPanel __instance)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                foreach (var slot in __instance.loadoutSlots)
                {
                    LanceLoadoutSlotCargoPreview cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoPreview>();
                    if (cargoInfo == null) { continue; }
                    cargoInfo.SetSlotsCount(0, false);
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel))]
    [HarmonyPatch("GenerateLanceConfigurationFromSelectedLance")]
    public static class LancePreviewPanel_GenerateLanceConfigurationFromSelectedLance
    {
        static void Prefix(ref bool __runOriginal, LancePreviewPanel __instance, ref LanceConfiguration __result)
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            if (__runOriginal == false) { return; }
            __runOriginal = false;
        }
        static void Postfix(LancePreviewPanel __instance, ref LanceConfiguration __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                __result = new LanceConfiguration();
                foreach (var slot in __instance.loadoutSlots)
                {
                    MechDef unitDef = null;
                    PilotDef pilot = null;
                    if (slot.SelectedMech != null)
                        unitDef = slot.SelectedMech.MechDef;
                    if (slot.SelectedPilot != null)
                        pilot = slot.SelectedPilot.Pilot.pilotDef;
                    if (unitDef != null && pilot != null)
                    {
                        __result.AddUnit("bf40fd39-ccf9-47c4-94a6-061809681140", unitDef, pilot);
                    }
                    else
                    {
                        __result.AddUnit("bf40fd39-ccf9-47c4-94a6-061809681140", string.Empty, string.Empty, UnitType.UNDEFINED);
                        continue;
                    }
                    LanceLoadoutSlotCargoPreview cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoPreview>();
                    if (cargoInfo == null) { continue; }
                    foreach (var cargoSlot in cargoInfo.cargoSlots)
                    {
                        if ((cargoSlot.SelectedMech == null) || (cargoSlot.selectedPilot == null))
                        {
                            __result.AddUnit(slot.GetSlotGUID(), string.Empty, string.Empty, UnitType.UNDEFINED);
                            continue;
                        }
                        __result.AddUnit(slot.GetSlotGUID(), cargoSlot.SelectedMech.MechDef, cargoSlot.SelectedPilot.Pilot.pilotDef);
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel))]
    [HarmonyPatch("GetLanceConfiguration")]
    public static class LancePreviewPanel_GetLanceConfiguration
    {
        static void Postfix(LancePreviewPanel __instance, ref LanceConfiguration __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                foreach (var slot in __instance.loadoutSlots)
                {
                    if (slot.SelectedMech == null) { continue; }
                    if (slot.SelectedPilot == null) { continue; }
                    LanceLoadoutSlotCargoPreview cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoPreview>();
                    if (cargoInfo == null) { continue; }
                    foreach (var cargoSlot in cargoInfo.cargoSlots)
                    {
                        if ((cargoSlot.SelectedMech == null) || (cargoSlot.selectedPilot == null))
                        {
                            //__result.AddUnit(slot.GetSlotGUID(), string.Empty, string.Empty, UnitType.UNDEFINED);
                            continue;
                        }
                        __result.AddUnit(slot.GetSlotGUID(), cargoSlot.SelectedMech.MechDef, cargoSlot.SelectedPilot.Pilot.pilotDef);
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(SkirmishSettings_Beta))]
    [HarmonyPatch("FinalizeLances")]
    public static class LancePreviewPanel_FinalizeLances
    {
        static void Postfix(SkirmishSettings_Beta __instance, ref LanceConfiguration __result)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                foreach (var slot in __instance.playerLancePreview.loadoutSlots)
                {
                    if (slot.SelectedMech == null) { continue; }
                    if (slot.SelectedPilot == null) { continue; }
                    LanceLoadoutSlotCargoPreview cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoPreview>();
                    if (cargoInfo == null) { continue; }
                    foreach (var cargoSlot in cargoInfo.cargoSlots)
                    {
                        if ((cargoSlot.SelectedMech == null) || (cargoSlot.selectedPilot == null))
                        {
                            //__result.AddUnit(slot.GetSlotGUID(), string.Empty, string.Empty, UnitType.UNDEFINED);
                            continue;
                        }
                        __result.AddUnit(slot.GetSlotGUID(), cargoSlot.SelectedMech.MechDef, cargoSlot.SelectedPilot.Pilot.pilotDef);
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }

    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("LoadLanceConfiguration")]
    public static class LanceConfiguratorPanel_LoadLanceConfiguration
    {
        static void Postfix(LanceConfiguratorPanel __instance, LanceConfiguration config)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                ModInit.modLog?.Info?.Write("LanceConfiguratorPanel.LoadLanceConfiguration");
                foreach (var lance in config.Lances)
                {
                    ModInit.modLog?.Info?.Write($" {lance.Key}");
                    for (int t = 0; t < lance.Value.Count; ++t)
                    {
                        ModInit.modLog?.Info?.Write($"  [{t}]{lance.Value[t].UnitId}:{lance.Value[t].PilotId}");
                    }
                }
                foreach (var slot in __instance.loadoutSlots)
                {
                    LanceLoadoutSlotCargoConfig cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                    if (cargoInfo == null) { continue; }
                    if ((slot.SelectedMech == null) || (slot.SelectedPilot == null)) { continue; }
                    SpawnableUnit[] cargoUnits = config.GetLanceUnits(slot.GetSlotGUID());
                    for (int t = 0; t < cargoUnits.Length; ++t)
                    {
                        IMechLabDraggableItem forcedMech = null;
                        if (t >= cargoInfo.cargoSlots.Count) { break; }
                        var cargoUnit = cargoUnits[t];
                        var cargoSlot = cargoInfo.cargoSlots[t];
                        if (cargoUnit.Unit != null)
                            forcedMech = __instance.mechListWidget.GetMechDefByGUID(cargoUnit.Unit.GUID);
                        if (forcedMech == null)
                            forcedMech = __instance.mechListWidget.GetInventoryItem(cargoUnit.UnitId);
                        if (forcedMech != null && !MechValidationRules.ValidateMechCanBeFielded(__instance.Sim, forcedMech.MechDef))
                            forcedMech = null;
                        var forcedPilot = __instance.pilotListWidget.GetPilot(cargoUnit.PilotId);
                        if (forcedPilot != null)
                        {
                            if ((forcedPilot as SGBarracksRosterSlot).Pilot.CanPilot == false)
                                forcedPilot = null;
                            else
                                __instance.pilotListWidget.RemovePilot(__instance.availablePilots.Find((Predicate<Pilot>)(x => x.Description.Id == cargoUnit.PilotId)));
                        }
                        cargoSlot.SetLockedData(forcedMech, forcedPilot, false);
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel))]
    [HarmonyPatch("RefreshLanceInitiative")]
    public static class LanceConfiguratorPanel_RefreshLanceInitiative
    {
        static void Postfix(LanceConfiguratorPanel __instance)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                foreach (var slot in __instance.loadoutSlots)
                {
                    LanceLoadoutSlotCargoConfig cargoInfo = slot.gameObject.GetComponent<LanceLoadoutSlotCargoConfig>();
                    if (cargoInfo == null) { continue; }
                    foreach (var cargoSlot in cargoInfo.cargoSlots)
                    {
                        cargoSlot.RefreshInitiativeData();
                    }
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(LanceSpawnerGameLogic))]
    [HarmonyPatch("InitEffectsOnSpawnedUnits")]
    public static class LanceConfiguratorPanel_InitEffectsOnSpawnedUnits
    {
        public class CargoUnitSpawnInfo
        {
            public SpawnableUnit unit;
            public AbstractActor carrier;
            public UnitSpawnPointGameLogic carrierSpawner;
            public AbstractActor actor;
            public bool isSpawned;
        }
        public class CargoUnitSpawnComponent: MonoBehaviour
        {
            public CargoSpawnInfo info;
            public void Update()
            {
                if (info == null) { return; }
                if(DeployManualHelper_RestoreVisiblityState.RestoreVisiblityStateCalled == false)
                {
                    return;
                }
                info.SpawnCargoAsync(this);
            }
        }
        public class CargoSpawnInfo
        {
            public List<CargoUnitSpawnInfo> units = new List<CargoUnitSpawnInfo>();
            public CombatGameState Combat = null;
            public void SpawnCargoAsync(CargoUnitSpawnComponent component)
            {
                ModInit.modLog?.Info?.Write($"CargoSpawnInfo.SpawnCargoAsync {DeployManualHelper_RestoreVisiblityState.RestoreVisiblityStateCalled}");
                DeployManualHelper_RestoreVisiblityState.RestoreVisiblityStateCalled = false;
                SpawnCargo();
                GameObject.Destroy(component);
            }
            public void SpawnCargo()
            {
                ModInit.modLog?.Info?.Write("CargoSpawnInfo.SpawnCargo");
                CombatGameState combat = null;
                foreach (var unitToSpawn in this.units)
                {
                    var team = unitToSpawn.carrier.team;
                    Mech mech = ActorFactory.CreateMech(
                        unitToSpawn.unit.Unit,
                        unitToSpawn.unit.Pilot,
                        unitToSpawn.carrierSpawner.EncounterTags,
                        UnityGameInstance.BattleTechGame.Combat,
                        unitToSpawn.carrierSpawner.GetNextUnitGuid(),
                        unitToSpawn.carrier.GUID,
                        team.HeraldryDef);
                    mech.Init(unitToSpawn.carrier.CurrentPosition, 0f, true);
                    mech.InitGameRep(null);
                    team.AddUnit(mech);
                    Lance spawnLance = team.GetLanceByUID(unitToSpawn.carrierSpawner.lanceGuid);
                    spawnLance.AddUnitGUID(mech.GUID);
                    mech.AddToLance(spawnLance);
                    mech.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
                    UnitSpawnedMessage unitSpawnedMessage = new UnitSpawnedMessage(unitToSpawn.carrier.GUID, mech.GUID);
                    mech.OnPositionUpdate(unitToSpawn.carrier.CurrentPosition, Quaternion.identity, -1, true, (List<DesignMaskDef>)null, false);
                    mech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(mech.Combat.BattleTechGame, mech, BehaviorTreeIDEnum.DoNothingTree); ;
                    UnityGameInstance.BattleTechGame.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new UnitSpawnedMessage("MOUNT_SPAWNER", mech.GUID));
                    ModInit.modLog?.Info?.Write(" spawned:" + mech.PilotableActorDef.Description.Id + ":" + mech.pilot.Description.Id + " " + mech.CurrentPosition);
                    //mech.PlaceFarAwayFromMap();
                    unitToSpawn.carrier.Combat.ItemRegistry.AddItem(mech);
                    combat = unitToSpawn.carrier.Combat;
                    unitToSpawn.actor = mech;
                }
                if (combat != null)
                {
                    combat.RebuildAllLists();
                    foreach (var unitToSpawn in this.units)
                    {
                        unitToSpawn.actor.InitPassiveTeamEffects();
                        unitToSpawn.carrier.MountBattleArmorToChassis(unitToSpawn.actor, true, true);
                        if (unitToSpawn.carrier is CustomMech custMech && custMech.FlyingHeight() > 1.5f)
                        {
                            var pos = custMech.CurrentPosition +
                                      Vector3.up * custMech.custGameRep.HeightController.CurrentHeight;
                            unitToSpawn.actor.TeleportActorNoResetPathing(pos);
                        }
                        combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(unitToSpawn.actor.DoneWithActor()));
                    }
                    if (CombatHUDMechwarriorTray_RefreshTeam.RefreshAlreadyCalled)
                    {
                        UIManager.Instance.uiNode.gameObject.GetComponentInChildren<CombatHUDMechwarriorTray>(true).RefreshTeam(combat.LocalPlayerTeam);
                        UIManager.Instance.uiNode.gameObject.GetComponentInChildren<CombatHUDMechwarriorTray>(true).RefreshPortraits();
                        CombatHUDMechwarriorTray_RefreshTeam.RefreshAlreadyCalled = false;
                    }
                }

            }
            public void OnLoadComplete()
            {
                try
                {
                    ModInit.modLog?.Info?.Write($"CargoSpawnInfo.OnLoadComplete manualSpawn:{this.Combat.ActiveContract.isManualSpawn()}");
                    if (this.Combat.ActiveContract.isManualSpawn() == false)
                    {
                        this.SpawnCargo();
                    }
                    else
                    {
                        DeployManualHelper_RestoreVisiblityState.RestoreVisiblityStateCalled = false;
                        UnityGameInstance.Instance.gameObject.AddComponent<CargoUnitSpawnComponent>().info = this;
                    }
                }
                catch (Exception e)
                {
                    ModInit.modLog?.Error?.Write(e.ToString());
                    UIManager.logger.LogException(e);
                }
            }
            public CargoSpawnInfo(List<CargoUnitSpawnInfo> units, CombatGameState combat)
            {
                this.units = units;
                this.Combat = combat;
            }
        }
        static void Postfix(LanceSpawnerGameLogic __instance)
        {
            try
            {
                if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
                if (__instance.teamDefinitionGuid != __instance.Combat.LocalPlayerTeamGuid) { return; }
                UnitSpawnPointGameLogic[] pointGameLogicList = __instance.unitSpawnPointGameLogicList;
                List<CargoUnitSpawnInfo> units_to_spawn = new List<CargoUnitSpawnInfo>();
                ModInit.modLog?.Info?.Write("LanceSpawnerGameLogic.InitEffectsOnSpawnedUnits");
                foreach (var unitSpawnPoint in pointGameLogicList)
                {
                    if (unitSpawnPoint.mechDefOverride == null) { continue; }
                    if (unitSpawnPoint.pilotDefOverride == null) { continue; }
                    ModInit.modLog?.Info?.Write($" {unitSpawnPoint.GetSlotGUID()}");
                    SpawnableUnit[] units = __instance.Combat.ActiveContract.Lances.GetLanceUnits(unitSpawnPoint.GetSlotGUID());
                    foreach (var unit in units)
                    {
                        if (unit.Unit == null) { continue; }
                        if (unit.Pilot == null) { continue; }
                        var info = new CargoUnitSpawnInfo()
                        {
                            unit = unit,
                            carrier = __instance.Combat.ItemRegistry.GetItemByGUID<AbstractActor>(unitSpawnPoint.lastUnitGuid),
                            carrierSpawner = unitSpawnPoint,
                            isSpawned = false,
                            actor = null
                        };
                        ModInit.modLog?.Info?.Write($"  carrier:{info.carrier.PilotableActorDef.ChassisID} {info.unit.Unit.Description.Id}:{info.unit.PilotId}");
                        units_to_spawn.Add(info);
                    }
                }
                DataManager.InjectedDependencyLoadRequest dependencyLoad = new DataManager.InjectedDependencyLoadRequest(__instance.Combat.DataManager);
                foreach (var unitToSpawn in units_to_spawn)
                {
                    if (unitToSpawn.unit.Unit.DependenciesLoaded(1000U) == false)
                    {
                        unitToSpawn.unit.Unit.GatherDependencies(__instance.Combat.DataManager, dependencyLoad, 1000U);
                    }
                    if (unitToSpawn.unit.Pilot.DependenciesLoaded(1000U) == false)
                    {
                        unitToSpawn.unit.Pilot.GatherDependencies(__instance.Combat.DataManager, dependencyLoad, 1000U);
                    }
                }
                if (dependencyLoad.DependencyCount() > 0)
                {
                    dependencyLoad.RegisterLoadCompleteCallback(new CargoSpawnInfo(units_to_spawn, __instance.Combat).OnLoadComplete);
                    __instance.Combat.DataManager.InjectDependencyLoader(dependencyLoad, 1000U);
                }
                else
                {
                    new CargoSpawnInfo(units_to_spawn, __instance.Combat).OnLoadComplete();
                }
            }
            catch (Exception e)
            {
                ModInit.modLog?.Error?.Write(e.ToString());
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(CombatHUDMechwarriorTray))]
    [HarmonyPatch("RefreshTeam")]
    [HarmonyPatch(MethodType.Normal)]
    public static class CombatHUDMechwarriorTray_RefreshTeam
    {
        public static bool RefreshAlreadyCalled = false;
        public static void Postfix()
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            RefreshAlreadyCalled = true;
        }
    }
    [HarmonyPatch(typeof(PlayerLanceSpawnerGameLogic))]
    [HarmonyPatch("ContractInitialize")]
    [HarmonyPatch(MethodType.Normal)]
    public static class PlayerLanceSpawnerGameLogic_ContractInitialize
    {
        public static void Prefix()
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            CombatHUDMechwarriorTray_RefreshTeam.RefreshAlreadyCalled = false;
        }
    }
    [HarmonyPatch(typeof(DeployManualHelper))]
    [HarmonyPatch("RestoreVisiblityState")]
    [HarmonyPatch(MethodType.Normal)]
    public static class DeployManualHelper_RestoreVisiblityState
    {
        public static bool RestoreVisiblityStateCalled = false;
        public static void Postfix()
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            RestoreVisiblityStateCalled = true;
            ModInit.modLog?.Info?.Write("DeployManualHelper.RestoreVisiblityState");
        }
    }
    public class LastUsedMount
    {
        public string mechId;
        public string pilotId;
        public LastUsedMount() { mechId = string.Empty; pilotId = string.Empty; }
        public LastUsedMount(string m, string p) { mechId = m; pilotId = p; }
    }
    public class LastUsedMounts
    {
        public static readonly string LAST_MOUNTS_STATISTIC_NAME = "SO_LAST_MOUNTS";
        public string GUID;
        public List<LastUsedMount> mounts = new List<LastUsedMount>();
    }
    [HarmonyPatch(typeof(SimGameState))]
    [HarmonyPatch("SaveLastLance")]
    [HarmonyPatch(MethodType.Normal)]
    public static class SimGameState_SaveLastLance
    {
        public static void Postfix(SimGameState __instance, LanceConfiguration config)
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            ModInit.modLog?.Info?.Write("SimGameState.SaveLastLance");
            List<LastUsedMounts> all_mounts = new List<LastUsedMounts>();
            foreach (SpawnableUnit lanceUnit in config.GetLanceUnits("bf40fd39-ccf9-47c4-94a6-061809681140"))
            {
                if (lanceUnit.Unit == null) { continue; }
                if (lanceUnit.Pilot == null) { continue; }
                LastUsedMounts mounts = new LastUsedMounts();
                mounts.GUID = lanceUnit.GetSlotGUID();
                foreach (SpawnableUnit cargoUnit in config.GetLanceUnits(mounts.GUID))
                {
                    if ((lanceUnit.Unit == null) || (lanceUnit.Pilot == null)) {
                        mounts.mounts.Add(new LastUsedMount(string.Empty, string.Empty));
                        continue;
                    }
                    mounts.mounts.Add(new LastUsedMount(cargoUnit.Unit.GUID, cargoUnit.Pilot.Description.Id));
                }
                all_mounts.Add(mounts);
            }
            Statistic lastMountsStat = __instance.CompanyStats.GetOrCreateStatisic<string>(LastUsedMounts.LAST_MOUNTS_STATISTIC_NAME, "[]");
            lastMountsStat.SetValue(JsonConvert.SerializeObject(all_mounts, Formatting.Indented));
            ModInit.modLog?.Info?.Write($" {lastMountsStat.Value<string>()}");
        }
    }
    [HarmonyPatch(typeof(SimGameState))]
    [HarmonyPatch("GetLastLance")]
    [HarmonyPatch(MethodType.Normal)]
    public static class SimGameState_GetLastLance
    {
        public static void Postfix(SimGameState __instance, ref LanceConfiguration __result)
        {
            if (ModInit.modSettings.UseOriginalBAMountInterface) { return; }
            ModInit.modLog?.Info?.Write("SimGameState.GetLastLance");
            Statistic lastMountsStat = __instance.CompanyStats.GetOrCreateStatisic<string>(LastUsedMounts.LAST_MOUNTS_STATISTIC_NAME, "[]");
            List<LastUsedMounts> all_mounts = JsonConvert.DeserializeObject<List<LastUsedMounts>>(lastMountsStat.Value<string>());
            ModInit.modLog?.Info?.Write($" {lastMountsStat.Value<string>()}");
            foreach (var mounts in all_mounts)
            {
                foreach(var mount in mounts.mounts)
                {
                    if(string.IsNullOrEmpty(mount.mechId) || (string.IsNullOrEmpty(mount.pilotId)))
                    {
                        __result.AddUnit(mounts.GUID, string.Empty, string.Empty, UnitType.UNDEFINED);
                        continue;
                    }
                    MechDef mechById = __instance.GetMechByID(mount.mechId);
                    PilotDef pilotDef = __instance.GetPilot(mount.pilotId)?.pilotDef;
                    if (mechById != null || pilotDef != null)
                    {
                        __result.AddUnit(mounts.GUID, mechById, pilotDef);
                    }
                }
            }
        }
    }
}