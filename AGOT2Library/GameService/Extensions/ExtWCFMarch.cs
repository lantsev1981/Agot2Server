namespace GameService
{
    internal static class ExtWCFMarch
    {
        internal static March ToMarch(this WCFMarch o, Step step)
        {
            March result = new March
            {
                Step1 = step,
                Step = step.Id,
                SourceOrder = o.SourceOrder,
                IsTerrainHold = o.IsTerrainHold
            };

            foreach (WCFMarchUnit item in o.MarchUnit)
                result.MarchUnit.Add(item.ToMarchUnit(result));

            return result;
        }
    }
}
