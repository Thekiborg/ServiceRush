using Verse.AI;

namespace ServiceRush
{
	public static class BillPathingHandler
	{
		private static readonly Dictionary<Pawn, BillPathingData> data = [];


		public static void TryInteractWithRoom(Toil toil)
		{
			data.TryAdd(toil.actor, new());
			if (toil.actor.pather.Moving)
			{
				data[toil.actor].Reset();
				data[toil.actor].AssignWaitTime();
				toil.handlingFacing = false;
				return;
			}

			// Wait while looking at the target
			if (!toil.actor.pather.Moving && data.TryGetValue(toil.actor).waitTicksLeft > 0)
			{
				toil.handlingFacing = true;
				data[toil.actor].waitTicksLeft--;
				if (data[toil.actor].thingLook is null)
				{
					Log.Message("No thinglook");
					data[toil.actor].thingLook = TryGetRandomBuildingToLookAt(toil);
				}
				else
				{
					toil.actor.rotationTracker.FaceTarget(data[toil.actor].thingLook);
				}
				return;
			}

			if (toil.actor.GetRoom().OutdoorsForWork)
			{
				return;
			}

			data[toil.actor].PickPathingTarget();
			IntVec3 cell = TryGetRandomCell(toil);

			if (cell == IntVec3.Invalid)
			{
				data[toil.actor].Reset();
				return;
			}
			toil.actor.pather.StartPath(cell, PathEndMode.OnCell);
		}


		private static bool IsCellWalkable(IntVec3 cell, Pawn pawn)
		{
			return cell.WalkableBy(pawn.Map, pawn)
					&& !cell.Filled(pawn.Map)
					&& cell.GetFirstBuilding(pawn.Map) is null;
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

			Log.Message("Not found?");
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


		private static IntVec3 TryGetRandomCell(Toil toil)
		{
			IntVec3 cell = IntVec3.Invalid;
			int iter = 0;

			var pickedBuilding = TryGetPathingTarget(toil);
			if (pickedBuilding is null)
				return cell;

			do
			{
				var tempCell = pickedBuilding.RandomAdjacentCellCardinal();
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
