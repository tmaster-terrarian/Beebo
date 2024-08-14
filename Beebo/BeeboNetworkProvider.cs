using System.IO;
using System.Text.Json;
using Beebo.GameContent;
using Beebo.Net;

using Jelly;
using Jelly.Net;
using Jelly.Unsafe;
using Microsoft.Xna.Framework;

namespace Beebo;

public class BeeboNetworkProvider : NetworkProvider
{
    public override bool NetworkingEnabled => Program.UseSteamworks && SteamManager.IsSteamRunning;

    public override bool IsHost => Main.IsHost;

    public override int GetNetID() => Main.NetID;

    public override int GetHostNetID()
    {
        return P2PManager.GetMemberIndex(P2PManager.GetLobbyOwner());
    }

    public override void SendSyncPacket(SyncPacketType syncPacketType, byte[] data, bool important)
    {
        switch(syncPacketType)
        {
            case SyncPacketType.EntityAdded: // 0x00:sceneId 0x08:entityId 0x10:..stringData
            {
                if(Main.Scene is null)
                    break;

                long entityId = 0;

                using(var reader = new BinaryReader(new MemoryStream(data[0x8..])))
                {
                    entityId = reader.ReadInt64();
                }

                Entity entity = Main.Scene.Entities.FindByID(entityId);
                if(entity is null)
                    break;

                byte[] buffer;

                {
                    using var stream = new MemoryStream();
                    var writer = new BinaryWriter(stream);

                    writer.Write(entity.Serialize());

                    buffer = [..data[..0x10], ..stream.GetBuffer()];
                }

                P2PManager.SendP2PPacket(PacketType.JellySync, [(byte)syncPacketType, ..buffer], important ? PacketSendMethod.Reliable : PacketSendMethod.Unreliable);
                break;
            }
            case SyncPacketType.ComponentAdded: // 0x00:sceneId 0x08:entityId 0x10:componentId 0x18:..stringData
            case SyncPacketType.ComponentUpdate:
            {
                if(Main.Scene is null)
                    break;

                long entityId = 0;
                long componentId = 0;

                using(var reader = new BinaryReader(new MemoryStream(data[0x8..])))
                {
                    entityId = reader.ReadInt64();
                    componentId = reader.ReadInt64();
                }

                Entity entity = Main.Scene.Entities.FindByID(entityId);
                if(entity is null)
                    break;

                Component component = entity.Components.FindByID(componentId);
                if(component is null)
                    break;

                byte[] buffer;

                {
                    using var stream = new MemoryStream();
                    var writer = new BinaryWriter(stream);

                    writer.Write(component.Serialize());

                    buffer = [..data[..0x18], ..stream.GetBuffer()];
                }

                P2PManager.SendP2PPacket(PacketType.JellySync, [(byte)syncPacketType, ..buffer], important ? PacketSendMethod.Reliable : PacketSendMethod.Unreliable);
                break;
            }
            default:
                P2PManager.SendP2PPacket(PacketType.JellySync, [(byte)syncPacketType, ..data], important ? PacketSendMethod.Reliable : PacketSendMethod.Unreliable);
                break;
        }
    }

    public static void PacketInterceptRead(byte[] data, int netId)
    {
        using var stream = new MemoryStream(data);
        var reader = new BinaryReader(stream);

        var scene = Main.Scene;

        switch((SyncPacketType)reader.ReadByte())
        {
            case SyncPacketType.EntityAdded:
            {
                if(scene is null) break;

                if(scene.SceneID != reader.ReadInt64())
                    break;

                var id = reader.ReadInt64();

                if(scene.Entities.FindByID(id) is Entity entity)
                {
                    entity.IgnoreNextSync();
                    scene.Entities.Remove(entity);
                }

                entity = EntityDef.Deserialize(reader.ReadString())?.Create(scene);
                break;
            }
            case SyncPacketType.ComponentAdded:
            case SyncPacketType.ComponentUpdate:
            {
                if(scene is null) break;

                if(scene.SceneID != reader.ReadInt64())
                    break;

                var entityId = reader.ReadInt64();
                var componentId = reader.ReadInt64();

                if(scene.Entities.FindByID(entityId) is Entity entity)
                {
                    entity.IgnoreNextSync();

                    if(entity.Components.FindByID(componentId) is Component component)
                    {
                        component.IgnoreNextSync();
                        entity.Remove(component);
                    }

                    component = JsonSerializer.Deserialize<Component>(reader.ReadString(), RegistryManager.SerializerOptions);
                    component.IgnoreNextSync();

                    entity.Add(component);

                    component.ReadPacket(data[(int)stream.Position..]);
                }

                break;
            }
        }
    }
}
