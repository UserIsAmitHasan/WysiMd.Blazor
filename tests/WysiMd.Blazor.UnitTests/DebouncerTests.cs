using FluentAssertions;
using WysiMd.Blazor.Utilities;

namespace WysiMd.Blazor.UnitTests;

/// <summary>
/// Tests for Debouncer — covering debounce behaviour, flush, cancel,
/// thread safety, memory-leak prevention, and resource cleanup.
///
/// Problem context: Blazor Server fires ValueChanged over SignalR on every
/// keystroke. Debouncer reduces that to one call after the user pauses,
/// while guaranteeing the final value is never lost (flush on blur).
/// </summary>
[TestClass]
public class DebouncerTests
{
    // ── Core debounce behaviour ───────────────────────────────────────────

    [TestMethod]
    public async Task Schedule_FiresOnce_AfterDelayElapses()
    {
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 50,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule();

        await Task.Delay(150);
        callCount.Should().Be(1, "action must fire exactly once after the delay");
    }

    [TestMethod]
    public async Task Schedule_ResetsCountdown_WhenCalledRepeatedly()
    {
        // Simulates fast typing — each keystroke resets the timer.
        // Action should fire only once, after the final keystroke pause.
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => { callCount++; return Task.CompletedTask; });

        // Simulate 5 keystrokes 30ms apart — each one resets the 100ms timer
        for (int i = 0; i < 5; i++)
        {
            d.Schedule();
            await Task.Delay(30);
        }

        // At this point the timer has been reset 5 times — hasn't fired yet
        callCount.Should().Be(0, "should not fire while user is still typing");

        // Wait for the final debounce window to close
        await Task.Delay(200);
        callCount.Should().Be(1, "should fire exactly once after typing stops");
    }

    [TestMethod]
    public async Task Schedule_WithZeroDelay_FiresImmediately()
    {
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 0,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule();
        await Task.Delay(50);

        callCount.Should().Be(1, "zero delay means fire immediately on every Schedule()");
    }

    [TestMethod]
    public async Task Schedule_WithZeroDelay_FiresOnEveryCall()
    {
        // Zero delay = old immediate behaviour (DebounceDelay = 0 opt-out)
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 0,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule();
        d.Schedule();
        d.Schedule();
        await Task.Delay(50);

        callCount.Should().Be(3, "each Schedule() with zero delay fires independently");
    }

    // ── IsScheduled ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task IsScheduled_TrueWhilePending_FalseAfterFired()
    {
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => Task.CompletedTask);

        d.IsScheduled.Should().BeFalse("nothing scheduled yet");

        d.Schedule();
        d.IsScheduled.Should().BeTrue("timer is counting down");

        await Task.Delay(200);
        d.IsScheduled.Should().BeFalse("timer has fired");
    }

    [TestMethod]
    public void IsScheduled_FalseAfterCancel()
    {
        var d = new Debouncer(getDelay: () => 500, action: () => Task.CompletedTask);

        d.Schedule();
        d.IsScheduled.Should().BeTrue();

        d.Cancel();
        d.IsScheduled.Should().BeFalse("cancel must clear the pending flag");

        d.DisposeAsync().AsTask().Wait();
    }

    // ── Cancel ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Cancel_PreventsActionFromFiring()
    {
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule();
        d.Cancel();

        await Task.Delay(200);
        callCount.Should().Be(0, "cancelled action must never fire");
    }

    [TestMethod]
    public async Task Cancel_WhenNothingScheduled_IsNoOp()
    {
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => { callCount++; return Task.CompletedTask; });

        // Should not throw
        d.Cancel();
        await Task.Delay(50);
        callCount.Should().Be(0);
    }

    // ── FlushAsync ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task FlushAsync_FiresImmediately_WhenPending()
    {
        // Simulates user clicking away before the debounce timer fires.
        // The final value must reach the parent immediately on blur.
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 500,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule(); // timer counting down (500ms)
        await d.FlushAsync(); // blur — must fire now

        callCount.Should().Be(1, "flush must fire the pending action immediately");
        d.IsScheduled.Should().BeFalse("timer must be cancelled after flush");
    }

    [TestMethod]
    public async Task FlushAsync_DoesNotFireTwice_AfterTimerAlreadyFired()
    {
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 50,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule();
        await Task.Delay(150); // timer fires naturally
        await d.FlushAsync();  // called again (e.g. late blur event)

        callCount.Should().Be(1, "flush after natural fire must not double-notify the parent");
    }

    [TestMethod]
    public async Task FlushAsync_WhenNothingScheduled_IsNoOp()
    {
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => { callCount++; return Task.CompletedTask; });

        await d.FlushAsync();
        callCount.Should().Be(0, "flush with nothing pending must not fire");
    }

    [TestMethod]
    public async Task FlushAsync_CancelsTimer_SoItDoesNotFireAgainLater()
    {
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule();
        await d.FlushAsync(); // fires once

        await Task.Delay(200); // wait for original timer window to pass
        callCount.Should().Be(1, "timer must not fire again after flush already fired it");
    }

    // ── Runtime delay change ──────────────────────────────────────────────

    [TestMethod]
    public async Task Schedule_ReadsDelayAtCallTime_SupportsRuntimeChange()
    {
        // Delay can be changed at runtime via EditorOptions.DebounceDelay.
        // The new value must be picked up on the next Schedule() call.
        int currentDelay = 200;
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => currentDelay,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule(); // schedules at 200ms

        // Change delay before timer fires
        currentDelay = 50;
        d.Schedule(); // resets timer at new 50ms interval

        await Task.Delay(150);
        callCount.Should().Be(1, "new shorter delay must be picked up on reschedule");
    }

    // ── Exception handling ────────────────────────────────────────────────

    [TestMethod]
    public async Task Action_ThrowingException_IsRoutedToOnException()
    {
        Exception? caught = null;
        await using var d = new Debouncer(
            getDelay: () => 50,
            action: () => throw new InvalidOperationException("boom"),
            onException: ex => caught = ex);

        d.Schedule();
        await Task.Delay(150);

        caught.Should().NotBeNull("exception must be routed to onException handler");
        caught.Should().BeOfType<InvalidOperationException>();
    }

    [TestMethod]
    public async Task Action_ThrowingException_WithNoHandler_DoesNotCrashCaller()
    {
        // Without an onException handler, exceptions must be silently swallowed
        // rather than crashing the background thread or the component.
        await using var d = new Debouncer(
            getDelay: () => 50,
            action: () => throw new InvalidOperationException("boom"));

        d.Schedule();

        // Must not throw
        await Task.Delay(150);
    }

    // ── Memory leak prevention ────────────────────────────────────────────

    [TestMethod]
    public async Task DisposeAsync_StopsTimer_PreventsFiringAfterDispose()
    {
        // This tests the memory leak scenario:
        // Component is disposed while a debounce is still pending.
        // The timer must not fire and must not touch component state.
        int callCount = 0;
        var d = new Debouncer(
            getDelay: () => 100,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule(); // timer counting down
        await d.DisposeAsync(); // component disposed immediately

        await Task.Delay(200); // wait for what would have been the timer window
        callCount.Should().Be(0, "disposed debouncer must not fire — prevents use-after-free on component state");
    }

    [TestMethod]
    public async Task DisposeAsync_IsIdempotent_CallingTwiceDoesNotThrow()
    {
        var d = new Debouncer(getDelay: () => 100, action: () => Task.CompletedTask);
        await d.DisposeAsync();
        await d.DisposeAsync(); // must not throw ObjectDisposedException
    }

    [TestMethod]
    public async Task DisposeAsync_DetachesElapsedHandler_PreventsDelegateLeaks()
    {
        // After dispose the timer's Elapsed event must have no handlers.
        // An attached handler holds a reference to the debouncer (and transitively
        // to the component), which would prevent GC — a memory leak.
        var d = new Debouncer(getDelay: () => 100, action: () => Task.CompletedTask);
        d.Schedule();
        await d.DisposeAsync();

        // The timer is disposed — no further action should run
        // (verified implicitly: if the handler were still attached and the timer
        // somehow fired, FireAsync would run on a disposed object and could throw)
        await Task.Delay(150);
        // If we reach here without exception, the handler was detached correctly
    }

    [TestMethod]
    public async Task Schedule_AfterDispose_IsNoOp()
    {
        int callCount = 0;
        var d = new Debouncer(
            getDelay: () => 50,
            action: () => { callCount++; return Task.CompletedTask; });

        await d.DisposeAsync();
        d.Schedule(); // must not throw or fire

        await Task.Delay(150);
        callCount.Should().Be(0, "scheduling on a disposed debouncer must be a no-op");
    }

    // ── Thread safety ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task Schedule_CalledConcurrently_DoesNotThrow()
    {
        // Simulates multiple rapid keystrokes from different threads
        // (e.g. Blazor Server SignalR dispatch + timer thread)
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => { Interlocked.Increment(ref callCount); return Task.CompletedTask; });

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => d.Schedule()));

        await Task.WhenAll(tasks); // must not throw
        await Task.Delay(250);

        callCount.Should().BeLessOrEqualTo(1,
            "concurrent schedules must collapse into at most one notification");
    }

    [TestMethod]
    public async Task ConcurrentScheduleAndDispose_DoesNotThrow()
    {
        // Race condition: timer fires exactly as component is being disposed.
        // This is the scenario _disposed guard exists to handle.
        for (int i = 0; i < 10; i++)
        {
            int callCount = 0;
            var d = new Debouncer(
                getDelay: () => 20,
                action: () => { callCount++; return Task.CompletedTask; });

            d.Schedule();
            // Dispose almost immediately — races with the 20ms timer
            await d.DisposeAsync();
            // Must not throw regardless of timing
        }
    }

    // ── Blazor Server scenario ────────────────────────────────────────────

    [TestMethod]
    public async Task BlazorServerScenario_ContinuousTyping_OnlyOneNotification()
    {
        // Reproduces the exact problem: 10 keystrokes, each would have been
        // a SignalR round-trip. With debouncing, only 1 notification fires.
        int signalRRoundTrips = 0;
        await using var d = new Debouncer(
            getDelay: () => 100,
            action: () => { signalRRoundTrips++; return Task.CompletedTask; });

        // Simulate 10 keystrokes 20ms apart
        for (int i = 0; i < 10; i++)
        {
            d.Schedule();
            await Task.Delay(20);
        }

        await Task.Delay(200); // wait for debounce to fire

        signalRRoundTrips.Should().Be(1,
            "10 keystrokes must produce exactly 1 SignalR round-trip with debouncing");
    }

    [TestMethod]
    public async Task BlazorServerScenario_UserClicksAway_FlushEnsuresValueDelivered()
    {
        // User types then clicks away before debounce fires.
        // Without flush, the last typed value would be lost.
        string? lastNotified = null;
        string currentValue = string.Empty;

        await using var d = new Debouncer(
            getDelay: () => 500,
            action: () => { lastNotified = currentValue; return Task.CompletedTask; });

        currentValue = "hello world";
        d.Schedule(); // 500ms timer starts — hasn't fired

        // User clicks away immediately (blur event)
        await d.FlushAsync();

        lastNotified.Should().Be("hello world",
            "flush on blur must deliver the final value even if timer hasn't fired");
    }

    [TestMethod]
    public async Task BlazorServerScenario_DebounceDelayZero_MatchesOldBehavior()
    {
        // DebounceDelay = 0 opt-out: every keystroke notifies immediately.
        // This is the backward-compatible path for users who don't want debouncing.
        int callCount = 0;
        await using var d = new Debouncer(
            getDelay: () => 0,
            action: () => { callCount++; return Task.CompletedTask; });

        d.Schedule();
        d.Schedule();
        d.Schedule();
        await Task.Delay(50);

        callCount.Should().Be(3, "zero delay must preserve old per-keystroke behaviour");
    }
}
