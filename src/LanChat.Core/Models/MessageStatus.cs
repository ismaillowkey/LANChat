namespace LanChat.Core.Models;

public enum MessageStatus
{
    /// <summary>
    /// Centang 1 abu-abu: Pesan sudah dikirim dari sender, tetapi belum diterima oleh receiver (receiver offline/belum connect).
    /// </summary>
    Sent = 0,

    /// <summary>
    /// Centang 2 abu-abu: Pesan sudah diterima oleh device receiver, tetapi belum dibuka/dibaca oleh receiver.
    /// </summary>
    Delivered = 1,

    /// <summary>
    /// Centang 2 biru: Pesan sudah dibuka dan dibaca oleh receiver di ruang chat.
    /// </summary>
    Read = 2
}
