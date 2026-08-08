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
    }
    void Start()
    {
        Debug.Log($"AudioManager: 开始初始化，sounds 列表共有 {sounds.Count} 个音频");
        //遍历音频
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
            //创建一个go，叫做Sound_加上音频名称，来存放音频
            GameObject soundObject = new GameObject("Sound_" + sound.name);
            soundObject.transform.SetParent(transform);
            AudioSource Source = soundObject.AddComponent<AudioSource>();
            //给音频源赋值
            Source.clip = sound.clip;
            Source.volume = sound.volume;
                                            // Source.pitch = sound.pitch;假如有这行就不能随机音调了
            Source.loop = sound.loop;
            Source.outputAudioMixerGroup = sound.outputGroup;
            Source.playOnAwake = sound.playOnAwake;
            //绑定运行时 AudioSource
            sound.source = Source;
            //将音频数据加入字典
            audioSourcesDic.Add(sound.name, sound);
            Debug.Log($"AudioManager: 已创建音频源 Sound_{sound.name} | volume={sound.volume} | loop={sound.loop} | playOnAwake={sound.playOnAwake}");
            //播放开局音频
            if (sound.playOnAwake)
            {
                Source.Play();
                Debug.Log($"AudioManager: 开局播放 {sound.name}");
            }
        }
        Debug.Log($"AudioManager: 初始化完成，共创建 {audioSourcesDic.Count} 个音频源");
    }

    public static void PlayAudio(string soundName, bool isWait)
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
        if (isWait)
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

