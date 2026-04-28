using System.Xml;

namespace Game1
{
    public sealed class PetSaveFile : ISaveFile
    {
        public string FileName => "pet.xml";
        public int Version => 1;

        public float happiness;
        public float excitement;
        public float sadness;
        public string currentState;
        public bool isUnlocked;

        public string ToXml()
        {
            return $"<PetSaveFile><happiness>{happiness}</happiness><excitement>{excitement}</excitement><sadness>{sadness}</sadness><currentState>{XmlEscape.EscapeXml(currentState)}</currentState><isUnlocked>{isUnlocked}</isUnlocked></PetSaveFile>";
        }

        public void ParseFromXml(XmlElement element)
        {
            if (element == null) return;

            var happinessNode = element.SelectSingleNode("happiness");
            happiness = happinessNode != null ? float.Parse(happinessNode.InnerText) : 0f;

            var excitementNode = element.SelectSingleNode("excitement");
            excitement = excitementNode != null ? float.Parse(excitementNode.InnerText) : 0f;

            var sadnessNode = element.SelectSingleNode("sadness");
            sadness = sadnessNode != null ? float.Parse(sadnessNode.InnerText) : 0f;

            var currentStateNode = element.SelectSingleNode("currentState");
            currentState = currentStateNode != null ? currentStateNode.InnerText : "Idle";

            var isUnlockedNode = element.SelectSingleNode("isUnlocked");
            isUnlocked = isUnlockedNode != null ? bool.Parse(isUnlockedNode.InnerText) : false;
        }
    }
}
