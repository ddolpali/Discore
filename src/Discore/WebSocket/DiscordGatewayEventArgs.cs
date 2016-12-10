﻿using System;

namespace Discore.WebSocket
{
    public abstract class DiscordGatewayEventArgs : EventArgs
    {
        public Shard Shard { get; }

        public DiscordGatewayEventArgs(Shard shard)
        {
            Shard = shard;
        }
    }

    public class IntegrationEventArgs : DiscordGatewayEventArgs
    {
        public DiscordIntegration Integration { get; }

        public IntegrationEventArgs(Shard shard, DiscordIntegration integration)
            : base(shard)
        {
            Integration = integration;
        }
    }

    public class UserEventArgs : DiscordGatewayEventArgs
    {
        public DiscordUser User { get; }

        public UserEventArgs(Shard shard, DiscordUser user)
            : base(shard)
        {
            User = user;
        }
    }

    public class TypingStartEventArgs : DiscordGatewayEventArgs
    {
        public DiscordUser User { get; }
        public DiscordChannel Channel { get; }
        public long Timestamp { get; }

        public TypingStartEventArgs(Shard shard, DiscordUser user, DiscordChannel channel, long timestamp)
            : base(shard)
        {
            User = user;
            Channel = channel;
            Timestamp = timestamp;
        }
    }

    public class GuildMemberEventArgs : DiscordGatewayEventArgs
    {
        public DiscordGuild Guild { get; }
        public DiscordGuildMember Member { get; }

        public GuildMemberEventArgs(Shard shard, DiscordGuild guild, DiscordGuildMember member)
            : base(shard)
        {
            Guild = guild;
            Member = member;
        }
    }

    public class GuildUserEventArgs : DiscordGatewayEventArgs
    {
        public DiscordGuild Guild { get; }
        public DiscordUser User { get; }

        public GuildUserEventArgs(Shard shard, DiscordGuild guild, DiscordUser user)
            : base(shard)
        {
            Guild = guild;
            User = user;
        }
    }

    public class GuildRoleEventArgs : DiscordGatewayEventArgs
    {
        public DiscordGuild Guild { get; }
        public DiscordRole Role { get; }

        public GuildRoleEventArgs(Shard shard, DiscordGuild guild, DiscordRole role)
            : base(shard)
        {
            Guild = guild;
            Role = role;
        }
    }

    public class DMChannelEventArgs : DiscordGatewayEventArgs
    {
        public DiscordDMChannel Channel { get; }

        public DMChannelEventArgs(Shard shard, DiscordDMChannel channel)
            : base(shard)
        {
            Channel = channel;
        }
    }

    public class GuildChannelEventArgs : DiscordGatewayEventArgs
    {
        public DiscordGuildChannel Channel { get; }

        public GuildChannelEventArgs(Shard shard, DiscordGuildChannel channel)
            : base(shard)
        {
            Channel = channel;
        }
    }

    public class GuildEventArgs : DiscordGatewayEventArgs
    {
        public DiscordGuild Guild { get; }

        public GuildEventArgs(Shard shard, DiscordGuild guild)
            : base(shard)
        {
            Guild = guild;
        }
    }

    public class MessageEventArgs : DiscordGatewayEventArgs
    {
        public DiscordMessage Message { get; }

        public MessageEventArgs(Shard shard, DiscordMessage message)
            : base(shard)
        {
            Message = message;
        }
    }

    public class MessageUpdateEventArgs : DiscordGatewayEventArgs
    {
        public DiscordMessage PartialMessage { get; }
        public DiscordApiData UpdateData { get; }

        public MessageUpdateEventArgs(Shard shard, DiscordMessage message, DiscordApiData updateData)
            : base(shard)
        {
            PartialMessage = message;
            UpdateData = updateData;
        }
    }

    public class MessageDeleteEventArgs : DiscordGatewayEventArgs
    {
        public Snowflake MessageId { get; }
        public DiscordChannel Channel { get; }

        public MessageDeleteEventArgs(Shard shard, Snowflake messageId, DiscordChannel channel)
            : base(shard)
        {
            MessageId = messageId;
            Channel = channel;
        }
    }

    public class MessageReactionEventArgs : DiscordGatewayEventArgs
    {
        public Snowflake MessageId { get; }
        public DiscordUser User { get; }
        public DiscordChannel Channel { get; }
        public DiscordReactionEmoji Emoji { get; }

        public MessageReactionEventArgs(Shard shard, Snowflake messageId, DiscordChannel channel, 
            DiscordUser user, DiscordReactionEmoji emoji)
            : base(shard)
        {
            MessageId = messageId;
            Channel = channel;
            User = user;
            Emoji = emoji;
        }
    }

    public class ShardExceptionEventArgs : DiscordGatewayEventArgs
    {
        public Exception Exception { get; }

        public ShardExceptionEventArgs(Shard shard, Exception exception)
            : base(shard)
        {
            Exception = exception;
        }
    }
}