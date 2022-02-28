using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MenuV2 : MonoBehaviour
{
    public RectTransform mainScreen;

    public RectTransform playScreen;
    public Image inputBlocker;

    [Header("Audio")] public AudioSource musicSource;
    public AudioClip menuClip;

    [Header("Background")] public Camera backgroundCamera;

    public SpriteRenderer backgroundSprite;

    public UIGradient backgroundGradient;

    [Header("Song List")] public RectTransform songListRect;

    public GameObject bundleButtonPrefab;

    public GameObject songButtonPrefab;

    public Sprite defaultCoverSprite;

    public bool canChangeSongs = true;

    private Dictionary<BundleButtonV2, List<SongButtonV2>> bundles =
        new Dictionary<BundleButtonV2, List<SongButtonV2>>();

    [Header("Song Info")] public Image songCoverImage;
    public TMP_Text songNameText;
    [FormerlySerializedAs("songCharterText")] public TMP_Text songCreditsText;
    public TMP_Text songDescriptionText;
    public TMP_Dropdown songDifficultiesDropdown;
    public TMP_Dropdown songModeDropdown;
    public GameObject selectSongScreen;
    public GameObject songInfoScreen;
    public GameObject loadingSongScreen;

    private SongMetaV2 _currentMeta;
    private string _songsFolder;
    
    public static MenuV2 Instance;
    public static int lastSelectedBundle;
    public static int lastSelectedSong;
    public static bool loadSongListOnStart; //This is used to load straight to the song list after finishing a song.
    
    // Start is called before the first frame update
    void Start()
    {
        InitializeMenu();
    }

    public void ReloadSongList()
    {
        selectSongScreen.SetActive(true);
        songInfoScreen.SetActive(false);
        
        if (songListRect.childCount != 0)
        {
            foreach (RectTransform child in songListRect)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (!Directory.Exists(_songsFolder))
        {
            Directory.CreateDirectory(_songsFolder);
        }
        
        SearchOption option = SearchOption.TopDirectoryOnly;

        List<string> allDirectories = new List<string>();
        allDirectories.AddRange(Directory.GetDirectories(_songsFolder, "*", option));

        allDirectories.AddRange(GameModLoader.bundleModDirectories);
        
        foreach (string dir in allDirectories)
        {
            if (File.Exists(dir + "/bundle-meta.json"))
            {
                BundleMeta bundleMeta =
                    JsonConvert.DeserializeObject<BundleMeta>(File.ReadAllText(dir + "/bundle-meta.json"));

                if (bundleMeta == null)
                {
                    Debug.LogError("Error whilst trying to read JSON file! " + dir + "/bundle-meta.json");
                    break;
                }

                BundleButtonV2 newWeek = Instantiate(bundleButtonPrefab, songListRect).GetComponent<BundleButtonV2>();

                newWeek.Creator = bundleMeta.authorName;
                newWeek.Name = bundleMeta.bundleName;
                newWeek.SongButtons = new List<SongButtonV2>();
                print("Searching in " + dir);

                List<SongButtonV2> songButtons = new List<SongButtonV2>();

                foreach (string songDir in Directory.GetDirectories(dir, "*", option))
                {
                    print("We got " + songDir);
                    if (File.Exists(songDir + "/meta.json") & File.Exists(songDir + "/Inst.ogg"))
                    {
                        SongMetaV2 meta = JsonConvert.DeserializeObject<SongMetaV2>(File.ReadAllText(songDir + "/meta.json"));

                        if (meta == null)
                        {
                            Debug.LogError("Error whilst trying to read JSON file! " + songDir + "/meta.json");
                            break;
                        }
                
                        SongButtonV2 newSong = Instantiate(songButtonPrefab,songListRect).GetComponent<SongButtonV2>();

                        newSong.Meta = meta;
                        newSong.Meta.songPath = songDir;
                
                        string coverDir = songDir + "/Cover.png";
                
                        if (File.Exists(coverDir))
                        {
                            byte[] coverData = File.ReadAllBytes(coverDir);

                            Texture2D coverTexture2D = new Texture2D(512,512);
                            coverTexture2D.LoadImage(coverData);

                            newSong.CoverArtSprite = Sprite.Create(coverTexture2D,
                                new Rect(0, 0, coverTexture2D.width, coverTexture2D.height), new Vector2(0, 0), 100);
                            newSong.Meta.songCover = newSong.CoverArtSprite;

                        }
                        else
                        {
                            newSong.CoverArtSprite = defaultCoverSprite;
                            newSong.Meta.songCover = defaultCoverSprite;
                        }

                        newWeek.SongButtons.Add(newSong);

                        newSong.gameObject.SetActive(false);

                        newSong.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            ChangeSong(newSong.Meta);

                            lastSelectedBundle = GetBundleIndex(newWeek);
                            lastSelectedSong = bundles[newWeek].IndexOf(newSong);
                        });

                        songButtons.Add(newSong);
                    }
                    else
                    {
                        Debug.LogError("Failed to find required files in " + songDir);
                    }
                }

                newWeek.UpdateCount();
                bundles.Add(newWeek, songButtons);
            }
            
            
        }

        if (loadSongListOnStart)
        {
            BundleButtonV2 bundleButton = bundles.Keys.ElementAt(lastSelectedBundle);
            bundleButton.ToggleSongsVisibility();
            ChangeSong(bundles[bundleButton][lastSelectedSong].Meta);
            loadSongListOnStart = false;
        }
    }

    public int GetBundleIndex(BundleButtonV2 item)
    {

        for (int i = 0; i < bundles.Keys.Count; i++)
        {
            if (bundles.Keys.ElementAt(i) == item)
            {
                return i;
            }
        }

        return 0;
    }
    
    public void ChangeSong(SongMetaV2 meta)
    {
        print("Checking if we can change songs. It is " + canChangeSongs);
        if (!canChangeSongs) return;
        print("Updating info");
        songNameText.text = meta.songName;
        songDescriptionText.text = "<color=yellow>Description:</color> " + meta.songDescription;
        songCoverImage.sprite = meta.songCover;

        songCreditsText.text = string.Empty;
        
        foreach (string role in meta.credits.Keys.ToList())
        {
            string memberName = meta.credits[role];

            songCreditsText.text += $"<color=yellow>{role}:</color> {memberName}\n";
        }
        
        songDifficultiesDropdown.ClearOptions();

        songDifficultiesDropdown.AddOptions(meta.difficulties.Keys.ToList());
        
        loadingSongScreen.SetActive(true);

        selectSongScreen.SetActive(false);
        songInfoScreen.SetActive(false);

        LeanTween.value(musicSource.gameObject, musicSource.volume, 0, 3f).setOnComplete(() =>
        {
            StartCoroutine(nameof(LoadSongAudio), meta.songPath+"/Inst.ogg");
        }).setOnUpdate(value =>
        {
            musicSource.volume = value;
        });
        

        _currentMeta = meta;
    }

    public void PlaySong()
    {
        var difficultiesList = _currentMeta.difficulties.Keys.ToList();
        Song.difficulty = difficultiesList[songDifficultiesDropdown.value];
        Song.modeOfPlay = songModeDropdown.value + 1;
        Song.currentSongMeta = _currentMeta;

        SceneManager.LoadScene("Game_Backup3");
    }

    IEnumerator LoadSongAudio(string path)
    {
        WWW www = new WWW(path);
        if (www.error != null)
        {
            Debug.LogError(www.error);
        }
        else
        {
            canChangeSongs = false;
            musicSource.clip = www.GetAudioClip();
            while (musicSource.clip.loadState != AudioDataLoadState.Loaded)
                yield return new WaitForSeconds(0.1f);
            musicSource.Play();
            LeanTween.value(musicSource.gameObject, musicSource.volume, Options.instVolume, 3f).setOnUpdate(value =>
            {
                musicSource.volume = value;
            });
            canChangeSongs = true;
            loadingSongScreen.SetActive(false);
            songInfoScreen.SetActive(true);
        }
    }

    
    public void InitializeMenu()
    {
        Instance = this;

        _songsFolder = Application.persistentDataPath + "/Bundles";
        
        if(!loadSongListOnStart)
        {

            backgroundSprite.color = Color.clear;
            inputBlocker.enabled = true;

            mainScreen.gameObject.SetActive(true);
            mainScreen.LeanMoveY(-720, 0);

            mainScreen.LeanMoveY(0, 1.5f).setDelay(.5f).setEaseOutExpo().setOnComplete(() =>
            {
                inputBlocker.enabled = false;
            });

            LeanTween.value(gameObject, Color.clear, Color.white, 1.5f).setDelay(.5f).setEaseOutExpo()
                .setOnUpdate(color => { backgroundSprite.color = color; });
        }
        else
        {
            canChangeSongs = true;

            mainScreen.gameObject.SetActive(false);
            playScreen.gameObject.SetActive(true);

            playScreen.LeanMoveY(0, 0f);

            ReloadSongList();
        }
        musicSource.clip = menuClip;
        musicSource.Play();
    }

    public void OpenPlayScreenFromMenu()
    {
        TransitionScreen(mainScreen, playScreen);
        canChangeSongs = true;
    }

    public void OpenMenuFromPlayScreen()
    {
        if (!canChangeSongs) return;
        TransitionScreen(playScreen, mainScreen);
        if (musicSource.clip != menuClip)
        {
            musicSource.Stop();
            musicSource.clip = menuClip;
            musicSource.Play();
        }
    }

    public void TransitionScreen(RectTransform oldScreen, RectTransform newScreen)
    {
        inputBlocker.enabled = true;
        oldScreen.LeanMoveY(-720,1f).setEaseOutExpo().setOnComplete(() =>
        {
            oldScreen.gameObject.SetActive(false);
            newScreen.gameObject.SetActive(true);
            newScreen.LeanMoveY(-720, 0);
            newScreen.LeanMoveY(0, 1f).setEaseOutExpo().setOnComplete(() =>
            {
                inputBlocker.enabled = false;
            });

        });
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}