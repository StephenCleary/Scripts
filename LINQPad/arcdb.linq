<Query Kind="Statements">
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <Namespace>System.IO.MemoryMappedFiles</Namespace>
  <Namespace>System.Buffers.Binary</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
</Query>

Test();
Test();

void Test()
{
	using var file = new FileStream(@"C:\Work\tmp.arcdb", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);

	using var mapping = MemoryMappedFile.CreateFromFile(file, null, 1000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
	using var view = mapping.CreateViewAccessor();
	using var overlay = new Overlay(view);
	ref Data data = ref overlay.As<Data>();
	data.First = 2;
	data.Second = 1;

	using var mapping2 = MemoryMappedFile.CreateFromFile(file, null, 20000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
	using var view2 = mapping2.CreateViewAccessor();
	using var overlay2 = new Overlay(view2);
	ref Data data2 = ref overlay.As<Data>();
	data2.Dump();

	view.Flush();
	file.Flush(flushToDisk: true); // FlushFileBuffers
	
	// file.SetLength(1000);
}

static class Helpers
{
	public static int ReadBigEndian(ref int littleEndian) => BinaryPrimitives.ReadInt32BigEndian(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref littleEndian, 1)));
	public static void WriteBigEndian(ref int littleEndian, int value) => BinaryPrimitives.WriteInt32BigEndian(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref littleEndian, 1)), value);
}

struct Data
{
	private int _First;
	private int _Second;
	public int First
	{
		get => Helpers.ReadBigEndian(ref _First);
		set => Helpers.WriteBigEndian(ref _First, value);
	}
	public int Second
	{
		get => Helpers.ReadBigEndian(ref _Second);
		set => Helpers.WriteBigEndian(ref _Second, value);
	}
}

sealed unsafe class Overlay : IDisposable
{
	private readonly MemoryMappedViewAccessor _view;
	private readonly byte* _pointer;
	
	public Overlay(MemoryMappedViewAccessor view)
	{
		_view = view;
		view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
	}
	
	public void Dispose()
	{
		_view.SafeMemoryMappedViewHandle.ReleasePointer();
	}
	
	public ref T As<T>() where T : struct
	{
		return ref Unsafe.AsRef<T>(_pointer);
	}
}