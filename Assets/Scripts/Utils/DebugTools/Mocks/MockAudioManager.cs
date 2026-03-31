using BlockAndDagger.Sound;

namespace BlockAndDagger.DebugTools.Mocks
{
    public sealed class MockAudioManager : IMobileAudioManager
    {
        private readonly AudioManagerUnloader _unloader = new();
        public AudioManagerUnloader Unloader => _unloader;
        public void PlaySFX(string key, float volume = 1f) { }
        public void PlayMusic(string key, float volume = 1f, bool loop = false) { }
        public void StopMusic(float fadetime = 0f) { }
        public void AdjustPlayingMusicVolume(float volume = 0f) { }
    }
}