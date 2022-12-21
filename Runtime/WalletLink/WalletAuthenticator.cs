using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Cors;
using UnityEngine;

namespace DBGames.UI.Wallet {

    /// <summary>
    /// Used to launch the wallet authentication web app.
    /// </summary>
    public class WalletAuthenticator {

        #region Types

        struct WalletResponse {

            [JsonProperty]
            internal string publicKey;
        }

        #endregion

        #region Properties

        /// <summary>
        /// If true, wallet connection events are logged in the console.
        /// </summary>
        private readonly bool useLogging;

        /// <summary>
        /// The URL to expect a <see cref="WalletResponse"/> from.
        /// </summary>
        private readonly string authURL;

        /// <summary>
        /// The time in milliseconds that the browser is allowed to cache a preflight request.
        /// </summary>
        private const int preflightTimeout = 120000;

        /// <summary>
        /// The IP used to listen for responses from the authenticator app.
        /// </summary>
        private const string listenerIP = "http://127.0.0.1";

        /// <summary>
        /// Whether this object is currently listening for an HTTPListenerRequest.
        /// </summary>
        private bool isListening = false;

        #endregion

        #region Constructors

        public WalletAuthenticator(bool useLogging, string authURL) {
            this.useLogging = useLogging;
            this.authURL = authURL;
        }

        #endregion

        #region Public

        /// <summary>
        /// Triggers this object to listen for a response if not already listening.
        /// </summary>
        /// <returns>The user's public key, or null if none is received or already listening.</returns>
        public async Task<string> ListenForWalletResponse(
            Action<string> portHandler
        ) {
            if (!isListening) {
                // Find port
                string port = GetFreeTcpPort().ToString();

                // Open HTTPListener
                HttpListener listener = new HttpListener();
                string listenerURL = string.Format("{0}:{1}/", listenerIP, port);
                listener.Prefixes.Add(listenerURL);
                listener.Start();
                if (useLogging) {
                    Debug.Log($"Listening for response on port {port}");
                }

                // Execute port callback once listener is running.
                portHandler?.Invoke(port);

                isListening = true;
                // Wait for response from authenticator web app.
                return await CaptureRequest(useLogging, listener);
            }
            return null;
        }

        #endregion

        #region Private

        private async Task<string> CaptureRequest(bool useLogging, HttpListener activeListener) {
            // Create and start listener on open port.
            // TODO: Find free port before listening.
            if (activeListener == null) {
                activeListener.Prefixes.Add(listenerIP + ":8080/");
                activeListener.Start();

                if (useLogging) {
                    Debug.Log($"Listening for response on port {"8080"}");
                }
            }

            HttpListenerContext ctx = await activeListener.GetContextAsync();
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            string publicKey = null;

            // Send necessary HTTP response.
            if (request.HttpMethod == HttpMethod.Options.Method) {
                SendPreflightResponse(response);
                return await CaptureRequest(useLogging, activeListener);
            } else if (request.HttpMethod == HttpMethod.Post.Method) {
                publicKey = GetPublicKey(request, useLogging);
                SendPostResponse(response);

                activeListener.Close();
                isListening = false;
                if (useLogging) {
                    Debug.Log("Listener closed");
                }
            }

            return publicKey;
        }

        /// <summary>
        /// Processes a request from the authenticator web app, and extracts the user's public key.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <returns>
        /// The user's public key contained in the request, or null if the request is invalid.
        /// </returns>
        private string GetPublicKey(HttpListenerRequest request, bool useLogging) {
            string[] origin = request.Headers.GetValues(CorsConstants.Origin);
            if (origin.Length > 0) {
                // Check response originated from the correct source, this is an extra layer of
                // security incase this response is sent without using CORS.

                // TODO: Add layer to protect against forgery attacks.
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
            if (useLogging) {
                Debug.Log("Sent POST response.");
            }
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
            if (useLogging) {
                Debug.Log("Sent PREFLIGHT response.");
            }
        }

        private int GetFreeTcpPort() {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        #endregion

    }
}
