namespace ServiceRush
{
	public class BillPathingData
	{
		public enum BillPathingTarget
		{
			None,
			BillGiver,
			Linkable,
			Storage
		}

		private const int PickBillGiverWeight = 6;
		private const int PickLinkableWeight = 3;
		private const int PickStorageWeight = 1;

		public readonly IntRange BillGiverWaitTime = new(200, 250);
		public readonly IntRange LinkableWaitTime = new(100, 200);
		public readonly IntRange StorageWaitTime = new(20, 50);

		public int waitTicksLeft;
		public Building thingLook;
		public BillPathingTarget pathingTarget;


		public void PickPathingTarget()
		{
			int total = PickBillGiverWeight + PickLinkableWeight + PickStorageWeight;
			int rand = Rand.Range(0, total);
			int run = 0;

			if (EvaluateWeight(PickBillGiverWeight))
			{
				pathingTarget = BillPathingTarget.BillGiver;
				return;
			}

			if (EvaluateWeight(PickLinkableWeight))
			{
				pathingTarget = BillPathingTarget.Linkable;
				return;
			}

			if (EvaluateWeight(PickStorageWeight))
			{
				pathingTarget = BillPathingTarget.Storage;
				return;
			}

			Log.Error("Bill target pulled impossible result");
			pathingTarget = BillPathingTarget.None;

			bool EvaluateWeight(int weight)
			{
				run += weight;
				return rand < run;
			}
		}


		public void AssignWaitTime()
		{
			switch (pathingTarget)
			{
				case BillPathingTarget.BillGiver:
					waitTicksLeft = BillGiverWaitTime.RandomInRange;
					break;
				case BillPathingTarget.Linkable:
					waitTicksLeft = LinkableWaitTime.RandomInRange;
					break;
				case BillPathingTarget.Storage:
					waitTicksLeft = StorageWaitTime.RandomInRange;
					break;
			}
		}


		public void Reset()
		{
			waitTicksLeft = 0;
			thingLook = null;
		}
	}
}
