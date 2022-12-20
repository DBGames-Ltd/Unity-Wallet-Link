using UnityEditor;
using UnityEngine;

namespace DBGames.UI.Wallet.Editor {

    [CustomEditor(typeof(WalletLink))]
    public class WalletLinkEditor : UnityEditor.Editor {

        #region Properties

        WalletLink walletLink;

        #endregion

        #region Editor

        public override void OnInspectorGUI() {
            Texture2D banner = Resources.Load<Texture2D>("DBGamesLogo");
            if (GUILayout.Button(new GUIContent(banner))) {
                Application.OpenURL("https://github.com/DBGames-Ltd/Unity-Wallet-Link");
            }
            base.OnInspectorGUI();
        }

        #endregion

        #region Unity Events

        private void OnEnable() {
            walletLink = (WalletLink)target;
        }

        #endregion
    }
}
