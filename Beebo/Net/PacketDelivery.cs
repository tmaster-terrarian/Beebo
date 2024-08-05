using Steamworks;

namespace Beebo.Net;

public enum PacketDelivery
{
    Unreliable = EP2PSend.k_EP2PSendUnreliable,
    UnreliableNoDelay = EP2PSend.k_EP2PSendUnreliableNoDelay,
    Reliable = EP2PSend.k_EP2PSendReliable,
    ReliableWithBuffering = EP2PSend.k_EP2PSendReliableWithBuffering,
}
