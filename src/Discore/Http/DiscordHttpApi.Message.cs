﻿using Discore.Http.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Discore.Http
{
    partial class DiscordHttpApi
    {
        /// <summary>
        /// Gets messages from a text channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<IReadOnlyList<DiscordMessage>> GetChannelMessages(Snowflake channelId,
            Snowflake? baseMessageId = null, int? limit = null,
            DiscordMessageGetStrategy getStrategy = DiscordMessageGetStrategy.Before)
        {
            string strat = getStrategy.ToString().ToLower();
            string limitStr = limit.HasValue ? $"&limit={limit.Value}" : "";

            DiscordApiData data = await rest.Get($"channels/{channelId}/messages?{strat}={baseMessageId}{limitStr}",
                "channels/channel/messages").ConfigureAwait(false);
            DiscordMessage[] messages = new DiscordMessage[data.Values.Count];

            for (int i = 0; i < messages.Length; i++)
                messages[i] = new DiscordMessage(app, data.Values[i]);

            return messages;
        }

        /// <summary>
        /// Gets a single message by ID from a channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<DiscordMessage> GetChannelMessage(Snowflake channelId, Snowflake messageId)
        {
            DiscordApiData data = await rest.Get($"channels/{channelId}/messages/{messageId}",
                "channels/channel/messages/message").ConfigureAwait(false);
            return new DiscordMessage(app, data);
        }

        /// <summary>
        /// Posts a message to a text channel.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> CreateMessage(Snowflake channelId, string content)
        {
            return CreateMessage(channelId, new DiscordMessageDetails(content));
        }

        /// <summary>
        /// Posts a message to a text channel.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="details"/> is null.</exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<DiscordMessage> CreateMessage(Snowflake channelId, DiscordMessageDetails details)
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));

            DiscordApiData requestData = new DiscordApiData(DiscordApiDataType.Container);
            requestData.Set("content", details.Content);
            requestData.Set("tts", details.TextToSpeech);
            requestData.Set("nonce", details.Nonce);

            if (details.Embed != null)
                requestData.Set("embed", details.Embed.Build());

            DiscordApiData returnData = await rest.Post($"channels/{channelId}/messages", requestData,
                "channels/channel/messages").ConfigureAwait(false);
            return new DiscordMessage(app, returnData);
        }

        /// <summary>
        /// Posts a message to a text channel with a file attachment.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="fileData"/> is null, 
        /// or if <paramref name="fileName"/> is null or only contains whitespace characters.
        /// </exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> CreateMessage(Snowflake channelId, Stream fileData, string fileName,
            DiscordMessageDetails details = null)
        {
            if (fileData == null)
                throw new ArgumentNullException(nameof(fileData));

            return CreateMessage(channelId, new StreamContent(fileData), fileName, details);
        }

        /// <summary>
        /// Posts a message to a text channel with a file attachment.
        /// <para>Note: Bot user accounts must connect to the Gateway at least once before being able to send messages.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="fileName"/> is null or only contains whitespace characters.
        /// </exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<DiscordMessage> CreateMessage(Snowflake channelId, ArraySegment<byte> fileData, string fileName,
            DiscordMessageDetails details = null)
        {
            return CreateMessage(channelId, new ByteArrayContent(fileData.Array, fileData.Offset, fileData.Count), fileName, details);
        }

        /// <exception cref="ArgumentNullException"></exception>
        async Task<DiscordMessage> CreateMessage(Snowflake channelId, HttpContent fileContent, string fileName, DiscordMessageDetails details)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                // Technically this is also handled when setting the field on the multipart form data
                throw new ArgumentNullException(nameof(fileName));

            DiscordApiData returnData = await rest.Send(() =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
                    $"{RestClient.BASE_URL}/channels/{channelId}/messages");

                MultipartFormDataContent data = new MultipartFormDataContent();
                data.Add(fileContent, "file", fileName);

                if (details != null)
                {
                    DiscordApiData payloadJson = new DiscordApiData();
                    payloadJson.Set("content", details.Content);
                    payloadJson.Set("tts", details.TextToSpeech);
                    payloadJson.Set("nonce", details.Nonce);

                    if (details.Embed != null)
                        payloadJson.Set("embed", details.Embed.Build());

                    data.Add(new StringContent(payloadJson.SerializeToJson()), "payload_json");
                }

                request.Content = data;

                return request;
            }, "channels/channel/messages").ConfigureAwait(false);
            return new DiscordMessage(app, returnData);
        }

        /// <summary>
        /// Edits an existing message in a text channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<DiscordMessage> EditMessage(Snowflake channelId, Snowflake messageId, string content)
        {
            DiscordApiData requestData = new DiscordApiData(DiscordApiDataType.Container);
            requestData.Set("content", content);

            DiscordApiData returnData = await rest.Patch($"channels/{channelId}/messages/{messageId}", requestData,
                "channels/channel/messages/message").ConfigureAwait(false);
            return new DiscordMessage(app, returnData);
        }

        /// <summary>
        /// Edits an existing message in a text channel.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<DiscordMessage> EditMessage(Snowflake channelId, Snowflake messageId, DiscordMessageEdit editDetails)
        {
            if (editDetails == null)
                throw new ArgumentNullException(nameof(editDetails));

            DiscordApiData requestData = new DiscordApiData(DiscordApiDataType.Container);
            requestData.Set("content", editDetails.Content);

            if (editDetails.Embed != null)
                requestData.Set("embed", editDetails.Embed.Build());

            DiscordApiData returnData = await rest.Patch($"channels/{channelId}/messages/{messageId}", requestData,
                "channels/channel/messages/message").ConfigureAwait(false);
            return new DiscordMessage(app, returnData);
        }

        /// <summary>
        /// Deletes a message from a text channel.
        /// </summary>
        /// <returns>Returns whether the operation was successful.</returns>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<bool> DeleteMessage(Snowflake channelId, Snowflake messageId)
        {
            DiscordApiData data = await rest.Delete($"channels/{channelId}/messages/{messageId}",
                "channels/channel/messages/message/delete").ConfigureAwait(false);
            return data.IsNull;
        }

        /// <summary>
        /// Deletes a group of messages all at once from a text channel.
        /// This is much faster than calling DeleteMessage for each message.
        /// </summary>
        /// <param name="filterTooOldMessages">Whether to ignore deleting messages that are older than 2 weeks (this causes an API error).</param>
        /// <returns>Returns whether the operation was successful.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public Task<bool> BulkDeleteMessages(Snowflake channelId, IEnumerable<DiscordMessage> messages,
            bool filterTooOldMessages = true)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            List<Snowflake> msgIds = new List<Snowflake>();
            foreach (DiscordMessage msg in messages)
                msgIds.Add(msg.Id);

            return BulkDeleteMessages(channelId, msgIds, filterTooOldMessages);
        }

        /// <summary>
        /// Deletes a group of messages all at once from a text channel.
        /// This is much faster than calling DeleteMessage for each message.
        /// </summary>
        /// <param name="filterTooOldMessages">Whether to ignore deleting messages that are older than 2 weeks (this causes an API error).</param>
        /// <returns>Returns whether the operation was successful.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<bool> BulkDeleteMessages(Snowflake channelId, IEnumerable<Snowflake> messageIds,
            bool filterTooOldMessages = true)
        {
            if (messageIds == null)
                throw new ArgumentNullException(nameof(messageIds));

            DiscordApiData requestData = new DiscordApiData(DiscordApiDataType.Container);
            DiscordApiData messages = requestData.Set("messages", new DiscordApiData(DiscordApiDataType.Array));

            ulong minimumAllowedSnowflake = 0;
            if (filterTooOldMessages)
            {
                // See https://github.com/hammerandchisel/discord-api-docs/issues/208

                ulong secondsSinceUnixEpoch = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds();
                minimumAllowedSnowflake = (secondsSinceUnixEpoch - 14 * 24 * 60 * 60) * 1000 - 1420070400000L << 22;
            }

            foreach (Snowflake messageId in messageIds)
            {
                if (!filterTooOldMessages && messageId.Id < minimumAllowedSnowflake)
                    continue;

                messages.Values.Add(new DiscordApiData(messageId));
            }

            DiscordApiData returnData = await rest.Post($"channels/{channelId}/messages/bulk-delete", requestData,
                "channels/channel/messages/message/delete/bulk").ConfigureAwait(false);
            return returnData.IsNull;
        }

        /// <summary>
        /// Gets a list of all pinned messages in a text channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<IReadOnlyList<DiscordMessage>> GetPinnedMessages(Snowflake channelId)
        {
            DiscordApiData data = await rest.Get($"channels/{channelId}/pins",
                "channels/channel/pins").ConfigureAwait(false);
            DiscordMessage[] messages = new DiscordMessage[data.Values.Count];

            for (int i = 0; i < messages.Length; i++)
                messages[i] = new DiscordMessage(app, data.Values[i]);

            return messages;
        }

        /// <summary>
        /// Pins a message in a text channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<bool> AddPinnedChannelMessage(Snowflake channelId, Snowflake messageId)
        {
            return (await rest.Put($"channels/{channelId}/pins/{messageId}",
                "channels/channel/pins/message").ConfigureAwait(false)).IsNull;
        }

        /// <summary>
        /// Unpins a message from a text channel.
        /// </summary>
        /// <exception cref="DiscordHttpApiException"></exception>
        public async Task<bool> DeletePinnedChannelMessage(Snowflake channelId, Snowflake messageId)
        {
            return (await rest.Delete($"channels/{channelId}/pins/{messageId}",
                "channels/channel/pins/message").ConfigureAwait(false)).IsNull;
        }
    }
}
