using NLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace Centromenu;






public sealed class MeetingMusicPlayer : MonoBehaviour
{
    private const string TrackId = "11554965";
    private const string ConsumerKey = "audiomack-web";
    private const string ConsumerSecret = "bd8a07e9f23fbe9d808646b730f89b8e";
    private const string PlaybackEndpoint = "https://api.audiomack.com/v1/music/play/" + TrackId;

    private static readonly HttpClient HttpClient = new();
    private readonly object _pendingLock = new();

    private AudioSource _audioSource;
    private DecodedAudio _pendingAudio;
    private int _requestGeneration;

    public void StartMeetingMusic()
    {
        StopMeetingMusic();
        int generation = _requestGeneration;
        _ = DownloadAndDecodeAsync(generation);
    }

    public void StopMeetingMusic()
    {
        _requestGeneration++;

        lock (_pendingLock)
        {
            _pendingAudio = null;
        }

        if (_audioSource)
        {
            _audioSource.Stop();
            if (_audioSource.clip) Destroy(_audioSource.clip);
            Destroy(_audioSource);
            _audioSource = null;
        }
    }

    private void Update()
    {


        if (_audioSource && !MeetingHud.Instance)
        {
            StopMeetingMusic();
            return;
        }

        DecodedAudio audio;
        lock (_pendingLock)
        {
            audio = _pendingAudio;
            _pendingAudio = null;
        }

        if (audio == null || audio.Generation != _requestGeneration || !MeetingHud.Instance) return;

        var clip = AudioClip.Create(
            "Death Note - Ryuk's Theme A",
            audio.Samples.Length / audio.Channels,
            audio.Channels,
            audio.SampleRate,
            false);

        clip.SetData(audio.Samples, 0);
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    private async Task DownloadAndDecodeAsync(int generation)
    {
        try
        {
            string playbackUrl = CreateSignedPlaybackUrl();
            string responseJson = await HttpClient.GetStringAsync(playbackUrl);
            string signedUrl = JsonSerializer.Deserialize<PlaybackResponse>(responseJson)?.signedUrl;
            if (string.IsNullOrWhiteSpace(signedUrl))
                throw new InvalidDataException("Audiomack returned no signed audio URL.");

            byte[] mp3 = await HttpClient.GetByteArrayAsync(signedUrl);
            DecodedAudio decoded = await Task.Run(() => DecodeMp3(mp3, generation));

            if (generation != _requestGeneration) return;
            lock (_pendingLock)
            {
                if (generation == _requestGeneration) _pendingAudio = decoded;
            }
        }
        catch (Exception exception)
        {
            Centromenu.Log.LogWarning($"Could not play meeting music: {exception.Message}");
        }
    }

    private static DecodedAudio DecodeMp3(byte[] mp3, int generation)
    {
        using var stream = new MemoryStream(mp3, false);
        using var mpeg = new MpegFile(stream);
        var samples = new List<float>(mpeg.SampleRate * mpeg.Channels * 90);
        var buffer = new float[16_384];
        int read;

        while ((read = mpeg.ReadSamples(buffer, 0, buffer.Length)) > 0)
            samples.AddRange(buffer.Take(read));

        return new DecodedAudio(samples.ToArray(), mpeg.SampleRate, mpeg.Channels, generation);
    }

    private static string CreateSignedPlaybackUrl()
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["oauth_consumer_key"] = ConsumerKey,
            ["oauth_nonce"] = Guid.NewGuid().ToString("N"),
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["oauth_version"] = "1.0",
            ["session"] = "centromenu"
        };

        string normalizedParameters = string.Join("&", parameters.Select(pair =>
            $"{PercentEncode(pair.Key)}={PercentEncode(pair.Value)}"));
        string signatureBase = $"GET&{PercentEncode(PlaybackEndpoint)}&{PercentEncode(normalizedParameters)}";

        using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(PercentEncode(ConsumerSecret) + "&"));
        string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase)));
        parameters["oauth_signature"] = signature;

        return PlaybackEndpoint + "?" + string.Join("&", parameters.Select(pair =>
            $"{PercentEncode(pair.Key)}={PercentEncode(pair.Value)}"));
    }

    private static string PercentEncode(string value) => Uri.EscapeDataString(value);

    private sealed class PlaybackResponse
    {
        public string signedUrl { get; set; }
    }

    private sealed class DecodedAudio
    {
        public readonly float[] Samples;
        public readonly int SampleRate;
        public readonly int Channels;
        public readonly int Generation;

        public DecodedAudio(float[] samples, int sampleRate, int channels, int generation)
        {
            Samples = samples;
            SampleRate = sampleRate;
            Channels = channels;
            Generation = generation;
        }
    }
}
