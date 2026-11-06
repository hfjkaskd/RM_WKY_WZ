namespace Whitebox.Foundation
{
    // Prices/AB catalog selection are supplied by their original owner; this
    // is the native package data model, not a replacement hardcoded catalog.
    public sealed class OriginalEgoPackage
    {
        public int CoinPrice { get; set; }
        public byte Moves { get; set; }
        public byte DifferentRewardCount { get; }
        public int FirstItemType { get; }
        public int SecondItemType { get; }
        private readonly byte[] specialItems;
        public OriginalEgoPackage(int coinPrice, byte moves, byte rockets = 0, byte tnt = 0,
            byte lightballs = 0, int firstItemType = 0, int secondItemType = 0) // 0x1e2a1ac.
        {
            CoinPrice = coinPrice; Moves = moves;
            specialItems = new[] { rockets, tnt, lightballs };
            DifferentRewardCount = unchecked((byte)(rockets + tnt + lightballs + 1));
            FirstItemType = firstItemType; SecondItemType = secondItemType;
        }
        public byte GetRocketCount() => specialItems[0];
        public byte GetTntCount() => specialItems[1];
        public byte GetLightballCount() => specialItems[2];
    }
}
