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
        /// <param name="portHandler">An action to execute when a free TCP port is found.</param>
        /// <returns>
        /// The user's public key, or null if none is received or already listening.
        /// </returns>
        public async Task<WalletResponse> ListenForWalletResponse(
            Action<string> portHandler
        ) {
            if (!isListening) {
                // Find port
                string port = GetFreeTcpPort().ToString();

                // Open HTTPListener
                HttpListener listener = new();
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
                return await CaptureRequest(listener, useLogging);
            }
            return null;
        }

        #endregion

        #region Private

        /// <summary>
        /// Uses <see cref="HttpListener"/> to listen for requests and extract a public key from a
        /// <see cref="WalletResponse"/>.
        /// </summary>
        /// <param name="activeListener">
        /// An active HTTPListener listening for <see cref="WalletResponse"/>s.
        /// </param>
        /// <param name="useLogging">Whether to log significant events.</param>
        /// <returns>A authenticated public key or null if the request is invalid.</returns>
        private async Task<WalletResponse> CaptureRequest(HttpListener activeListener, bool useLogging) {
            if (activeListener == null) {
                Debug.LogError("No active HTTPListener.");
                return null;
            }
            HttpListenerContext ctx = await activeListener.GetContextAsync();
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            WalletResponse wallet = null;

            // Send necessary HTTP response.
            if (request.HttpMethod == HttpMethod.Options.Method) {
                SendPreflightResponse(response);
                return await CaptureRequest(activeListener, useLogging);
            } else if (request.HttpMethod == HttpMethod.Post.Method) {
                wallet = GetWalletResponse(request, useLogging);
                if (useLogging) {
                    Debug.Log($"Found wallet: {wallet}");
                }
                SendPostResponse(response);
                activeListener.Close();
                isListening = false;
                if (useLogging) {
                    Debug.Log("Listener closed");
                }
            }

            return wallet;
        }

        /// <summary>
        /// Processes a request from the authenticator web app, and extracts the user's public key.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="useLogging">Whether significant events should be logged.</param>
        /// <returns>
        /// The user's public key contained in the request, or null if the request is invalid.
        /// </returns>
        private WalletResponse GetWalletResponse(HttpListenerRequest request, bool useLogging) {
            string[] origin = request.Headers.GetValues(CorsConstants.Origin);
            // Check response originated from the correct source, this is an extra layer of
            // security incase this response is sent without using CORS.
            if (origin.Length > 0 && origin[0] == authURL) {
                string body = new StreamReader(request.InputStream).ReadToEnd();
                WalletResponse response = JsonConvert.DeserializeObject<WalletResponse>(body);
                return response;
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

        /// <summary>
        /// Finds a free TCP port.
        /// </summary>
        /// <returns>An integer representation of the first free TCP port.</returns>
        private int GetFreeTcpPort() {
            TcpListener l = new(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        #endregion

    }
}
