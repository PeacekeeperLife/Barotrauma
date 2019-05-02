﻿using Barotrauma.Items.Components;
using FarseerPhysics;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace Barotrauma
{
    class AIObjectiveFixLeak : AIObjective
    {
        public override string DebugTag => "fix leak";

        public override bool KeepDivingGearOn => true;
        public override bool ForceRun => true;

        private readonly Gap leak;

        private AIObjectiveFindDivingGear findDivingGear;
        private AIObjectiveGoTo gotoObjective;
        private AIObjectiveOperateItem operateObjective;
        
        public Gap Leak
        {
            get { return leak; }
        }

        public AIObjectiveFixLeak(Gap leak, Character character, AIObjectiveManager objectiveManager, float priorityModifier = 1) : base (character, objectiveManager, priorityModifier)
        {
            this.leak = leak;
        }

        public override bool IsCompleted()
        {
            return leak.Open <= 0.0f || leak.Removed;
        }

        public override bool CanBeCompleted => !abandon && base.CanBeCompleted;

        public override float GetPriority()
        {
            if (leak.Open == 0.0f) { return 0.0f; }
            // Vertical distance matters more than horizontal (climbing up/down is harder than moving horizontally)
            float dist = Math.Abs(character.WorldPosition.X - leak.WorldPosition.X) + Math.Abs(character.WorldPosition.Y - leak.WorldPosition.Y) * 2.0f;
            float distanceFactor = MathHelper.Lerp(1, 0.25f, MathUtils.InverseLerp(0, 10000, dist));
            float severity = AIObjectiveFixLeaks.GetLeakSeverity(leak);
            float max = Math.Min((AIObjectiveManager.OrderPriority - 1), 90);
            float devotion = Math.Min(Priority, 10) / 100;
            return MathHelper.Lerp(0, max, MathHelper.Clamp(devotion + severity * distanceFactor * PriorityModifier, 0, 1));
        }

        public override bool IsDuplicate(AIObjective otherObjective)
        {
            AIObjectiveFixLeak fixLeak = otherObjective as AIObjectiveFixLeak;
            if (fixLeak == null) return false;
            return fixLeak.leak == leak;
        }

        protected override void Act(float deltaTime)
        {
            if (!leak.IsRoomToRoom)
            {
                if (findDivingGear == null)
                {
                    findDivingGear = new AIObjectiveFindDivingGear(character, true, objectiveManager);
                    AddSubObjective(findDivingGear);
                }
                else if (!findDivingGear.CanBeCompleted)
                {
                    abandon = true;
                    return;
                }
            }

            var weldingTool = character.Inventory.FindItemByTag("weldingtool");

            if (weldingTool == null)
            {
                AddSubObjective(new AIObjectiveGetItem(character, "weldingtool", objectiveManager, true));
                return;
            }
            else
            {
                var containedItems = weldingTool.ContainedItems;
                if (containedItems == null) return;
                
                var fuelTank = containedItems.FirstOrDefault(i => i.HasTag("weldingfueltank") && i.Condition > 0.0f);
                if (fuelTank == null)
                {
                    AddSubObjective(new AIObjectiveContainItem(character, "weldingfueltank", weldingTool.GetComponent<ItemContainer>(), objectiveManager));
                    return;
                }
            }

            var repairTool = weldingTool.GetComponent<RepairTool>();
            if (repairTool == null) { return; }

            Vector2 gapDiff = leak.WorldPosition - character.WorldPosition;

            // TODO: use the collider size/reach?
            if (!character.AnimController.InWater && Math.Abs(gapDiff.X) < 100 && gapDiff.Y < 0.0f && gapDiff.Y > -150)
            {
                HumanAIController.AnimController.Crouching = true;
            }

            float reach = ConvertUnits.ToSimUnits(repairTool.Range);
            bool canReach = ConvertUnits.ToSimUnits(gapDiff.Length()) < reach;
            if (canReach)
            {
                Limb sightLimb = null;
                if (character.Inventory.IsInLimbSlot(repairTool.Item, InvSlotType.RightHand))
                {
                    sightLimb = character.AnimController.GetLimb(LimbType.RightHand);
                }
                else if (character.Inventory.IsInLimbSlot(repairTool.Item, InvSlotType.LeftHand))
                {
                    sightLimb = character.AnimController.GetLimb(LimbType.LeftHand);
                }
                canReach = character.CanSeeTarget(leak, sightLimb);
            }
            else
            {
                if (gotoObjective != null)
                {
                    // Check if the objective is already removed -> completed/impossible
                    if (!subObjectives.Contains(gotoObjective))
                    {
                        if (!gotoObjective.CanBeCompleted)
                        {
                            abandon = true;
                        }
                        gotoObjective = null;
                        return;
                    }
                }
                else
                {
                    gotoObjective = new AIObjectiveGoTo(ConvertUnits.ToSimUnits(GetStandPosition()), character, objectiveManager)
                    {
                        CloseEnough = reach
                    };
                    if (!subObjectives.Contains(gotoObjective))
                    {
                        AddSubObjective(gotoObjective);
                    }
                }
            }
            if (gotoObjective == null || gotoObjective.IsCompleted())
            {
                if (operateObjective == null)
                {
                    operateObjective = new AIObjectiveOperateItem(repairTool, character, objectiveManager, option: "", requireEquip: true, operateTarget: leak);
                    AddSubObjective(operateObjective);
                }
                else if (!subObjectives.Contains(operateObjective))
                {
                    operateObjective = null;
                }
            }   
        }

        private Vector2 GetStandPosition()
        {
            Vector2 standPos = leak.Position;
            var hull = leak.FlowTargetHull;

            if (hull == null) return standPos;
            
            if (leak.IsHorizontal)
            {
                standPos += Vector2.UnitX * Math.Sign(hull.Position.X - leak.Position.X) * leak.Rect.Width;
            }
            else
            {
                standPos += Vector2.UnitY * Math.Sign(hull.Position.Y - leak.Position.Y) * leak.Rect.Height;
            }

            return standPos;            
        }
    }
}
