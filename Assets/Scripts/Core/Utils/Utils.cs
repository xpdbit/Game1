namespace Game1
{
  public static class Utils
  {
    /// <summary>
    /// 从 ID 中获取指定部分，部分之间用 '.' 分隔，部分索引从 0 开始。
    /// 如果 partLength 小于 0 则获取剩余所有部分。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="partIndex"></param>
    /// <param name="partLength"></param>
    /// <returns></returns>
    public static string GetIDPart(string id, int partIndex, int partLength = 1)
    {
      var parts = id.Split('.');
      if (partIndex < 0 || partIndex >= parts.Length)
      {
        return string.Empty;
      }
      if (partLength < 0)
      {
        partLength = parts.Length - partIndex;
      }
      return string.Join(".", parts, partIndex, partLength);
    }
  }
}