using System.Collections.ObjectModel;

namespace Prerender.io
{
    static class PredefinedValues
    {
        public static readonly ReadOnlyCollection<string> CrawlerUserAgents = new ReadOnlyCollection<string>(
            new[]
            {
                "googlebot", "yahoo", "bingbot", "yandex", "baiduspider", "facebookexternalhit", "twitterbot", "rogerbot", "linkedinbot",
                "embedly", "quora link preview", "showyoubot", "outbrain", "pinterest/0.",
                "developers.google.com/+/web/snippet", "slackbot", "vkShare", "W3C_Validator",
                "redditbot", "Applebot", "WhatsApp", "flipboard", "tumblr", "bitlybot",
                "SkypeUriPreview", "nuzzel", "Discordbot", "Google Page Speed", "x-bufferbot"
            }
        );

        public static readonly ReadOnlyCollection<string> ExtensionsToIgnore = new ReadOnlyCollection<string>(
            new[]{".js", ".css", ".less", ".png", ".jpg", ".jpeg",
                ".gif", ".pdf", ".doc", ".txt", ".zip", ".mp3", ".rar", ".exe", ".wmv", ".doc", ".avi", ".ppt", ".mpg",
                ".mpeg", ".tif", ".wav", ".mov", ".psd", ".ai", ".xls", ".mp4", ".m4a", ".swf", ".dat", ".dmg",
                ".iso", ".flv", ".m4v", ".torrent", ".ico" }
        );


    }
}
