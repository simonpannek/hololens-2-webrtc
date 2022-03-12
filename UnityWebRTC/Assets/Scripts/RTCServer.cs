﻿using UnityEngine;
using Microsoft.MixedReality.WebRTC;
using System.Threading.Tasks;
using System.Collections.Generic;

public class RTCServer : MonoBehaviour
{
    Signaler signaler;
    Transceiver audioTransceiver = null;
    Transceiver videoTransceiver = null;
    AudioTrackSource audioTrackSource = null;
    VideoTrackSource videoTrackSource = null;
    LocalAudioTrack localAudioTrack = null;
    LocalVideoTrack localVideoTrack = null;

    IReadOnlyList<VideoCaptureDevice> deviceList = null;

    public bool NeedVideo = true;
    public bool NeedAudio = false;

    public uint VideoWidth = 640;
    public uint VideoHeight = 400;
    public uint VideoFps = 30;
    public string VideoProfileId = "";

    public int Port = 9999;

    public bool UseRemoteStun = false;

    async void Start()
    {
        deviceList = await DeviceVideoTrackSource.GetCaptureDevicesAsync();
        Debug.Log($"Found {deviceList.Count} devices.");
        foreach (var device in deviceList)
        {
            Debug.Log($"Found webcam {device.name} (id: {device.id}):");
            var profiles = await DeviceVideoTrackSource.GetCaptureProfilesAsync(device.id);
            if (profiles.Count > 0)
            {
                foreach (var profile in profiles)
                {
                    Debug.Log($"+ Profile '{profile.uniqueId}'");
                    var configs = await DeviceVideoTrackSource.GetCaptureFormatsAsync(device.id, profile.uniqueId);
                    foreach (var config in configs)
                    {
                        Debug.Log($"  - {config.width}x{config.height}@{config.framerate}");
                    }
                }
            }
            else
            {
                var configs = await DeviceVideoTrackSource.GetCaptureFormatsAsync(device.id);
                foreach (var config in configs)
                {
                    Debug.Log($"- {config.width}x{config.height}@{config.framerate}");
                }
            }
        }

        // Setup signaling
        Debug.Log("Starting signaling...");
        signaler = new TCPSignaler(Port);

        signaler.ClientConnected += OnClientConnected;
        signaler.ClientDisconnected += OnClientDisconnected;
        if (UseRemoteStun)
        {
            signaler.IceServers.Add(new IceServer { Urls = { "stun:stun.l.google.com:19302" } });
        }
        signaler.Start();
    }

    async void OnClientConnected()
    {
        var pc = signaler.PeerConnection;

        // Record video from local webcam, and send to remote peer
        if (NeedVideo)
        {
            // For example, print them to the standard output

            var deviceSettings = new LocalVideoDeviceInitConfig
            {
                width = VideoWidth,
                height = VideoHeight,
            };
            if (VideoFps > 0)
            {
                deviceSettings.framerate = VideoFps;
            }
            if (VideoProfileId.Length > 0)
            {
                deviceSettings.videoProfileId = VideoProfileId;
            }

            Debug.Log($"Attempt to grab Camera - {deviceSettings.videoProfileId}: {deviceSettings.width}x{deviceSettings.height}@{deviceSettings.framerate}fps");
            videoTrackSource = await DeviceVideoTrackSource.CreateAsync(deviceSettings);

            Debug.Log($"Create local video track... {videoTrackSource}");
            var trackSettings = new LocalVideoTrackInitConfig
            {
                trackName = "webcam_track"
            };
            localVideoTrack = LocalVideoTrack.CreateFromSource(videoTrackSource, trackSettings);

            Debug.Log("Create video transceiver and add webcam track...");
            videoTransceiver = pc.AddTransceiver(MediaKind.Video);
            videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            videoTransceiver.LocalVideoTrack = localVideoTrack;
        }

        // Record audio from local microphone, and send to remote peer
        if (NeedAudio)
        {
            Debug.Log("Opening local microphone...");
            audioTrackSource = await DeviceAudioTrackSource.CreateAsync();

            Debug.Log("Create local audio track...");
            var trackSettings = new LocalAudioTrackInitConfig { trackName = "mic_track" };
            localAudioTrack = LocalAudioTrack.CreateFromSource(audioTrackSource, trackSettings);

            Debug.Log("Create audio transceiver and add mic track...");
            audioTransceiver = pc.AddTransceiver(MediaKind.Audio);
            audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            audioTransceiver.LocalAudioTrack = localAudioTrack;
        }

        // Add data channel for communication
        var channel = await pc.AddDataChannelAsync("detection", true, false);

        channel.MessageReceived += (byte[] message) => {
            string s = System.Text.Encoding.UTF8.GetString(message, 0, message.Length);
            Debug.Log($"Message received: {s}");
        };

        // Start peer connection
        // we need a short delay here for the video stream to settle...
        // I assume my Logitech webcam is sending some garbage frames in the beginning.
        await Task.Delay(200);
        pc.CreateOffer();
        Debug.Log("Send offer to remote peer");
    }

    public void OnClientDisconnected()
    {
        localAudioTrack?.Dispose();
        localVideoTrack?.Dispose();
        audioTrackSource?.Dispose();
        videoTrackSource?.Dispose();
    }

    void OnDisable()
    {
        OnClientDisconnected();
        signaler?.Stop();
        Debug.Log("Program terminated.");
    }
}
