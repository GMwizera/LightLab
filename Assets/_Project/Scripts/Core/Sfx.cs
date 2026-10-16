using UnityEngine;

namespace PassTheLight.Core
{
    public class Sfx : MonoBehaviour
    {
        const string MusicKey = "ptl_music_on";

        [SerializeField] AudioSource effects;
        [SerializeField] AudioSource pitched;
        [SerializeField] AudioSource music;

        [SerializeField] AudioClip click;
        [SerializeField] AudioClip pickUp;
        [SerializeField] AudioClip drop;
        [SerializeField] AudioClip deskLit;
        [SerializeField] AudioClip levelLit;
        [SerializeField] AudioClip correct;
        [SerializeField] AudioClip wrong;
        [SerializeField] AudioClip star;
        [SerializeField] AudioClip pop;

        static Sfx instance;

        public static bool MusicOn => PlayerPrefs.GetInt(MusicKey, 1) == 1;

        void Awake()
        {
            instance = this;
            music.loop = true;
            music.mute = !MusicOn;
            music.Play();
        }

        public static void Click() => instance.effects.PlayOneShot(instance.click, 0.4f);
        public static void PickUp() => instance.effects.PlayOneShot(instance.pickUp, 0.5f);
        public static void Drop() => instance.effects.PlayOneShot(instance.drop, 0.7f);
        public static void DeskLit() => instance.effects.PlayOneShot(instance.deskLit, 0.6f);
        public static void LevelLit() => instance.effects.PlayOneShot(instance.levelLit, 0.7f);
        public static void Correct() => instance.effects.PlayOneShot(instance.correct, 0.7f);
        public static void Wrong() => instance.effects.PlayOneShot(instance.wrong, 0.5f);
        public static void Pop() => instance.effects.PlayOneShot(instance.pop, 0.35f);

        public static void Star(int starNumber)
        {
            instance.pitched.pitch = 1f + 0.2f * starNumber;
            instance.pitched.PlayOneShot(instance.star, 0.7f);
        }

        public static void ToggleMusic()
        {
            bool turnOn = !MusicOn;
            PlayerPrefs.SetInt(MusicKey, turnOn ? 1 : 0);
            PlayerPrefs.Save();
            instance.music.mute = !turnOn;
        }
    }
}
