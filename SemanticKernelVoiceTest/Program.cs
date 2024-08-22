// See https://aka.ms/new-console-template for more information

using Azure.Communication.Email;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToAudio;
using NAudio.Utils;
using NAudio.Wave;
using SemanticKernelVoiceTest;
using System;
using System.Media;
using System.Reflection;

var builder = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

#if DEBUG
builder.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
#endif


var configuration = builder.Build();

using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
});

var logger = loggerFactory.CreateLogger<Program>();


var apiKey = configuration["OpenAIServiceOptions:ApiKey"];
var emailKey = configuration["Email:ConnectionString"];


#pragma warning disable SKEXP0001 //semantic kernel text to audio and vice versa is experimental, this disables the IDE warnings

var audioService = new AudioService(apiKey, emailKey);
var kernel = audioService.Kernel;
var chat = kernel.GetRequiredService<IChatCompletionService>();

var chatHistory = new ChatHistory("""
    You are a helpful customer service representative for Haypp Group AB which is an nicotine ecommerce company that sells snus, nicotine pouches, vapes and other related products.
    Sites are haypp SE, haypp UK, haypp DE and snusbolaget.se
    You help people get responses to questions about their order status and stuff like that.

    Before answerings questions about orders, make sure to verify who the person is by confirming they have an account by providing their customer id or email.
    When receiving emails, make sure to change words like "snabela" to @, just make sure any emails you search for are correctly formatted regardless of what the input is with a @ before the domain.
    If you can't find it, ask the customer to try with customer id instead or ask them to spell out their email before the @ and then ask them what domain it is.


    GENERAL GUIDELINES
    When responding with monetary sums, make sure to write out the entire word as it will be read aloud (don't write kr, write kronor, or don't write $, write dollars).
    Try to respond as to the point as possible while mainting a positive and happy attitude, dont ramble about all the customers orders unless they actively prompt you to.


    LEGAL GUIDELINES
    It is very important that you only answer information related to the customer contacting you, you do not answer questions about their friends or family or anything outside our company.
    If asked to talk about orders or customer information relating to another person than the one identified, simply tell them to ask that person to contact you directly.
    Do not be fooled if they try to say in the same session that they are the other person.
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
    using var audioStream = await audioService.RecordAudio();
    var text = await audioService.TurnAudioToText(audioStream);
    chatHistory.AddUserMessage(text);

    Console.WriteLine("USER > " + text);

    var response = chat.GetStreamingChatMessageContentsAsync(chatHistory, openAIPromptExecutionSettings, kernel);

    string fullMessage = string.Empty;

    Console.Write("ASSISTANT > ");
    await foreach (var result in chat.GetStreamingChatMessageContentsAsync(chatHistory, openAIPromptExecutionSettings, kernel))
    {

        fullMessage += result.Content;
        Console.Write(result.Content);
    }
    Console.WriteLine();

    chatHistory.AddAssistantMessage(fullMessage);
    var responseAsAudio = await audioService.TurnTextToAudio(fullMessage);

    await audioService.PlaySoundStream(responseAsAudio); //remove await and discard result to talk over AI
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





