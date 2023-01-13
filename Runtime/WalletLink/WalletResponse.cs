namespace DBGames.UI.Wallet {

    public struct WalletResponse {

        public byte[] PublicKey { get; set; }
        public byte[] MsgSig { get; set; }
        public byte[] Message { get; set; }
    }
}
