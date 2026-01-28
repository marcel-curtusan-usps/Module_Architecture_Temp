# Parent Process Monitoring

This document describes the parent process monitoring feature implemented in the submodules to prevent orphaned processes.

## Overview

The parent process monitoring feature ensures that submodules (DataStore, ContainerDetectionModule, etc.) automatically detect when the main application has stopped running and gracefully terminate themselves. This prevents orphaned processes that continue running after the parent application has exited.

## How It Works

### 1. Process ID Communication

When the MainApp starts a submodule, it passes its own process ID (PID) to the submodule via the `--parent-pid` command-line argument:

```bash
dotnet DataStore.dll --name DataStore --port 6000 --parent-pid 1234
```

### 2. Periodic Health Checks

Each submodule starts a `ParentProcessMonitor` service that:
- Periodically checks (every 5 seconds by default) if the parent process is still running
- Validates that the process with the given PID actually exists
- Verifies the process name matches "MainApp" to prevent false positives from PID reuse
- Logs all monitoring activities for debugging and auditing

### 3. Graceful Termination

If the parent process is no longer detected:
1. The monitor logs a warning message
2. The monitor triggers the application's graceful shutdown
3. The submodule cleans up its resources and exits

## Configuration

The check interval can be configured in the `appsettings.json` file of each submodule:

```json
{
  "ParentProcessMonitor": {
    "CheckIntervalSeconds": 5
  }
}
```

To adjust the monitoring frequency, change the `CheckIntervalSeconds` value. Lower values provide faster detection but use more CPU resources.

## Architecture

### ParentProcessMonitor Class

The `ParentProcessMonitor` class provides the following features:

**Cross-Platform Support:**
- Works on Windows, macOS, and Linux
- Uses `System.Diagnostics.Process` for process queries

**PID Reuse Detection:**
- Validates that the process name matches the expected parent process name
- Prevents false positives when a PID is reused by another process

**Logging:**
- Logs initialization with parent PID and check interval
- Logs warnings when parent process is not detected
- Logs graceful shutdown initiation

**Resource Management:**
- Implements `IDisposable` for proper cleanup
- Uses `Timer` for efficient periodic checks
- Stops the timer before triggering shutdown to prevent redundant checks

## Implementation Details

### MainApp Changes

The `ModuleManagerService` was updated to:
- Get the current process ID using `Environment.ProcessId`
- Pass the PID to submodules via `--parent-pid` command-line argument
- Log the parent PID for debugging

### Submodule Changes

Both DataStore and ContainerDetectionModule were updated to:
- Parse the `--parent-pid` argument from command-line
- Initialize the `ParentProcessMonitor` with the parent PID
- Read the check interval from configuration
- Log warnings if started without parent process monitoring

## Testing

### Manual Testing Steps

1. **Start the MainApp:**
   ```bash
   cd MainApp
   dotnet run
   ```

2. **Verify submodules started with monitoring:**
   Check logs for messages like:
   ```
   DataStore started with parent process monitoring (Parent PID: 1234, Check Interval: 5s)
   ```

3. **Kill the MainApp:**
   ```bash
   kill <MainApp_PID>
   ```

4. **Verify submodules terminate:**
   Within approximately 5-10 seconds (check interval + processing time), all submodules should terminate gracefully.

### Expected Behavior

- **Normal operation:** Submodules run continuously, checking parent status every 5 seconds
- **Parent stops:** Submodules detect the absence within one check interval and terminate
- **PID reuse:** Submodules validate process name and terminate if it doesn't match
- **No parent PID provided:** Submodules log a warning but continue running (backward compatible)

## Edge Cases Handled

1. **PID Reuse:** The monitor verifies the process name to ensure the PID belongs to the expected parent process
2. **Process Access Denied:** Gracefully handles cases where process information cannot be accessed
3. **Parent Never Existed:** Logs a warning at startup if the parent PID doesn't exist or has wrong name
4. **Race Conditions:** Uses proper disposal patterns to prevent timer callbacks after shutdown

## Logging Examples

**Successful monitoring:**
```
info: ParentProcessMonitor initialized. Monitoring parent process ID: 1234, Check interval: 5s
info: DataStore started with parent process monitoring (Parent PID: 1234, Check Interval: 5s)
info: Parent process monitoring is active.
```

**Parent process stopped:**
```
warn: Parent process (PID: 1234) is no longer running. Initiating graceful shutdown to prevent orphaned process.
info: Application is shutting down...
```

**PID reuse detected:**
```
warn: Process with PID 1234 exists but has name 'OtherApp', expected 'MainApp'. This may indicate PID reuse.
warn: Parent process (PID: 1234) is no longer running. Initiating graceful shutdown to prevent orphaned process.
```

## Benefits

1. **No Orphaned Processes:** Submodules automatically clean up when the parent stops
2. **Resource Efficiency:** Prevents memory and CPU usage by zombie processes
3. **Cross-Platform:** Works consistently across Windows, macOS, and Linux
4. **Configurable:** Check interval can be adjusted per environment requirements
5. **Robust:** Handles edge cases like PID reuse
6. **Observable:** Comprehensive logging for debugging and monitoring
7. **Backward Compatible:** Modules work with or without parent process monitoring

## Limitations

1. **Detection Latency:** There's a delay (up to one check interval) between parent termination and submodule detection
2. **System Process:** Cannot be used if the parent is a system process (PID 1 on Linux)
3. **Cross-User:** May not work if parent and child run under different user accounts with restricted permissions

## Future Enhancements

Potential improvements for future versions:
- Environment variable support for passing parent PID
- Heartbeat-based monitoring (parent actively sends heartbeats)
- Configurable grace period before termination
- Support for monitoring multiple parent processes
- Metrics and telemetry integration
