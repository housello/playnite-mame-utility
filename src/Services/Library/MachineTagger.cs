using MAMEUtility.Models;
using MAMEUtility.Services.Engine.Platforms;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MAMEUtility.Services.Engine.Library
{
    /// <summary>
    /// Recalculates the managed Machine: tags for the selected Playnite games.
    /// This action only uses MAME machine metadata and does not change game names,
    /// platforms, categories, ROM paths, or unrelated tags.
    /// </summary>
    class MachineTagger
    {
        private const string MachineTagPrefix = "Machine: ";

        public static void tagSelectedGames()
        {
            if (MachinesService.getSourceFormat() != SourceFormat.MAME)
            {
                UI.UIService.showError(
                    "MAME data required",
                    "The 'Tag machines' action requires MAME machine data. Select MAME as the source in MAME Utility settings.");
                return;
            }

            int taggedCount = 0;

            UI.UIService.showSelectedGamesProgress("Recalculating machine tags", (game, machines, progressArgs) =>
            {
                // Remove only tags managed by this action, so each run recalculates them from scratch.
                if (game.TagIds == null)
                    game.TagIds = new List<Guid>();

                Guid[] oldMachineTagIds = MAMEUtilityPlugin.playniteAPI.Database.Tags
                    .Where(tag => tag.Name != null && tag.Name.StartsWith(MachineTagPrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(tag => tag.Id)
                    .ToArray();

                int removed = game.TagIds.RemoveAll(id => oldMachineTagIds.Contains(id));

                string machineTag = findMachineTagForGame(machines, game);
                if (!string.IsNullOrEmpty(machineTag))
                {
                    game.TagIds.AddMissing<Guid>(getTagId(machineTag));
                    taggedCount++;
                }

                if (removed > 0 || !string.IsNullOrEmpty(machineTag))
                    MAMEUtilityPlugin.playniteAPI.Database.Games.Update(game);
            });

            UI.UIService.showMessage($"Machine tags recalculated. {taggedCount} selected games received a machine tag.");
        }

        private static string findMachineTagForGame(Dictionary<string, RomsetMachine> machines, Game game)
        {
            // First use the same exact ROM-name/path matching as the existing MAME Utility features.
            RomsetMachine machine = MachinesService.findMachineByPlayniteGame(machines, game);
            if (machine != null)
                return getMachineTag(machine);

            // RomM or another library importer can provide a title without a local ROM path.
            // In that case, try an exact match against the MAME XML description.
            if (string.IsNullOrWhiteSpace(game.Name))
                return null;

            List<RomsetMachine> titleMatches = machines.Values
                .Where(item => string.Equals(item.description, game.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (titleMatches.Count == 0)
                return null;

            // Duplicate MAME descriptions are safe only when all matching entries map to the same tag.
            List<string> possibleTags = titleMatches
                .Select(getMachineTag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return possibleTags.Count == 1 ? possibleTags[0] : null;
        }

        private static string getMachineTag(RomsetMachine machine)
        {
            if (machine == null || machine.isBios || machine.isDevice || string.IsNullOrWhiteSpace(machine.sourceFile))
                return null;

            string sourceFile = machine.sourceFile.Replace('\\', '/');
            sourceFile = sourceFile.Substring(sourceFile.LastIndexOf('/') + 1);
            string driver = Path.GetFileNameWithoutExtension(sourceFile).ToLowerInvariant();
            string romOf = (machine.romOf ?? string.Empty).Trim().ToLowerInvariant();

            switch (driver)
            {
                case "naomi":
                    // Naomi, Naomi 2 and Atomiswave share a MAME driver. romof distinguishes Atomiswave.
                    if (romOf == "awbios")
                        return MachineTagPrefix + "Sammy Atomiswave";
                    return MachineTagPrefix + "Sega Naomi / Naomi 2";

                case "hikaru":
                    return MachineTagPrefix + "Sega Hikaru";
                case "cps1":
                    return MachineTagPrefix + "Capcom CPS-1";
                case "cps2":
                    return MachineTagPrefix + "Capcom CPS-2";
                case "cps3":
                    return MachineTagPrefix + "Capcom CPS-3";
                case "model2":
                    return MachineTagPrefix + "Sega Model 2";
                case "model3":
                    return MachineTagPrefix + "Sega Model 3";

                // Shared tag for PlayStation-derived arcade hardware: Namco System 11/12,
                // Sony ZN-1/ZN-2 and Taito G-NET.
                case "namcos11":
                case "namcos12":
                case "zn":
                case "taitogn":
                    return MachineTagPrefix + "PlayStation-based Arcade";

                default:
                    return null;
            }
        }

        private static Guid getTagId(string tagName)
        {
            foreach (var tag in MAMEUtilityPlugin.playniteAPI.Database.Tags)
            {
                if (string.Equals(tag.Name, tagName, StringComparison.OrdinalIgnoreCase))
                    return tag.Id;
            }

            var newTag = new Playnite.SDK.Models.Tag(tagName);
            MAMEUtilityPlugin.playniteAPI.Database.Tags.Add(newTag);
            return newTag.Id;
        }
    }
}
