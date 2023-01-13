using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Cors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DBGames.UI.Wallet.Tests {
    public class WalletAuthenticatorTests {

        #region Properties

        private const string authURL = "https://www.Test.com";
        private const string invalidOriginURL = "https://www.invalid.com";
        private const string listenerIP = "http://127.0.0.1";
        private const string testKey = "Test1234";
        private readonly HttpClient client = new();

        #endregion

        #region Set Up

        [SetUp]
        public void ClearClientHeaders() {
            client.DefaultRequestHeaders.Clear();
        }

        #endregion

        #region Tests

        /// <summary>
        /// Tests whether the correct public key is sent when a valid request is received.
        /// </summary>
        [UnityTest]
        public IEnumerator WalletAuthenticatorTests_TestSuccesfulResponse() {
            WalletAuthenticator subject = new(true, authURL);
            Task<string> publicKey = subject.ListenForWalletResponse(async port => {
                client.DefaultRequestHeaders.Add(CorsConstants.Origin, authURL);
                HttpResponseMessage response = await SendPostRequest(port);

                Assert.DoesNotThrow(delegate {
                    response.EnsureSuccessStatusCode();
                });
            });
            yield return new WaitUntil(() => publicKey.IsCompleted);
            Assert.AreEqual(testKey, publicKey.Result);
        }

        /// <summary>
        /// Tests for a valid preflight response being sent when a preflight request is made.
        /// </summary>
        [UnityTest]
        public IEnumerator WalletAuthenticatorTests_TestPreflightResponse() {
            WalletAuthenticator subject = new(true, authURL);
            Task<string> publicKey = subject.ListenForWalletResponse(async port => {
                HttpResponseMessage preflight = await SendPreflightRequest(port);

                Assert.DoesNotThrow(delegate {
                    preflight.EnsureSuccessStatusCode();
                });

                client.DefaultRequestHeaders.Add(CorsConstants.Origin, authURL);
                HttpResponseMessage response = await SendPostRequest(port);

                Assert.DoesNotThrow(delegate {
                    response.EnsureSuccessStatusCode();
                });
            });
            yield return new WaitUntil(() => publicKey.IsCompleted);
            Assert.AreEqual(testKey, publicKey.Result);
        }

        /// <summary>
        /// Tests for a null wallet response when a request comes from an invalid origin.
        /// </summary>
        [UnityTest]
        public IEnumerator WalletAuthenticatorTests_TestInvalidOrigin() {
            WalletAuthenticator subject = new(true, authURL);
            Task<string> publicKey = subject.ListenForWalletResponse(async port => {
                client.DefaultRequestHeaders.Add(CorsConstants.Origin, invalidOriginURL);
                HttpResponseMessage response = await SendPostRequest(port);

                Assert.DoesNotThrow(delegate {
                    response.EnsureSuccessStatusCode();
                });
            });
            yield return new WaitUntil(() => publicKey.IsCompleted);
            Assert.Null(publicKey.Result);
        }

        #endregion

        #region Helper

        private string MakePublicKeyResponse() {
            return string.Format("{{\"publicKey\":\"{0}\"}}", testKey);
        }

        private async Task<HttpResponseMessage> SendPostRequest(string port) {
            string requestURL = string.Format("{0}:{1}/", listenerIP, port);
            string requestJson = MakePublicKeyResponse();
            HttpContent content = new StringContent(requestJson);
            return await client.PostAsync(requestURL, content);
        }

        private async Task<HttpResponseMessage> SendPreflightRequest(string port) {
            string requestURL = string.Format("{0}:{1}/", listenerIP, port);
            HttpRequestMessage request = new(
                HttpMethod.Options, 
                requestURL
            );
            return await client.SendAsync(request);
        }

        #endregion
    }
}
