﻿using Discore.Http.Net;
using System;
using System.Collections.Generic;
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
        public DiscordUser Recipient { get { return cache != null ? cache.Users[recipientId] : recipient; } }

        IDiscordApplication app;
        HttpChannelsEndpoint channelsHttp;
        Snowflake lastMessageId;

        DiscoreCache cache;
        DiscordUser recipient;
        Snowflake recipientId;

        internal DiscordDMChannel(DiscoreCache cache, IDiscordApplication app, DiscordApiData data)
            : this(app, data, true)
        {
            this.cache = cache;
        }

        internal DiscordDMChannel(IDiscordApplication app, DiscordApiData data)
            : this(app, data, false)
        { }

        private DiscordDMChannel(IDiscordApplication app, DiscordApiData data, bool isWebSocket) 
            : base(app, data, DiscordChannelType.DirectMessage)
        {
            this.app = app;
            channelsHttp = app.HttpApi.InternalApi.Channels;

            lastMessageId = data.GetSnowflake("last_message_id").Value;

            if (!isWebSocket)
            {
                DiscordApiData recipientData = data.Get("recipient");
                recipient = new DiscordUser(recipientData);
            }
            else
                recipientId = data.LocateSnowflake("recipient.id").Value;
        }

        /// <summary>
        /// Gets the id of the last message sent in this channel.
        /// </summary>
        public Snowflake GetLastMessageId()
        {
            try { return GetLastMessageIdAsync().Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Gets the id of the last message sent in this channel.
        /// </summary>
        public async Task<Snowflake> GetLastMessageIdAsync()
        {
            Snowflake lastId = lastMessageId;
            while (true)
            {
                IList<DiscordMessage> messages = await GetMessagesAsync(lastId, 100, DiscordMessageGetStrategy.After);

                lastId = messages[0].Id;

                if (messages.Count < 100)
                    break;
            }

            lastMessageId = lastId;
            return lastId;
        }

        /// <summary>
        /// Sends a message to this channel.
        /// </summary>
        /// <param name="content">The message text content.</param>
        /// <param name="splitIfTooLong">Whether this message should be split into multiple messages if too long.</param>
        /// <param name="tts">Whether this should be played over text-to-speech.</param>
        /// <returns>Returns the created message (or first if split into multiple).</returns>
        public DiscordMessage SendMessage(string content, bool splitIfTooLong = false, bool tts = false)
        {
            try { return SendMessageAsync(content, splitIfTooLong, tts).Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Sends a message to this channel.
        /// </summary>
        /// <param name="content">The message text content.</param>
        /// <param name="splitIfTooLong">Whether this message should be split into multiple messages if too long.</param>
        /// <param name="tts">Whether this should be played over text-to-speech.</param>
        /// <returns>Returns the created message (or first if split into multiple).</returns>
        public async Task<DiscordMessage> SendMessageAsync(string content, bool splitIfTooLong = false, bool tts = false)
        {
            DiscordApiData firstOrOnlyMessageData = null;

            if (splitIfTooLong && content.Length > DiscordMessage.MAX_CHARACTERS)
            {
                await SplitSendMessage(content,
                    async message =>
                    {
                        DiscordApiData msgData = await channelsHttp.CreateMessage(Id, message, tts);

                        if (firstOrOnlyMessageData == null)
                            firstOrOnlyMessageData = msgData;
                    });
            }
            else
                firstOrOnlyMessageData = await channelsHttp.CreateMessage(Id, content, tts);

            return new DiscordMessage(app, firstOrOnlyMessageData);
        }

        /// <summary>
        /// Sends a message with a file attachment to this channel.
        /// </summary>
        /// <param name="fileAttachment">The file data to attach.</param>
        /// <param name="content">The message text content.</param>
        /// <param name="splitIfTooLong">Whether this message should be split into multiple messages if too long.</param>
        /// <param name="tts">Whether this should be played over text-to-speech.</param>
        /// <returns>Returns the created message (or first if split into multiple).</returns>
        public DiscordMessage SendMessage(byte[] fileAttachment, string content = null,
            bool splitIfTooLong = false, bool tts = false)
        {
            try { return SendMessageAsync(fileAttachment, content, splitIfTooLong, tts).Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Sends a message with a file attachment to this channel.
        /// </summary>
        /// <param name="fileAttachment">The file data to attach.</param>
        /// <param name="content">The message text content.</param>
        /// <param name="splitIfTooLong">Whether this message should be split into multiple messages if too long.</param>
        /// <param name="tts">Whether this should be played over text-to-speech.</param>
        /// <returns>Returns the created message (or first if split into multiple).</returns>
        public async Task<DiscordMessage> SendMessageAsync(byte[] fileAttachment, string content = null, 
            bool splitIfTooLong = false, bool tts = false)
        {
            DiscordApiData firstOrOnlyMessageData = null;

            if (splitIfTooLong && content.Length > DiscordMessage.MAX_CHARACTERS)
            {
                await SplitSendMessage(content,
                    async message =>
                    {
                        if (firstOrOnlyMessageData == null)
                        {
                            DiscordApiData msgData = await channelsHttp.UploadFile(Id, fileAttachment, message, tts);
                            firstOrOnlyMessageData = msgData;
                        }
                        else
                            await channelsHttp.CreateMessage(Id, message, tts);
                    });
            }
            else
                firstOrOnlyMessageData = await channelsHttp.UploadFile(Id, fileAttachment, content, tts);

            return new DiscordMessage(app, firstOrOnlyMessageData);
        }

        async Task SplitSendMessage(string content, Func<string, Task> createMessageCallback)
        {
            int i = 0;
            while (i < content.Length)
            {
                int maxChars = Math.Min(DiscordMessage.MAX_CHARACTERS, content.Length - i);
                int lastNewLine = content.LastIndexOf('\n', i + maxChars - 1, maxChars - 1);

                string subMessage;
                if (lastNewLine > -1)
                    subMessage = content.Substring(i, lastNewLine - i);
                else
                    subMessage = content.Substring(i, maxChars);

                if (!string.IsNullOrWhiteSpace(subMessage))
                    await createMessageCallback(subMessage);

                i += subMessage.Length;
            }
        }

        /// <summary>
        /// Deletes a list of messages in one API call.
        /// Much quicker than calling Delete() on each message instance.
        /// </summary>
        /// <returns>Returns whether the operation was successful.</returns>
        public bool BulkDeleteMessages(IEnumerable<Snowflake> messageIds)
        {
            try { return BulkDeleteMessagesAsync(messageIds).Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Deletes a list of messages in one API call.
        /// Much quicker than calling Delete() on each message instance.
        /// </summary>
        /// <returns>Returns whether the operation was successful.</returns>
        public async Task<bool> BulkDeleteMessagesAsync(IEnumerable<Snowflake> messageIds)
        {
            DiscordApiData data = await channelsHttp.BulkDeleteMessages(Id, messageIds);
            return data.IsNull;
        }

        /// <summary>
        /// Causes the current authenticated user to appear as typing in this channel.
        /// </summary>
        /// <returns>Returns whether the operation was successful.</returns>
        public bool TriggerTypingIndicator()
        {
            try { return TriggerTypingIndicatorAsync().Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Causes the current authenticated user to appear as typing in this channel.
        /// </summary>
        /// <returns>Returns whether the operation was successful.</returns>
        public async Task<bool> TriggerTypingIndicatorAsync()
        {
            DiscordApiData data = await channelsHttp.TriggerTypingIndicator(Id);
            return data.IsNull;
        }

        /// <summary>
        /// Gets a list of all pinned messages in this channel.
        /// </summary>
        public IList<DiscordMessage> GetPinnedMessages()
        {
            try { return GetPinnedMessagesAsync().Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Gets a list of all pinned messages in this channel.
        /// </summary>
        public async Task<IList<DiscordMessage>> GetPinnedMessagesAsync()
        {
            DiscordApiData messagesArray = await channelsHttp.GetPinnedMessages(Id);
            DiscordMessage[] messages = new DiscordMessage[messagesArray.Values.Count];

            for (int i = 0; i < messages.Length; i++)
                messages[i] = new DiscordMessage(app, messagesArray.Values[i]);

            return messages;
        }

        /// <summary>
        /// Gets a message in this channel.
        /// </summary>
        public DiscordMessage GetMessage(Snowflake messageId)
        {
            try { return GetMessageAsync(messageId).Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Gets a message in this channel.
        /// </summary>
        public async Task<DiscordMessage> GetMessageAsync(Snowflake messageId)
        {
            DiscordApiData data = await channelsHttp.GetMessage(Id, messageId);

            return new DiscordMessage(app, data);
        }

        /// <summary>
        /// Gets a list of messages in this channel.
        /// </summary>
        /// <param name="baseMessageId">The message id the list will start at (is not included in the final list).</param>
        /// <param name="limit">Maximum number of messages to be returned.</param>
        /// <param name="getStrategy">The way messages will be located based on the <paramref name="baseMessageId"/>.</param>
        public IList<DiscordMessage> GetMessages(Snowflake baseMessageId, int? limit = null,
            DiscordMessageGetStrategy getStrategy = DiscordMessageGetStrategy.Before)
        {
            try { return GetMessagesAsync(baseMessageId, limit, getStrategy).Result; }
            catch (AggregateException aex) { throw aex.InnerException; }
        }

        /// <summary>
        /// Gets a list of messages in this channel.
        /// </summary>
        /// <param name="baseMessageId">The message id the list will start at (is not included in the final list).</param>
        /// <param name="limit">Maximum number of messages to be returned.</param>
        /// <param name="getStrategy">The way messages will be located based on the <paramref name="baseMessageId"/>.</param>
        public async Task<IList<DiscordMessage>> GetMessagesAsync(Snowflake baseMessageId, int? limit = null, 
            DiscordMessageGetStrategy getStrategy = DiscordMessageGetStrategy.Before)
        {
            DiscordApiData messagesArray = await channelsHttp.GetMessages(Id, baseMessageId, limit, getStrategy);
            DiscordMessage[] messages = new DiscordMessage[messagesArray.Values.Count];

            for (int i = 0; i < messages.Length; i++)
                messages[i] = new DiscordMessage(app, messagesArray.Values[i]);

            return messages;
        }

        public override string ToString()
        {
            return $"DM Channel: {Recipient}";
        }
    }
}