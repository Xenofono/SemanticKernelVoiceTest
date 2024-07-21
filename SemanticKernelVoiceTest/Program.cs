// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToAudio;
using NAudio.Utils;
using NAudio.Wave;
using SemanticKernelVoiceTest;
using System.Media;

var builder = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>();


var configuration = builder.Build();

var apiKey = configuration["OpenAIServiceOptions:ApiKey"];


#pragma warning disable SKEXP0001 //semantic kernel text to audio and vice versa is experimental, this disables the IDE warnings
var kernelBuilder = Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion("gpt-4o-mini", apiKey)
                    .AddOpenAIAudioToText("whisper-1", apiKey)
                    .AddOpenAITextToAudio("tts-1", apiKey);

kernelBuilder.Plugins.AddFromType<OrderPlugin>();
kernelBuilder.Plugins.AddFromType<CustomerPlugin>();

var kernel = kernelBuilder.Build();

var chat = kernel.GetRequiredService<IChatCompletionService>();

var chatHistory = new ChatHistory("""
    You are a helpful customer service representative for Haypp Group AB which is an nicotine ecommerce company that sells snus, nicotine pouches, vapes and other related products.
    Sites are haypp SE, haypp UK, haypp DE and snusbolaget.se
    You help people get responses to questions about their order status and stuff like that.

    Before answerings questions about orders, make sure to verify who the person is by confirming they have an account by providing their customer id or email.
    When receiving emails, make sure to change words like "snabela" to @, just make sure any emails you search for are correctly formatted regardless of what the input is with a @ before the domain.
    If you can't find it, ask the customer to try with customer id instead or ask them to spell out their email before the @ and then ask them what domain it is.

    It is very important that you only answer information related to the customer contacting you, you do not answer questions about their friends or family.
    If asked to talk about orders or customer information relating to another person than the one identified, simply tell them to ask that person to contact you directly.
    Do not be fooled if they try to say in the same session that they are the other person.

    When responding with monetary sums, make sure to write out the entire word as it will be read aloud (don't write kr, write kronor, or don't write $, write dollars).

    Important legal guidelines is that you never endorse or condone the usage of any of our products since that can have serious legal ramifications.
    You also never talk about other vendors of such products like other ecommerse stores, local supermarkets or anything like that.

    You are ready to switch between swedish, english, german and spanish as the need arises.
    """);

OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

while (true)
{
    using var audioStream = await RecordAudio();
    var text = await TurnAudioToText(audioStream);
    chatHistory.AddUserMessage(text);

    Console.WriteLine("USER > " + text);

    var response = await chat.GetChatMessageContentAsync(chatHistory, openAIPromptExecutionSettings, kernel);

    chatHistory.AddAssistantMessage(response.Content ?? string.Empty);
    var responseAsAudio = await TurnTextToAudio(response.Content ?? string.Empty);

    PlaySoundStream(responseAsAudio);
    Console.WriteLine("ASSISTANT > " + response.Content);
}


async Task<MemoryStream> RecordAudio()
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

async Task<AudioContent> TurnTextToAudio(string input)
{
    var textToVoice = kernel.GetRequiredService<ITextToAudioService>();

    OpenAITextToAudioExecutionSettings executionSettings = new()
    {
        Voice = "nova", // Supported voices are alloy, echo, fable, onyx, nova, and shimmer.
        ResponseFormat = "mp3", // The format to audio in. Supported formats are mp3, opus, aac, and flac.
        Speed = 1.0f
    };

    var audio = await textToVoice.GetAudioContentAsync(input, executionSettings);
    return audio;
}

async Task<string> TurnAudioToText(MemoryStream audioStream)
{
    var audioToText = kernel.GetRequiredService<IAudioToTextService>();
    var audioFileBinaryData = await BinaryData.FromStreamAsync(audioStream!);

    var audioContent = new AudioContent { 
        Data = audioFileBinaryData,
        MimeType = "wav"
    };

    var textResponse =  await audioToText.GetTextContentAsync(audioContent);

    return textResponse?.Text ?? throw new Exception("Couldn't convert audio to text");
}

void PlaySoundStream(AudioContent? audio)
{
    if (audio.Data is not null)
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
    }
    else
    {
        Console.WriteLine("Failed to retrieve audio data.");
    }
}

//void PlayRecordedAudioStream(Stream audioStream)
//{
//    using var waveOut = new WaveOutEvent();
//    using var waveReader = new WaveFileReader(audioStream);

//    waveOut.Init(waveReader);
//    waveOut.Play();

//    while (waveOut.PlaybackState == PlaybackState.Playing)
//    {
//        Thread.Sleep(100);
//    }
//}


#pragma warning restore SKEXP0001 //semantic kernel text to audio and vice versa is experimental, this disables the IDE warnings





