using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Cors;
using NUnit.Framework;

namespace DBGames.UI.Wallet.Tests {
    public class WalletAuthenticatorTests {

        #region Properties

        private const string authURL = "https://www.Test.com";
        private const string invalidOriginURL = "https://www.invalid.com";
        private const string listenerIP = "http://127.0.0.1";
        private const string testKey = "Test1234";
        private readonly HttpClient client = new HttpClient();

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
        [Test]
        public async void WalletAuthenticatorTests_TestSuccesfulResponse() {
            WalletAuthenticator subject = new WalletAuthenticator(true, authURL);
            string publicKey = await subject.ListenForWalletResponse(async port => {
                client.DefaultRequestHeaders.Add(CorsConstants.Origin, authURL);
                HttpResponseMessage response = await SendPostRequest(port);

                Assert.DoesNotThrow(delegate {
                    response.EnsureSuccessStatusCode();
                });
            });
            Assert.AreEqual(publicKey, testKey);
        }

        /// <summary>
        /// Tests for a valid preflight response being sent when a preflight request is made.
        /// </summary>
        [Test]
        public async void WalletAuthenticatorTests_TestPreflightResponse() {
            WalletAuthenticator subject = new WalletAuthenticator(true, authURL);
            string publicKey = await subject.ListenForWalletResponse(async port => {
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
            Assert.AreEqual(publicKey, testKey);
        }

        /// <summary>
        /// Tests for a null wallet response when a request comes from an invalid origin.
        /// </summary>
        [Test]
        public async void WalletAuthenticatorTests_TestInvalidOrigin() {
            WalletAuthenticator subject = new WalletAuthenticator(true, authURL);
            string publicKey = await subject.ListenForWalletResponse(async port => {
                client.DefaultRequestHeaders.Add(CorsConstants.Origin, invalidOriginURL);
                HttpResponseMessage response = await SendPostRequest(port);

                Assert.DoesNotThrow(delegate {
                    response.EnsureSuccessStatusCode();
                });
            });
            Assert.Null(publicKey);
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
            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Options, 
                requestURL
            );
            return await client.SendAsync(request);
        }

        #endregion
    }
}
