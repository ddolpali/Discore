﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Discore.WebSocket.Net
{
    partial class Gateway
    {
        delegate void PayloadCallback(DiscordApiData payload, DiscordApiData data);

        Dictionary<GatewayOPCode, PayloadCallback> payloadHandlers;

        void InitializePayloadHandlers()
        {
            payloadHandlers = new Dictionary<GatewayOPCode, PayloadCallback>();
            payloadHandlers[GatewayOPCode.Dispath] = HandleDispatchPayload;
            payloadHandlers[GatewayOPCode.Hello] = HandleHelloPayload;
            payloadHandlers[GatewayOPCode.HeartbeatAck] = HandleHeartbeatAckPayload;
            payloadHandlers[GatewayOPCode.InvalidSession] = HandleInvalidSessionPayload;
        }

        void HandleDispatchPayload(DiscordApiData payload, DiscordApiData data)
        {
            sequence = payload.GetInteger("s") ?? sequence;
            string eventName = payload.GetString("t");

            DispatchCallback callback;
            if (dispatchHandlers.TryGetValue(eventName, out callback))
                callback(data);
            else
                log.LogWarning($"Missing handler for dispatch event: {eventName}");
        }

        void HandleHelloPayload(DiscordApiData payload, DiscordApiData data)
        {
            heartbeatInterval = data.GetInteger("heartbeat_interval") ?? heartbeatInterval;
            log.LogVerbose($"[Hello] heartbeat_interval: {heartbeatInterval}ms");
        }

        void HandleHeartbeatAckPayload(DiscordApiData payload, DiscordApiData data)
        {
            // Reset heartbeat timeout
            heartbeatTimeoutAt = Environment.TickCount + (heartbeatInterval * HEARTBEAT_TIMEOUT_MISSED_PACKETS);
        }

        void HandleReconnectPayload(DiscordApiData payload, DiscordApiData data)
        {
            log.LogInfo("[Reconnect] Performing resume...");

            // We won't worry about sending the resume payload here,
            // as that will be handled by the reconnect procedure,
            // when we pass true.
            Reconnect(true);
        }

        void HandleInvalidSessionPayload(DiscordApiData payload, DiscordApiData data)
        {
            log.LogInfo("[InvalidSession] Reconnecting...");

            Disconnect();
            Reconnect();
        }

        void SendPayload(GatewayOPCode op, DiscordApiData data)
        {
            DiscordApiData payload = new DiscordApiData(DiscordApiDataType.Container);
            payload.Set("op", (int)op);
            payload.Set("d", data);

            socket.Send(payload);
        }

        void SendIdentifyPayload()
        {
            DiscordApiData data = new DiscordApiData(DiscordApiDataType.Container);
            data.Set("token", app.Authenticator.GetToken());
            data.Set("compress", true);
            data.Set("large_threshold", 250);

            if (app.ShardManager.ShardCount > 1)
            {
                DiscordApiData shardData = new DiscordApiData(DiscordApiDataType.Array);
                shardData.Values.Add(new DiscordApiData(shard.ShardId));
                shardData.Values.Add(new DiscordApiData(app.ShardManager.ShardCount));
                data.Set("shard", shardData);
            }

            DiscordApiData props = data.Set("properties", new DiscordApiData(DiscordApiDataType.Container));
            props.Set("$os", RuntimeInformation.OSDescription);
            props.Set("$browser", "discore");
            props.Set("$device", "discore");
            props.Set("$referrer", "");
            props.Set("$referring_domain", "");

            SendPayload(GatewayOPCode.Identify, data);
        }

        void SendHeartbeatPayload()
        {
            SendPayload(GatewayOPCode.Heartbeat, new DiscordApiData(sequence));
        }

        void SendResumePayload()
        {
            DiscordApiData data = new DiscordApiData(DiscordApiDataType.Container);
            data.Set("token", app.Authenticator.GetToken());
            data.Set("session_id", sessionId);
            data.Set("seq", sequence);

            SendPayload(GatewayOPCode.Resume, data);
        }

        internal void SendStatusUpdate(string game = null, int ? idleSince = null)
        {
            DiscordApiData data = new DiscordApiData(DiscordApiDataType.Container);
            data.Set("idle_since", idleSince);

            if (game != null)
            {
                DiscordApiData gameData = new DiscordApiData(DiscordApiDataType.Container);
                data.Set("game", gameData);

                gameData.Set("name", game);
            }

            SendPayload(GatewayOPCode.StatusUpdate, data);
        }

        internal void SendRequestGuildMembersPayload(Snowflake guildId, string query, int limit)
        {
            DiscordApiData data = new DiscordApiData(DiscordApiDataType.Container);
            data.Set("guild_id", guildId);
            data.Set("query", query);
            data.Set("limit", limit);

            SendPayload(GatewayOPCode.RequestGuildMembers, data);
        }

        internal void SendVoiceStateUpdatePayload(Snowflake guildId, Snowflake? channelId, bool isMute, bool isDeaf)
        {
            DiscordApiData data = new DiscordApiData(DiscordApiDataType.Container);
            data.Set("guild_id", guildId);
            data.Set("channel_id", channelId);
            data.Set("self_mute", isMute);
            data.Set("self_deaf", isDeaf);

            SendPayload(GatewayOPCode.VoiceStateUpdate, data);
        }
    }
}
