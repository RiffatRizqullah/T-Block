using System.Collections;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

/// <summary>
/// FMOD-backed drop-in replacement for UnityEngine.AudioSource.
/// Assign an FMOD Event instead of an AudioClip, then use familiar
/// Play / Stop / Pause / PlayOneShot / volume / pitch / playOnAwake.
/// </summary>
[AddComponentMenu("Audio/FMOD Audio Source")]
public class FMODAudioSource : MonoBehaviour
{
    [Header("FMOD Event (replaces AudioClip)")]
    [Tooltip("Replaces AudioSource.clip. Pick an FMOD event, e.g. event:/Checkpoint for SFX or event:/WackyWayZone for music.")]
    [EventRef]
    public EventReference soundEvent;

    [Header("Familiar AudioSource controls")]
    [Tooltip("Same as AudioSource.playOnAwake.")]
    public bool playOnAwake = true;
    [Tooltip("Same as AudioSource.loop. NOTE: real looping is authored in FMOD Studio. If the event is not looped in Studio, this restarts it when it ends (gap possible).")]
    public bool loop = false;
    [Tooltip("Same as AudioSource.mute.")]
    public bool mute = false;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    [Range(0f, 1f)]
    [Tooltip("Same as AudioSource.spatialBlend, but FMOD events are fixed 2D or 3D in Studio. 0 = expects 2D event (music), 1 = expects 3D event (SFX). Mismatch logs a warning.")]
    public float spatialBlend = 1f;
    [Tooltip("Same as AudioSource.minDistance. Overrides Studio attenuation when overrideAttenuation is on.")]
    public float minDistance = 1f;
    [Tooltip("Same as AudioSource.maxDistance. Overrides Studio attenuation when overrideAttenuation is on.")]
    public float maxDistance = 20f;
    [Tooltip("Same as AudioSource.panStereo. Stored for compatibility; true panning must be authored in Studio.")]
    [Range(-1f, 1f)] public float panStereo = 0f;
    [Tooltip("Same as AudioSource.priority. Stored for compatibility; voice priority is managed by FMOD Studio.")]
    [Range(0, 256)] public int priority = 128;

    [Header("FMOD behaviour")]
    [Tooltip("Fade out on Stop, like StudioEventEmitter.AllowFadeout.")]
    public bool allowFadeout = true;
    [Tooltip("Override Studio min/max distance with the fields above.")]
    public bool overrideAttenuation = false;
    [Tooltip("Preload sample data on Start (good for music, wasteful for spammy SFX).")]
    public bool preload = false;
    [Tooltip("Stop this event when the GameObject is disabled (AudioSource-like).")]
    public bool stopOnDisable = true;
    [Tooltip("Route label only. Real routing is authored in FMOD Studio, e.g. bus:/Music or bus:/SFX. Replaces outputAudioMixerGroup.")]
    public string busPath = "";
    [Tooltip("Initial local parameter values, applied on Play. Global params (e.g. Adrenaline) use SetGlobalParameter instead.")]
    public ParamRef[] startParams = new ParamRef[0];

    [Header("Compatibility no-ops (kept so old code compiles)")]
    [Tooltip("No FMOD equivalent. Kept for compatibility, ignored.")]
    public bool bypassEffects = false;
    [Tooltip("No FMOD equivalent. Kept for compatibility, ignored.")]
    public bool bypassListenerEffects = false;
    [Tooltip("No FMOD equivalent. Kept for compatibility, ignored.")]
    public bool bypassReverbZones = false;
    [Tooltip("No FMOD equivalent. Doppler is computed from velocity in FMOD. Kept for compatibility, ignored.")]
    public float dopplerLevel = 1f;
    [Tooltip("No FMOD equivalent. Spread must be authored in Studio. Kept for compatibility, ignored.")]
    public float spread = 0f;
    [Tooltip("No FMOD equivalent. Reverb must be authored in Studio. Kept for compatibility, ignored.")]
    public float reverbZoneMix = 1f;

    EventDescription eventDescription;
    EventInstance instance;
    bool isQuitting;
    Coroutine scheduledRoutine;
    float cachedVolumeBeforeMute = 1f;

    /// <summary>Same as AudioSource.isPlaying.</summary>
    public bool isPlaying
    {
        get
        {
            if (instance.isValid())
            {
                instance.getPlaybackState(out PLAYBACK_STATE state);
                return state != PLAYBACK_STATE.STOPPED;
            }
            return false;
        }
    }

    /// <summary>Same as AudioSource.time (seconds into a streamed event, e.g. music).</summary>
    public float time
    {
        get
        {
            if (instance.isValid())
            {
                instance.getTimelinePosition(out int ms);
                return ms / 1000f;
            }
            return 0f;
        }
        set
        {
            if (instance.isValid())
                instance.setTimelinePosition((int)(value * 1000f));
        }
    }

    void Start()
    {
        RuntimeUtils.EnforceLibraryOrder();
        if (preload && !soundEvent.IsNull)
        {
            try
            {
                var desc = RuntimeManager.GetEventDescription(soundEvent);
                if (desc.isValid()) desc.loadSampleData();
            }
            catch (EventNotFoundException e) { Debug.LogWarning($"[FMODAudioSource] Preload failed: {e.Message}", this); }
        }
    }

    void OnEnable()
    {
        if (playOnAwake) Play();
    }

    void OnDisable()
    {
        if (stopOnDisable) Stop();
    }

    void OnDestroy()
    {
        CancelScheduled();
        if (!isQuitting && instance.isValid())
        {
            RuntimeManager.DetachInstanceFromGameObject(instance);
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
            instance.clearHandle();
        }
        if (preload && eventDescription.isValid())
            eventDescription.unloadSampleData();
    }

    void OnApplicationQuit() => isQuitting = true;

    void Update()
    {
        // Emulated loop for events that are not looped in Studio:
        // if we started it and it reached STOPPED naturally, restart it.
        if (loop && Application.isPlaying && !isQuitting && startedOnce && instance.isValid())
        {
            instance.getPlaybackState(out PLAYBACK_STATE state);
            if (state == PLAYBACK_STATE.STOPPED)
                Play();
        }
    }

    bool startedOnce;

    // ---- Core transport (AudioSource-like) ----

    /// <summary>Same as AudioSource.Play().</summary>
    public void Play()
    {
        if (soundEvent.IsNull)
        {
            Debug.LogWarning("[FMODAudioSource] Play() called with empty soundEvent.", this);
            return;
        }
        CancelScheduled();
        EnsureDescription();
        if (!eventDescription.isValid()) return;

        // Enforce 2D/3D expectation from spatialBlend.
        eventDescription.is3D(out bool is3D);
        if (spatialBlend < 0.5f && is3D)
            Debug.LogWarning($"[FMODAudioSource] spatialBlend=0 (2D) but event {soundEvent.Path} is 3D. Sound may be silent without a listener in range.", this);
        if (spatialBlend >= 0.5f && !is3D)
            Debug.LogWarning($"[FMODAudioSource] spatialBlend=1 (3D) but event {soundEvent.Path} is 2D. Position will be ignored.", this);

        if (!instance.isValid())
        {
            eventDescription.createInstance(out instance);
            if (is3D)
            {
                var rb = GetComponent<Rigidbody>();
                var rb2d = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject, rb));
                    RuntimeManager.AttachInstanceToGameObject(instance, gameObject, rb);
                }
                else if (rb2d != null)
                {
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject, rb2d));
                    RuntimeManager.AttachInstanceToGameObject(instance, gameObject, rb2d);
                }
                else
                {
                    instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                    RuntimeManager.AttachInstanceToGameObject(instance, gameObject);
                }
            }
        }

        foreach (var p in startParams)
        {
            if (!string.IsNullOrEmpty(p.Name))
                instance.setParameterByName(p.Name, p.Value);
        }
        if (overrideAttenuation && is3D)
        {
            instance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, minDistance);
            instance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, maxDistance);
        }
        ApplyVolumeAndPitch();
        instance.setPaused(false);
        instance.start();
        startedOnce = true;
    }

    /// <summary>Same as AudioSource.Stop().</summary>
    public void Stop()
    {
        CancelScheduled();
        startedOnce = false;
        if (instance.isValid())
        {
            RuntimeManager.DetachInstanceFromGameObject(instance);
            instance.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            instance.release();
            if (!allowFadeout) instance.clearHandle();
            // With fadeout the handle stays valid until playback stops; clear it next Play.
            if (allowFadeout) instance.clearHandle();
        }
    }

    /// <summary>Same as AudioSource.Pause().</summary>
    public void Pause()
    {
        if (instance.isValid()) instance.setPaused(true);
    }

    /// <summary>Same as AudioSource.UnPause().</summary>
    public void UnPause()
    {
        if (instance.isValid()) instance.setPaused(false);
    }

    /// <summary>Same as AudioSource.PlayOneShot(clip) but uses the attached soundEvent. Fire-and-forget, no Stop needed.</summary>
    public void PlayOneShot()
    {
        if (soundEvent.IsNull)
        {
            Debug.LogWarning("[FMODAudioSource] PlayOneShot() with empty soundEvent.", this);
            return;
        }
        RuntimeManager.PlayOneShot(soundEvent, transform.position);
    }

    /// <summary>Play a different event one-shot without changing soundEvent. Equivalent of swapping clip for one PlayOneShot call.</summary>
    public void PlayOneShot(EventReference oneShotEvent)
    {
        RuntimeManager.PlayOneShot(oneShotEvent, transform.position);
    }

    /// <summary>Same as AudioSource.PlayDelayed(). Emulated with a coroutine (not sample-accurate).</summary>
    public void PlayDelayed(float delaySeconds)
    {
        CancelScheduled();
        scheduledRoutine = StartCoroutine(DelayedPlay(delaySeconds));
    }

    /// <summary>Emulated AudioSource.PlayScheduled(). Uses realtime delay, not DSP clock.</summary>
    public void PlayScheduled(double dspTime)
    {
        double delay = dspTime - AudioSettings.dspTime;
        if (delay < 0) delay = 0;
        PlayDelayed((float)delay);
    }

    /// <summary>Emulated SetScheduledStartTime. Chains a delayed Play.</summary>
    public void SetScheduledStartTime(double dspTime) => PlayScheduled(dspTime);

    /// <summary>Emulated SetScheduledEndTime. Stops after a delay.</summary>
    public void SetScheduledEndTime(double dspTime)
    {
        double delay = dspTime - AudioSettings.dspTime;
        if (delay < 0) delay = 0;
        CancelScheduled();
        scheduledRoutine = StartCoroutine(DelayedStop((float)delay));
    }

    // ---- Volume / pitch / params ----

    /// <summary>Apply volume/pitch/mute to the live instance. Called automatically on Play and when fields change in Inspector.</summary>
    public void ApplyVolumeAndPitch()
    {
        if (!instance.isValid()) return;
        instance.setVolume(mute ? 0f : Mathf.Max(0f, volume));
        instance.setPitch(Mathf.Max(0.01f, pitch));
    }

    void OnValidate()
    {
        if (Application.isPlaying && instance.isValid())
            ApplyVolumeAndPitch();
    }

    /// <summary>FMOD-only: set a LOCAL parameter on this instance (e.g. Surface, Intensity).</summary>
    public void SetParameter(string paramName, float value, bool ignoreSeekSpeed = false)
    {
        if (instance.isValid())
            instance.setParameterByName(paramName, value, ignoreSeekSpeed);
        else
            Debug.LogWarning($"[FMODAudioSource] SetParameter({paramName}) called with no live instance. Call Play() first.", this);
    }

    /// <summary>FMOD-only: set a GLOBAL parameter (e.g. your Adrenaline 0-1). Affects all events.</summary>
    public static void SetGlobalParameter(string paramName, float value, bool ignoreSeekSpeed = false)
    {
        RuntimeManager.StudioSystem.setParameterByName(paramName, value, ignoreSeekSpeed);
    }

    /// <summary>AudioSource.PlayClipAtPoint equivalent. Static fire-and-forget at a position.</summary>
    public static void PlayOneShotAt(EventReference evt, Vector3 position)
    {
        RuntimeManager.PlayOneShot(evt, position);
    }

    /// <summary>AudioSource.PlayClipAtPoint equivalent using a path string.</summary>
    public static void PlayOneShotAt(string eventPath, Vector3 position)
    {
        RuntimeManager.PlayOneShot(eventPath, position);
    }

    // ---- Internals ----

    void EnsureDescription()
    {
        if (!eventDescription.isValid())
        {
            try { eventDescription = RuntimeManager.GetEventDescription(soundEvent); }
            catch (EventNotFoundException e) { Debug.LogWarning($"[FMODAudioSource] Event not found: {e.Message}. Did banks import from C:/Users/Kino/Documents/BoundaryAlert/Build?", this); }
        }
    }

    IEnumerator DelayedPlay(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        scheduledRoutine = null;
        Play();
    }

    IEnumerator DelayedStop(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        scheduledRoutine = null;
        Stop();
    }

    void CancelScheduled()
    {
        if (scheduledRoutine != null)
        {
            StopCoroutine(scheduledRoutine);
            scheduledRoutine = null;
        }
    }
}
