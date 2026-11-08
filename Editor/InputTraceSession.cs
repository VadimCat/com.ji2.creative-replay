using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Ji2.CreativeReplay.Editor
{
    /// <summary>Owns native InputEventTrace memory and playback devices; no game-specific state.</summary>
    public sealed class InputTraceSession : IDisposable
    {
        private InputEventTrace _trace;
        private InputEventTrace.ReplayController _playback;
        private readonly List<InputDevice> _disabled=new();
        private long _recordedBytes;
        public bool IsRecording=>_trace!=null && _trace.enabled;
        public bool IsPlaying=>_playback!=null && !_playback.finished;
        public bool Finished=>_playback!=null && _playback.finished;
        public long EventCount=>_trace?.eventCount??0;
        public bool Overflowed {get;private set;}
        private const long Budget=64*1024*1024;

        public void BeginRecording(Func<InputEventPtr,InputDevice,bool> filter=null)
        {
            Dispose();Overflowed=false;_recordedBytes=0;
            _trace=new InputEventTrace(1024*1024,true,128*1024*1024){recordFrameMarkers=true};
            _trace.onFilterEvent=(eventPtr,device)=>
            {
                if(device!=null && !(device is Mouse || device is Touchscreen || device is Keyboard))return false;
                if(filter!=null && device!=null && !filter(eventPtr,device))return false;
                _recordedBytes+=eventPtr.sizeInBytes;
                if(_recordedBytes>Budget){Overflowed=true;return false;}
                return true;
            };
            _trace.Enable();
        }

        public void SaveRecording(string path)
        {
            if(!IsRecording)throw new InvalidOperationException("No recording is active.");
            _trace.Disable();
            if(Overflowed)throw new InvalidOperationException("Take exceeded the 64 MiB recording budget. No truncated take was saved.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            _trace.WriteTo(path);
        }

        public void BeginReplay(string path,bool frameAccurate=true)
        {
            Dispose();
            try
            {
                _trace=InputEventTrace.LoadFrom(path);
                foreach(var device in InputSystem.devices.ToArray())
                    if(device.enabled && (device is Mouse || device is Touchscreen || device is Keyboard))
                    { _disabled.Add(device);InputSystem.DisableDevice(device); }
                _playback=_trace.Replay().WithAllDevicesMappedToNewInstances();
                if(frameAccurate)_playback.PlayAllFramesOneByOne();
                else _playback.PlayAllEventsAccordingToTimestamps();
            }
            catch{Dispose();throw;}
        }

        public void SetPaused(bool paused)
        {if(_playback==null)throw new InvalidOperationException("No playback is active.");_playback.paused=paused;}

        public void Dispose()
        {
            _playback?.Dispose();_playback=null;
            _trace?.Dispose();_trace=null;
            foreach(var device in _disabled)if(device.added)InputSystem.EnableDevice(device);
            _disabled.Clear();
        }
    }
}
