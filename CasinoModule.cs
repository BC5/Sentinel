using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImageMagick;

namespace Sentinel;


[Group("casino","99% of gambling addicts quit right before they're about to hit it big")]
public class CasinoModule : InteractionModuleBase
{
    private Config _config;
    private Sentinel _core;
    private Random _random;
    private Data _data;
    
    public CasinoModule(Sentinel core, Random rand, Data data)
    {
        _core = core;
        _config = core.GetConfig();
        _random = rand;
        _data = data;
    }

    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("stats","Casino stats")]
    public async Task Stats()
    {
        await DeferAsync(ephemeral: true);
        var txns = _data.Transactions.Where(txn => txn.Reason == Transaction.TxnType.CasinoSlots && txn.ServerID == Context.Guild.Id).ToAsyncEnumerable();

        int SlotsRevenue = 0;
        int SlotsPayouts = 0;
        
        await foreach (Transaction txn in txns)
        {
            if (txn.RecipientID == txn.ServerID) //House wins
            {
                SlotsRevenue = SlotsRevenue + txn.Amount;
            }
            else
            {
                SlotsPayouts = SlotsPayouts + txn.Amount;
            }
        }

        await FollowupAsync($"**Slots Revenue:** £{SlotsRevenue:n0}\n**Slots Payouts:** £{SlotsPayouts:n0}\n**Slots Profit:** £{SlotsRevenue-SlotsPayouts:n0}");

    }

    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("slotscfg","Casino stats")]
    public async Task SlotsConfig(int basePayout, int fee)
    {
        var srv = await _data.GetServerConfig(Context.Guild.Id);

        srv.SlotsFee = fee;
        srv.SlotsPayout = basePayout;

        await _data.SaveChangesAsync();
        await RespondAsync($"Slots base payout now £{srv.SlotsPayout:n0}, with play fee of £{srv.SlotsFee:n0}");
    }

    [SlashCommand("slots","Slots machine. Self-explanatory.")]
    public async Task Slots()
    {
        await DeferAsync();
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        var usr = await _data.GetServerUser((IGuildUser) Context.User);

        if (usr.Balance < srv.SlotsFee)
        {
            await FollowupAsync("Credit check failed. Try coming back to the casino when you're not broke.");
            return;
        }

        SlotMachine slotMachine = new SlotMachine(_random, _config.SlotsEmotes);

        string msg;
        if (slotMachine.IsWin())
        {
            int payout = srv.SlotsPayout * slotMachine.WinValue();

            //If you hit a negative jackpot that's more than your balance just take everything
            if (payout < 0)
            {
                if (usr.Balance < 0 - payout)
                {
                    payout = 0-usr.Balance;
                }
            }
            
            msg = slotMachine.GetPayline() + $"\n**You've won £{payout:n0}!**";
            await _data.Transact(null,usr,payout-srv.SlotsFee,Transaction.TxnType.CasinoSlots,allowSeizure:true,allowDebt:true);
        }
        else
        {
            msg = slotMachine.GetPayline() + $"\n-£{srv.SlotsFee:n0}";
            await _data.Transact(usr,null,srv.SlotsFee,Transaction.TxnType.CasinoSlots);
        }
        await _data.SaveChangesAsync();

        if (!slotMachine.IsWin())
        {
            ComponentBuilder cb = new();
            cb.WithButton("Play Again", $"slots-reroll-{Context.User.Id}", ButtonStyle.Primary, new Emoji("🎰"));
            await FollowupAsync(msg, components: cb.Build());
        }
        else
        {
            await FollowupAsync(msg);
        }
        
    }

    [ComponentInteraction("slots-reroll-*",ignoreGroupNames: true)]
    public async Task SlotsReroll(ulong uid)
    {
        var interaction = (IComponentInteraction) Context.Interaction;
        var omsg = (SocketUserMessage) ((IComponentInteraction) Context.Interaction).Message;
        
        if (uid != Context.User.Id)
        {
            await RespondAsync("Get your own slot machine loser.", ephemeral: true);
            return;
        }
        
        await DeferAsync();
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        var usr = await _data.GetServerUser((IGuildUser) Context.User);

        if (usr.Balance < srv.SlotsFee)
        {
            await FollowupAsync("Credit check failed. Try coming back to the casino when you're not broke.");
            return;
        }

        SlotMachine slotMachine = new SlotMachine(_random, _config.SlotsEmotes);

        string msg;
        if (slotMachine.IsWin())
        {
            int payout = srv.SlotsPayout * slotMachine.WinValue();

            //If you hit a negative jackpot that's more than your balance just take everything
            if (payout < 0)
            {
                if (usr.Balance < 0 - payout)
                {
                    payout = 0-usr.Balance;
                }
            }
            
            msg = slotMachine.GetPayline() + $"\n**You've won £{payout:n0}!**";
            await _data.Transact(null,usr,payout-srv.SlotsFee,Transaction.TxnType.CasinoSlots,allowSeizure:true,allowDebt:true);
        }
        else
        {
            msg = slotMachine.GetPayline() + $"\n-£{srv.SlotsFee:n0}";
            await _data.Transact(usr,null,srv.SlotsFee,Transaction.TxnType.CasinoSlots);
        }
        await _data.SaveChangesAsync();

        if (slotMachine.IsWin())
        {
            
            await omsg.ModifyAsync(x =>
            {
                x.Content = msg;
                x.Components = null;
            });
            await FollowupAsync();
        }
        else
        {
            await omsg.ModifyAsync(x => x.Content = msg);
            await FollowupAsync();
        }
    }

    public class SlotMachine
    {
        public int slotCount;
        Config.SlotsEmote[] slots;
        private Random _random;
        private List<Config.SlotsEmote> _emotes;

        public SlotMachine(Random random, List<Config.SlotsEmote> emotes, int slotCount = 3)
        {
            slots = new Config.SlotsEmote[slotCount];
            _random = random;
            _emotes = emotes;
            Roll();
        }

        public void Roll()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = _emotes[_random.Next(_emotes.Count)];
            }
        }

        public string GetPayline()
        {
            string payline = "# ";
            for (int i = 0; i < slots.Length; i++)
            {
                payline = payline + slots[i].emote;
            }
            return payline;
        }

        public bool IsWin()
        {
            bool win = true;
            for (int i = 1; i < slots.Length; i++)
            {
                if (slots[0].emote != slots[i].emote)
                {
                    win = false;
                    break;
                }
            }
            return win;
        }

        public int WinValue()
        {
            if (!IsWin()) return 0;
            return slots[0].value;
        }
        
    }

    public Config.SlotsEmote GetSlot()
    {
        return _config.SlotsEmotes[_random.Next(_config.SlotsEmotes.Count)];
    }
    
}