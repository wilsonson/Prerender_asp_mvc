using System;
using System.Collections.Generic;
using System.Configuration;

namespace Prerender.io
{
    public sealed class PrerenderConfigSection : ConfigurationSection
    {
        private const string DefaultServiceUrl = "http://service.prerender.io/";
        private object syncRoot = new object();
        private List<string> crawlerUserAgentsList = null;
        private List<string> whitelistSringList = null;
        private List<string> blacklistSringList = null;
        private List<string> extensionsToIgnoreStringList = null;

        [ConfigurationProperty("prerenderServiceUrl", DefaultValue = DefaultServiceUrl)]
        public string PrerenderServiceUrl
        {
            get
            {
                var prerenderServiceUrl = (string)this["prerenderServiceUrl"];
                return !string.IsNullOrWhiteSpace(prerenderServiceUrl) ? prerenderServiceUrl.Trim() : DefaultServiceUrl;
            }
            set
            {
                this["prerenderServiceUrl"] = value;
            }
        }

        [ConfigurationProperty("stripApplicationNameFromRequestUrl", DefaultValue = false)]
        public bool StripApplicationNameFromRequestUrl
        {
            get
            {
                return (bool)this["stripApplicationNameFromRequestUrl"];
            }
            set
            {
                this["stripApplicationNameFromRequestUrl"] = value;
            }
        }

        [ConfigurationProperty("whitelist")]
        public string WhitelistString
        {
            get
            {
                return (string)this["whitelist"];
            }
            set
            {
                this["whitelist"] = value;
            }
        }

        public IEnumerable<string> Whitelist
        {
            get
            {
                if (whitelistSringList == null)
                {
                    lock (syncRoot)
                    {
                        if (whitelistSringList == null)
                        {
                            whitelistSringList = new List<string>();
                            if (!string.IsNullOrWhiteSpace(WhitelistString))
                            {
                                whitelistSringList.AddRange(WhitelistString.Trim().Split(','));
                            }
                        }
                    }
                }
                return whitelistSringList;
            }
        }

        [ConfigurationProperty("blacklist")]
        public string BlacklistString
        {
            get
            {
                return (string)this["blacklist"];
            }
            set
            {
                this["blacklist"] = value;
            }
        }

        public IEnumerable<String> Blacklist
        {
            get
            {
                if (blacklistSringList == null)
                {
                    lock (syncRoot)
                    {
                        if (blacklistSringList == null)
                        {
                            blacklistSringList = new List<string>();
                            if (!string.IsNullOrWhiteSpace(BlacklistString))
                            {
                                blacklistSringList.AddRange(BlacklistString.Trim().Split(','));
                            }
                        }
                    }
                }
                return blacklistSringList;
            }
        }

        [ConfigurationProperty("extensionsToIgnore")]
        public string ExtensionsToIgnoreString
        {
            get
            {
                return (string)this["extensionsToIgnore"];
            }
            set
            {
                this["extensionsToIgnore"] = value;
            }
        }

        public IEnumerable<String> ExtensionsToIgnore
        {
            get
            {
                if (extensionsToIgnoreStringList == null)
                {
                    lock (syncRoot)
                    {
                        if (extensionsToIgnoreStringList == null)
                        {
                            extensionsToIgnoreStringList = new List<string>();
                            if (!string.IsNullOrWhiteSpace(ExtensionsToIgnoreString))
                            {
                                extensionsToIgnoreStringList.AddRange(ExtensionsToIgnoreString.Trim().Split(','));
                            }
                        }
                    }
                }
                return extensionsToIgnoreStringList;
            }
        }


        [ConfigurationProperty("crawlerUserAgents")]
        public string CrawlerUserAgentsString
        {
            get
            {
                return (string)this["crawlerUserAgents"];
            }
            set
            {
                this["crawlerUserAgents"] = value;
            }
        }

        public IEnumerable<string> CrawlerUserAgents
        {
            get
            {
                if (crawlerUserAgentsList == null)
                {
                    lock (syncRoot)
                    {
                        if (crawlerUserAgentsList == null)
                        {
                            crawlerUserAgentsList = new List<string>();
                            if (!string.IsNullOrWhiteSpace(CrawlerUserAgentsString))
                            {
                                crawlerUserAgentsList.AddRange(CrawlerUserAgentsString.Trim().Split(','));
                            }
                        }
                    }
                }
                return crawlerUserAgentsList;
            }
        }

        [ConfigurationProperty("Proxy")]
        public ProxyConfigElement Proxy
        {
            get
            {
                return (ProxyConfigElement)this["Proxy"];
            }
            set
            {
                this["Proxy"] = value;
            }
        }

        [ConfigurationProperty("token")]
        public string Token
        {
            get
            {
                return (string)this["token"];
            }
            set
            {
                this["token"] = value;
            }
        }
    }
}
