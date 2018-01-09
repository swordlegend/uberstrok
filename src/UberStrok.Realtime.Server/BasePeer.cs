﻿using log4net;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UberStrok.Realtime.Server
{
    // Server -> Client peer.
    public class BasePeer : ClientPeer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BasePeer).Name);

        public BasePeer(InitRequest initRequest) : base(initRequest)
        {
            // Check the client version.
            if (initRequest.ApplicationId != RealtimeVersion.Current)
                Disconnect();

            _opHandlers = new Dictionary<int, BaseOperationHandler>();
        }

        private readonly Dictionary<int, BaseOperationHandler> _opHandlers;

        public void AddOpHandler(BaseOperationHandler handler)
        {
            _opHandlers.Add(handler.Id, handler);
        }

        public void RemoveOpHandler(int id)
        {
            _opHandlers.Remove(id);
        }

        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            foreach (var opHandler in _opHandlers.Values)
            {
                try { opHandler.OnDisconnect(this, reasonCode, reasonDetail); }
                catch (Exception ex)
                {
                    Log.Error($"Error while handling disconnection of peer -> {opHandler.GetType().Name}");
                    Log.Error(ex);
                }
            }
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            /* 
                OperationRequest should contain 1 parameters.
                [0] -> (Int32 - OperationHandler ID) ->> (Byte[] - Data).

                Then we use OperationRequest.OperationCode & OperationHandler ID to,
                determine how to read stuff.

                Check if we've got enough parameters.
             */
            if (operationRequest.Parameters.Count < 1)
            {
                Log.Warn("Disconnecting client since its does not have enough parameters!");
                Disconnect();
                return;
            }

            var opHandlerId = operationRequest.Parameters.Keys.First();
            var handler = default(BaseOperationHandler);
            if (_opHandlers.TryGetValue(opHandlerId, out handler))
            {
                var data = (byte[])operationRequest.Parameters[opHandlerId];
                using (var bytes = new MemoryStream(data))
                {
                    try { handler.OnOperationRequest(this, operationRequest.OperationCode, bytes); }
                    catch (Exception ex)
                    {
                        Log.Error($"Error while handling request {handler.GetType().Name}:{opHandlerId} -> OpCode: {operationRequest.OperationCode}");
                        Log.Error(ex);
                    }
                }
            }
            else
            {
                Log.Warn($"Unable to handle operation request -> not implemented operation handler: {opHandlerId}");
            }
        }
    }
}
