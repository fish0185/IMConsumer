using System;

namespace IMConsumer.Model
{
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(MessageResponse messageResponse)
        {
            MessageResponse = messageResponse;
        }

        public MessageResponse MessageResponse { get; }
    }
}
