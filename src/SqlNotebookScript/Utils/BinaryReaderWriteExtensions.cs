using System;
using System.IO;

namespace SqlNotebookScript.Utils;

public static class BinaryReaderWriterExtensions
{
    public static object ReadScalar(this BinaryReader reader)
    {
        var which = reader.ReadByte();
        return which switch
        {
            1 => reader.ReadInt32(),
            2 => reader.ReadInt64(),
            3 => reader.ReadSingle(),
            4 => reader.ReadDouble(),
            5 => reader.ReadString(),
            _ => throw new Exception("The file is corrupt."),
        };
    }

    public static void WriteScalar(this BinaryWriter writer, object value)
    {
        switch (value)
        {
            case int a:
                writer.Write((byte)1);
                writer.Write(a);
                break;

            case long a:
                writer.Write((byte)2);
                writer.Write(a);
                break;

            case float a:
                writer.Write((byte)3);
                writer.Write(a);
                break;

            case double a:
                writer.Write((byte)4);
                writer.Write(a);
                break;

            default:
                writer.Write((byte)5);
                writer.Write(value.ToString());
                break;
        }
    }
}
