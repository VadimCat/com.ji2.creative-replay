# JI2 Creative Replay

Editor-only reusable ownership wrapper around Unity's `InputEventTrace`, using Input System 1.17. It records mouse, touchscreen and keyboard events with frame markers and writes Unity's native input trace format. No runtime assembly is included in players.

Use `InputTraceSession.BeginRecording(filter)` and `SaveRecording(path)` to create a trace. `BeginReplay(path, frameAccurate)` maps recorded devices to new temporary instances and disables existing supported input devices until disposal. `Dispose()` always releases native trace memory, playback devices and restores the devices it disabled. Recording above its conservative 64 MiB event budget is rejected rather than saving a truncated take.

`CreativeGameView.SetResolution(width,height)` configures an exact native Game View. In a batch editor it can create an offscreen host without focusing the user's display. Frame-based playback uses Unity frame markers; timestamp playback delegates to Unity's timestamp controller.

The consumer owns initial state, content/version compatibility, final-state verification and video export. PushColorMerge's adapter and integration tests are documented in `Docs/CREATIVE_ACTION_RECORDER.md` and `Assets/Tests/Editor/SkinAndCreativeTests.cs`.

Source and provenance: PushColorMerge `agent/skins-creative-replay`, 27 October 2026; registered in the shared UnityGameModules catalog associated with `D:/Codex/JigglyAI`.
