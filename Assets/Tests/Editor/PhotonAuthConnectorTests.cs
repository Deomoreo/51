using NUnit.Framework;
using Photon.Realtime;
using Project51.Auth;
using UnityEngine;

namespace Project51.Tests
{
    /// <summary>Fase 10 B7: una caduta di rete durante il primo collegamento a Photon deve arrivare subito come fallimento.</summary>
    public class PhotonAuthConnectorTests
    {
        private GameObject go;
        private PhotonAuthConnector connector;
        private int failures;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("PhotonAuthConnectorTest");
            connector = go.AddComponent<PhotonAuthConnector>(); // in EditMode Awake/OnEnable non partono: niente callback Photon
            connector.OnConnectionFailed += _ => failures++;
            failures = 0;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        private void SetConnecting() => typeof(PhotonAuthConnector).GetProperty("IsConnecting").SetValue(connector, true);

        [Test]
        public void DropWhileConnecting_RaisesConnectionFailedOnce()
        {
            SetConnecting();
            connector.OnDisconnected(DisconnectCause.ServerTimeout);
            Assert.AreEqual(1, failures);
            Assert.IsFalse(connector.IsConnecting);
        }

        [Test]
        public void ClientLogicDisconnect_WhileConnecting_RaisesNothing()
        {
            SetConnecting();
            connector.OnDisconnected(DisconnectCause.DisconnectByClientLogic);
            Assert.AreEqual(0, failures);
        }

        [Test]
        public void DropAfterConnected_RaisesNothing()
        {
            connector.OnDisconnected(DisconnectCause.ServerTimeout);
            Assert.AreEqual(0, failures);
        }
    }
}
