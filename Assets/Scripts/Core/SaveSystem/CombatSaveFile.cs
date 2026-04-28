using System.Xml;

namespace Game1
{
    public sealed class CombatSaveFile : ISaveFile
    {
        public string FileName => "combat.xml";
        public int Version => 1;

        public int totalBattles;
        public int victories;
        public int defeats;
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int totalGoldEarned;

        public string ToXml()
        {
            return $"<CombatSaveFile><totalBattles>{totalBattles}</totalBattles><victories>{victories}</victories><defeats>{defeats}</defeats><totalDamageDealt>{totalDamageDealt}</totalDamageDealt><totalDamageTaken>{totalDamageTaken}</totalDamageTaken><totalGoldEarned>{totalGoldEarned}</totalGoldEarned></CombatSaveFile>";
        }

        public void ParseFromXml(XmlElement element)
        {
            totalBattles = int.Parse(element.SelectSingleNode("totalBattles")?.InnerText ?? "0");
            victories = int.Parse(element.SelectSingleNode("victories")?.InnerText ?? "0");
            defeats = int.Parse(element.SelectSingleNode("defeats")?.InnerText ?? "0");
            totalDamageDealt = int.Parse(element.SelectSingleNode("totalDamageDealt")?.InnerText ?? "0");
            totalDamageTaken = int.Parse(element.SelectSingleNode("totalDamageTaken")?.InnerText ?? "0");
            totalGoldEarned = int.Parse(element.SelectSingleNode("totalGoldEarned")?.InnerText ?? "0");
        }
    }
}