using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToAudio;
using Microsoft.SemanticKernel;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelVoiceTest;

#pragma warning disable SKEXP0001 //semantic kernel text to audio and vice versa is experimental, this disables the IDE warnings
public class AudioService
{
    public Kernel Kernel { get;  }

    public AudioService(string apiKey)
    {
        var kernelBuilder = Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion("gpt-4o-mini", apiKey)
                    .AddOpenAIAudioToText("whisper-1", apiKey)
                    .AddOpenAITextToAudio("tts-1", apiKey);

        kernelBuilder.Plugins.AddFromType<OrderPlugin>();
        kernelBuilder.Plugins.AddFromType<CustomerPlugin>();
        Kernel = kernelBuilder.Build();
    }

    public async Task<MemoryStream> RecordAudio()
    {
        var memoryStream = new MemoryStream();

        var waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(44100, 1) // 44.1kHz mono
        };

        var writer = new WaveFileWriter(new IgnoreDisposeStream(memoryStream), waveIn.WaveFormat);

        var tcs = new TaskCompletionSource<bool>();

        waveIn.DataAvailable += (sender, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        };

        waveIn.RecordingStopped += async (sender, e) =>
        {
            writer.Flush();
            await writer.DisposeAsync();
            waveIn.Dispose();
            tcs.SetResult(true);
            Console.WriteLine("Recording stopped.");
        };


        waveIn.StartRecording();
        Console.WriteLine("Recording... Press any key to stop.");
        Console.ReadKey(intercept: true);
        waveIn.StopRecording();

        //weird hack because RecordingStopped doesn't seem to actually finish before this methods finishes
        await tcs.Task;

        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<AudioContent> TurnTextToAudio(string input)
    {
        var textToVoice = Kernel.GetRequiredService<ITextToAudioService>();

        OpenAITextToAudioExecutionSettings executionSettings = new()
        {
            Voice = "nova", // Supported voices are alloy, echo, fable, onyx, nova, and shimmer.
            ResponseFormat = "mp3", // The format to audio in. Supported formats are mp3, opus, aac, and flac.
            Speed = 1.0f
        };

        var audio = await textToVoice.GetAudioContentAsync(input, executionSettings);
        return audio;
    }

    public async Task<string> TurnAudioToText(MemoryStream audioStream)
    {
        var audioToText = Kernel.GetRequiredService<IAudioToTextService>();
        var audioFileBinaryData = await BinaryData.FromStreamAsync(audioStream!);

        var audioContent = new AudioContent
        {
            Data = audioFileBinaryData,
            MimeType = "wav"
        };

        var textResponse = await audioToText.GetTextContentAsync(audioContent);

        return textResponse?.Text ?? throw new Exception("Couldn't convert audio to text");
    }

    public async Task PlaySoundStream(AudioContent? audio)
    {
        if (audio.Data is not null)
        {
            await Task.Run(() =>
            {
                using var stream = new MemoryStream(audio.Data.Value.ToArray());

                using var mp3Reader = new Mp3FileReader(stream);
                using var waveOut = new WaveOutEvent();

                waveOut.Init(mp3Reader);
                waveOut.Play();

                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            });
           
        }
        else
        {
            Console.WriteLine("Failed to retrieve audio data.");
        }
    }

}

#pragma warning restore SKEXP0001 //semantic kernel text to audio and vice versa is experimental, this disables the IDE warnings
