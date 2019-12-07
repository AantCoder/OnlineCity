using OCUnion;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorldOnlineCity
{
    public class Dialog_TradeOnline : Window
    {
        private const float TitleRectHeight = 40f;

        private const float BottomAreaHeight = 55f;

        private const float ExitDirectionTitleHeight = 30f;

        private const float MaxDaysWorthOfFoodToShowWarningDialog = 5f;

        public const float MassLabelYOffset = 32f;
        
        private Action onClosed;

        private bool showEstTimeToDestinationButton;

        private bool thisWindowInstanceEverOpened;

        private List<TransferableOneWay> transferables;
        
        private TransferableOneWayWidget itemsTransfer;
        
        private bool massUsageDirty = true;

        private float cachedMassUsage;
        
        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private readonly Vector2 ExitDirectionRadioSize = new Vector2(250f, 30f);

        private string WhoName;

        private float FreeWeight;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)Verse.UI.screenHeight);
            }
        }
        

        private float MassUsage
        {
            get
            {
                if (this.massUsageDirty)
                {
                    this.massUsageDirty = false;
                    bool autoStripCorpses = true;
                    this.cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, autoStripCorpses);
                }
                return this.cachedMassUsage;
            }
        }

        private float MassCapacity
        {
            get
            {
                /*
                if (this.massCapacityDirty)
                {
                    this.massCapacityDirty = false;
                    this.cachedMassCapacity = CollectionsMassCalculator.CapacityTransferables(this.transferables);
                }*/
                return FreeWeight > 0 ? FreeWeight : 9999999; // this.cachedMassCapacity;
            }
        }

        public IEnumerable<Thing> AllItem;

        public bool IsCancel = false;

        public Dictionary<Thing, int> GetSelect()
        {
            var result = new Dictionary<Thing, int>();
            if (IsCancel) return result;

            List<ThingCount> stackParts = new List<ThingCount>();
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableUtility.TransferNoSplit(transferables[i].things, transferables[i].CountToTransfer, delegate (Thing originalThing, int toTake)
                {
                    stackParts.Add(new ThingCount(originalThing, toTake));
                }, false, false);
            }
            
            float num = 0f;
            for (int i = 0; i < stackParts.Count; i++)
            {
                int count = stackParts[i].Count;
                if (count > 0)
                {
                    Thing thing = stackParts[i].Thing;
                    result.Add(thing, count);
                    /*
                    {
                        Thing innerIfMinified = thing.GetInnerIfMinified();
                        num += innerIfMinified.GetStatValue(StatDefOf.Mass, true) * (float)count;
                        
                    }*/
                }
            }
            return result;
        }

        public Dialog_TradeOnline(IEnumerable<Thing> allItem, string who, float freeWeight, Action onClosed = null, bool showEstTimeToDestinationButton = true)
        {
            AllItem = allItem;
            WhoName = who;
            FreeWeight = freeWeight;
            closeOnCancel = false;
            closeOnAccept = false;
            this.onClosed = onClosed;
            this.showEstTimeToDestinationButton = showEstTimeToDestinationButton;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            if (!this.thisWindowInstanceEverOpened)
            {
                this.thisWindowInstanceEverOpened = true;
                this.CalculateAndRecacheTransferables();
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.onClosed != null)
            {
                this.onClosed();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 40f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "OCity_Dialog_TradeOnline_Trade".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            inRect.yMin += 72f;
            Widgets.DrawMenuSection(inRect/*, true*/);

            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();

            Rect rect3 = rect2;
            rect3.y += 32f;
            rect3.xMin += rect2.width - 515f;
            this.DrawMassAndFoodInfo(rect3);

            this.DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 59f;
            bool flag = false;
            this.itemsTransfer.OnGUI(inRect2, out flag);

            if (flag)
            {
                this.CountToTransferChanged();
            }
            GUI.EndGroup();
        }
        
        private void DrawMassAndFoodInfo(Rect rect)
        {
            //метод из Dialog_SplitCaravan с удалением лишнего
            //TransferableUIUtility.DrawMassInfo(rect, this.SourceMassUsage, this.SourceMassCapacity, "SplitCaravanMassUsageTooltip".Translate(), -9999f, false);
            //CaravanUIUtility.DrawDaysWorthOfFoodInfo(new Rect(rect.x, rect.y + 19f, rect.width, rect.height), this.SourceDaysWorthOfFood.First, this.SourceDaysWorthOfFood.Second, this.EnvironmentAllowsEatingVirtualPlantsNow, false, 3.40282347E+38f);
            /*TransferableUIUtility.*/DrawMassInfo(rect, this.MassUsage, MassCapacity, "SplitCaravanMassUsageTooltip".Translate(), -9999f, true);
            //CaravanUIUtility.DrawDaysWorthOfFoodInfo(new Rect(rect.x, rect.y + 19f, rect.width, rect.height), this.DestDaysWorthOfFood.First, this.DestDaysWorthOfFood.Second, this.EnvironmentAllowsEatingVirtualPlantsNow, true, 3.40282347E+38f);
        }
        
        public static void DrawMassInfo(Rect rect, float usedMass, float availableMass, string tip, float lastMassFlashTime = -9999f, bool alignRight = false)
        {
            if (usedMass > availableMass)
            {
                GUI.color = Color.red;
            }
            else
            {
                GUI.color = Color.gray;
            }
            string text = "MassUsageInfo".Translate(new object[]
            {
                usedMass.ToString("0.##"),
                availableMass.ToString("0.##")
            });
            Vector2 vector = Text.CalcSize(text);
            Rect rect2;
            if (alignRight)
            {
                rect2 = new Rect(rect.xMax - vector.x, rect.y, vector.x, vector.y);
            }
            else
            {
                rect2 = new Rect(rect.x, rect.y, vector.x, vector.y);
            }
            bool flag = Time.time - lastMassFlashTime < 1f;
            if (flag)
            {
                GUI.DrawTexture(rect2, TransferableUIUtility.FlashTex);
            }
            Widgets.Label(rect2, text);
            TooltipHandler.TipRegion(rect2, tip);
            GUI.color = Color.white;
        }


        private void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - this.BottomButtonSize.x / 2f, rect.height - 55f, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true))
            {/*
                var cachedCurrencyTradeable = (from x in TradeSession.deal.AllTradeables
                                               where x.IsCurrency
                                               select x).FirstOrDefault<Tradeable>();
                var outText = DevelopTest.TextObj(cachedCurrencyTradeable, true);
                File.WriteAllText(Loger.PathLog + @"Car.txt", outText, Encoding.UTF8);
                */
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);

                IsCancel = false;
                this.Close(false);
            }
            Rect rect3 = new Rect(rect2.x - 10f - this.BottomButtonSize.x, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                this.CalculateAndRecacheTransferables();
            }
            Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                IsCancel = true;
                this.Close(true);
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            this.transferables = GameUtils.DistinctThings(AllItem);
                // Faction.OfPlayer.Name; - "Поселение"
                // WorldObjectDefOf.Caravan.LabelCap - "Караван"
            CreateCaravanTransferableWidgets(this.transferables
                , out this.itemsTransfer
                , WorldObjectDefOf.Caravan.LabelCap
                , WhoName //WorldObjectDefOf.Caravan.LabelCap
                , "FormCaravanColonyThingCountTip".Translate()
                , IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload
                , () => this.MassCapacity - this.MassUsage
                , false);
            this.CountToTransferChanged();
        }
        
        public static void CreateCaravanTransferableWidgets(List<TransferableOneWay> transferables
            , out TransferableOneWayWidget itemsTransfer, string sourceLabel, string destLabel, string thingCountTip
            , IgnorePawnsInventoryMode ignorePawnInventoryMass, Func<float> availableMassGetter, bool ignoreCorpsesGearAndInventoryMass)
        {
            //метод из CaravanUIUtility с удалением лишнего
            itemsTransfer = new TransferableOneWayWidget(from x in transferables
                                                         //where x.ThingDef.category != ThingCategory.Pawn
                                                         select x, sourceLabel, destLabel, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 24f, ignoreCorpsesGearAndInventoryMass);
        }
        
        private void CountToTransferChanged()
        {
            this.massUsageDirty = true;
            //this.massCapacityDirty = true;
            //this.daysWorthOfFoodDirty = true;
        }
        
    }
}
