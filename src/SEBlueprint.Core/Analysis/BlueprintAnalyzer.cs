using SEBlueprint.Core.Blueprints;
using SEBlueprint.Core.Data;

namespace SEBlueprint.Core.Analysis;

public static class BlueprintAnalyzer
{
    public static BlueprintReport Analyze(BlueprintData bp, GameDatabase db, CostCalculator? cost = null)
    {
        var mods = bp.Mods.Select(m => m.Id).ToList();
        var r = new BlueprintReport { Name = bp.Name };
        r.Mods.AddRange(bp.Mods);
        r.MissingMods.AddRange(mods.Where(id => !db.ModNames.ContainsKey(id)).Distinct());

        var thrust = new Dictionary<string, double>();
        var jumpDrives = new List<BlockDefinition>();

        foreach (var grid in bp.Grids)
        {
            int gridBlocks = 0, gridPcu = 0;
            foreach (var block in grid.Blocks)
            {
                var def = db.Resolve(block.Id, mods, out var unlisted);
                if (def == null)
                {
                    r.UnknownBlocks[block.Id] = r.UnknownBlocks.GetValueOrDefault(block.Id) + 1;
                    gridBlocks++;
                    var unknownType = block.Id.Split('/')[0];
                    r.BlockPairCounts[unknownType] = r.BlockPairCounts.GetValueOrDefault(unknownType) + 1;
                    continue;
                }
                if (def.ModId != null) r.UsesMods = true;
                if (unlisted) r.BlocksFromUnlistedMods++;

                gridBlocks++;
                gridPcu += def.Pcu;
                var pair = def.PairName ?? def.TypeId;
                r.BlockPairCounts[pair] = r.BlockPairCounts.GetValueOrDefault(pair) + 1;

                foreach (var (comp, count) in def.Components)
                {
                    r.Components[comp] = r.Components.GetValueOrDefault(comp) + count;
                    r.MassKg += (db.ResolveItem(comp, mods)?.Mass ?? 0) * count;
                }

                switch (def.Category)
                {
                    case BlockCategory.Turret: r.Turrets++; break;
                    case BlockCategory.FixedWeapon: r.FixedWeapons++; break;
                    case BlockCategory.Cargo: r.CargoContainers++; break;
                    case BlockCategory.Thruster: r.Thrusters++; break;
                    case BlockCategory.JumpDrive: jumpDrives.Add(def); break;
                }
                r.CargoLiters += def.CargoLiters;
                if (def.ThrustForce > 0)
                    thrust[block.Forward] = thrust.GetValueOrDefault(block.Forward) + def.ThrustForce * def.ThrustSpaceEff;
                r.PowerOutputMW += def.PowerOutput;
                r.PowerStorageMWh += def.PowerStorage;
            }
            r.Blocks += gridBlocks;
            r.Pcu += gridPcu;
            r.Grids.Add(new GridSummary { Name = grid.Name, GridSize = grid.GridSize, Blocks = gridBlocks, Pcu = gridPcu });
        }

        r.MaxThrustN = thrust.Values.DefaultIfEmpty(0).Max();
        r.JumpDrives = jumpDrives.Count;
        r.JumpRangeKm = jumpDrives.Sum(d =>
            r.MassKg <= 0 || r.MassKg <= d.JumpMaxMass ? d.JumpDistance : d.JumpDistance * d.JumpMaxMass / r.MassKg);

        var (ingots, ore) = (cost ?? new CostCalculator()).Compute(r.Components, db, mods);
        r.Ingots = ingots;
        r.Ore = ore;
        return r;
    }
}
