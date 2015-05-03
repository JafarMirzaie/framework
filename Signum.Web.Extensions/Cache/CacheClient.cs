﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Mvc;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using System.Web.Mvc.Html;
using Signum.Web.Omnibox;
using Signum.Entities.Cache;
using Signum.Web.Maps;
using Signum.Engine.Cache;

namespace Signum.Web.Cache
{
    public static class CacheClient
    {
        public static string ViewPrefix = "~/Cache/Views/{0}.cshtml";
        public static JsModule Model = new JsModule("Extensions/Signum.Web.Extensions/Cache/Scripts/Cache");
        public static JsModule ColorModel = new JsModule("Extensions/Signum.Web.Extensions/Cache/Scripts/CacheColors");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(CacheClient));

                SpecialOmniboxProvider.Register(new SpecialOmniboxAction("ViewCache",
                    () => CachePermission.ViewCache.IsAuthorized(),
                    uh => uh.Action((CacheController cc) => cc.View())));

                MapClient.GetColorProviders += GetMapColors;
            }
        }

        static MapColorProvider[] GetMapColors()
        {
            if (!CachePermission.ViewCache.IsAuthorized())
                return new MapColorProvider[0];

            var groups = CacheLogic.Statistics().SelectMany(a => a.Explore()).GroupToDictionary(a => a.Table.Name.ToString());
            var semi = CacheLogic.SemiControllers.ToHashSet();

            var s = Schema.Current;

            var semiNames = semi.Select(t => s.Table(t).Name.ToString()).ToArray();

            return new[]
            {
                new MapColorProvider
                { 
                    Name = "cache-rows", 
                    NiceName = "Cache - Rows", 
                    GetJsProvider =  ColorModel["cacheColors"](MapClient.NodesConstant, "Rows", "cache-rows"),
                    AddExtra = t => 
                    {
                        if (groups.ContainsKey(t.tableName)) 
                        { 
                            var isSemi = semiNames.Contains(t.tableName);
                            t.extra["cache-semi"] = isSemi;
                            foreach (var mt in t.mlistTables)
                                mt.extra["cache-semi"] = isSemi;

                            t.extra["cache-rows"] = groups[t.tableName].Sum(a => a.Count);
                            foreach (var mt in t.mlistTables)
                                mt.extra["cache-rows"] = groups[mt.tableName].Sum(a => a.Count);
                        }
                    }
                },

                new MapColorProvider
                { 
                    Name = "cache-invalidations", 
                    NiceName = "Cache - Invalidations", 
                    GetJsProvider =  ColorModel["cacheColors"](MapClient.NodesConstant, "Invalidations", "cache-invalidations"),
                    AddExtra = t => 
                    {
                        if (groups.ContainsKey(t.tableName))
                        {
                            t.extra["cache-invalidations"] = groups[t.tableName].Sum(a => a.Invalidations);
                            foreach (var mt in t.mlistTables)
                                mt.extra["cache-invalidations"] = groups[mt.tableName].Sum(a => a.Invalidations);
                        }
                    }
                },
                        
                new MapColorProvider
                { 
                    Name = "cache-loads", 
                    NiceName = "Cache - Loads", 
                    GetJsProvider =  ColorModel["cacheColors"](MapClient.NodesConstant, "Loads", "cache-loads"),
                    AddExtra = t => 
                    {
                        if (groups.ContainsKey(t.tableName))
                        {
                            t.extra["cache-loads"] = groups[t.tableName].Sum(a => a.Loads);
                            foreach (var mt in t.mlistTables)
                                mt.extra["cache-loads"] = groups[mt.tableName].Sum(a => a.Loads);
                        }
                    }
                },

                new MapColorProvider
                { 
                    Name = "cache-load-time", 
                    NiceName = "Cache - Load Time", 
                    GetJsProvider =  ColorModel["cacheColors"](MapClient.NodesConstant, "ms Load Time", "cache-load-time"),
                    AddExtra = t => 
                    {
                        if (groups.ContainsKey(t.tableName))
                        {
                            t.extra["cache-load-time"] = groups[t.tableName].Sum(a => a.SumLoadTime.Milliseconds);
                            foreach (var mt in t.mlistTables)
                                mt.extra["cache-load-time"] = groups[mt.tableName].Sum(a => a.SumLoadTime.Milliseconds);
                        }
                    }
                },
            };
        }


        static IEnumerable<CachedTableBase> Explore(this CachedTableBase root)
        {
            yield return root;

            if (root.SubTables != null)
            {
                foreach (var tab in root.SubTables)
                {
                    foreach (var tab2 in tab.Explore()) //Quadratic but tipically very small
                    {
                        yield return tab2;
                    }
                }
            }
        }
    }
}
