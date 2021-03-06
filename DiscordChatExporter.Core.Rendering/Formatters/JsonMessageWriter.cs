﻿using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Models;
using DiscordChatExporter.Core.Rendering.Internal;
using DiscordChatExporter.Core.Rendering.Logic;

namespace DiscordChatExporter.Core.Rendering.Formatters
{
    public class JsonMessageWriter : MessageWriterBase
    {
        private readonly Utf8JsonWriter _writer;

        private long _messageCount;

        public JsonMessageWriter(Stream stream, RenderContext context)
            : base(stream, context)
        {
            _writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true
            });
        }

        public override async Task WritePreambleAsync()
        {
            // Root object (start)
            _writer.WriteStartObject();

            // Guild
            _writer.WriteStartObject("guild");
            _writer.WriteString("id", Context.Guild.Id);
            _writer.WriteString("name", Context.Guild.Name);
            _writer.WriteString("iconUrl", Context.Guild.IconUrl);
            _writer.WriteEndObject();

            // Channel
            _writer.WriteStartObject("channel");
            _writer.WriteString("id", Context.Channel.Id);
            _writer.WriteString("type", Context.Channel.Type.ToString());
            _writer.WriteString("name", Context.Channel.Name);
            _writer.WriteString("topic", Context.Channel.Topic);
            _writer.WriteEndObject();

            // Date range
            _writer.WriteStartObject("dateRange");
            _writer.WriteString("after", Context.After);
            _writer.WriteString("before", Context.Before);
            _writer.WriteEndObject();

            // Message array (start)
            _writer.WriteStartArray("messages");

            await _writer.FlushAsync();
        }

        public override async Task WriteMessageAsync(Message message)
        {
            _writer.WriteStartObject();

            // Metadata
            _writer.WriteString("id", message.Id);
            _writer.WriteString("type", message.Type.ToString());
            _writer.WriteString("timestamp", message.Timestamp);
            _writer.WriteString("timestampEdited", message.EditedTimestamp);
            _writer.WriteBoolean("isPinned", message.IsPinned);

            // Content
            var content = PlainTextRenderingLogic.FormatMessageContent(Context, message);
            _writer.WriteString("content", content);

            // Author
            _writer.WriteStartObject("author");
            _writer.WriteString("id", message.Author.Id);
            _writer.WriteString("name", message.Author.Name);
            _writer.WriteString("discriminator", $"{message.Author.Discriminator:0000}");
            _writer.WriteBoolean("isBot", message.Author.IsBot);
            _writer.WriteString("avatarUrl", message.Author.AvatarUrl);
            _writer.WriteEndObject();

            // Attachments
            _writer.WriteStartArray("attachments");

            foreach (var attachment in message.Attachments)
            {
                _writer.WriteStartObject();

                _writer.WriteString("id", attachment.Id);
                _writer.WriteString("url", attachment.Url);
                _writer.WriteString("fileName", attachment.FileName);
                _writer.WriteNumber("fileSizeBytes", (long) attachment.FileSize.Bytes);

                _writer.WriteEndObject();
            }

            _writer.WriteEndArray();

            // Embeds
            _writer.WriteStartArray("embeds");

            foreach (var embed in message.Embeds)
            {
                _writer.WriteStartObject();

                _writer.WriteString("title", embed.Title);
                _writer.WriteString("url", embed.Url);
                _writer.WriteString("timestamp", embed.Timestamp);
                _writer.WriteString("description", embed.Description);

                // Author
                if (embed.Author != null)
                {
                    _writer.WriteStartObject("author");
                    _writer.WriteString("name", embed.Author.Name);
                    _writer.WriteString("url", embed.Author.Url);
                    _writer.WriteString("iconUrl", embed.Author.IconUrl);
                    _writer.WriteEndObject();
                }

                // Thumbnail
                if (embed.Thumbnail != null)
                {
                    _writer.WriteStartObject("thumbnail");
                    _writer.WriteString("url", embed.Thumbnail.Url);
                    _writer.WriteNumber("width", embed.Thumbnail.Width);
                    _writer.WriteNumber("height", embed.Thumbnail.Height);
                    _writer.WriteEndObject();
                }

                // Image
                if (embed.Image != null)
                {
                    _writer.WriteStartObject("image");
                    _writer.WriteString("url", embed.Image.Url);
                    _writer.WriteNumber("width", embed.Image.Width);
                    _writer.WriteNumber("height", embed.Image.Height);
                    _writer.WriteEndObject();
                }

                // Footer
                if (embed.Footer != null)
                {
                    _writer.WriteStartObject("footer");
                    _writer.WriteString("text", embed.Footer.Text);
                    _writer.WriteString("iconUrl", embed.Footer.IconUrl);
                    _writer.WriteEndObject();
                }

                // Fields
                _writer.WriteStartArray("fields");

                foreach (var field in embed.Fields)
                {
                    _writer.WriteStartObject();

                    _writer.WriteString("name", field.Name);
                    _writer.WriteString("value", field.Value);
                    _writer.WriteBoolean("isInline", field.IsInline);

                    _writer.WriteEndObject();
                }

                _writer.WriteEndArray();

                _writer.WriteEndObject();
            }

            _writer.WriteEndArray();

            // Reactions
            _writer.WriteStartArray("reactions");

            foreach (var reaction in message.Reactions)
            {
                _writer.WriteStartObject();

                // Emoji
                _writer.WriteStartObject("emoji");
                _writer.WriteString("id", reaction.Emoji.Id);
                _writer.WriteString("name", reaction.Emoji.Name);
                _writer.WriteBoolean("isAnimated", reaction.Emoji.IsAnimated);
                _writer.WriteString("imageUrl", reaction.Emoji.ImageUrl);
                _writer.WriteEndObject();

                // Count
                _writer.WriteNumber("count", reaction.Count);

                _writer.WriteEndObject();
            }

            _writer.WriteEndArray();

            _writer.WriteEndObject();

            _messageCount++;

            // Flush every 100 messages
            if (_messageCount % 100 == 0)
                await _writer.FlushAsync();
        }

        public override async Task WritePostambleAsync()
        {
            // Message array (end)
            _writer.WriteEndArray();

            // Message count
            _writer.WriteNumber("messageCount", _messageCount);

            // Root object (end)
            _writer.WriteEndObject();

            await _writer.FlushAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            await _writer.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}