using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImageMagick;
using Newtonsoft.Json;
using Sentinel.ImageProcessing.Operations;

namespace Sentinel.ImageProcessing;

public class TaskHandler : InteractionModuleBase
{

    private AssetManager _assets;
    
    public TaskHandler(AssetManager assets)
    {
        _assets = assets;
    }
    
    
    [SlashCommand("InvokeTask","Create image from template")]
    public async Task InvokeTask(string task)
    {
        try
        {
            string jsontext = File.ReadAllText(_assets.GetJsonPath("operations", task));
            var opt = new JsonSerializerSettings();
            opt.TypeNameHandling = TypeNameHandling.Auto;
            OperationSequence? sequence = JsonConvert.DeserializeObject<OperationSequence>(jsontext, opt);

            if (sequence == null)
            {
                await RespondAsync("File problem. Malformed JSON?",ephemeral: true);
                return;
            }
            
            ModalBuilder mb = new ModalBuilder();
            mb = mb.WithTitle(sequence.Name)
                .WithCustomId($"task-{task}");
            for (int i = 0; i < sequence.ArgumentAssigment.Count; i++)
            {
                string argname = $"Arg {i}";
                if (i < sequence.ArgumentNames.Count)
                {
                    argname = sequence.ArgumentNames[i];
                }
                mb = mb.AddTextInput(argname,$"task_arg{i}",TextInputStyle.Paragraph);
            }

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }
        catch (Exception e)
        {
            await RespondAsync("Oopsie! " + e.Message);
        }
    }

    public async static Task Execute(SocketModal modal, AssetManager assets)
    {
        string taskId = modal.Data.CustomId.Substring(6);
        
        string jsontext = File.ReadAllText(assets.GetJsonPath("operations", taskId));
        var opt = new JsonSerializerSettings();
        opt.TypeNameHandling = TypeNameHandling.Auto;
        OperationSequence? sequence = JsonConvert.DeserializeObject<OperationSequence>(jsontext, opt);

        if (sequence == null)
        {
            await modal.RespondAsync("Something broke. That really shouldn't happen here. Weird.");
            return;
        }

        string args = "";
        
        var components = modal.Data.Components.ToList();
        foreach (var component in components)
        {
            if (args == "") args = args + component.Value.Replace(';', ';'); //Replace semicolons with greek question marks
            else args = args + ";" + component.Value.Replace(';', ';');
        }
        string[] argss = args.Split(";");
        object? img = sequence.Execute(assets, argss);

        if (img == null)
        {
            await modal.RespondAsync("Error", ephemeral: true);
        }
        else
        {
            if (img is MagickImageCollection)
            {
                MagickImageCollection gif = (MagickImageCollection) img;
                var ms = new MemoryStream(gif.ToByteArray());
                await modal.RespondWithFileAsync(ms, "operation.gif");
            }
            else if(img is MagickImage)
            {
                MagickImage png = (MagickImage) img;
                var ms = new MemoryStream(png.ToByteArray());
                await modal.RespondWithFileAsync(ms, "operation.png");
            }
            else
            {
                await modal.RespondAsync("Error", ephemeral: true);
                Console.WriteLine("Unexpected type " + img.GetType());
            }
        }
        
    }

}