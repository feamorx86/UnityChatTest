using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using MiscUtil.IO;
using MiscUtil.Conversion;
using JsonFx.Json;

public class DataMessage
{
    private int service;
    private int action;
    private string session;
    private byte[] data;
    private int dataLength;
    private int dataOffset;

    public DataMessage(int service, int action, string session)
    {
        this.service = service;
        this.action = action;
        this.session = session;
    }

    public DataMessage()
    {

    }

    public int DataLength
    {
        get { return dataLength; }
        set { dataLength = value; }
    }

    public int DataOffset
    {
        get { return dataOffset; }
        set { dataOffset = value; }
    }


    public int Action
    {
        get { return action; }
        set { action = value; }
    }


    public string Session
    {
        get { return session; }
        set { session = value; }
    }


    public byte[] Data
    {
        get { return data; }
        set { data = value; }
    }

    public int Service
    {
        get { return service; }
        set { service = value; }
    }

    public void setData(byte[] data, int dataOffset, int dataLength)
    {
        this.data = data;
        this.dataOffset = dataOffset;
        this.dataLength = dataLength;
    }

    public void setData(byte[] data)
    {
        this.data = data;
        this.dataOffset = 0;
        this.dataLength = data.Length;
    }

    private MemoryStream memoryStream;
    private EndianBinaryReader reader;
    private EndianBinaryWriter writer;

    public void createReader()
    {
        if (memoryStream != null)
            throw new Exception("Can't create Reader, memory stream already used!");
        memoryStream = new MemoryStream(data);
        reader = new EndianBinaryReader(BigEndianBitConverter.Big, memoryStream);
    }

    public void closeReader()
    {
        if (reader != null && memoryStream != null)
        {
            reader.Close();
            memoryStream.Close();
            reader = null;
            memoryStream = null;
        }
        else throw new Exception("Can't close Reader, memory stream or reader is null!");
    }

    public int readInt32()
    {
        return reader.ReadInt32();
    }

    public bool readBool()
    {
        return reader.ReadBoolean();
    }

    public string readString()
    {
        return readString(reader);
    }

    public Dictionary<string, object> readJson()
    {
        Dictionary<string, object> result = readJson(reader);
        return result;
    }


    public DataMessage createWriter()
    {
        if (memoryStream != null)
            throw new Exception("Can't create Writer, memory stream already used!");
        memoryStream = new MemoryStream();
        writer = new EndianBinaryWriter(BigEndianBitConverter.Big, memoryStream);
        return this;
    }

    public void closeWriter()
    {
        if (writer != null && memoryStream != null)
        {
            writer.Flush();
            memoryStream.Flush();

            dataOffset = 0;
            dataLength = (int)memoryStream.Position;
            data = memoryStream.GetBuffer();

            writer.Close();
            memoryStream.Close();
            writer = null;
            memoryStream = null;
        }
        else throw new Exception("Can't close Writer, memory stream or writer is null!");
    }

    public DataMessage writerInt32(int value)
    {
        writer.Write((Int32)value);
        return this;
    }

    public DataMessage writerLong(long value)
    {
        writer.Write((long)value);
        return this;
    }

    public DataMessage writerBool(bool value)
    {
        writer.Write(value);
        return this;
    }

    public DataMessage writerString(string value)
    {
        int lenght = -1;
        byte[] data = null;
        if (value != null)
        {
            data = Encoding.UTF8.GetBytes(value);
            lenght = data.Length;
        }
        writer.Write(lenght);
        if (data != null)
        {
            writer.Write(data);
        }
        return this;
    }

    public DataMessage writeJson(Dictionary<string, object> json)
    {
        JsonFx.Json.JsonWriter jsonWriter = new JsonFx.Json.JsonWriter();
        string data = jsonWriter.Write((Dictionary<string, object>)json);
        writerString(data);
        return this;
    }

    public EndianBinaryReader getReader()
    {
        return reader;
    }

    public EndianBinaryWriter getWriter()
    {
        return writer;
    }

    public static System.String readString(EndianBinaryReader reader)
    {
        int lenght = reader.ReadInt32();
        String result = null;
        if (lenght > 0)
        {
            byte[] data = reader.ReadBytes(lenght);
            result = Encoding.UTF8.GetString(data);
        }
        return result;
    }

    public static Dictionary<string, System.Object> readJson(EndianBinaryReader reader)
    {
        String jsonString = readString(reader);
        Dictionary<String, System.Object> json = null;
        if (!String.IsNullOrEmpty(jsonString))
        {
            JsonReader jr = new JsonReader();
            json = jr.Read<Dictionary<String, System.Object>>(jsonString);
        }
        return json;
    }
}