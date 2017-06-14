using Matrix.Xmpp.Client;
using System;
using System.Diagnostics;
using System.Threading;

namespace Opux
{
    class ReconnectXmppWrapper
    {
        XmppClient xmppClient;
        bool onLogin;
        Timer connectTimer;
        TimerCallback connectTimerCallback;

        public ReconnectXmppWrapper(string xmppdomain, string username, string password)
        {
            try
            {
                xmppClient = new XmppClient
                {
                    XmppDomain = xmppdomain,
                    Username = username,
                    Password = password
                };

                xmppClient.OnMessage += Functions.OnMessage;
                xmppClient.OnClose += OnClose;
                xmppClient.OnBind += OnBind;
                xmppClient.OnBindError += OnBindError;
                xmppClient.OnAuthError += OnAuthError;
                xmppClient.OnError += OnError;
                xmppClient.OnStreamError += OnStreamError;
                xmppClient.OnXmlError += OnXmlError;
                xmppClient.OnLogin += OnLogin;

                connectTimerCallback = Connect;
                connectTimer = new Timer(connectTimerCallback, null, 5000, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }


        #region xmpp error handlers
        private void OnBindError(object sender, IqEventArgs e)
        {
            Console.WriteLine("OnBindError");
            xmppClient.Close();
        }

        private void OnStreamError(object sender, Matrix.StreamErrorEventArgs e)
        {
            Console.WriteLine(String.Format("OnStreamError: error condition {0}", e.Error.Condition.ToString()));
        }

        private void OnXmlError(object sender, Matrix.ExceptionEventArgs e)
        {
            Console.WriteLine("OnXmlError");
            Console.WriteLine(e.Exception.Message);
            Console.WriteLine(e.Exception.StackTrace);
        }

        private void OnAuthError(object sender, Matrix.Xmpp.Sasl.SaslEventArgs e)
        {
            Console.WriteLine("OnAuthError");
        }

        private void OnError(object sender, Matrix.ExceptionEventArgs e)
        {
            string msg = (e != null ? (e.Exception != null ? e.Exception.Message : "") : "");
            Console.WriteLine("OnError: " + msg);

            if (!onLogin)
                StartConnectTimer();
        }
        #endregion

        #region << XMPP handlers >>

        private void OnLogin(object sender, Matrix.EventArgs e)
        {
            Console.WriteLine("OnLogin");
            onLogin = true;
        }

        private void OnBind(object sender, Matrix.JidEventArgs e)
        {
            Console.WriteLine("OnBind: XMPP connected. JID: " + e.Jid);
        }

        private void OnClose(object sender, Matrix.EventArgs e)
        {
            Console.WriteLine("OnClose: XMPP connection closed");
            StartConnectTimer();
        }
        #endregion


        private void StartConnectTimer()
        {
            Console.WriteLine("starting reconnect timer...");
            connectTimer.Change(5000, Timeout.Infinite);
        }

        public void Connect(Object obj)
        {
            if (!xmppClient.StreamActive)
            {
                Console.WriteLine("StreamActive=" + xmppClient.StreamActive);
                Console.WriteLine("connect: XMPP connecting.... ");
                onLogin = false;
                xmppClient.Open();
            }
        }
    }
}
