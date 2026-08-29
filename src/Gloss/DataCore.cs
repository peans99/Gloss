using System.Text;

namespace Gloss;

/// <summary>One record in the DataCore: a game object with a path and a type.</summary>
public sealed record DataRecord(string Name, string FileName, int StructIndex, Guid Hash);

/// <summary>
/// Reads Star Citizen's DataCore blob, <c>Data\Game2.dcb</c>.
/// </summary>
/// <remarks>
/// <para>
/// The file is a schema and a set of records: struct definitions describing
/// types, property definitions describing their fields, typed value arrays, and
/// record instances that index into all of it. Everything is offsets into two
/// string tables, which is the detail that makes or breaks a reader — names come
/// from the <b>blob</b> table and file paths from the <b>text</b> table, and
/// mixing them up produces confident nonsense that lands mid-string.
/// </para>
/// <para>
/// The layout is undocumented by CIG. It is legible because the community worked
/// it out — scdatatools and the DataForge lineage in unp4k — and published what
/// they found. This is our own implementation of that understanding; nothing of
/// theirs is vendored. See CREDITS.md.
/// </para>
/// <para>
/// The header declares the value counts in a different order from the one the
/// sections appear in, which is a trap worth stating: booleans are declared
/// sixth and stored ninth. Getting that wrong shifts every offset after it.
/// </para>
/// </remarks>
public sealed class DataCore
{
    private readonly byte[] _data;
    private readonly long _textOffset;
    private readonly long _blobOffset;

    public int FileVersion { get; }
    public int StructDefinitionCount { get; }
    public int PropertyDefinitionCount { get; }
    public int EnumDefinitionCount { get; }
    public int RecordDefinitionCount { get; }

    private readonly long _structOffset;
    private readonly long _recordOffset;
    private readonly int _recordSize;

    public DataCore(byte[] data)
    {
        _data = data;

        int I(long at) => BitConverter.ToInt32(_data, (int)at);
        uint U(long at) => BitConverter.ToUInt32(_data, (int)at);

        FileVersion = I(4);

        // Counts, in the order the header declares them.
        var at = 0x10L;
        int Next() { var v = I(at); at += 4; return v; }

        StructDefinitionCount = Next();
        PropertyDefinitionCount = Next();
        EnumDefinitionCount = Next();
        var dataMappingCount = Next();
        RecordDefinitionCount = Next();

        var boolean = Next();
        var int8 = Next(); var int16 = Next(); var int32 = Next(); var int64 = Next();
        var uint8 = Next(); var uint16 = Next(); var uint32 = Next(); var uint64 = Next();
        var single = Next(); var dbl = Next(); var guid = Next();
        var str = Next(); var locale = Next(); var @enum = Next();
        var strong = Next(); var weak = Next(); var reference = Next();
        var enumOption = Next();

        var textLength = U(at); at += 4;
        var blobLength = U(at);

        // Sections follow the header at 0x78, in this order. Note it is NOT the
        // order the counts are declared in.
        _structOffset = 0x78;
        var propertyOffset = _structOffset + StructDefinitionCount * 16L;
        var enumOffset = propertyOffset + PropertyDefinitionCount * 12L;
        var mappingOffset = enumOffset + EnumDefinitionCount * 8L;
        _recordOffset = mappingOffset + dataMappingCount * 8L;

        _recordSize = FileVersion < 8 ? 32 : 36;
        var cursor = _recordOffset + RecordDefinitionCount * (long)_recordSize;

        cursor += int8 * 1L; cursor += int16 * 2L; cursor += int32 * 4L; cursor += int64 * 8L;
        cursor += uint8 * 1L; cursor += uint16 * 2L; cursor += uint32 * 4L; cursor += uint64 * 8L;
        cursor += boolean * 1L;
        cursor += single * 4L; cursor += dbl * 8L;
        cursor += guid * 16L;
        cursor += str * 4L; cursor += locale * 4L; cursor += @enum * 4L;
        cursor += strong * 8L; cursor += weak * 8L;
        cursor += reference * 20L;
        cursor += enumOption * 4L;

        _textOffset = cursor;
        _blobOffset = _textOffset + textLength;

        TextLength = textLength;
        BlobLength = blobLength;
    }

    public uint TextLength { get; }
    public uint BlobLength { get; }
    public long TextOffset => _textOffset;
    public long BlobOffset => _blobOffset;

    /// <summary>True when the computed offsets land on readable string tables.</summary>
    /// <remarks>
    /// Worth checking rather than assuming: every offset after a mis-sized
    /// section is wrong, and the failure looks like plausible fragments rather
    /// than an exception.
    /// </remarks>
    public bool LooksSane =>
        _textOffset > 0 && _blobOffset + BlobLength <= _data.LongLength
        && Readable(_textOffset) && Readable(_blobOffset);

    private bool Readable(long at)
    {
        if (at < 0 || at + 256 > _data.LongLength) return false;

        var printable = 0;
        for (var i = at; i < at + 256; i++)
        {
            var b = _data[i];
            if (b == 0) continue;
            if (b < 32 || b >= 127) return false;
            printable++;
        }

        return printable > 32;
    }

    private string StringAt(long table, uint offset)
    {
        var at = table + offset;
        if (at < 0 || at >= _data.LongLength) return string.Empty;

        var end = at;
        while (end < _data.LongLength && _data[end] != 0) end++;

        return Encoding.UTF8.GetString(_data, (int)at, (int)(end - at));
    }

    /// <summary>A type name, which lives in the blob table.</summary>
    public string Blob(uint offset) => StringAt(_blobOffset, offset);

    /// <summary>A record path, which lives in the text table.</summary>
    public string Text(uint offset) => StringAt(_textOffset, offset);

    /// <summary>The name of struct definition <paramref name="index"/>.</summary>
    public string StructName(int index)
    {
        if (index < 0 || index >= StructDefinitionCount) return string.Empty;
        return Blob(BitConverter.ToUInt32(_data, (int)(_structOffset + index * 16L)));
    }

    /// <summary>Every record, with its path and type.</summary>
    public IEnumerable<DataRecord> Records()
    {
        for (var i = 0; i < RecordDefinitionCount; i++)
        {
            var at = _recordOffset + i * (long)_recordSize;

            var name = Blob(BitConverter.ToUInt32(_data, (int)at));
            var fileName = Text(BitConverter.ToUInt32(_data, (int)(at + 4)));
            var structIndex = BitConverter.ToInt32(_data, (int)(at + 12));

            yield return new DataRecord(name, fileName, structIndex, Guid.Empty);
        }
    }
}
