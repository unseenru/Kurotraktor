using UnityEngine;
using Zenject;

public class MenuMusicController : MonoBehaviour
{
    private AudioSettings _audioSettings;

    [Inject]
    public void Construct(AudioSettings audioSettings)
    {
        _audioSettings = audioSettings;
    }

    private void Start()
    {
        _audioSettings.PlayMusic(MusicClip.Menu);
    }
}