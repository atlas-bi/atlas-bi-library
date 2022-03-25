using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas_Web.Models;
using Atlas_Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using UAParser;

namespace Atlas_Web.Pages.Analytics
{
    public class VisitsModel : PageModel
    {
        private readonly Atlas_WebContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public VisitsModel(Atlas_WebContext context, IMemoryCache cache, IConfiguration config)
        {
            _context = context;
            _cache = cache;
            _config = config;
        }

        public class BarData
        {
            public string Key { get; set; }
            public string Href { get; set; }
            public string TitleOne { get; set; }
            public string TitleTwo { get; set; }

            public double Count { get; set; }
            public double? Percent { get; set; }
        }

        public class AccessHistoryData
        {
            public string Date { get; set; }
            public int Pages { get; set; }
            public int Sessions { get; set; }
            public double LoadTime { get; set; }
        }

        public int Views { get; set; }
        public int Visitors { get; set; }
        public double LoadTime { get; set; }

        public List<AccessHistoryData> AccessHistory { get; set; }

        public List<BarData> BarDataSet { get; set; }

        public async Task<ActionResult> OnGetAsync(double start_at = -86400, double end_at = 0)
        {
            // double diff = end_at - start_at;

            /*
            when start - end < 2days, use 1 AM, 2 AM...
            when start - end < 8 days use  Sun 3/20, Mon 3/21...
            when start - end < 365 days use Mar 1, Mar 2 ...
            when start - end > 365 days use Jan, Feb ...

            when using all time, get first day and last day and use the above rules
            */

            var subquery = _context.Analytics.Where(
                x =>
                    x.AccessDateTime >= DateTime.Now.AddSeconds(start_at)
                    && x.AccessDateTime <= DateTime.Now.AddSeconds(end_at)
            );
            switch (end_at - start_at)
            {
                // for < 2 days
                // 1 AM, 2 AM etc..
                default:
                case < 172800:
                    AccessHistory = await (
                        from a in subquery
                        group a by new DateTime(
                            (a.AccessDateTime ?? DateTime.Now).Year,
                            (a.AccessDateTime ?? DateTime.Now).Month,
                            (a.AccessDateTime ?? DateTime.Now).Day,
                            (a.AccessDateTime ?? DateTime.Now).Hour,
                            0,
                            0
                        ) into grp
                        select new AccessHistoryData
                        {
                            Date = grp.Key.ToString("h tt"),
                            Sessions = grp.Select(x => x.SessionId).Distinct().Count(),
                            Pages = grp.Select(x => x.PageId).Distinct().Count(),
                            LoadTime = Math.Round(
                                (grp.Average(x => (long)Convert.ToDouble(x.LoadTime)) / 1000),
                                1
                            )
                        }
                    ).ToListAsync();

                    break;
                // for < 8 days
                //  Sun 3/20, Mon 3/21...
                case < 691200:
                    AccessHistory = await (
                        from a in subquery
                        group a by new DateTime(
                            (a.AccessDateTime ?? DateTime.Now).Year,
                            (a.AccessDateTime ?? DateTime.Now).Month,
                            (a.AccessDateTime ?? DateTime.Now).Day,
                            0,
                            0,
                            0
                        ) into grp
                        select new AccessHistoryData
                        {
                            Date = grp.Key.ToString("ddd M/d"),
                            Sessions = grp.Select(x => x.SessionId).Distinct().Count(),
                            Pages = grp.Select(x => x.PageId).Distinct().Count(),
                            LoadTime = Math.Round(
                                (grp.Average(x => (long)Convert.ToDouble(x.LoadTime)) / 1000),
                                1
                            )
                        }
                    ).ToListAsync();
                    break;
                // for < 365 days
                // Mar 1, Mar 2
                case < 31536000:
                    AccessHistory = await (
                        from a in subquery
                        group a by new DateTime(
                            (a.AccessDateTime ?? DateTime.Now).Year,
                            (a.AccessDateTime ?? DateTime.Now).Month,
                            (a.AccessDateTime ?? DateTime.Now).Day,
                            0,
                            0,
                            0
                        ) into grp
                        select new AccessHistoryData
                        {
                            Date = grp.Key.ToString("MMM d"),
                            Sessions = grp.Select(x => x.SessionId).Distinct().Count(),
                            Pages = grp.Select(x => x.PageId).Distinct().Count(),
                            LoadTime = Math.Round(
                                (grp.Average(x => (long)Convert.ToDouble(x.LoadTime)) / 1000),
                                1
                            )
                        }
                    ).ToListAsync();
                    break;
                case >= 31536000:
                    AccessHistory = await (
                        from a in subquery
                        group a by new DateTime(
                            (a.AccessDateTime ?? DateTime.Now).Year,
                            (a.AccessDateTime ?? DateTime.Now).Month,
                            0,
                            0,
                            0,
                            0
                        ) into grp
                        select new AccessHistoryData
                        {
                            Date = grp.Key.ToString("MMM"),
                            Sessions = grp.Select(x => x.SessionId).Distinct().Count(),
                            Pages = grp.Select(x => x.PageId).Distinct().Count(),
                            LoadTime = Math.Round(
                                (grp.Average(x => (long)Convert.ToDouble(x.LoadTime)) / 1000),
                                1
                            )
                        }
                    ).ToListAsync();
                    break;
            }

            Views = subquery.Count();
            ;
            Visitors = subquery.Select(x => x.SessionId).Distinct().Count();
            LoadTime = Math.Round(
                (subquery.Average(x => (long)Convert.ToDouble(x.LoadTime)) / 1000),
                1
            );

            return Page();
        }

        public async Task<ActionResult> OnGetBrowsersAsync(
            double start_at = -86400,
            double end_at = 0
        )
        {
            var uaParser = Parser.GetDefault();
            var subquery = _context.Analytics.Where(
                x =>
                    x.AccessDateTime >= DateTime.Now.AddSeconds(start_at)
                    && x.AccessDateTime <= DateTime.Now.AddSeconds(end_at)
            );
            double total = subquery.Count();
            var grouped = await subquery
                .GroupBy(x => x.UserAgent)
                .Select(x => new { x.Key, Count = x.Count() })
                .ToListAsync();
            BarDataSet = (
                from a in grouped.Select(x => new { Key = uaParser.Parse(x.Key), x.Count })
                group a by new { a.Key.UA.Family, a.Key.UA.Major } into grp
                select new BarData
                {
                    Key = grp.Key.Family + " " + grp.Key.Major,
                    Count = grp.Sum(x => x.Count),
                    Percent = (double)grp.Sum(x => x.Count) / total,
                    TitleOne = "Browser",
                    TitleTwo = "Views"
                }
            ).OrderByDescending(x => x.Count).Take(10).ToList();

            return new PartialViewResult() { ViewName = "Partials/_BarData", ViewData = ViewData };
        }

        public async Task<ActionResult> OnGetOsAsync(double start_at = -86400, double end_at = 0)
        {
            var uaParser = Parser.GetDefault();
            var subquery = _context.Analytics.Where(
                x =>
                    x.AccessDateTime >= DateTime.Now.AddSeconds(start_at)
                    && x.AccessDateTime <= DateTime.Now.AddSeconds(end_at)
            );
            double total = subquery.Count();
            var grouped = await subquery
                .GroupBy(x => x.UserAgent)
                .Select(x => new { x.Key, Count = x.Count() })
                .ToListAsync();
            BarDataSet = (
                from a in grouped.Select(x => new { Key = uaParser.Parse(x.Key), x.Count })
                group a by new { a.Key.OS.Family, a.Key.OS.Major } into grp
                select new BarData
                {
                    Key = grp.Key.Family + " " + grp.Key.Major,
                    Count = grp.Sum(x => x.Count),
                    Percent = (double)grp.Sum(x => x.Count) / total,
                    TitleOne = "Operating System",
                    TitleTwo = "Views"
                }
            ).OrderByDescending(x => x.Count).Take(10).ToList();

            return new PartialViewResult() { ViewName = "Partials/_BarData", ViewData = ViewData };
        }

        public async Task<ActionResult> OnGetResolutionAsync(
            double start_at = -86400,
            double end_at = 0
        )
        {
            var subquery = _context.Analytics.Where(
                x =>
                    x.AccessDateTime >= DateTime.Now.AddSeconds(start_at)
                    && x.AccessDateTime <= DateTime.Now.AddSeconds(end_at)
            );
            double total = subquery.Count();
            BarDataSet = await (
                from a in subquery
                group a by new { a.ScreenWidth, a.ScreenHeight } into grp
                select new BarData
                {
                    Key = grp.Key.ScreenWidth + "x" + grp.Key.ScreenHeight,
                    Count = grp.Count(),
                    Percent = (double)grp.Count() / total,
                    TitleOne = "Window Resolution",
                    TitleTwo = "Views"
                }
            ).OrderByDescending(x => x.Count).Take(10).ToListAsync();

            return new PartialViewResult() { ViewName = "Partials/_BarData", ViewData = ViewData };
        }

        public async Task<ActionResult> OnGetUsersAsync(double start_at = -86400, double end_at = 0)
        {
            var subquery = _context.Analytics.Where(
                x =>
                    x.AccessDateTime >= DateTime.Now.AddSeconds(start_at)
                    && x.AccessDateTime <= DateTime.Now.AddSeconds(end_at)
            );
            double total = subquery.Count();
            BarDataSet = await (
                from a in subquery
                group a by new { a.UserId, a.User.FullnameCalc } into grp
                select new BarData
                {
                    Key = grp.Key.FullnameCalc,
                    Count = grp.Count(),
                    Percent = (double)grp.Count() / total,
                    Href =
                        (
                            _config["features:enable_user_profile"] == null
                            || _config["features:enable_user_profile"].ToString().ToLower()
                                == "true"
                        )
                            ? "/users?id=" + grp.Key.UserId
                            : null,
                    TitleOne = "Top Users",
                    TitleTwo = "Views"
                }
            ).OrderByDescending(x => x.Count).Take(10).ToListAsync();

            return new PartialViewResult() { ViewName = "Partials/_BarData", ViewData = ViewData };
        }

        public async Task<ActionResult> OnGetLoadTimesAsync(
            double start_at = -86400,
            double end_at = 0
        )
        {
            var subquery = _context.Analytics.Where(
                x =>
                    x.AccessDateTime >= DateTime.Now.AddSeconds(start_at)
                    && x.AccessDateTime <= DateTime.Now.AddSeconds(end_at)
            );

            BarDataSet = await (
                from a in subquery
                group a by a.Pathname into grp
                select new BarData
                {
                    Key = grp.Key,
                    Count = Math.Round(
                        (grp.Average(x => (long)Convert.ToDouble(x.LoadTime)) / 1000),
                        1
                    ),
                    TitleOne = "Load Times",
                    TitleTwo = "Seconds"
                }
            ).OrderByDescending(x => x.Count).Take(10).ToListAsync();

            return new PartialViewResult() { ViewName = "Partials/_BarData", ViewData = ViewData };
        }

        // this is being removed in place of adding a report access table with better details.
        // public async Task<ActionResult> OnGetReportsAsync(
        //     double start_at = -86400,
        //     double end_at = 0
        // )
        // {
        //     var subquery = _context.Analytics.Where(
        //         x =>
        //             x.Pathname.ToLower() == "/reports"
        //             && x.AccessDateTime >= DateTime.Now.AddSeconds(start_at)
        //             && x.AccessDateTime <= DateTime.Now.AddSeconds(end_at)
        //     );
        //     double total = subquery.Count();

        //     if (total == 0)
        //     {
        //         BarDataSet = new List<BarData>();
        //         return new PartialViewResult()
        //         {
        //             ViewName = "Partials/_BarData",
        //             ViewData = ViewData
        //         };
        //     }

        //     var parsed = await subquery
        //         .Where(x => x.Search.IndexOf("id=") != -1)
        //         .Select(x => x.Search)
        //         .ToListAsync();

        //     var grouped = parsed
        //         .Select(x => Regex.Match(x, @"id=(\d+)").Groups[1].Value)
        //         .GroupBy(x => x)
        //         .OrderBy(x => x.Count())
        //         .Select(x => new { Key = Int32.Parse(x.Key), Count = x.Count() })
        //         .Take(10)
        //         .ToList();

        //     BarDataSet = (
        //         from a in grouped
        //         select new BarData
        //         {
        //             Key = _context.ReportObjects
        //                 .Where(x => x.ReportObjectId == a.Key)
        //                 .Select(r => r.DisplayTitle != null ? r.DisplayTitle : r.Name)
        //                 .FirstOrDefault(),
        //             Count = a.Count,
        //             Percent = (double)a.Count / total,
        //             Href = "/reports?id=" + a.Key,
        //             TitleOne = "Reports",
        //             TitleTwo = "Views"
        //         }
        //     ).OrderByDescending(x => x.Count).ToList();

        //     return new PartialViewResult() { ViewName = "Partials/_BarData", ViewData = ViewData };
        // }
    }
}
