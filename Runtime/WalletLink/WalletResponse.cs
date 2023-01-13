using Chaos.NaCl;
using Newtonsoft.Json;
using Solana.Unity.Wallet.Utilities;
using System;

namespace DBGames.UI.Wallet {

    [JsonObject(MemberSerialization.OptOut)]
    public class WalletResponse {

        public byte[] PublicKey { get; set; }
        public byte[] MsgSig { get; set; }
        public byte[] Message { get; set; }

        [JsonIgnore]
        public string PublicKeyBase58 {
            get {
                return Encoders.Base58.EncodeData(PublicKey);
            }
        }

        [JsonIgnore]
        public bool IsVerified {
            get {
                try {
                    return Ed25519.Verify(MsgSig, Message, PublicKey);
                } catch (ArgumentException) {
                    return false;
                }
            }
        }
    }
}
