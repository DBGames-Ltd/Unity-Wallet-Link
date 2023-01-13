using Newtonsoft.Json;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Events;

namespace DBGames.UI.Wallet {

    /// <summary>
    /// Provides an external browser interface for the user to authenticate ownership of a wallet
    /// using Phantom.
    /// </summary>
    public class WalletLink : MonoBehaviour {

        #region Types

        struct WalletResponse {

            [JsonProperty]
            internal string publicKey;
        }

        #endregion

        #region Properties

        // TODO: Discuss whether we should point to a generic DB Games x Phantom Auth app, or
        // provide a skeleton React App alongside this package.
        [SerializeField]
        [Tooltip("The URL to expect an authorization response from.")]
        private string authURL = "http://localhost:3000/";//"https://auth.re-evolution.io";

        [SerializeField]
        [Tooltip("If true, wallet connection events are logged in the console.")]
        private bool useLogging;

        [SerializeField]
        [Tooltip("Called when the user's wallet address is received, must be set as a dynamic parameter.")]
        private UnityEvent<string> onWalletReceived;

        private WalletAuthenticator authenticator;

        #endregion

        #region Public

        /// <summary>
        /// Triggers the <see cref="WalletAuthenticator"/> to begin listening for a response, and
        /// executes <see cref="onWalletReceived"/> if a public key is received.
        /// </summary>
        public async void AuthenticateWallet() {
            string publicKey = await authenticator.ListenForWalletResponse(port => {
                OpenAuthenticator(port);
            });
            if (publicKey != null) {
                onWalletReceived?.Invoke(publicKey);
            }
        }

        #endregion

        #region Unity Events

        void Awake() {
            authenticator = new WalletAuthenticator(useLogging, authURL);
        }

        #endregion

        #region Private

        private void OpenAuthenticator(string port) {
            string appURL = string.Format(
                "{0}?{1}={2}", 
                authURL, 
                nameof(port),
                port
            );
            Application.OpenURL(appURL);
            if (useLogging) {
                Debug.Log("Authenticator started.");
            }
        }

        #endregion

    }
}
