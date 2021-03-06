﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Discore.Voice.Internal
{
    partial class VoiceWebSocket
    {
        delegate void PayloadCallback(DiscordApiData payload, DiscordApiData data);

        [AttributeUsage(AttributeTargets.Method)]
        class PayloadAttribute : Attribute
        {
            public VoiceOPCode OPCode { get; }

            public PayloadAttribute(VoiceOPCode opCode)
            {
                OPCode = opCode;
            }
        }

        Dictionary<VoiceOPCode, PayloadCallback> payloadHandlers;

        void InitializePayloadHandlers()
        {
            payloadHandlers = new Dictionary<VoiceOPCode, PayloadCallback>();

            Type gatewayType = typeof(VoiceWebSocket);
            Type payloadType = typeof(PayloadCallback);

            foreach (MethodInfo method in gatewayType.GetTypeInfo().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                PayloadAttribute attr = method.GetCustomAttribute<PayloadAttribute>();
                if (attr != null)
                    payloadHandlers[attr.OPCode] = (PayloadCallback)method.CreateDelegate(payloadType, this);
            }
        }
    }
}
