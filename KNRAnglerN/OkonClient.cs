﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KNRAnglerN
{
    public class OkonClient : IDisposable
    {
        public const int Version = 3;
        public event PacketEventHandler PacketReceived;
        public readonly string hostname;
        public readonly ushort port;
        private readonly IInfo _info;
        private volatile bool _connected;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _thread;
        private ConcurrentQueue<Packet> _toSend;

        public OkonClient(string hostname, ushort port, IInfo info)
        {
            this.hostname = hostname;
            this.port = port;
            this._info = info;
            _info.YeetLog("OkonClient instance created");
            _toSend = new ConcurrentQueue<Packet>();
        }
        public void Connect()
        {
            if (_connected) throw new Exception("Already connected");
            _connected = true;
            try
            {
                _client = new TcpClient(hostname, port);
                _stream = _client.GetStream();
                _thread = new Thread(Transreceive)
                {
                    Name = "Transreceive",
                    IsBackground = true
                };

                _thread.Start();
                _info.YeetLog("Connection successful");
            }
            catch (Exception exp)
            {
                _info.YeetException(exp);
                throw;
            }
        }
        private void Transreceive()
        {
            byte packetType, packetFlag;
            byte[] dataLenBytes = new byte[4];
            byte[] dataBytes;
            try
            {
                while (_client.Connected)
                {
                    if (_stream.DataAvailable)
                    {
                        packetType = ReadByteFromStream(_stream);
                        packetFlag = ReadByteFromStream(_stream);
                        ReadAllFromStream(_stream, dataLenBytes, 4);
                        int dataLength = System.BitConverter.ToInt32(dataLenBytes, 0);
                        dataBytes = new byte[dataLength];
                        ReadAllFromStream(_stream, dataBytes, dataLength);
                        PacketReceived(this, new PacketEventArgs(packetType, packetFlag, dataBytes));
                        _info.YeetLog("Received packet type: " + packetType.ToString("x2") + " flag:" + Convert.ToString(packetFlag, 2).PadLeft(8, '0') + " len: " + dataLength);
                    }
                    else
                    {
                        if (!_toSend.IsEmpty)
                        {
                            while (!_toSend.IsEmpty)
                                if (_toSend.TryDequeue(out Packet packet))
                                    Send(packet.packetType, packet.flag, packet.bytes);
                        }
                        else Thread.Sleep(1);
                    }
                }
            }
            catch (Exception exp)
            {
                //_info.YeetLog()
                _info.YeetException(exp);
            }
            finally
            {
                Disconnect();
            }
        }

        public void EnqueuePacket(byte packetType, byte packetFlag, string json) => _toSend.Enqueue(new Packet(packetType, packetFlag, Encoding.ASCII.GetBytes(json)));
        public void EnqueuePacket(byte packetType, string json) => _toSend.Enqueue(new Packet(packetType, 0, Encoding.ASCII.GetBytes(json)));
        public void EnqueuePacket(byte packetType, byte packetFlag, byte[] bytes) => _toSend.Enqueue(new Packet(packetType, packetFlag, bytes));
        public void EnqueuePacket(byte packetType, byte[] bytes) => _toSend.Enqueue(new Packet(packetType, 0, bytes));


        private void Send(byte packetType, byte packetFlag, string json)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(json);
            _stream.WriteByte(packetType);
            _stream.WriteByte(packetFlag);
            _stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
            _stream.Write(bytes, 0, bytes.Length);
        }
            
        private void Send(byte packetType, byte packetFlag, byte[] bytes)
        {
            _stream.WriteByte(packetType);
            _stream.WriteByte(packetFlag);
            _stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
            _stream.Write(bytes, 0, bytes.Length);
        }

        private void SendBytes(byte packetType, byte[] data)
        {
            _stream.WriteByte(packetType);
            _stream.WriteByte(0);
            _stream.Write(BitConverter.GetBytes(data.Length), 0, 4);
            _stream.Write(data, 0, data.Length);
        }

        private void SendString(byte packetType, string data)
        {
            if (_stream == null) return;
            _stream.WriteByte(packetType);
            _stream.WriteByte(0);
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            _stream.Write(System.BitConverter.GetBytes(bytes.Length), 0, 4);
            if (bytes.Length > 0) _stream.Write(bytes, 0, bytes.Length);
        }

        public void Disconnect()
        {
            if (!_connected) return;
            _thread?.Abort();
            _stream?.Dispose();
            _client?.Dispose();
            _info.YeetLog("Disconnected");
        }

        public void Dispose() => Disconnect();

        public delegate void PacketEventHandler(object source, PacketEventArgs e);
        public class PacketEventArgs : EventArgs
        {
            public readonly byte[] packetData;
            public readonly byte packetType;
            public readonly byte packetFlag;
            public PacketEventArgs(byte type, byte flag, byte[] data)
            {
                this.packetType = type;
                this.packetFlag = flag;
                this.packetData = data;
            }
        }
        private static void ReadAllFromStream(NetworkStream stream, byte[] buffer, int len)
        {
            int current = 0;
            while (current < buffer.Length)
                current += stream.Read(buffer, current, len - current > buffer.Length ? buffer.Length : len - current);
        }

        private static byte ReadByteFromStream(NetworkStream stream)
        {
            int ret;
            do ret = stream.ReadByte();
            while (ret == -1);
            return (byte)ret;
        }

        public interface IInfo
        {
            void YeetException(Exception exp);
            void YeetLog(string info);
        }

        public bool IsConnected() {
            return _connected;
        }

        public struct Packet
        {
            public byte packetType;
            public byte flag;
            public byte[] bytes;

            public Packet(byte packetType, byte flag, byte[] bytes)
            {
                this.packetType = packetType;
                this.flag = flag;
                this.bytes = bytes;
            }
        }
    }
}
