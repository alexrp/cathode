using Vezel.Cathode.Native;

namespace Vezel.Cathode.Terminals;

internal sealed class NativeTerminalWriter : TerminalWriter
{
    // Unlike NativeTerminalReader, the buffer size here is arbitrary and only has performance implications.
    private const int WriteBufferSize = 256;

    public NativeVirtualTerminal Terminal { get; }

    public nuint Handle { get; }

    public override sealed Stream Stream { get; }

    public override sealed TextWriter TextWriter { get; }

    public override sealed bool IsValid { get; }

    public override sealed bool IsInteractive { get; }

    private readonly SemaphoreSlim _semaphore;

    private readonly Action<nuint, CancellationToken>? _cancellationHook;

    public NativeTerminalWriter(
        NativeVirtualTerminal terminal,
        nuint handle,
        SemaphoreSlim semaphore,
        Action<nuint, CancellationToken>? cancellationHook)
    {
        Terminal = terminal;
        Handle = handle;
        _semaphore = semaphore;
        _cancellationHook = cancellationHook;
        Stream = new SynchronizedStream(new TerminalOutputStream(this));
        TextWriter =
            new SynchronizedTextWriter(new StreamWriter(Stream, Cathode.Terminal.Encoding, WriteBufferSize, true)
            {
                AutoFlush = true,
            });
        IsValid = TerminalInterop.IsValid(handle, write: true);
        IsInteractive = TerminalInterop.IsInteractive(handle);
    }

    private unsafe int WritePartialNative(scoped ReadOnlySpan<byte> buffer, CancellationToken cancellationToken)
    {
        using var guard = Terminal.Control.Guard();

        // If the descriptor is invalid, just present the illusion to the user that it has been redirected to /dev/null
        // or something along those lines, i.e. pretend we wrote everything.
        if (buffer is [] || !IsValid)
            return buffer.Length;

        using (_semaphore.Enter(cancellationToken))
        {
            _cancellationHook?.Invoke(Handle, cancellationToken);

            int progress;

            fixed (byte* p = buffer)
                TerminalInterop.Write(Handle, p, buffer.Length, &progress).ThrowIfError();

            return progress;
        }
    }

    protected override int WritePartialCore(scoped ReadOnlySpan<byte> buffer)
    {
        return WritePartialNative(buffer, default);
    }

    protected override ValueTask<int> WritePartialCoreAsync(
        ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        // We currently have no native async support.
        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : new(Task.Run(() => WritePartialNative(buffer.Span, cancellationToken), cancellationToken));
    }
}
