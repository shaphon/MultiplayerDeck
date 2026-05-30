using GameDataEditor;
using MultiplayerDeck.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MultiplayerDeck
{
    public static class StageMapSerializer
    {
        public static NetStageMapPacket mapPacket;

        [Serializable]
        public class NetStageMapPacket
        {
            public string StageKey;
            public int StageNum;
            public Data_Map MapData;
        }

        public static NetStageMapPacket CreateMapPacket()
        {

            var mapData = new Data_Map();
            mapData.Save();     // 依赖StageSystem.instance.Map

            return new NetStageMapPacket
            {
                StageKey = mapData.StageDataKey,
                StageNum = PlayData.TSavedata.StageNum,
                MapData = mapData
            };
        }

        /// <summary>
        /// 序列化地图数据为 XML 字节数组（不含消息头，由 MessageDispatcher 处理）。
        /// </summary>
        public static byte[] SerializeMapPayload(NetStageMapPacket packet)
        {
            var serializer = new XmlSerializer(typeof(NetStageMapPacket), ExtraTypes);

            using (var payloadStream = new MemoryStream())
            using (var writer = new StreamWriter(payloadStream, Encoding.UTF8))
            {
                serializer.Serialize(writer, packet);
                writer.Flush();
                return payloadStream.ToArray();
            }
        }

        /// <summary>
        /// 从 XML 字节数组反序列化地图数据（不含消息头）。
        /// </summary>
        public static NetStageMapPacket DeserializeMapPacketFromPayload(byte[] payload)
        {
            var serializer = new XmlSerializer(typeof(NetStageMapPacket), ExtraTypes);

            using (var payloadStream = new MemoryStream(payload))
            using (var reader = new StreamReader(payloadStream, Encoding.UTF8))
            {
                return (NetStageMapPacket)serializer.Deserialize(reader);
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
