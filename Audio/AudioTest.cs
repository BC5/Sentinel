using System.Timers;
using Discord.Audio;
using Microsoft.EntityFrameworkCore;

namespace Sentinel;

public class AudioTest
{
    private IAudioClient _client;
    private Dictionary<ulong, AudioInStream> _streams;
    private string _savePath = @"../../../data/recording";
    private int _tick = 0;
    private string _sessionName;
    public AudioTest(IAudioClient client, ulong server)
    {
        _sessionName = $"{DateTimeOffset.Now:yyyy-MM-dd HH.mm} [{server}]";
        Directory.CreateDirectory($@"{_savePath}/{_sessionName}");
        
        _client = client;
        _streams = new();
        _client.StreamCreated += NewStream;
        _client.StreamDestroyed += DelStream;
        _client.SpeakingUpdated += SpeakUpdate;
        _client.Connected += ConnectAsync;
        
        foreach (var streamentry in _client.GetStreams())
        {
            _streams.Add(streamentry.Key,streamentry.Value);
        }
        
        StartTicking();
    }

    private async Task ConnectAsync()
    {
        foreach (var streamentry in _client.GetStreams())
        {
            _streams.Add(streamentry.Key,streamentry.Value);
        }
        await Beep();
    }

    private async Task Beep()
    {
        await _client.SetSpeakingAsync(true);
        var stream = _client.CreateDirectPCMStream(AudioApplication.Mixed, bitrate: 48000);
        int samplerate = 48000;
        int bps = 50;
        byte[] buffer = new byte[2*samplerate/bps];

        bool high = false;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (i % 4800 == 0) high = !high;
            if (high)
            {
                buffer[i] = Byte.MaxValue;
            }
        }
        for (int i = 0; i < 20;i++) await stream.WriteAsync(buffer, 0, buffer.Length);
        Console.WriteLine("Done");
    }

    private void StartTicking()
    {
        var timer = new System.Timers.Timer(250);
        timer.Elapsed += Tick;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private async void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        if (_tick == 0)
        {
            await Beep();
        }
        _tick++;
        List<Task> tasks = new List<Task>();
        foreach (var streamp in _streams)
        {
            tasks.Add(Task.Run(async () =>
            {
                await WriteStream(streamp.Value);
            }));
        }
        await Task.WhenAll(tasks);
    }

    private async Task WriteStream(AudioInStream stream)
    {
        while (stream.AvailableFrames > 0)
        {
            var frame = await stream.ReadFrameAsync(CancellationToken.None);
            Console.WriteLine(frame.Timestamp);
        }
        Console.WriteLine("exited loop");
    }

    private async Task SpeakUpdate(ulong id, bool speaking)
    {
        Console.WriteLine($"{id} speaking: {speaking}");
    }

    private async Task DelStream(ulong id)
    {
        Console.WriteLine($"Stream destroyed: {id}");
        _streams.Remove(id);
    }

    private async Task NewStream(ulong id, AudioInStream stream)
    {
        Console.WriteLine($"Stream created: {id}");
        _streams.Add(id,stream);
    }

}