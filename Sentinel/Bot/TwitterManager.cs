using System.Reflection.Emit;
using Discord;
using Tweetinvi;
using Tweetinvi.Models.V2;
using Tweetinvi.Parameters.V2;

namespace Sentinel.Bot;

public class TwitterManager
{
    private SentinelBot _bot;
    private Config _conf;
    private TwitterClient _client;

    public TwitterManager(SentinelBot bot)
    {
        _bot = bot;
        _conf = bot.GetConfig();
        _client = new TwitterClient(_conf.TwitterAPIKey, _conf.TwitterAPISecret, _conf.TwitterAPIToken);
    }

    public void Reconnect()
    {
        _client = new TwitterClient(_conf.TwitterAPIKey, _conf.TwitterAPISecret, _conf.TwitterAPIToken);
    }

    public async Task<List<Embed>?> ThreadEmbed(List<TweetV2Response> thread)
    {
        Dictionary<string, UserV2> authors = new();
        var author = await _client.UsersV2.GetUserByIdAsync(thread[0].Tweet.AuthorId);
        List<Embed> embeds = new List<Embed>();
        foreach (var tweet in thread)
        {
            if (!authors.ContainsKey(tweet.Tweet.AuthorId))
            {
                authors.Add(tweet.Tweet.AuthorId, (await _client.UsersV2.GetUserByIdAsync(tweet.Tweet.AuthorId)).User);
            }

            embeds.Add(TweetEmbed(tweet, authors[tweet.Tweet.AuthorId]));
        }
        return embeds;
    }

    public Embed TweetEmbed(TweetV2Response tweet, UserV2 author)
    {
        EmbedBuilder eb = new EmbedBuilder();
        eb.WithAuthor($"{author.Name} (@{author.Username})", author.ProfileImageUrl, $"https://twitter.com/i/web/status/{tweet.Tweet.Id}");
        eb.WithDescription(tweet.Tweet.Text);
        if (tweet.Includes.Media != null)
        {
            if (tweet.Includes.Media.Length > 0)
            {
                string footer = "";
                if (tweet.Includes.Media[0].Type == "photo")
                {
                    eb.WithImageUrl(tweet.Includes.Media[0].Url);
                }
                else
                {
                    eb.WithImageUrl(tweet.Includes.Media[0].PreviewImageUrl);
                    footer = footer + "This is a video. Watch it on Twitter. ";
                }

                if (tweet.Includes.Media.Length > 1)
                {
                    footer = footer + $"{tweet.Includes.Media.Length - 1} attachments not shown here. ";
                }
                eb.WithFooter(footer);
            }
        }
        eb.WithColor(new Color(29, 161, 242));
        return eb.Build();
    }

    public async Task<List<TweetV2Response>?> GetThread(long id)
    {
        List<TweetV2Response> thread = new List<TweetV2Response>();

        TweetV2Response original = await _client.TweetsV2.GetTweetAsync(id);

        if (original == null)
        {
            Console.WriteLine("Null response");
            return null;
        }

        thread.Add(original);

        TweetV2Response tweet = original;
        while (tweet.Tweet.ReferencedTweets != null)
        {
            string next = "";
            foreach (var reference in tweet.Tweet.ReferencedTweets)
            {
                if (reference.Type.Contains("replied_to"))
                {
                    next = reference.Id;
                }
            }
            if (next == "") break;
            tweet = await _client.TweetsV2.GetTweetAsync(next);
            thread.Add(tweet);
        }

        return thread;
    }
}