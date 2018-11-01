﻿using Discore.Http;
using System.Threading.Tasks;

namespace Discore
{
    public sealed class DiscordGuildEmbed
    {
        /// <summary>
        /// Gets whether this embed is enabled.
        /// </summary>
        public bool Enabled { get; }
        /// <summary>
        /// Gets the embed channel ID.
        /// </summary>
        public Snowflake ChannelId { get; }
        /// <summary>
        /// Gets the ID of the guild this embed is for.
        /// </summary>
        public Snowflake GuildId { get; }

        DiscordHttpClient http;

        internal DiscordGuildEmbed(DiscordHttpClient http, Snowflake guildId, DiscordApiData data)
        {
            this.http = http;

            GuildId = guildId;

            Enabled = data.GetBoolean("enabled").Value;
            ChannelId = data.GetSnowflake("channel_id").Value;
        }

        /// <summary>
        /// Modifies the properties of this guild embed.
        /// <para>Requires <see cref="DiscordPermission.ManageGuild"/>.</para>
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordGuildEmbed> Modify(ModifyGuildEmbedOptions options)
        {
            return http.ModifyGuildEmbed(GuildId, options);
        }
    }
}
