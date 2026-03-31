namespace BlockAndDagger.Sound
{
    public interface IMobileAudioManager
    {
        void PlaySFX(string key, float volume = 1f);
        void PlayMusic(string key, float volume = 1f, bool loop = false);
        void StopMusic(float fadetime = 0f);
        void AdjustPlayingMusicVolume(float volume = 0f);
        AudioManagerUnloader Unloader { get; }
    }
}
