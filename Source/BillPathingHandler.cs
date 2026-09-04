using System.Linq;
using Verse.AI;

namespace ServiceRush
{
	public static class BillPathingHandler
	{
		private static readonly Dictionary<Pawn, BillPathingData> data = [];
		private static readonly List<IntVec3> tempFoundCells = [];


		public static void TryInteractWithRoom(Toil toil)
		{
			if (toil.actor.GetRoom().OutdoorsForWork)
			{
				return;
			}


			data.TryAdd(toil.actor, new());
			// Should only path once, unless it can't find a cell, then it runs this once more
			if (data.TryGetValue(toil.actor).waitTicksLeft <= 0)
			{
				data[toil.actor].Reset();
				data[toil.actor].PickPathingTarget();
				data[toil.actor].AssignWaitTime();

				IntVec3 potentialCell = TryGetPathingCell(toil);
				if (potentialCell == IntVec3.Invalid)
				{
					data[toil.actor].Reset();
					return;
				}

				data[toil.actor].cellTarget = potentialCell;
				toil.actor.pather.StartPath(potentialCell, PathEndMode.OnCell);

				toil.handlingFacing = false;
				return;
			}

			// Wait while looking at the target
			if (toil.actor.Position == data.TryGetValue(toil.actor).cellTarget && data.TryGetValue(toil.actor).waitTicksLeft > 0)
			{
				toil.handlingFacing = true;
				data[toil.actor].waitTicksLeft--;
				if (data[toil.actor].thingLook is null)
				{
					data[toil.actor].thingLook = TryGetRandomBuildingToLookAt(toil);
				}
				else
				{
					toil.actor.rotationTracker.FaceTarget(data[toil.actor].thingLook);
				}
				return;
			}
		}


		private static bool IsCellWalkable(IntVec3 cell, Pawn pawn)
		{
			return cell.WalkableBy(pawn.Map, pawn)
					&& !cell.Filled(pawn.Map);
		}


		private static Building TryGetRandomBuildingToLookAt(Toil toil)
		{
			int iter = 0;
			do
			{
				var tempCell = toil.actor.RandomAdjacentCellCardinal();
				Building building = tempCell.GetFirstBuilding(toil.actor.Map);
				if (IsBuildingValid(toil, building))
					return building;

				iter++;
			}
			while (iter < 10);

			//Log.Message("Not found?");
			return null;
		}


		private static Building TryGetPathingTarget(Toil toil)
		{
			return data[toil.actor].pathingTarget switch
			{
				BillPathingData.BillPathingTarget.BillGiver => toil.GetActor().CurJob.targetA.Thing as Building,
				BillPathingData.BillPathingTarget.Linkable or BillPathingData.BillPathingTarget.Storage => GetAllValidBuildingsInRoom(toil).RandomElement(),
				_ => null,
			};
		}


		private static IntVec3 TryGetPathingCell(Toil toil)
		{
			IntVec3 cell = IntVec3.Invalid;
			int iter = 0;

			var pickedBuilding = TryGetPathingTarget(toil);
			if (pickedBuilding is null)
				return cell;

			do
			{
				IntVec3 tempCell = IntVec3.Zero;
				switch (data[toil.actor].pathingTarget)
				{
					case BillPathingData.BillPathingTarget.BillGiver:
						tempCell = GetInteractionCellOrCellsFacingRot(pickedBuilding).RandomElement();
						break;

					case BillPathingData.BillPathingTarget.Linkable:
					case BillPathingData.BillPathingTarget.Storage:
						tempCell = pickedBuilding.RandomAdjacentCellCardinal();
						break;
				}
				if (tempCell.GetRoom(pickedBuilding.Map) == pickedBuilding.GetRoom()
					&& IsCellWalkable(tempCell, toil.actor))
				{
					cell = tempCell;
					break;
				}

				iter++;
			}
			while (iter < 10 && cell == IntVec3.Invalid);

			return cell;
		}


		private static List<IntVec3> GetInteractionCellOrCellsFacingRot(Thing building)
		{
			tempFoundCells.Clear();
			if (TryGetInteractionCellIfChairPresent(building, out IntVec3 foundCell))
			{
				tempFoundCells.Add(foundCell);
				return tempFoundCells;
			}

			var occupiedCells = GenAdj.CellsOccupiedBy(building).ToList();
			for (int i = 0; i < occupiedCells.Count; i++)
			{
				tempFoundCells.Add(occupiedCells[i] + CellOffsetByRotation(building.Rotation));
			}

			return tempFoundCells;
		}


		private static bool TryGetInteractionCellIfChairPresent(Thing building, out IntVec3 foundCell)
		{
			foundCell = IntVec3.Invalid;
			Building chair = building.InteractionCell.GetEdifice(building.Map);
			if (chair is not null && chair.def.category == ThingCategory.Building && chair.def.building.isSittable)
			{
				foundCell = chair.Position;
				return true;
			}
			return false;
		}


		private static IntVec3 CellOffsetByRotation(Rot4 rotation)
		{
			if (rotation == Rot4.South)
				return IntVec3.North;
			else if (rotation == Rot4.North)
				return IntVec3.South;
			else if (rotation == Rot4.East)
				return IntVec3.West;
			else if (rotation == Rot4.West)
				return IntVec3.East;
			else
				return IntVec3.Zero;
		}


		private static List<Building> GetAllValidBuildingsInRoom(Toil toil)
		{
			List<Building> buildings = [];
			foreach (var roomCells in toil.actor.GetRoom().Cells)
			{
				Building building = roomCells.GetFirstBuilding(toil.actor.Map);
				if (building is not null
					&& IsBuildingValid(toil, building))
				{
					buildings.AddDistinct(building);
				}
			}
			return buildings;
		}


		private static bool IsBuildingValid(Toil toil, Building otherBuilding)
		{
			Building billGiver = toil.GetActor().CurJob.targetA.Thing as Building;

			return data[toil.actor].pathingTarget switch
			{
				BillPathingData.BillPathingTarget.BillGiver => otherBuilding == billGiver,
				BillPathingData.BillPathingTarget.Linkable => billGiver.TryGetComp<CompAffectedByFacilities>() is CompAffectedByFacilities comp
					&& comp.LinkedFacilitiesListForReading.Contains(otherBuilding),
				BillPathingData.BillPathingTarget.Storage or BillPathingData.BillPathingTarget.Storage => otherBuilding is Building_Storage,
				_ => false,
			};
		}
	}
}
