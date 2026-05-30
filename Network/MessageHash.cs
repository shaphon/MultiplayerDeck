namespace MultiplayerDeck.Network
{
    /// <summary>
    /// FNV-1a 确定性哈希。用于从类名生成稳定的网络消息 ID。
    /// 同一个字符串永远产生相同的 hash，不依赖 .NET 版本或进程重启。
    /// </summary>
    public static class MessageHash
    {
        public static int Compute(string id)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in id)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }
    }
}
