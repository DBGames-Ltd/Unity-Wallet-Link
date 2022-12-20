using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Cors;
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

        [SerializeField]
        [Tooltip("If true, wallet connection events are logged in the console.")]
        private bool useLogging;

        [SerializeField]
        [Tooltip("Called when the user's wallet address is received, must be set as a dynamic parameter.")]
        private UnityEvent<string> onWalletReceived;

        // TODO: Discuss whether we should point to a generic DB Games x Phantom Auth app, or
        // provide a skeleton React App alongside this package.
        private const string authURL = "https://auth.re-evolution.io";

        /// <summary>
        /// The time in milliseconds that the browser is allowed to cache a preflight request.
        /// </summary>
        private const int preflightTimeout = 120000;

        /// <summary>
        /// The IP used to listen for responses from the authenticator app.
        /// </summary>
        private const string listenerIP = "http://127.0.0.1";

        /// <summary>
        /// Whether the <see cref="HttpListener"/> is currently running or not.
        /// </summary>
        private bool isListening = false;

        #endregion

        #region Public 

        public async void StartListeningForWalletResponse() {
            Application.OpenURL(authURL);
            if (useLogging) {
                Debug.Log("Authenticator Opened.");
            }
            if (!isListening) {
                string publicKey = await ListenForWalletResponse();
                if (publicKey != null) {
                    onWalletReceived?.Invoke(publicKey);
                }
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Starts listening for a POST request from the wallet authenticator web app.
        /// </summary>
        /// <returns>The user's public key contained within the POST request.</returns>
        private async Task<string> ListenForWalletResponse() {
            isListening = true;

            // Create and start listener on open port.
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerIP + ":8080/");
            listener.Start();

            if (useLogging) {
                Debug.Log($"Listening for response on port {"8080"}");
            }

            // Wait for response from authenticator web app.
            HttpListenerContext ctx = await listener.GetContextAsync();
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            string publicKey = null;

            // Send necessary HTTP response.
            if (request.HttpMethod == HttpMethod.Options.Method) {
                SendPreflightResponse(response);
            } else if (request.HttpMethod == HttpMethod.Post.Method) {
                publicKey = GetPublicKey(request);
                SendPostResponse(response);
            }

            listener.Close();
            if (useLogging) {
                Debug.Log("Listener closed");
            }
            isListening = false;
            return publicKey;
        }

        /// <summary>
        /// Processes a request from the authenticator web app, and extracts the user's public key.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <returns>
        /// The user's public key contained in the request, or null if the request is invalid.
        /// </returns>
        private string GetPublicKey(HttpListenerRequest request) {
            string[] origin = request.Headers.GetValues(CorsConstants.Origin);
            if (origin.Length > 0) {
                // Check response originated from the correct source, this is an extra layer of
                // security incase this response is sent without using CORS.
                if (origin[0] == authURL) {
                    string body = new StreamReader(request.InputStream).ReadToEnd();
                    WalletResponse wallet = JsonConvert.DeserializeObject<WalletResponse>(body);
                    
                    if (useLogging) {
                        Debug.Log($"Found wallet: {wallet.publicKey}");
                    }
                    return wallet.publicKey;
                }
            }

            if (useLogging) {
                Debug.Log("Invalid request origin");
            }
            return null;
        }

        /// <summary>
        /// Sends an HTTP response to the authenticator web app when a POST request has been 
        /// processed.
        /// </summary>
        /// <param name="response">The response to send.</param>
        private void SendPostResponse(HttpListenerResponse response) {
            response.AddHeader(
                CorsConstants.AccessControlAllowOrigin, 
                authURL
            );
            response.StatusCode = (int)HttpStatusCode.OK;
            response.Close();
        }

        /// <summary>
        /// Sends an HTTP response to the authenticator web app when a preflight response is
        /// requested.
        /// </summary>
        /// <param name="response">The response to send.</param>
        private void SendPreflightResponse(HttpListenerResponse response) {
            response.AddHeader(
                CorsConstants.AccessControlAllowOrigin, 
                authURL
            );
            response.AddHeader(
                CorsConstants.AccessControlAllowMethods, 
                HttpMethod.Post.Method
            );
            response.AddHeader(
                CorsConstants.AccessControlMaxAge, 
                preflightTimeout.ToString()
            );
            response.StatusCode = (int)HttpStatusCode.OK;
            response.Close();
        }

        #endregion
    }
}
