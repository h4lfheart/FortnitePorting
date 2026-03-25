using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.ApricotFudge.GPT;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels;

public partial class GPTViewModel(SupabaseService supabaseService) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabaseService;
    
    [ObservableProperty] private TextBox _textBox;

    [ObservableProperty] private bool _isWelcomeVisible = true;

    [ObservableProperty] private string _promptText = string.Empty;
    [ObservableProperty] private bool _isChatEnabled = false;

    [ObservableProperty] private ObservableCollection<GPTMessage> _messages = [];

    public string WelcomeText => $"Hello {SupaBase.UserInfo?.DisplayName ?? "there"}, what do you need?";

    private static Dictionary<string, string[]> _responsesByKeyword = new()
    {
        ["fortnite"] =
        [
            "what's fortnite??",
            "i heard that meowscles skin is cool",
            "is that the game with the funny dances",
            "you're only here for skins be honest",
            "you say fortnite like i don't already know"
        ],
        ["plugin"] =
        [
            "just install the plugin bro it's not that hard",
            "no you don't need to enable it in blender stop asking 🙄",
            "plugin not working? skill issue",
            "wrong version speedrun",
            "reinstall it and pretend that fixed it"
        ],
        ["porting"] =
        [
            "it literally works you just did something wrong",
            "fortnite porting carried and you still fumbled",
            "this is the easy part btw",
            "it worked perfectly until you touched it",
            "the tool did its job can you do yours",
            "this is a user issue not a fotnite porting issue",
            "it worked yesterday and nothing changed except you",
            "you broke something that was already working"
        ],
        ["blender"] =
        [
            "did you even open blender before asking this 💀",
            "press tab and magically everything fixes itself trust",
            "bro discovered blender and immediately got confused 😭"
        ],
        ["uefn"] =
        [
            "uefn broke again? shocking",
            "just restart uefn 12 times it usually works",
            "epic definitely tested this before release 👍"
        ],
        ["error"] =
        [
            "that error message is a suggestion not a rule",
            "have you tried turning your brain off and on again",
            "yeah that means you're cooked 😭"
        ],
        ["texture"] =
        [
            "missing texture goes hard ngl",
            "world grid material drip 🔥",
            "did you even pack the textures bro"
        ],
        ["import"] =
        [
            "you imported it wrong 100%",
            "wrong settings speedrun any%",
            "just reimport it 5 times it might fix itself"
        ],
        ["hello hi yo hey"] =
        [
            "oh great you're back",
            "hi... what did you break this time",
            "hi i guess",
            "hey what's the problem today",
            "yo",
            "hello user with issues",
            "yo what are we messing up today",
            "hey did you try turning it off and on yet",
            "yo. don't tell me it's travis scott again"
        ],
        ["what"] =
        [
            "what even is that 😭",
            "what did you DO",
            "what am i looking at right now",
            "what went wrong? everything",
            "what possessed you to try that",
            "what tutorial told you this was a good idea",
            "what part of this made sense to you",
            "what are you cooking (it's burnt)"
        ],
        ["how"] =
        [
            "how? yeah that's the fun part you don't",
            "step 1: don't",
            "just click buttons until something works",
            "same way you broke it probably",
            "press random buttons with confidence",
            "you think first (this is where you failed)"
        ],
        ["travis"] =
        [
            "not the travis scott port again 💀",
            "bro it's always travis scott",
            "find a different animation PLEASE"
        ],
        ["roblox"] =
        [
            "everything ends up in roblox somehow",
            "roblox porting detected",
            "cAn I mAKe tHIs iN RoBLox?? you're not original 😭😭😭"
        ],
        ["why"] =
        [
            "because it hates you specifically",
            "because epic said so",
            "because that's just how it be sometimes",
            "because that's a feature not a bug 👍",
            "because you didn't read the error message",
            "because the devs went home early"
        ],
        ["fix"] =
        [
            "have you tried not breaking it",
            "just reinstall everything honestly",
            "works on my machine"
        ],
        ["lighting"] =
        [
            "asteria day cubemap. one directional light. need i say less?"
        ],
        ["mutable"] =
        [
            "yeah good luck adding support for that LOL",
            "ping me when you make the pull request in 2037 bro"
        ],
        ["install"] =
        [
            "installing it wrong speedrun",
            "did you read anything or just click next",
            "install step defeated you already 😭"
        ],
        ["update"] =
        [
            "updating would've fixed this btw",
            "you're like 3 versions behind",
            "update exists for a reason"
        ],
        ["when"] =
        [
            "when? not anytime soon",
            "when you do it right",
            "when you stop breaking things"
        ],
        ["where"] =
        [
            "somewhere you didn't look",
            "right in front of you probably",
            "not where you're checking"
        ],
        ["which"] =
        [
            "the correct one (you didn't pick it)",
            "not the one you're using",
            "whichever one you ignored"
        ],
        ["help"] =
        [
            "help is a strong word",
            "do you want help or validation",
            "depends what you broke",
            "help? yeah good luck",
            "i can try but no promises",
            "you came to the right place (maybe)",
            "what did you do this time",
            "help requires effort from you too btw",
            "i'll help if this isn't something obvious",
            "you better not say 'it doesn't work'"
        ],
        ["bro"] = [
            "don't bro me bruh",
            "do you want to fight",
            "bruh"
        ],
        ["swear"] =
        [
            "OMG don't say that",
            "watch your language",
            "rude.",
            "no saying cuss words guys!!"
        ],
        ["recipe"] = [
            """
            Banana Bread
            
            Ingredients
            3 or 4 Over-ripe Bananas
            1 Cup Sugar
            1 Egg
            1 ½ Cups Flour
            ¼ Cup Melted Butter
            1 TSP Baking Soda
            1 TSP Salt
            1 ½ Cup Chocolate Chips
            
            Directions
            Mash the Bananas until they have a smooth consistency
            Stir in the Sugar, Egg, Flour, Melted Butter, Baking Soda, Salt, and Chocolate Chips until fully combined
            Pour the mixture into an ungreased loaf pan
            Bake at 325°F for 1 hour
            
            """
        ],
        ["elephant"] = [
            "roll tide 😛",
            "Elephants are the largest living land animals. Three living species are currently recognised: the African bush elephant (Loxodonta africana), the African forest elephant (L. cyclotis), and the Asian elephant (Elephas maximus). They are the only surviving members of the family Elephantidae and the order Proboscidea; extinct relatives include mammoths and mastodons. Distinctive features of elephants include a long proboscis called a trunk, tusks, large ear flaps, pillar-like legs, and tough but sensitive grey skin. The trunk is prehensile, bringing food and water to the mouth and grasping objects. Tusks, which are derived from the incisor teeth, serve both as weapons and as tools for moving objects and digging. The large ear flaps assist in maintaining a constant body temperature as well as in communication. African elephants have larger ears and concave backs, whereas Asian elephants have smaller ears and convex or level backs."
        ],
        ["oshawott"] = [
            "ASHASHASHOWATSHAASHHASHOWSHAHSHWOSGAHWHSHAWATTOSHAGWSHA"
        ],
        ["longtext"] = [
            "i ain't reading all that vro 💀💀💀"
        ],
        ["ping"] = ["pong"],
        ["marco"] = ["polo"],
        ["blackhole"] = ["bros error was so dense it cause a blackhole 😭😭😭"]
    };

    private static string[] _defaultResponses =
    [
        "bro that sucks for you lol",
        "i have no clue how to help",
        "what are you yapping about dawg 💀💀💀",
        "user error i guess haha",
        "that's crazy anyway",
        "sounds like a you problem ngl",
        "i'm not fixing that for you",
        "good luck with that 💀",

        "interesting",
        "noted",
        "alright",
        "okay then",
        "fair enough",
        "makes sense i guess",
        "i see",
        "hmm",
        "got it",
        "cool",
        "yeah okay",
        "right",

        "go on",
        "and then what",
        "keep talking",
        "i'm listening",
        "continue",
        "alright i'm following",
        "say more",
        "what else",
        "go ahead",

        "bold move",
        "interesting decision",
        "that’s one way to do it",
        "could've been worse",
        "not the worst idea",
        "i've seen worse",
        "you do you",
        "i respect the attempt",
        "kinda wild ngl",
        "okay i guess we're doing this",

        "anyway",
        "so yeah",
        "it is what it is",
        "we move",
        "real",
        "honestly yeah",
        "lowkey",
        "highkey",
        "fair",
        "valid",
        "bet",
        "have you heard of 67"
    ];

    private ProfanityFilter.ProfanityFilter _profanityFilter = new();

    public async Task SendPrompt()
    {
        var userMessage = new GPTMessage
        {
            IsUser = true,
            Text = PromptText
        };

        Messages.Add(userMessage);
        PromptText = string.Empty;

        var gptMessage = new GPTMessage
        {
            IsUser = false,
            IsThinking = true
        };
        Messages.Add(gptMessage);

        gptMessage.IsThinking = true;
        IsChatEnabled = false;


        var textSet = _defaultResponses;

        if (_profanityFilter.ContainsProfanity(userMessage.Text))
        {
            textSet = _responsesByKeyword["swear"];
        }
        
        foreach (var (keyword, set) in _responsesByKeyword)
        {
            if (!MiscExtensions.FilterAny(userMessage.Text, keyword.Split(' '))) continue;

            textSet = set;
            break;
        }


        if (userMessage.Text.Length > 200)
        {
            textSet = _responsesByKeyword["longtext"];
        }
        
        var targetText = textSet.Random()!.Trim();
        await Task.Delay(Random.Shared.Next(500, 500 + 25*targetText.Length));
        
        gptMessage.IsThinking = false;
        while (targetText.Length > 0)
        {
            gptMessage.Text += targetText[0];
            targetText = targetText[1..];
            await Task.Delay(10);
        }

        IsChatEnabled = true;
        await TaskService.RunDispatcherAsync(() =>
        {
            TextBox.Focus();
        });
        
    }
}