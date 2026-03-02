using System.Text;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Common.Reporting
{
    public static class ExistingFactions
    {
		public static StringBuilder Report(StringBuilder sb)
		{
            sb.AppendLine();
            sb.AppendFormat("{0, -2}Existing Factions", " ");
            sb.AppendLine("__________________________________________________");
            sb.AppendLine();

            // SteamId > 0 denotes player; no reason to see / save their ID though
            sb.AppendFormat("{0, -4}[FactionId][Tag][IsEveryoneNpc] Display Name\n", " ");
            sb.AppendLine();

            foreach (var faction in MyAPIGateway.Session.Factions.Factions)
            {
                sb.AppendFormat("{0, -4}[{1:D18}][{2}][{3}] {4}\n", " ",
                    faction.Key, faction.Value.Tag, faction.Value.IsEveryoneNpc(), faction.Value.Name);
            }

            sb.AppendLine();
            return sb;
		}
	}
}
