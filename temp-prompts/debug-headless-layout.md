# Debug Headless Layout

## Pattern
When debugging UI layout/sizing issues in Avalonia views, use Serilog to log element dimensions at runtime, then run in headless mode and check client.log.

## Steps

### 1. Add debug logging in code-behind
```csharp
using Serilog;

// In Loaded / after-layout handler:
Log.Debug($"[DEBUG] ElementName: Width={Width}, Height={Height}, Bounds={Bounds}, DesiredSize={DesiredSize}");
Log.Debug($"[DEBUG] ChildPanel: Width={child.Width}, Height={child.Height}, Bounds={child.Bounds}");
```

### 2. Build
```bash
dotnet build AvClientExe/AvClientExe.csproj --framework net8.0
```

### 3. Run headless
```bash
rm -f AvClientExe/bin/Debug/net8.0/Client/client.log
timeout 12 dotnet exec AvClientExe/bin/Debug/net8.0/clientav.dll --headless
```

### 4. Check results
```bash
grep "\[DEBUG\]" AvClientExe/bin/Debug/net8.0/Client/client.log
grep -i "error\|fatal\|crash" AvClientExe/bin/Debug/net8.0/Client/client.log
```

## Key Points
- Use `Serilog.Log.Debug()` — NEVER `System.Diagnostics.Debug.WriteLine()` (won't appear in client.log)
- `Bound` shows the actual arranged size after layout
- `Width`/`Height` shows explicitly set values (NaN = auto/Stretch)
- `DesiredSize` shows what the control asked for during measure
- Canvas with `HorizontalAlignment="Center"` and explicit Width may report Bounds=0x0 — use `Stretch` alignment instead
