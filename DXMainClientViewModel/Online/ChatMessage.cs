using System;

namespace DXMainClientViewModel.Online
{
    public class ChatMessage : IChatMessage
    {
        /// <summary>
        /// Creates a new ChatMessage instance.
        /// </summary>
        /// <param name="senderName">The sender of the message. Use null for none (system messages).</param>
        /// <param name="r">Red component of the message color.</param>
        /// <param name="g">Green component of the message color.</param>
        /// <param name="b">Blue component of the message color.</param>
        /// <param name="dateTime">The date and time of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string senderName, int r, int g, int b, DateTime dateTime, string message)
        {
            SenderName = senderName;
            R = r;
            G = g;
            B = b;
            DateTime = dateTime;
            Message = message;
        }

        /// <summary>
        /// Creates a chat message with the date and time set to the current system date and time.
        /// </summary>
        /// <param name="senderName">The sender of the message. Use null for none (system messages).</param>
        /// <param name="r">Red component of the message color.</param>
        /// <param name="g">Green component of the message color.</param>
        /// <param name="b">Blue component of the message color.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string senderName, int r, int g, int b, string message) : this(senderName, r, g, b, DateTime.Now, message) { }

        /// <summary>
        /// Creates a new ChatMessage instance.
        /// </summary>
        /// <param name="senderName">The sender of the message. Use null for none (system messages).</param>
        /// <param name="ident">The IRC identifier of the sender.</param>
        /// <param name="senderIsAdmin">The sender of the message is a channel admin.</param>
        /// <param name="r">Red component of the message color.</param>
        /// <param name="g">Green component of the message color.</param>
        /// <param name="b">Blue component of the message color.</param>
        /// <param name="dateTime">The date and time of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string senderName, string ident, bool senderIsAdmin, int r, int g, int b, DateTime dateTime, string message) : this(senderName, r, g, b, dateTime, message)
        {
            SenderIdent = ident;
            SenderIsAdmin = senderIsAdmin;
        }

        /// <summary>
        /// Creates a chat message that has no sender and has the date and time set to the
        /// current system date and time.
        /// </summary>
        /// <param name="r">Red component of the message color.</param>
        /// <param name="g">Green component of the message color.</param>
        /// <param name="b">Blue component of the message color.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(int r, int g, int b, string message) : this(null, r, g, b, DateTime.Now, message) { }

        /// <summary>
        /// Creates a chat message that has no sender and has the date and time set to the
        /// current system date and time.
        /// </summary>
        /// <param name="message">The message.</param>
        public ChatMessage(string message) : this(255, 255, 255, message) { }

        public string SenderName { get; private set; }
        public string SenderIdent { get; private set; }
        public int R { get; private set; }
        public int G { get; private set; }
        public int B { get; private set; }
        public DateTime DateTime { get; private set; }
        public string Message { get; private set; }
        public bool SenderIsAdmin { get; private set; }

        public bool IsUser => SenderIdent != null;
    }
}
