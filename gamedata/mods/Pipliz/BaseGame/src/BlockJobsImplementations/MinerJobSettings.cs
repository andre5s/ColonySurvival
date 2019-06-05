﻿using BlockTypes;
using Jobs;
using NPC;
using System.Collections.Generic;
using UnityEngine;
using static NPC.NPCBase;

namespace Pipliz.Mods.BaseGame
{
	public class MinerJobSettings : IBlockJobSettings
	{
		public virtual ItemTypes.ItemType[] BlockTypes { get; set; }
		public virtual NPCType NPCType { get; set; }
		public virtual string NPCTypeKey { get; set; }
		public virtual InventoryItem RecruitmentItem { get; set; }
		public virtual string OnCraftedAudio { get; set; }
		public int MaxCraftsPerRun { get; set; }
		public float NPCShopGameHourMinimum { get { return TimeCycle.Settings.SleepTimeEnd; } }
		public float NPCShopGameHourMaximum { get { return TimeCycle.Settings.SleepTimeStart; } }

		public virtual bool ToSleep { get { return TimeCycle.ShouldSleep; } }

		// buffer for onnpcgathered npc code should be threadsafe
		static protected List<ItemTypes.ItemTypeDrops> GatherResults = new List<ItemTypes.ItemTypeDrops>();

		public MinerJobSettings ()
		{
			BlockTypes = new ItemTypes.ItemType[] {
				BuiltinBlocks.Types.minerjob,
				BuiltinBlocks.Types.minerjobxp,
				BuiltinBlocks.Types.minerjobxn,
				BuiltinBlocks.Types.minerjobzp,
				BuiltinBlocks.Types.minerjobzn
			};
			NPCTypeKey = "pipliz.minerjob";
			NPCType = NPCType.GetByKeyNameOrDefault(NPCTypeKey);
			OnCraftedAudio = "stoneDelete";
			MaxCraftsPerRun = 5;
		}

		public virtual void OnGoalChanged (BlockJobInstance instance, NPCGoal oldGoal, NPCGoal newGoal) { }

		public virtual Vector3Int GetJobLocation (BlockJobInstance instance)
		{
			return instance.Position;
		}

		public virtual void OnNPCAtJob (BlockJobInstance blockJobInstance, ref NPCState state)
		{
			MinerJobInstance instance = (MinerJobInstance)blockJobInstance;
			if (BlockTypes.ContainsByReference(instance.BlockType, out int index)) {
				Vector3 rotate = instance.NPC.Position.Vector;
				switch (index) {
					case 1:
						rotate.x += 1f;
						break;
					case 2:
						rotate.x -= 1f;
						break;
					case 3:
						rotate.z += 1f;
						break;
					case 4:
						rotate.z -= 1f;
						break;
				}
				instance.NPC.LookAt(rotate);
			}

			AudioManager.SendAudio(instance.Position.Vector, "stoneDelete");

			GatherResults.Clear();
			var itemList =  instance.BlockTypeBelow.OnRemoveItems;
			for (int i = 0; i < itemList.Count; i++) {
				GatherResults.Add(itemList[i]);
			}

			ModLoader.Callbacks.OnNPCGathered.Invoke(instance, instance.Position.Add(0, -1, 0), GatherResults);

			InventoryItem toShow = ItemTypes.ItemTypeDrops.GetWeightedRandom(GatherResults);
			if (toShow.Amount > 0) {
				state.SetIndicator(new Shared.IndicatorState(instance.MiningCooldown, toShow.Type));
			} else {
				state.SetCooldown(instance.MiningCooldown);
			}

			state.Inventory.Add(GatherResults);
			state.JobIsDone = true;

			instance.GatheredItemCount++;
			if (instance.GatheredItemCount >= MaxCraftsPerRun) {
				instance.ShouldTakeItems = true;
			}
		}

		public virtual void OnNPCAtStockpile (BlockJobInstance blockJobInstance, ref NPCState state)
		{
			MinerJobInstance inst = (MinerJobInstance)blockJobInstance;
			inst.ShouldTakeItems = false;
			inst.GatheredItemCount = 0;
			state.Inventory.Dump(blockJobInstance.Owner.Stockpile);
			state.SetCooldown(0.3);
			state.JobIsDone = true;
		}
	}
}