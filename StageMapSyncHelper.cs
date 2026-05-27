using GameDataEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MultiplayerDeck
{
    public static class StageMapSyncHelper
    {
        public static NetStageMapPacket mapPacket;

        [Serializable]
        public class NetStageMapPacket
        {
            public string StageKey;
            public int StageNum;
            public Data_Map MapData;
        }

        public static NetStageMapPacket CreateMapPacket(HexMap map)
        {

            //var mapData = new Data_Map();
            //mapData.Save();
            // 如果你现在仍然用 mapData.Save()，它依赖 StageSystem.instance.Map，
            // 在 Postfix 里 StageSystem.instance.Map 可能还没赋值成 __result。
            // 最好写一个 SaveFromMap(map)。
            var mapData = SaveFromMap(map);

            return new NetStageMapPacket
            {
                StageKey = mapData.StageDataKey,
                StageNum = PlayData.TSavedata.StageNum,
                MapData = mapData
            };
        }

        public static Data_Map SaveFromMap(HexMap map)
        {
            var data = new Data_Map();

            data.StageDataKey = map.StageData.Key;
            data.MapSize = map.Size;
            data.MapTileInfo = new List<List<Data_MapTileData>>();

            for (int x = 0; x < map.Size.x; x++)
            {
                data.MapTileInfo.Add(new List<Data_MapTileData>());

                for (int y = 0; y < map.Size.y; y++)
                {
                    data.MapTileInfo[x].Add(new Data_MapTileData
                    {
                        MainTileInfo = map.MapObject[x, y].Info
                    });
                }
            }

            foreach (MapTile eventTile in map.EventTileList)
            {
                var tileData = data.MapTileInfo[(int)eventTile.Pos.x][(int)eventTile.Pos.y];
                tileData.IsEventList = true;

                if (eventTile.TileEventObject != null &&
                    eventTile.TileEventObject.MainBaseEventObject != null &&
                    eventTile.TileEventObject.MainBaseEventObject.MainSaveData != null)
                {
                    tileData.BaseEvent = eventTile.TileEventObject.MainBaseEventObject.MainSaveData;
                    tileData.BaseEvent.Save();
                    tileData.BaseEvent.IsUsed = eventTile.TileEventObject.BUseless;
                    tileData.BaseEvent.EventMonster = eventTile.TileEventObject.Monster;
                }
            }

            if (map.MainCamp != null)
            {
                map.MainCamp.Save.Save();
                data.CampSaveData = map.MainCamp.Save;
            }

            return data;
        }

        public static byte[] SerializeMapPacket(NetStageMapPacket packet)
        {
            var serializer = new XmlSerializer(typeof(NetStageMapPacket), ExtraTypes);

            byte[] payload;

            using (var payloadStream = new MemoryStream())
            using (var writer = new StreamWriter(payloadStream, Encoding.UTF8))
            {
                serializer.Serialize(writer, packet);
                writer.Flush();
                payload = payloadStream.ToArray();
            }

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((int)NetDataType.StageMap);
                bw.Write(payload.Length);
                bw.Write(payload);
                return ms.ToArray();
            }
        }

        public static NetStageMapPacket DeserializeMapPacket(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var br = new BinaryReader(ms))
            {
                NetDataType type = (NetDataType)br.ReadInt32();

                if (type != NetDataType.StageMap)
                    throw new InvalidOperationException("Invalid packet type: " + type);

                int payloadLength = br.ReadInt32();

                if (payloadLength < 0 || payloadLength > ms.Length - ms.Position)
                    throw new InvalidOperationException("Invalid payload length: " + payloadLength);

                byte[] payload = br.ReadBytes(payloadLength);

                var serializer = new XmlSerializer(typeof(NetStageMapPacket), ExtraTypes);

                using (var payloadStream = new MemoryStream(payload))
                using (var reader = new StreamReader(payloadStream, Encoding.UTF8))
                {
                    return (NetStageMapPacket)serializer.Deserialize(reader);
                }
            }
        }

        public static Type[] BuildMapExtraTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    (
                        typeof(TileType).IsAssignableFrom(t) ||
                        typeof(BaseEventClass).IsAssignableFrom(t)
                    ))
                .ToArray();
        }

        private static Type[] extraTypes;
        private static Type[] ExtraTypes
        {
            get
            {
                if (extraTypes == null)
                {
                    extraTypes = BuildMapExtraTypes();
                }
                return extraTypes;
            }
        }

        public static HexMap BuildMapFromData(Data_Map data, GDEStageData stageData)
        {
            HexMap hexMap = new HexMap();
            hexMap.StageData = stageData;
            hexMap.MapObject = new MapTile[(int)data.MapSize.x, (int)data.MapSize.y];

            for (int x = 0; x < data.MapSize.x; x++)
            {
                for (int y = 0; y < data.MapSize.y; y++)
                {
                    Data_MapTileData tileData = data.MapTileInfo[x][y];

                    hexMap.MapObject[x, y] = new MapTile();
                    hexMap.MapObject[x, y].Info = tileData.MainTileInfo;
                    hexMap.MapObject[x, y].MyMap = hexMap;
                    hexMap.MapObject[x, y].SaveMapTileData = tileData;

                    if (tileData.BaseEvent != null)
                        tileData.BaseEvent.Load();

                    if (tileData.IsEventList)
                        hexMap.EventTileList.Add(hexMap.MapObject[x, y]);
                }
            }

            return hexMap;
        }

        public static HexMap LoadRemoteMap(NetStageMapPacket packet)
        {
            if (packet == null || packet.MapData == null)
                throw new Exception("Invalid map packet");

            var stageData = new GDEStageData(packet.StageKey);

            PlayData.TSavedata.Data_StageMapData = packet.MapData;
            PlayData.TSavedata.NowStageMapKey = packet.StageKey;

            return BuildMapFromData(packet.MapData, stageData);
        }
    }
}
