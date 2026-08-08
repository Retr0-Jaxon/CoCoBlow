using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
public class AudioManager : MonoBehaviour
{
    // 单例实例
    public static AudioManager Instance { get; private set; }

    //存单个音频的信息
    [System.Serializable]
    public class Sound
    {
        [Header("音频文件")]
        public AudioClip clip;
        [Header("音频名称")]
        public string name;

        [Header("音频输出组")]
        public AudioMixerGroup outputGroup;

        [Header("音频音量")]
        [Range(0f, 5f)]
        public float volume;

        [Header("音频音调")]
        [Range(0.8f, 2f)]
        public float pitch=1f;

        [Header("音频是否循环")]
        public bool loop;
        [Header("音频是否开局播放")]
        public bool playOnAwake;
        [Header("音频是否为3D音效")]
        public bool is3D;
        [Header("音频是否随机音调")]
        public bool israndomPitch;

        // 运行时绑定，不在 Inspector 显示
        [HideInInspector]
        public AudioSource source;

    }
    public List<Sound> sounds;
    private Dictionary<string, Sound> audioSourcesDic;


    void Awake()
    {
        // 单例初始化
        PatternSingleton();
        audioSourcesDic = new Dictionary<string, Sound>();
        // 音频初始化移到 Awake，确保其他脚本 Start 调用时已就绪
        InitializeAudio();
    }

    void InitializeAudio()
    {
        Debug.Log($"AudioManager: 开始初始化，sounds 列表共有 {sounds.Count} 个音频");
        foreach (var sound in sounds)
        {
            if (sound.clip == null)
            {
                Debug.LogWarning($"AudioManager: 第 {sounds.IndexOf(sound)} 个 Sound 的 Clip 为空，已跳过");
                continue;
            }
            if (string.IsNullOrEmpty(sound.name))
            {
                Debug.LogWarning($"AudioManager: 第 {sounds.IndexOf(sound)} 个 Sound 的 Name 为空，已跳过");
                continue;
            }
            GameObject soundObject = new GameObject("Sound_" + sound.name);
            soundObject.transform.SetParent(transform);
            AudioSource Source = soundObject.AddComponent<AudioSource>();
            Source.clip = sound.clip;
            Source.volume = sound.volume;
            Source.loop = sound.loop;
            Source.outputAudioMixerGroup = sound.outputGroup;
            Source.playOnAwake = sound.playOnAwake;
            sound.source = Source;
            audioSourcesDic.Add(sound.name, sound);
            Debug.Log($"AudioManager: 已创建音频源 Sound_{sound.name} | volume={sound.volume} | loop={sound.loop} | playOnAwake={sound.playOnAwake}");
            if (sound.playOnAwake)
            {
                Source.Play();
                Debug.Log($"AudioManager: 开局播放 {sound.name}");
            }
        }
        Debug.Log($"AudioManager: 初始化完成，共创建 {audioSourcesDic.Count} 个音频源");
    }














    /// <summary>
    /// /////////////////////api////////////////
    /// </summary>
    //播放音频api
    public static void PlayAudio(string soundName, bool isPreventOverLap)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager: 实例未初始化，无法播放音频");
            return;
        }
        if (!Instance.audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogError($"AudioManager: 未找到名为 {soundName} 的音频源");
            return;
        }
        Sound soundData = Instance.audioSourcesDic[soundName];
        //如果是随机音调，随机pitch（未完工）
        if (soundData.israndomPitch)
        {
            float randomPitch = Random.Range(0.4f, 1.5f);
            soundData.source.pitch = randomPitch;
            Debug.Log($"AudioManager: 音频 {soundName} 随机音调为 {randomPitch}");
        }
        //播放音频
        if (isPreventOverLap)
        {
            if (soundData.source.isPlaying)
            {
                Debug.Log($"AudioManager: 音频 {soundName} 正在播放，等待播放完成");
                return;
            }
            else
            {
                soundData.source.Play();
                Debug.Log($"AudioManager: 播放音频 {soundName}");
            }
        }
        else
        {
            soundData.source.Play();
            Debug.Log($"AudioManager: 播放音频 {soundName}");
        }
    }
    //停止音频api
    public static void StopAudio(string soundName)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager: 实例未初始化，无法停止音频");
            return;
        }
        if (!Instance.audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogError($"AudioManager: 未找到名为 {soundName} 的音频源");
            return;
        }
        Instance.audioSourcesDic[soundName].source.Stop();
        Debug.Log($"AudioManager: 停止音频 {soundName}");
    }
    //3d音效的播放api，我没有选择新建component，而是创建一个临时go跟着跑，所以稍微逻辑复杂，不过我个人认为更好
    public static void PlayAudio3D(string soundName,GameObject fromGO)
    {
        if (Instance == null)
        {
            Debug.LogError("AudioManager: 实例未初始化，无法播放音频");
            return;
        }
        if (!Instance.audioSourcesDic.ContainsKey(soundName))
        {
            Debug.LogError($"AudioManager: 未找到名为 {soundName} 的音频源");
            return;
        }
        if (fromGO == null)
        {
            Debug.LogError("AudioManager: 传入的 GameObject 为 null，无法播放3D音频");
            return;
        }
        Sound soundData = Instance.audioSourcesDic[soundName];
        //如果是随机音调，随机pitch（未完工）
        if (soundData.israndomPitch)
        {
            float randomPitch = Random.Range(0.4f, 1.5f);
            soundData.source.pitch = randomPitch;
            Debug.Log($"AudioManager: 音频 {soundName} 随机音调为 {randomPitch}");
        }
        //播放音频
        soundData.source.spatialBlend = 1f; // 设置为3D音效
        //创建临时go随着东西跑（待靠go池子优化性能）
        GameObject tempGO = new GameObject("TempAudio_" + soundName);
        tempGO.transform.position = fromGO.transform.position;
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        tempSource.clip = soundData.clip;
        tempSource.volume = soundData.volume;
        tempSource.pitch = soundData.source.pitch;
        tempSource.loop = soundData.loop;
        tempSource.outputAudioMixerGroup = soundData.outputGroup;
        tempSource.spatialBlend = 1f;
        tempSource.Play();
        Instance.StartCoroutine(FollowAndDestroy(tempGO, fromGO, soundData.clip.length));
    }

    private static IEnumerator FollowAndDestroy(GameObject tempGO, GameObject target, float clipLength)
    {
        float elapsed = 0f;
        while (elapsed < clipLength)
        {
            if (tempGO == null) yield break;
            if (target == null)
            {
                // 原物体没了，销毁 tempGO
                Destroy(tempGO);
                yield break;
            }
            tempGO.transform.position = target.transform.position;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(tempGO);
    }









































    void PatternSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
}

