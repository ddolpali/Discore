﻿using Discore.Http;
using Discore.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Discore
{
    /// <summary>
    /// Direct message channels represent a one-to-one conversation between two users, outside of the scope of guilds.
    /// </summary>
    public sealed class DiscordDMChannel : DiscordChannel, ITextChannel
    {
        /// <summary>
        /// Gets the user on the other end of this channel.
        /// </summary>
        public DiscordUser Recipient { get; }

        DiscordHttpClient http;
        Snowflake lastMessageId;

        internal DiscordDMChannel(DiscordHttpClient http, MutableDMChannel channel)
            : base(http, DiscordChannelType.DirectMessage)
        {
            this.http = http;

            Id = channel.Id;
            Recipient = channel.Recipient.ImmutableEntity;
            lastMessageId = channel.LastMessageId;
        }

        internal DiscordDMChannel(DiscordHttpClient http, DiscordApiData data)
            : base(http, data, DiscordChannelType.DirectMessage)
        {
            this.http = http;

            lastMessageId = data.GetSnowflake("last_message_id") ?? default(Snowflake);

            // Normal DM should only ever have exactly one recipient
            DiscordApiData recipientData = data.GetArray("recipients").First();
            Recipient = new DiscordUser(false, recipientData);
        }

        /// <summary>
        /// Gets the ID of the last message sent in this channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException">Thrown if failed to retrieve channel messages.</exception>
        public async Task<Snowflake> GetLastMessageId()
        {
            Snowflake lastId = lastMessageId;
            while (true)
            {
                IReadOnlyList<DiscordMessage> messages = await GetMessages(lastId, 100, MessageGetStrategy.After)
                    .ConfigureAwait(false);

                lastId = messages.Count == 0 ? default(Snowflake) : messages[0].Id;

                if (messages.Count < 100)
                    break;
            }

            lastMessageId = lastId;
            return lastId;
        }

        /// <summary>
        /// Creates a message in this channel.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <param name="content">The message text content.</param>
        /// <returns>Returns the created message.</returns>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> CreateMessage(string content)
        {
            return http.CreateMessage(Id, content);
        }

        /// <summary>
        /// Creates a message in this channel.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <param name="details">The details of the message to create.</param>
        /// <returns>Returns the created message.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> CreateMessage(CreateMessageOptions details)
        {
            return http.CreateMessage(Id, details);
        }

        /// <summary>
        /// Posts a message with a file attachment.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <param name="fileData">A stream of the file contents.</param>
        /// <param name="fileName">The name of the file to use when uploading.</param>
        /// <param name="details">Optional extra details of the message to create.</param>
        /// <returns>Returns the created message.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> CreateMessage(Stream fileData, string fileName, CreateMessageOptions details = null)
        {
            return http.CreateMessage(Id, fileData, fileName, details);
        }
        /// <summary>
        /// Posts a message with a file attachment.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <param name="fileData">The file contents.</param>
        /// <param name="fileName">The name of the file to use when uploading.</param>
        /// <param name="details">Optional extra details of the message to create.</param>
        /// <returns>Returns the created message.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> CreateMessage(ArraySegment<byte> fileData, string fileName, CreateMessageOptions details = null)
        {
            return http.CreateMessage(Id, fileData, fileName, details);
        }

        /// <summary>
        /// Deletes a list of messages in one API call.
        /// Much quicker than calling Delete() on each message instance.
        /// <para>Note: can only delete messages sent by the current bot.</para>
        /// </summary>
        /// <param name="filterTooOldMessages">Whether to ignore deleting messages that are older than 2 weeks
        /// (messages that are too old cause an API error).</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task BulkDeleteMessages(IEnumerable<DiscordMessage> messages, bool filterTooOldMessages = true)
        {
            return http.BulkDeleteMessages(Id, messages, filterTooOldMessages);
        }

        /// <summary>
        /// Deletes a list of messages in one API call.
        /// Much quicker than calling Delete() on each message instance.
        /// <para>Note: can only delete messages sent by the current bot.</para>
        /// </summary>
        /// <param name="filterTooOldMessages">Whether to ignore deleting messages that are older than 2 weeks
        /// (messages that are too old cause an API error).</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task BulkDeleteMessages(IEnumerable<Snowflake> messageIds, bool filterTooOldMessages = true)
        {
            return http.BulkDeleteMessages(Id, messageIds, filterTooOldMessages);
        }

        /// <summary>
        /// Causes the current bot's user to appear as typing in this channel.
        /// <para>Note: it is recommended that bots do not generally use this route.
        /// This should only be used if the bot is responding to a command that is expected
        /// to take a few seconds or longer.</para>
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task TriggerTypingIndicator()
        {
            return http.TriggerTypingIndicator(Id);
        }

        /// <summary>
        /// Gets a list of all pinned messages in this channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<IReadOnlyList<DiscordMessage>> GetPinnedMessages()
        {
            return http.GetPinnedMessages(Id);
        }

        /// <summary>
        /// Gets a message in this channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> GetMessage(Snowflake messageId)
        {
            return http.GetChannelMessage(Id, messageId);
        }

        /// <summary>
        /// Gets a list of messages in this channel.
        /// </summary>
        /// <param name="baseMessageId">The message ID the list will start at (is not included in the final list).</param>
        /// <param name="limit">Maximum number of messages to be returned.</param>
        /// <param name="getStrategy">The way messages will be located based on the <paramref name="baseMessageId"/>.</param>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<IReadOnlyList<DiscordMessage>> GetMessages(Snowflake baseMessageId, int? limit = null, 
            MessageGetStrategy getStrategy = MessageGetStrategy.Before)
        {
            return http.GetChannelMessages(Id, baseMessageId, limit, getStrategy);
        }

        public override string ToString()
        {
            return $"DM Channel: {Recipient}";
        }
    }
}
