using System.Collections.Generic;
using PlantGame.Core;
using PlantGame.Plants;

namespace PlantGame.World
{
    /// <summary>
    /// Checks a generated chain (no side islands) without building anything: islands do not overlap, every gap has a seed source of its kind
    /// within two islands before it, the start island has 2-3 sources, and a can that starts empty always finds water before a gap
    /// (2 charges per refill, one per planted gap). Returns the problems; empty = beatable.
    /// </summary>
    public static class WorldValidator
    {
        public static List<string> Validate(LevelPlan plan, LevelLayout lay)
        {
            var issues = new List<string>(lay.problems);
            int main = plan.MainLinks;
            for (int i = 0; i < plan.links.Count; i++)
            {
                var l = plan.links[i];
                if (i >= main)   // optional side island: leaves from a main island with water, its seed lies on that island or within two before
                {
                    if (l.from < 1 || l.from >= main) issues.Add("side link " + i + " starts on island " + l.from);
                    else if (!plan.collectorOnly && !plan.waterIslands.Contains(l.from)) issues.Add("side link " + i + " leaves from an island without water");
                    bool ss = false;
                    foreach (var s in plan.sources) if (s.item == l.seedItem && s.island <= l.from && s.island >= l.from - 2) ss = true;
                    if (!ss) issues.Add($"no {l.seedItem} for side link {i}");
                    continue;
                }
                if (l.from != i) { issues.Add("chain expected: link " + i + " starts on island " + l.from); continue; }
                bool seed = false;
                foreach (var s in plan.sources) if (s.item == l.seedItem && s.island <= i && s.island >= i - 2) seed = true;
                if (!seed) issues.Add($"no {l.seedItem} within two islands before gap {i}");
            }
            int start = 0; foreach (var s in plan.sources) if (s.island == 0) start++;
            if (start < 2 || start > 3) issues.Add("start island has " + start + " seed sources");
            if (!plan.waterIslands.Contains(0) && !plan.collectorOnly) issues.Add("start island has no water");
            if (!plan.collectorOnly)
            {
                int charges = 0;
                for (int i = 0; i < main; i++)
                {
                    if (plan.waterIslands.Contains(i)) charges = WateringCan.MaxCharges;
                    if (plan.links[i].kind == PlantKind.Glider) continue;   // no plant, no water
                    if (charges <= 0) issues.Add("no water for gap " + i);
                    else charges--;
                }
            }
            return issues;
        }
    }
}
