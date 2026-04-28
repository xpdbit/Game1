using System;

namespace Game1
{
    /// <summary>
    /// XML特殊字符转义辅助类
    /// </summary>
    internal static class XmlEscape
    {
        /// <summary>
        /// XML特殊字符转义
        /// </summary>
        public static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}