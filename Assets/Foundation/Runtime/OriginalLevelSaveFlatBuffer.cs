using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // BLevelSaver 0x23e04e8 and the FLevelSaveData accessors. References
    // are forward uoffsets and table fields use the recovered vtable slots.
    internal static class OriginalLevelSaveFlatBuffer
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly Shape Root = new Shape(4, 4, 4, 4, 4, 4);
        private static readonly Shape Validation = new Shape(4, 8, 4);
        private static readonly Shape Board = new Shape(4, 4, 4);
        private static readonly Shape Grid = new Shape(4, 4, 4, 4);
        private static readonly Shape Cell = new Shape(4, 4);
        private static readonly Shape Item = new Shape(2, 2);
        private static readonly Shape Pair = new Shape(4, 4);
        private static readonly Shape Queue = new Shape(4);

        internal static byte[] Encode(FLevelSaveData data)
        {
            using var stream = new MemoryStream(4096);
            using var output = new BinaryWriter(stream);
            var writer = new Writer(output);
            output.Write(0);
            writer.Link(0, writer.Level(data));
            return stream.ToArray();
        }

        internal static FLevelSaveData Decode(byte[] bytes)
        {
            var reader = new Reader(bytes);
            return reader.Level();
        }

        private static int Align(int value, int alignment) => (value + alignment - 1) & -alignment;

        private sealed class Shape
        {
            internal readonly int[] Fields;
            internal readonly int Alignment = 4;
            internal readonly int Size;
            internal int VtableSize => 4 + Fields.Length * 2;

            internal Shape(params int[] widths)
            {
                Fields = new int[widths.Length];
                int cursor = 4;
                for (int i = 0; i < widths.Length; i++)
                {
                    int width = widths[i];
                    Alignment = Math.Max(Alignment, width);
                    Fields[i] = cursor = Align(cursor, width);
                    cursor += width;
                }
                Size = Align(cursor, Alignment);
            }
        }

        private readonly struct Table
        {
            internal readonly int Body, Vtable;
            internal readonly Shape Shape;
            internal Table(int body, int vtable, Shape shape)
            { Body = body; Vtable = vtable; Shape = shape; }
        }

        private sealed class Writer
        {
            private readonly BinaryWriter output;
            private Stream Stream => output.BaseStream;
            private int Position => (int)Stream.Position;
            internal Writer(BinaryWriter output) { this.output = output; }

            private void Reserve(int end)
            {
                Stream.SetLength(end);
                Stream.Position = end;
            }

            private Table Start(Shape shape)
            {
                int vtable = Align(Position, 2);
                int body = Align(vtable + shape.VtableSize, shape.Alignment);
                Reserve(body + shape.Size);
                Stream.Position = vtable;
                output.Write((ushort)shape.VtableSize);
                output.Write((ushort)shape.Size);
                Stream.Position = body;
                output.Write(body - vtable);
                Stream.Position = Stream.Length;
                return new Table(body, vtable, shape);
            }

            private void SelectField(Table table, int index)
            {
                int offset = table.Shape.Fields[index];
                Stream.Position = table.Vtable + 4 + index * 2;
                output.Write((ushort)offset);
                Stream.Position = table.Body + offset;
            }

            private void Short(Table table, int index, short value)
            { SelectField(table, index); output.Write(value); Stream.Position = Stream.Length; }
            private void Int(Table table, int index, int value)
            { SelectField(table, index); output.Write(value); Stream.Position = Stream.Length; }
            private void Long(Table table, int index, long value)
            { SelectField(table, index); output.Write(value); Stream.Position = Stream.Length; }
            private void Ref(Table table, int index, int target)
            {
                if (target == 0) return;
                SelectField(table, index);
                Link(Position, target);
            }

            internal void Link(int field, int target)
            {
                Stream.Position = field;
                output.Write(target - field);
                Stream.Position = Stream.Length;
            }

            private int StartVector(int count, int stride)
            {
                // Alignment applies to the first element after the length word.
                int vector = Align(Position + 4, Math.Max(4, stride)) - 4;
                Reserve(checked(vector + 4 + count * stride));
                Stream.Position = vector;
                output.Write(count);
                return vector;
            }

            private int Vector<T>(T[] values, int stride, Action<T> write)
            {
                if (values == null) return 0;
                int vector = StartVector(values.Length, stride);
                foreach (T value in values) write(value);
                return vector;
            }

            private int Tables<T>(T[] values, Func<T, int> write) where T : class
            {
                if (values == null) return 0;
                int vector = StartVector(values.Length, 4);
                Stream.Position = Stream.Length;
                for (int i = 0; i < values.Length; i++) Link(vector + 4 + i * 4, write(values[i]));
                return vector;
            }

            private int String(string value)
            {
                if (value == null) return 0;
                byte[] utf8 = Utf8.GetBytes(value);
                int text = StartVector(utf8.Length, 1);
                output.Write(utf8);
                output.Write((byte)0);
                return text;
            }

            internal int Level(FLevelSaveData data)
            {
                var table = Start(Root);
                Ref(table, 0, WriteValidation(data.ValidationSaveData));
                Ref(table, 1, WriteBoard(data.BoardSaveData));
                Ref(table, 2, Vector(data.GameStateSaveData, 8, output.Write));
                Ref(table, 3, Vector(data.BoosterSaveData, 8, output.Write));
                Ref(table, 4, Vector(data.LevelStatisticsSaveData, 8, output.Write));
                Ref(table, 5, Vector(data.EventSaveData, 8, output.Write));
                return table.Body;
            }

            private int WriteValidation(FValidationSaveData data)
            {
                var table = Start(Validation);
                Ref(table, 0, String(data.LevelName));
                Long(table, 1, data.Checksum);
                Int(table, 2, data.AppVersion);
                return table.Body;
            }

            private int WriteBoard(FBoardSaveData data)
            {
                var table = Start(Board);
                Ref(table, 0, WriteGrid(data.GridData));
                Ref(table, 1, Tables(data.Goals, WriteGoals));
                Ref(table, 2, WriteCreator(data.ItemModelCreatorSaveData));
                return table.Body;
            }

            private int WriteGrid(FCellGridSaveData data)
            {
                var table = Start(Grid);
                Ref(table, 0, Tables(data.Grid, WriteCell));
                Int(table, 1, data.Width);
                Int(table, 2, data.Height);
                Ref(table, 3, Vector(data.ShifterData, 4, output.Write));
                return table.Body;
            }

            private int WriteCell(FCellSaveData data)
            {
                var table = Start(Cell);
                if (data.CurrentItem != null) Ref(table, 0, WriteItem(data.CurrentItem));
                Ref(table, 1, Tables(data.StaticItems, WriteStatic));
                return table.Body;
            }

            private int WriteItem(FItemSaveData item) => WriteItemFields(item.TiledId, item.Data);
            private int WriteStatic(FStaticItemSaveData item) => WriteItemFields(item.TiledId, item.Data);
            private int WriteItemFields(short id, short data)
            {
                var table = Start(Item);
                Short(table, 0, id);
                Short(table, 1, data);
                return table.Body;
            }

            private int WriteGoals(FBoardGoalSaveData data)
            {
                var table = Start(Board);
                Ref(table, 0, Vector(data.GoalType, 2, output.Write));
                Ref(table, 1, Vector(data.LeftCount, 2, output.Write));
                Ref(table, 2, Vector(data.TargetCount, 2, output.Write));
                return table.Body;
            }

            private int WriteCreator(FItemModelCreatorSaveData data)
            {
                var table = Start(Cell);
                Ref(table, 0, Tables(data.CreatedSetItemCountExceptMatchItem, WritePair));
                if (data.PredefinedItemsSaveData != null)
                {
                    var predefined = Start(Queue);
                    Ref(predefined, 0, Tables(data.PredefinedItemsSaveData.Queues, WriteQueue));
                    Ref(table, 1, predefined.Body);
                }
                return table.Body;
            }

            private int WritePair(FIntIntPair data)
            {
                var table = Start(Pair);
                Int(table, 0, data.Key);
                Int(table, 1, data.Value);
                return table.Body;
            }

            private int WriteQueue(FItemModelQueueSaveData data)
            {
                var table = Start(Queue);
                Ref(table, 0, Tables(data.Items, WriteItem));
                return table.Body;
            }
        }

        private sealed class Reader
        {
            private readonly byte[] bytes;
            private int remainingElements = 32768;
            internal Reader(byte[] bytes) { this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes)); }

            private ReadOnlySpan<byte> Slice(int offset, int size)
            {
                if (offset < 0 || size < 0 || offset > bytes.Length - size)
                    throw new InvalidDataException("Level save reference is outside the buffer.");
                return bytes.AsSpan(offset, size);
            }

            private short Short(int offset) => BinaryPrimitives.ReadInt16LittleEndian(Slice(offset, 2));
            private int Int(int offset) => BinaryPrimitives.ReadInt32LittleEndian(Slice(offset, 4));
            private long Long(int offset) => BinaryPrimitives.ReadInt64LittleEndian(Slice(offset, 8));
            private int UShort(int offset) => BinaryPrimitives.ReadUInt16LittleEndian(Slice(offset, 2));

            private int Link(int offset)
            {
                int distance = Int(offset);
                if (distance < 4 || distance > bytes.Length - offset - 4)
                    throw new InvalidDataException("Invalid forward reference in level save.");
                return offset + distance;
            }

            private int Field(int table, int index, int width)
            {
                int displacement = Int(table);
                long position = (long)table - displacement;
                if (displacement == 0 || position < 0 || position > bytes.Length - 4)
                    throw new InvalidDataException("Invalid vtable reference in level save.");
                int vtable = (int)position;
                int vtableSize = UShort(vtable), tableSize = UShort(vtable + 2);
                if (vtableSize < 4 || (vtableSize & 1) != 0 || tableSize < 4)
                    throw new InvalidDataException("Invalid level save table size.");
                Slice(vtable, vtableSize);
                Slice(table, tableSize);
                int slot = 4 + index * 2;
                if (slot >= vtableSize) return 0;
                int field = UShort(vtable + slot);
                if (field != 0 && (field < 4 || field > tableSize - width))
                    throw new InvalidDataException("Invalid level save field offset.");
                return field == 0 ? 0 : table + field;
            }

            private int Ref(int table, int index)
            { int field = Field(table, index, 4); return field == 0 ? 0 : Link(field); }
            private short ReadShort(int table, int index)
            { int field = Field(table, index, 2); return field == 0 ? (short)0 : Short(field); }
            private int ReadInt(int table, int index)
            { int field = Field(table, index, 4); return field == 0 ? 0 : Int(field); }
            private long ReadLong(int table, int index)
            { int field = Field(table, index, 8); return field == 0 ? 0 : Long(field); }

            private int Count(int vector, int stride)
            {
                int count = Int(vector);
                if (count < 0 || count > (bytes.Length - vector - 4) / stride)
                    throw new InvalidDataException("Invalid level save vector length.");
                return count;
            }

            private T[] Vector<T>(int table, int index, int stride, Func<int, T> read)
            {
                int vector = Ref(table, index);
                if (vector == 0) return null;
                int count = Count(vector, stride);
                remainingElements -= count;
                if (remainingElements < 0) throw new InvalidDataException("Level save has too many vector elements.");
                var values = new T[count];
                for (int i = 0; i < values.Length; i++) values[i] = read(vector + 4 + i * stride);
                return values;
            }

            private T[] Tables<T>(int table, int index, Func<int, T> read) =>
                Vector(table, index, 4, offset => read(Link(offset)));

            private string String(int offset)
            {
                if (offset == 0) return null;
                int length = Count(offset, 1);
                if (Slice(offset + 4 + length, 1)[0] != 0)
                    throw new InvalidDataException("Level save string has no terminator.");
                return Utf8.GetString(bytes, offset + 4, length);
            }

            private T Optional<T>(int table, int index, Func<int, T> read) where T : class
            { int child = Ref(table, index); return child == 0 ? null : read(child); }

            internal FLevelSaveData Level()
            {
                int table = Link(0);
                return new FLevelSaveData {
                    ValidationSaveData = Optional(table, 0, ReadValidation), BoardSaveData = Optional(table, 1, ReadBoard),
                    GameStateSaveData = Vector(table, 2, 8, Long), BoosterSaveData = Vector(table, 3, 8, Long),
                    LevelStatisticsSaveData = Vector(table, 4, 8, Long), EventSaveData = Vector(table, 5, 8, Long)
                };
            }

            private FValidationSaveData ReadValidation(int table) => new FValidationSaveData {
                LevelName = String(Ref(table, 0)), Checksum = ReadLong(table, 1), AppVersion = ReadInt(table, 2)
            };
            private FBoardSaveData ReadBoard(int table) => new FBoardSaveData {
                GridData = Optional(table, 0, ReadGrid), Goals = Tables(table, 1, ReadGoals),
                ItemModelCreatorSaveData = Optional(table, 2, ReadCreator)
            };
            private FCellGridSaveData ReadGrid(int table) => new FCellGridSaveData {
                Grid = Tables(table, 0, ReadCell), Width = ReadInt(table, 1), Height = ReadInt(table, 2),
                ShifterData = Vector(table, 3, 4, Int)
            };
            private FCellSaveData ReadCell(int table) => new FCellSaveData {
                CurrentItem = Optional(table, 0, ReadItem), StaticItems = Tables(table, 1, ReadStatic)
            };
            private FItemSaveData ReadItem(int table) => new FItemSaveData {
                TiledId = ReadShort(table, 0), Data = ReadShort(table, 1)
            };
            private FStaticItemSaveData ReadStatic(int table) => new FStaticItemSaveData {
                TiledId = ReadShort(table, 0), Data = ReadShort(table, 1)
            };
            private FBoardGoalSaveData ReadGoals(int table) => new FBoardGoalSaveData {
                GoalType = Vector(table, 0, 2, Short), LeftCount = Vector(table, 1, 2, Short),
                TargetCount = Vector(table, 2, 2, Short)
            };
            private FItemModelCreatorSaveData ReadCreator(int table) => new FItemModelCreatorSaveData {
                CreatedSetItemCountExceptMatchItem = Tables(table, 0, ReadPair),
                PredefinedItemsSaveData = Optional(table, 1, ReadPredefined)
            };
            private FIntIntPair ReadPair(int table) => new FIntIntPair {
                Key = ReadInt(table, 0), Value = ReadInt(table, 1)
            };
            private FPredefinedItemsSaveData ReadPredefined(int table) => new FPredefinedItemsSaveData {
                Queues = Tables(table, 0, ReadQueue)
            };
            private FItemModelQueueSaveData ReadQueue(int table) => new FItemModelQueueSaveData {
                Items = Tables(table, 0, ReadItem)
            };
        }
    }
}
