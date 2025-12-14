using System.Collections.Generic;
using UnityEngine;

namespace HockeyGame.Core
{
    /// <summary>
    /// Singleton Sound Manager for hockey game audio.
    /// Handles all game sounds including skating, puck impacts, crowd reactions, and arena ambience.
    /// Uses object pooling for efficient one-shot audio playback.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        #region Singleton
        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SoundManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SoundManager");
                        _instance = go.AddComponent<SoundManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Audio Clip References
        [Header("Skating Sounds")]
        [SerializeField] private AudioClip skatingLoop;

        [Header("Puck Sounds")]
        [SerializeField] private AudioClip puckStickHit;
        [SerializeField] private AudioClip puckBoardHit;
        [SerializeField] private AudioClip puckGoalPost;
        [SerializeField] private AudioClip puckIceSlide;

        [Header("Game Events")]
        [SerializeField] private AudioClip refereeWhistle;
        [SerializeField] private AudioClip goalHorn;

        [Header("Crowd Sounds")]
        [SerializeField] private AudioClip crowdAmbient;
        [SerializeField] private AudioClip crowdCheer;
        [SerializeField] private AudioClip crowdBoo;

        [Header("Impact Sounds")]
        [SerializeField] private AudioClip bodyCheckImpact;
        [SerializeField] private AudioClip glassHit;
        #endregion

        #region Audio Sources
        [Header("Audio Source Settings")]
        [SerializeField] private int audioSourcePoolSize = 10;
        [SerializeField] private float maxSoundDistance = 50f;
        [SerializeField] private float spatialBlend = 0.7f;

        private AudioSource skatingAudioSource;
        private AudioSource crowdAmbientSource;
        private List<AudioSource> audioSourcePool = new List<AudioSource>();
        private Queue<AudioSource> availableAudioSources = new Queue<AudioSource>();
        #endregion

        #region Volume Settings
        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float crowdVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float skatingVolume = 0.5f;

        private float currentCrowdIntensity = 0.5f;
        #endregion

        #region Initialization
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            // Create skating audio source
            skatingAudioSource = gameObject.AddComponent<AudioSource>();
            skatingAudioSource.loop = true;
            skatingAudioSource.spatialBlend = 0f; // 2D sound
            skatingAudioSource.volume = 0f;
            skatingAudioSource.clip = skatingLoop;

            // Create crowd ambient audio source
            crowdAmbientSource = gameObject.AddComponent<AudioSource>();
            crowdAmbientSource.loop = true;
            crowdAmbientSource.spatialBlend = 0f; // 2D sound
            crowdAmbientSource.volume = crowdVolume * currentCrowdIntensity * masterVolume;
            crowdAmbientSource.clip = crowdAmbient;
            if (crowdAmbient != null)
            {
                crowdAmbientSource.Play();
            }

            // Create audio source pool for one-shot sounds
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = spatialBlend;
                source.maxDistance = maxSoundDistance;
                source.rolloffMode = AudioRolloffMode.Linear;

                audioSourcePool.Add(source);
                availableAudioSources.Enqueue(source);
            }
        }
        #endregion

        #region Skating Sounds
        /// <summary>
        /// Starts playing the skating loop sound.
        /// </summary>
        public void PlaySkating()
        {
            if (skatingAudioSource != null && skatingLoop != null)
            {
                if (!skatingAudioSource.isPlaying)
                {
                    skatingAudioSource.Play();
                }
                skatingAudioSource.volume = skatingVolume * masterVolume;
            }
        }

        /// <summary>
        /// Stops the skating loop sound.
        /// </summary>
        public void StopSkating()
        {
            if (skatingAudioSource != null)
            {
                skatingAudioSource.volume = 0f;
            }
        }

        /// <summary>
        /// Sets the skating sound volume (for variable speed).
        /// </summary>
        public void SetSkatingVolume(float normalizedVolume)
        {
            if (skatingAudioSource != null)
            {
                skatingAudioSource.volume = Mathf.Clamp01(normalizedVolume) * skatingVolume * masterVolume;
            }
        }
        #endregion

        #region Puck Sounds
        /// <summary>
        /// Plays a puck stick hit sound at the specified position.
        /// </summary>
        public void PlayPuckHit(Vector3 position)
        {
            PlayOneShotAt(puckStickHit, position, sfxVolume);
        }

        /// <summary>
        /// Plays a puck stick hit sound at the specified position with custom intensity.
        /// </summary>
        public void PlayPuckHit(Vector3 position, float intensity)
        {
            PlayOneShotAt(puckStickHit, position, sfxVolume * Mathf.Clamp01(intensity));
        }

        /// <summary>
        /// Plays a puck board hit sound at the specified position.
        /// </summary>
        public void PlayBoardHit(Vector3 position)
        {
            PlayOneShotAt(puckBoardHit, position, sfxVolume);
        }

        /// <summary>
        /// Plays a puck goal post hit sound at the specified position.
        /// </summary>
        public void PlayGoalPostHit(Vector3 position)
        {
            PlayOneShotAt(puckGoalPost, position, sfxVolume);
        }

        /// <summary>
        /// Plays a puck ice slide sound at the specified position.
        /// </summary>
        public void PlayPuckIceSlide(Vector3 position)
        {
            PlayOneShotAt(puckIceSlide, position, sfxVolume * 0.6f);
        }
        #endregion

        #region Game Event Sounds
        /// <summary>
        /// Plays the referee whistle sound.
        /// </summary>
        public void PlayWhistle()
        {
            PlayOneShotAt(refereeWhistle, Vector3.zero, sfxVolume, false);
        }

        /// <summary>
        /// Plays the goal horn sound.
        /// </summary>
        public void PlayGoalHorn()
        {
            PlayOneShotAt(goalHorn, Vector3.zero, sfxVolume * 1.2f, false);
        }
        #endregion

        #region Impact Sounds
        /// <summary>
        /// Plays a body check impact sound at the specified position.
        /// </summary>
        public void PlayBodyCheck(Vector3 position)
        {
            PlayOneShotAt(bodyCheckImpact, position, sfxVolume);
        }

        /// <summary>
        /// Plays a body check impact sound at the specified position with custom intensity.
        /// </summary>
        public void PlayBodyCheck(Vector3 position, float intensity)
        {
            PlayOneShotAt(bodyCheckImpact, position, sfxVolume * Mathf.Clamp01(intensity));
        }

        /// <summary>
        /// Plays a glass hit sound at the specified position.
        /// </summary>
        public void PlayGlassHit(Vector3 position)
        {
            PlayOneShotAt(glassHit, position, sfxVolume);
        }
        #endregion

        #region Crowd Sounds
        /// <summary>
        /// Sets the crowd ambient volume based on intensity (0-1).
        /// Higher intensity = louder crowd.
        /// </summary>
        public void SetCrowdIntensity(float intensity)
        {
            currentCrowdIntensity = Mathf.Clamp01(intensity);
            if (crowdAmbientSource != null)
            {
                crowdAmbientSource.volume = crowdVolume * currentCrowdIntensity * masterVolume;
            }
        }

        /// <summary>
        /// Triggers a crowd cheer sound.
        /// </summary>
        public void TriggerCrowdCheer()
        {
            PlayOneShotAt(crowdCheer, Vector3.zero, crowdVolume * 1.1f, false);
        }

        /// <summary>
        /// Triggers a crowd boo sound.
        /// </summary>
        public void TriggerCrowdBoo()
        {
            PlayOneShotAt(crowdBoo, Vector3.zero, crowdVolume * 0.9f, false);
        }
        #endregion

        #region Audio Source Pool Management
        /// <summary>
        /// Plays a one-shot audio clip at a specific position using the audio source pool.
        /// </summary>
        private void PlayOneShotAt(AudioClip clip, Vector3 position, float volume, bool is3D = true)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableAudioSource();
            if (source != null)
            {
                source.transform.position = position;
                source.spatialBlend = is3D ? spatialBlend : 0f;
                source.volume = volume * masterVolume;
                source.clip = clip;
                source.Play();

                StartCoroutine(ReturnToPoolWhenFinished(source, clip.length));
            }
            else
            {
                Debug.LogWarning("SoundManager: No available audio sources in pool. Consider increasing pool size.");
            }
        }

        /// <summary>
        /// Gets an available audio source from the pool.
        /// </summary>
        private AudioSource GetAvailableAudioSource()
        {
            if (availableAudioSources.Count > 0)
            {
                return availableAudioSources.Dequeue();
            }

            // If pool is empty, try to find a source that's not playing
            foreach (AudioSource source in audioSourcePool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an audio source to the pool after it finishes playing.
        /// </summary>
        private System.Collections.IEnumerator ReturnToPoolWhenFinished(AudioSource source, float duration)
        {
            yield return new WaitForSeconds(duration);

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                availableAudioSources.Enqueue(source);
            }
        }
        #endregion

        #region Volume Control
        /// <summary>
        /// Sets the master volume for all sounds.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        /// <summary>
        /// Sets the SFX volume.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Sets the crowd volume.
        /// </summary>
        public void SetCrowdVolume(float volume)
        {
            crowdVolume = Mathf.Clamp01(volume);
            if (crowdAmbientSource != null)
            {
                crowdAmbientSource.volume = crowdVolume * currentCrowdIntensity * masterVolume;
            }
        }

        /// <summary>
        /// Updates all currently playing volumes.
        /// </summary>
        private void UpdateAllVolumes()
        {
            if (skatingAudioSource != null && skatingAudioSource.isPlaying)
            {
                skatingAudioSource.volume = skatingVolume * masterVolume;
            }

            if (crowdAmbientSource != null)
            {
                crowdAmbientSource.volume = crowdVolume * currentCrowdIntensity * masterVolume;
            }
        }
        #endregion

        #region Utility
        /// <summary>
        /// Stops all currently playing sounds.
        /// </summary>
        public void StopAllSounds()
        {
            StopSkating();

            foreach (AudioSource source in audioSourcePool)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        /// <summary>
        /// Pauses all sounds.
        /// </summary>
        public void PauseAllSounds()
        {
            if (skatingAudioSource != null) skatingAudioSource.Pause();
            if (crowdAmbientSource != null) crowdAmbientSource.Pause();

            foreach (AudioSource source in audioSourcePool)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                }
            }
        }

        /// <summary>
        /// Resumes all paused sounds.
        /// </summary>
        public void ResumeAllSounds()
        {
            if (skatingAudioSource != null) skatingAudioSource.UnPause();
            if (crowdAmbientSource != null) crowdAmbientSource.UnPause();

            foreach (AudioSource source in audioSourcePool)
            {
                source.UnPause();
            }
        }
        #endregion
    }
}
