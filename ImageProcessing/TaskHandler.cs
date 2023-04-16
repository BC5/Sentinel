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
    
    
    [SlashCommand("invoketask","Create image from template")]
    public async Task InvokeTask([Summary("task"), Autocomplete(typeof(TaskAutocompleteHandler))] string task)
    {
        
        //[Summary("parameter_name"), Autocomplete(typeof(ExampleAutocompleteHandler))]
        
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

    public class TaskAutocompleteHandler : AutocompleteHandler
    {
        private AssetManager _assets;
        public TaskAutocompleteHandler(AssetManager assets)
        {
            _assets = assets;
        }
        
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter, IServiceProvider services)
        {
            string[] tasks = _assets.GetLibraryContents("operations", AssetManager.AssetType.Json);
            List<AutocompleteResult> results = new();
            string user = (autocompleteInteraction.Data.Current.Value.ToString() ?? "").ToLower();
            foreach (string task in tasks)
            {
                if(task.StartsWith(user) || user == "")
                    results.Add(new AutocompleteResult(task,task));
            }
            return AutocompletionResult.FromSuccess(results.Take(25));
            
        }
    }

    public async static Task Execute(SocketModal modal, AssetManager assets)
    {
        string taskId = modal.Data.CustomId.Substring(5);

        
        
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
        await modal.DeferAsync();
        object? img = sequence.Execute(assets, argss);

        if (img == null)
        {
            await modal.FollowupAsync("Error", ephemeral: true);
        }
        else
        {
            if (img is MagickImageCollection)
            {
                MagickImageCollection gif = (MagickImageCollection) img;
                var ms = new MemoryStream(gif.ToByteArray());
                await modal.FollowupWithFileAsync(ms, "operation.gif");
            }
            else if(img is MagickImage)
            {
                MagickImage png = (MagickImage) img;
                var ms = new MemoryStream(png.ToByteArray());
                await modal.FollowupWithFileAsync(ms, "operation.png");
            }
            else
            {
                await modal.FollowupAsync("Error", ephemeral: true);
                Console.WriteLine("Unexpected type " + img.GetType());
            }
        }
        
    }

}