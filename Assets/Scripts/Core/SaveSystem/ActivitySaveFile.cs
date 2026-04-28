using System.Xml;

namespace Game1
{
    public sealed class ActivitySaveFile : ISaveFile
    {
        public string FileName => "activity.xml";
        public int Version => 1;

        public float accumulatedActivity;
        public int displayedActivity;
        public int peakActivity;

        public string ToXml()
        {
            return $"<ActivitySaveFile><accumulatedActivity>{accumulatedActivity}</accumulatedActivity><displayedActivity>{displayedActivity}</displayedActivity><peakActivity>{peakActivity}</peakActivity></ActivitySaveFile>";
        }

        public void ParseFromXml(XmlElement element)
        {
            var accNode = element.SelectSingleNode("accumulatedActivity");
            accumulatedActivity = accNode != null ? float.Parse(accNode.InnerText) : 0f;

            var dispNode = element.SelectSingleNode("displayedActivity");
            displayedActivity = dispNode != null ? int.Parse(dispNode.InnerText) : 0;

            var peakNode = element.SelectSingleNode("peakActivity");
            peakActivity = peakNode != null ? int.Parse(peakNode.InnerText) : 0;
        }
    }
}