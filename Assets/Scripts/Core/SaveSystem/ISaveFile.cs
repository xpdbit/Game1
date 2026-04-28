using System.Xml;

namespace Game1
{
    /// <summary>
    /// 职能存档文件接口
    /// 每个实现类对应一个独立的职能XML文件
    /// </summary>
    public interface ISaveFile
    {
        /// <summary>
        /// 文件名（例如 "player.xml", "world.xml"）
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// 文件的版本号（用于版本迁移）
        /// </summary>
        int Version { get; }

        /// <summary>
        /// 序列化为XML字符串
        /// </summary>
        string ToXml();

        /// <summary>
        /// 从XML元素解析
        /// </summary>
        void ParseFromXml(XmlElement element);
    }
}
