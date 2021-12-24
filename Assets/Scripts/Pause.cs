﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;

public class Pause : MonoBehaviour
{

    public GameObject pauseScreen;
    public bool editingVolume;
    public bool quitting;

    public static Pause instance;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(Player.pauseKey) & !Player.autoPlay & !Song.instance.isDead & Song.instance.songStarted)
        {
            if(!pauseScreen.activeSelf & !Menu.instance.menuCanvas.enabled)
                PauseSong();
        }
                
        if (Player.autoPlay && Input.GetKeyDown(Player.pauseKey))
        {
            QuitSong();
        }
    }

    public void EditVolume()
    {
        pauseScreen.SetActive(false);
        Menu.instance.menuCanvas.enabled = true;
        Song.instance.battleCanvas.enabled = false;
        Options.instance.volumeScreen.SetActive(true);
        Menu.instance.mainMenu.SetActive(false);
        editingVolume = true;
    }

    public void SaveVolume()
    {
        pauseScreen.SetActive(true);
        Song.instance.battleCanvas.enabled = true;
        Menu.instance.menuCanvas.enabled = false;
        Options.instance.volumeScreen.SetActive(false);

        LeanTween.delayedCall(1.25f, () =>
        {
            Options.instance.mainOptionsScreen.SetActive(false);
            Menu.instance.mainMenu.SetActive(true);
        });
        editingVolume = false;
    }
    
    public void PauseSong()
    {
        
        if (Options.instance.isTesting)
        {
            Song.instance.subtitleDisplayer.StopSubtitles();
            return;
        }
        
        Song.instance.modInstance?.Invoke("OnPause");

        Song.instance.subtitleDisplayer.paused = true;
        
        Song.instance.stopwatch.Stop();
        Song.instance.beatStopwatch.Stop();

        foreach (AudioSource source in Song.instance.musicSources)
        {
            source.Pause();
        }

        Song.instance.vocalSource.Pause();

        pauseScreen.SetActive(true);
    }

    public void ContinueSong()
    {
        Song.instance.modInstance?.Invoke("OnUnpause");
        
        Song.instance.stopwatch.Start();
        Song.instance.beatStopwatch.Start();

        Song.instance.subtitleDisplayer.paused = false;

        foreach (AudioSource source in Song.instance.musicSources)
        {
            source.UnPause();
        }

        Song.instance.vocalSource.UnPause();

        pauseScreen.SetActive(false);
    }

    public void RestartSong()
    {
        Song.instance.subtitleDisplayer.StopSubtitles();
        Song.instance.PlaySong();
        pauseScreen.SetActive(false);
    }

    public void QuitSong()
    {
        quitting = true;

        ContinueSong();
        Song.instance.subtitleDisplayer.StopSubtitles();
        foreach (AudioSource source in Song.instance.musicSources)
        {
            source.Stop();
        }

        Song.instance.vocalSource.Stop();
    }
}
